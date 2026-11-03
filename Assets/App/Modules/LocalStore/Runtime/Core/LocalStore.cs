using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Biome.LocalStore.Abstract;
using Biome.LocalStore.Config;
using Biome.LocalStore.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Biome.LocalStore
{
    /// <summary>
    /// The implementation of <see cref="ILocalStoreService"/>: manifest of own keys + common prefix, per-record
    /// versioned envelope, UTF-8 byte-size limits that throw with the store unchanged, a raw-string cache, and the
    /// single module-owned serializer settings. An instance behind the contract — never a static.
    /// <para><b>Main thread only.</b></para>
    /// <para>The public factory (with the PlayerPrefs backend) is not part of this slice; the internal constructor is
    /// the entry the contract tests use over the in-memory backend.</para>
    /// </summary>
    internal sealed class LocalStore : ILocalStoreService
    {
        private readonly ILocalStoreBackend _backend;
        private readonly ILocalStoreRegistry _registry;
        private readonly LocalStoreLimits _limits;
        private readonly JsonSerializer _serializer = LocalStoreJson.CreateSerializer();

        /// <summary>Raw strings only — never the deserialized object (a shared reference would alias two consumers).</summary>
        private readonly Dictionary<string, string> _cache = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>The store's own prefixed backend keys; loaded lazily from the manifest key, self-healed on load.</summary>
        private List<string> _manifest;

        internal LocalStore(ILocalStoreBackend backend, ILocalStoreRegistry registry, LocalStoreLimits limits)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _limits = limits;
        }

        // ---------------------------------------------------------------- contract

        public bool TryGet<T>(out T value, out ReadStatus status)
        {
            var entry = Resolve(typeof(T));
            var key = BackendKey(entry.Key);
            value = default;

            if (!TryReadRaw(key, out var raw))
            {
                status = ReadStatus.Missing;
                return false;
            }

            status = Decode(raw, entry.Version, out value);
            return status == ReadStatus.Ok;
        }

        public void Set<T>(T value)
        {
            var entry = Resolve(typeof(T));
            var key = BackendKey(entry.Key);

            // Serialize and validate BEFORE any write: a limit violation leaves the store untouched.
            var raw = Encode(value, entry.Version);
            EnsureWithinLimits(key, raw);

            _backend.Set(key, raw);
            _cache[key] = raw;
            var manifest = Manifest();
            if (!manifest.Contains(key))
            {
                manifest.Add(key);
                WriteManifest();
            }
        }

        public bool Has<T>()
        {
            var entry = Resolve(typeof(T));
            var key = BackendKey(entry.Key);
            if (_backend.Has(key))
            {
                return true;
            }
            Prune(key);
            return false;
        }

        public void Remove<T>()
        {
            var entry = Resolve(typeof(T));
            var key = BackendKey(entry.Key);
            _backend.Delete(key);
            _cache.Remove(key);
            if (Manifest().Remove(key))
            {
                WriteManifest();
            }
        }

        public void Clear()
        {
            // Own keys only — walk the manifest; never the backend's bulk delete (it would wipe foreign keys).
            foreach (var key in Manifest())
            {
                    _backend.Delete(key);
            }
            Manifest().Clear();
            _cache.Clear();
            _backend.Delete(LocalStoreKeys.ManifestKey);
        }

        public void Flush() => _backend.Save();

        // ---------------------------------------------------------------- resolution / keys

        private LocalStoreEntry Resolve(Type type)
        {
            if (!_registry.TryResolve(type, out var entry))
            {
                    throw new LocalStoreUnregisteredTypeException(type);
            }
            if (string.IsNullOrEmpty(entry.Key))
            {
                    throw new InvalidOperationException($"LocalStore: registry entry for '{type.FullName}' has an empty key.");
            }
            return entry;
        }

        private static string BackendKey(string registryKey)
        {
            var key = LocalStoreKeys.Prefix + registryKey;
            if (string.Equals(key, LocalStoreKeys.ManifestKey, StringComparison.Ordinal))
            {
                    throw new InvalidOperationException("LocalStore: the manifest key is reserved and cannot be a registry key.");
            }
            return key;
        }

        // ---------------------------------------------------------------- raw read / cache

        private bool TryReadRaw(string key, out string raw)
        {
            if (_cache.TryGetValue(key, out raw))
            {
                if (_backend.Has(key))
                {
                    return true;
                }
                _cache.Remove(key); // deleted from outside behind the cache
            }
            if (_backend.TryGet(key, out raw))
            {
                _cache[key] = raw;
                return true;
            }
            Prune(key);
            raw = null;
            return false;
        }

        /// <summary>Self-healing: a manifest entry with no value behind it is not an error — it is pruned.</summary>
        private void Prune(string key)
        {
            if (Manifest().Remove(key))
            {
                WriteManifest();
            }
        }

        // ---------------------------------------------------------------- envelope

        private string Encode<T>(T value, int version)
        {
            var envelope = new JObject
            {
                [LocalStoreRecordFormat.VersionField] = version,
                [LocalStoreRecordFormat.PayloadField] = value == null ? JValue.CreateNull() : JToken.FromObject(value, _serializer),
            };
            return envelope.ToString(Formatting.None);
        }

        /// <summary>One strict policy: any parse failure or missing envelope field → Corrupt.</summary>
        private ReadStatus Decode<T>(string raw, int expectedVersion, out T value)
        {
            value = default;
            JObject envelope;
            try
            {
                using (var reader = new JsonTextReader(new StringReader(raw))
                {
                    DateParseHandling = DateParseHandling.None,
                    MaxDepth = LocalStoreJson.Settings.MaxDepth,
                })
                {
                    var token = JToken.ReadFrom(reader);
                    if (reader.Read())
                    {
                        return ReadStatus.Corrupt; // trailing content
                    }
                    envelope = token as JObject;
                }
            }
            catch (JsonException) { return ReadStatus.Corrupt; }

            if (envelope == null)
            {
                return ReadStatus.Corrupt;
            }
            if (!envelope.TryGetValue(LocalStoreRecordFormat.VersionField, out var v) || v.Type != JTokenType.Integer)
            {
                return ReadStatus.Corrupt;
            }
            if (!envelope.TryGetValue(LocalStoreRecordFormat.PayloadField, out var d))
            {
                return ReadStatus.Corrupt;
            }

            if (v.Value<int>() != expectedVersion)
            {
                return ReadStatus.VersionMismatch;
            }

            try
            {
                value = d.Type == JTokenType.Null ? default : d.ToObject<T>(_serializer);
            }
            catch (JsonException) { value = default; return ReadStatus.Corrupt; }
            catch (ArgumentException) { value = default; return ReadStatus.Corrupt; }
            catch (FormatException) { value = default; return ReadStatus.Corrupt; }
            catch (OverflowException) { value = default; return ReadStatus.Corrupt; }
            return ReadStatus.Ok;
        }

        // ---------------------------------------------------------------- limits

        private void EnsureWithinLimits(string key, string raw)
        {
            if (key.Length > _limits.MaxKeyLength)
            {
                    throw new LocalStoreLimitException(key, key.Length, _limits.MaxKeyLength,
                        $"LocalStore: key '{key}' is {key.Length} chars; limit {_limits.MaxKeyLength}.");
            }

            var bytes = Utf8Bytes(raw);
            if (bytes > _limits.MaxValueBytes)
            {
                    throw new LocalStoreLimitException(key, bytes, _limits.MaxValueBytes,
                        $"LocalStore: record '{key}' is {bytes} UTF-8 bytes; limit {_limits.MaxValueBytes}.");
            }

            long total = bytes;
            foreach (var owned in Manifest().ToArray()) // copy: a dangling entry may be pruned while measuring
            {
                if (string.Equals(owned, key, StringComparison.Ordinal))
                {
                    continue; // being replaced
                }
                if (TryReadRaw(owned, out var other))
                {
                    total += Utf8Bytes(other);
                }
            }
            if (total > _limits.MaxTotalBytes)
            {
                    throw new LocalStoreLimitException(key, total, _limits.MaxTotalBytes,
                        $"LocalStore: writing '{key}' would bring the store to {total} UTF-8 bytes; budget {_limits.MaxTotalBytes}.");
            }
        }

        private static int Utf8Bytes(string s) => Encoding.UTF8.GetByteCount(s);

        // ---------------------------------------------------------------- manifest

        private List<string> Manifest()
        {
            if (_manifest != null)
            {
                return _manifest;
            }

            _manifest = new List<string>();
            if (_backend.TryGet(LocalStoreKeys.ManifestKey, out var raw))
            {
                try
                {
                    var arr = JArray.Parse(raw);
                    foreach (var t in arr)
                    {
                        if (t.Type == JTokenType.String)
                        {
                            _manifest.Add(t.Value<string>());
                        }
                    }
                }
                catch (JsonException)
                {
                    // An unreadable manifest is rebuilt from empty: it only lists what the store owns, and a
                    // dangling record without a manifest entry is unreachable anyway (its key is registry-resolved).
                    _manifest.Clear();
                }
            }

            // Self-heal on load: entries whose value is gone (wiped plist, cleared registry) are pruned.
            var pruned = _manifest.RemoveAll(k => !_backend.Has(k)) > 0;
            if (pruned)
            {
                WriteManifest();
            }
            return _manifest;
        }

        private void WriteManifest()
        {
            var arr = new JArray();
            foreach (var k in _manifest)
            {
                arr.Add(k);
            }
            _backend.Set(LocalStoreKeys.ManifestKey, arr.ToString(Formatting.None));
        }
    }
}

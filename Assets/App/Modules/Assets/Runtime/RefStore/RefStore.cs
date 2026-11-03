using System;
using System.Collections.Generic;
using System.Threading;
using Biome.Assets.Abstract;
using Cysharp.Threading.Tasks;

namespace Biome.Assets
{
    /// <summary>
    /// The flat store behind every context: one entry per key, the set of contexts holding it, the in-flight load
    /// shared by its waiters. An entry leaves memory (its handle is released) when no context holds it; a shared load
    /// is cancelled when its last waiter is gone. Main thread only.
    /// </summary>
    internal sealed class RefStore
    {
        private readonly IAssetBackend _backend;
        private readonly Dictionary<AssetKey, Entry> _entries = new Dictionary<AssetKey, Entry>();
        private readonly Dictionary<Guid, HashSet<AssetKey>> _byContext = new Dictionary<Guid, HashSet<AssetKey>>();

        public RefStore(IAssetBackend backend)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        }

        /// <summary>Entries currently alive (loaded or loading) — for tests.</summary>
        public int EntryCount => _entries.Count;

        public bool TryGetHeld(Guid context, AssetKey key, out object asset)
        {
            if (_entries.TryGetValue(key, out Entry entry) && entry.Loaded && entry.Holders.Contains(context))
            {
                asset = entry.Handle.Asset;
                return true;
            }

            asset = null;
            return false;
        }

        public UniTask<IReadOnlyList<AssetKey>> ResolveLabelAsync(AssetLabel label, CancellationToken ct)
            => _backend.ResolveLabelAsync(label, ct);

        /// <summary>Holds <paramref name="key"/> for <paramref name="context"/>; joins an in-flight load or starts one.</summary>
        public UniTask<object> AcquireAsync(Guid context, AssetKey key, Type type, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                return UniTask.FromCanceled<object>(ct);
            }

            var start = false;
            if (_entries.TryGetValue(key, out Entry entry))
            {
                if (entry.Loaded)
                {
                    AddHolder(entry, context);
                    return UniTask.FromResult(entry.Handle.Asset);
                }
            }
            else
            {
                entry = new Entry(key, type);
                _entries.Add(key, entry);
                start = true;
            }

            var waiter = new Waiter(context, new UniTaskCompletionSource<object>());
            entry.Waiters.Add(waiter);
            Track(context, key);
            if (ct.CanBeCanceled)
            {
                Entry e = entry;
                waiter.Registration = ct.Register(() => OnWaiterCancelled(e, waiter, ct));
            }

            if (start)
            {
                RunLoadAsync(entry).Forget();
            }

            return waiter.Source.Task;
        }

        /// <summary>Drops <paramref name="context"/>'s hold (and its pending waiters) on <paramref name="key"/>.</summary>
        public void Release(Guid context, AssetKey key)
        {
            if (!_entries.TryGetValue(key, out Entry entry))
            {
                Untrack(context, key);
                return;
            }

            CancelWaiters(entry, context);
            entry.Holders.Remove(context);
            Untrack(context, key);
            if (entry.Loaded)
            {
                if (entry.Holders.Count == 0)
                {
                    _entries.Remove(key);
                    entry.Handle.Release();
                }
            }
            else if (entry.Waiters.Count == 0)
            {
                Abandon(entry);
            }
        }

        /// <summary>Releases every key the context holds or waits for.</summary>
        public void ReleaseAll(Guid context)
        {
            if (!_byContext.TryGetValue(context, out HashSet<AssetKey> keys))
            {
                return;
            }

            foreach (AssetKey key in new List<AssetKey>(keys))
            {
                Release(context, key);
            }

            _byContext.Remove(context);
        }

        private async UniTaskVoid RunLoadAsync(Entry entry)
        {
            IAssetHandle handle = null;
            Exception error = null;
            try
            {
                handle = await _backend.LoadAsync(entry.Key, entry.Type, entry.Cts.Token);
            }
            catch (Exception ex)
            {
                error = ex;
            }

            if (entry.Abandoned)
            {
                handle?.Release();
                entry.Cts.Dispose();
                return;
            }

            if (error != null || handle == null)
            {
                error = error ?? new InvalidOperationException($"Assets: the backend returned no handle for '{entry.Key}'.");
                RemoveEntry(entry);
                Waiter[] failed = entry.Waiters.ToArray();
                entry.Waiters.Clear();
                foreach (Waiter waiter in failed)
                {
                    waiter.Registration.Dispose();
                    Untrack(waiter.Context, entry.Key);
                    if (error is OperationCanceledException)
                    {
                        waiter.Source.TrySetCanceled();
                    }
                    else
                    {
                        waiter.Source.TrySetException(error);
                    }
                }

                entry.Cts.Dispose();
                return;
            }

            entry.Handle = handle;
            entry.Loaded = true;
            Waiter[] waiting = entry.Waiters.ToArray();
            entry.Waiters.Clear();
            foreach (Waiter waiter in waiting)
            {
                waiter.Registration.Dispose();
                AddHolder(entry, waiter.Context);
            }

            entry.Cts.Dispose();
            foreach (Waiter waiter in waiting)
            {
                waiter.Source.TrySetResult(handle.Asset);
            }
        }

        private void OnWaiterCancelled(Entry entry, Waiter waiter, CancellationToken ct)
        {
            if (!entry.Waiters.Remove(waiter))
            {
                return;
            }

            waiter.Source.TrySetCanceled(ct);
            if (!entry.Holders.Contains(waiter.Context) && !HasWaiter(entry, waiter.Context))
            {
                Untrack(waiter.Context, entry.Key);
            }

            if (!entry.Loaded && entry.Waiters.Count == 0)
            {
                Abandon(entry);
            }
        }

        private static void CancelWaiters(Entry entry, Guid context)
        {
            for (int i = entry.Waiters.Count - 1; i >= 0; i--)
            {
                Waiter waiter = entry.Waiters[i];
                if (waiter.Context != context)
                {
                    continue;
                }

                entry.Waiters.RemoveAt(i);
                waiter.Registration.Dispose();
                waiter.Source.TrySetCanceled();
            }
        }

        private void Abandon(Entry entry)
        {
            entry.Abandoned = true;
            RemoveEntry(entry);
            entry.Cts.Cancel();
        }

        private void RemoveEntry(Entry entry)
        {
            if (_entries.TryGetValue(entry.Key, out Entry current) && ReferenceEquals(current, entry))
            {
                _entries.Remove(entry.Key);
            }
        }

        private static bool HasWaiter(Entry entry, Guid context)
        {
            foreach (Waiter waiter in entry.Waiters)
            {
                if (waiter.Context == context)
                {
                    return true;
                }
            }

            return false;
        }

        private void AddHolder(Entry entry, Guid context)
        {
            entry.Holders.Add(context);
            Track(context, entry.Key);
        }

        private void Track(Guid context, AssetKey key)
        {
            if (!_byContext.TryGetValue(context, out HashSet<AssetKey> keys))
            {
                keys = new HashSet<AssetKey>();
                _byContext.Add(context, keys);
            }

            keys.Add(key);
        }

        private void Untrack(Guid context, AssetKey key)
        {
            if (_byContext.TryGetValue(context, out HashSet<AssetKey> keys))
            {
                keys.Remove(key);
            }
        }

        private sealed class Waiter
        {
            public Waiter(Guid context, UniTaskCompletionSource<object> source)
            {
                Context = context;
                Source = source;
            }

            public Guid Context { get; }

            public UniTaskCompletionSource<object> Source { get; }

            public CancellationTokenRegistration Registration { get; set; }
        }

        private sealed class Entry
        {
            public Entry(AssetKey key, Type type)
            {
                Key = key;
                Type = type;
            }

            public AssetKey Key { get; }

            public Type Type { get; }

            public HashSet<Guid> Holders { get; } = new HashSet<Guid>();

            public List<Waiter> Waiters { get; } = new List<Waiter>();

            public CancellationTokenSource Cts { get; } = new CancellationTokenSource();

            public IAssetHandle Handle { get; set; }

            public bool Loaded { get; set; }

            public bool Abandoned { get; set; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using Biome.Configs.Abstract;
using Cysharp.Threading.Tasks;

namespace Biome.Configs.Fakes
{
    /// <summary>
    /// The in-memory fake of <see cref="IConfigsService"/>: the same two-step init and loaders run as production over
    /// a plain dictionary, so a consumer cannot tell them apart by behaviour. Fail-fast and no freeze beyond the
    /// "no second run" rule. <see cref="WithConfigs"/> makes one that is already initialised and loaded. Main thread only.
    /// </summary>
    public sealed class FakeConfigsService : IConfigsService
    {
        /// <summary>How many times one loader runs before its failure surfaces; the production value.</summary>
        public const int MaxLoadAttempts = 3;

        private readonly Dictionary<Type, object> _configs = new Dictionary<Type, object>();
        private readonly IReadOnlyList<IConfigLoader> _loaders;
        private readonly IReadOnlyList<object> _baseConfigs;
        private readonly IConfigSourceReader _sourceReader;
        private ConfigSource[] _sources = Array.Empty<ConfigSource>();
        private bool _isLoading;

        /// <summary>
        /// Creates the fake over its loaders and base configs. Throws <see cref="ArgumentNullException"/> for a null
        /// argument or a null element of either list. The loaders get <paramref name="sourceReader"/>, or, when it is
        /// null, a reader that throws on any read: the fake has no asset source of its own.
        /// </summary>
        public FakeConfigsService(IReadOnlyList<IConfigLoader> loaders, IReadOnlyList<object> baseConfigs, IConfigSourceReader sourceReader = null)
        {
            _sourceReader = sourceReader ?? new NoSourceReader();
            _loaders = loaders ?? throw new ArgumentNullException(nameof(loaders));
            _baseConfigs = baseConfigs ?? throw new ArgumentNullException(nameof(baseConfigs));

            foreach (var loader in _loaders)
            {
                if (loader == null)
                {
                    throw new ArgumentNullException(nameof(loaders), "A loader is null.");
                }
            }

            foreach (var baseConfig in _baseConfigs)
            {
                if (baseConfig == null)
                {
                    throw new ArgumentNullException(nameof(baseConfigs), "A base config is null.");
                }
            }
        }

        /// <inheritdoc />
        public bool IsInitialized { get; private set; }

        /// <inheritdoc />
        public bool IsLoaded { get; private set; }

        /// <summary>
        /// A fake that is already initialised and loaded, holding <paramref name="configs"/>. Throws
        /// <see cref="InvalidOperationException"/> when two of them share a type.
        /// </summary>
        public static FakeConfigsService WithConfigs(params object[] configs)
        {
            var service = new FakeConfigsService(Array.Empty<IConfigLoader>(), configs);
            service.Land();
            service.IsInitialized = true;
            service.IsLoaded = true;
            return service;
        }

        /// <inheritdoc />
        public bool TryGet<T>(out T config) where T : class
        {
            if (_configs.TryGetValue(typeof(T), out var stored))
            {
                config = (T)stored;
                return true;
            }

            config = null;
            return false;
        }

        /// <summary>
        /// Validates the environment against the loaders, then lands the base configs. A faulted task carries the
        /// same exceptions as production: <see cref="ArgumentNullException"/> for a null environment,
        /// <see cref="InvalidOperationException"/> on a second init, a duplicate config type, or a loader without a
        /// source, and <see cref="OperationCanceledException"/> on a cancelled token.
        /// </summary>
        public UniTask InitializeAsync(IConfigsEnvironment environment, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                return UniTask.FromCanceled(ct);
            }

            try
            {
                Initialize(environment);
                return UniTask.CompletedTask;
            }
            catch (Exception e)
            {
                return UniTask.FromException(e);
            }
        }

        /// <summary>
        /// Runs every loader at once and waits for all of them, each up to <see cref="MaxLoadAttempts"/> times, and
        /// publishes the configs only when every loader succeeded. Throws like production: <see cref="InvalidOperationException"/>
        /// before the init, after a finished run or during a run, <see cref="ConfigLoadException"/> for the first
        /// loader that kept failing, <see cref="OperationCanceledException"/> on a cancelled token.
        /// </summary>
        public async UniTask LoadAllAsync(CancellationToken ct)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The configs are not initialized; call InitializeAsync first.");
            }

            if (IsLoaded)
            {
                throw new InvalidOperationException("The configs are already loaded; there is no reload within a session.");
            }

            if (_isLoading)
            {
                throw new InvalidOperationException("A configs load is already running.");
            }

            ct.ThrowIfCancellationRequested();
            _isLoading = true;
            try
            {
                var runs = new UniTask<LoadOutcome>[_loaders.Count];
                for (var i = 0; i < runs.Length; i++)
                {
                    runs[i] = LoadOneAsync(_loaders[i], _sources[i], ct);
                }

                var outcomes = await UniTask.WhenAll(runs);
                ct.ThrowIfCancellationRequested();

                foreach (var outcome in outcomes)
                {
                    if (outcome.Error != null)
                    {
                        throw outcome.Error;
                    }
                }

                foreach (var outcome in outcomes)
                {
                    _configs[outcome.ConfigType] = outcome.Config;
                }

                IsLoaded = true;
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void Initialize(IConfigsEnvironment environment)
        {
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            if (IsInitialized)
            {
                throw new InvalidOperationException("The configs are already initialized; there is no re-init within a session.");
            }

            if (environment is IConfigsEnvironmentGuard guard && guard.TryFindDuplicateType(out var duplicate))
            {
                throw new InvalidOperationException($"Environment '{environment.Id}' names more than one source for config type '{duplicate}'.");
            }

            var sources = new ConfigSource[_loaders.Count];
            var types = new HashSet<Type>();
            for (var i = 0; i < sources.Length; i++)
            {
                var configType = _loaders[i].ConfigType;
                if (configType == null || !types.Add(configType))
                {
                    throw new InvalidOperationException($"More than one loader, or a loader without a type, for config type '{configType?.FullName}'.");
                }

                if (!environment.TryGetSource(configType, out sources[i]))
                {
                    throw new InvalidOperationException($"Environment '{environment.Id}' has no source for config type '{configType.FullName}'.");
                }
            }

            foreach (var baseConfig in _baseConfigs)
            {
                if (!types.Add(baseConfig.GetType()))
                {
                    throw new InvalidOperationException($"Config type '{baseConfig.GetType().FullName}' has more than one origin: a loader or another base config.");
                }
            }

            Land();
            _sources = sources;
            IsInitialized = true;
        }

        private void Land()
        {
            var landed = new Dictionary<Type, object>();
            foreach (var baseConfig in _baseConfigs)
            {
                if (!landed.TryAdd(baseConfig.GetType(), baseConfig))
                {
                    throw new InvalidOperationException($"Config type '{baseConfig.GetType().FullName}' is preset more than once.");
                }
            }

            foreach (var pair in landed)
            {
                _configs[pair.Key] = pair.Value;
            }
        }

        private async UniTask<LoadOutcome> LoadOneAsync(IConfigLoader loader, ConfigSource source, CancellationToken ct)
        {
            Exception lastFailure = null;
            for (var attempt = 1; attempt <= MaxLoadAttempts; attempt++)
            {
                try
                {
                    ct.ThrowIfCancellationRequested();
                    var config = await loader.LoadAsync(source, _sourceReader, ct);
                    if (config == null)
                    {
                        throw new InvalidOperationException($"The loader of '{loader.ConfigType.FullName}' returned null.");
                    }

                    if (!loader.ConfigType.IsInstanceOfType(config))
                    {
                        throw new InvalidOperationException($"The loader of '{loader.ConfigType.FullName}' returned a '{config.GetType().FullName}'.");
                    }

                    return new LoadOutcome(loader.ConfigType, config, null);
                }
                catch (OperationCanceledException e)
                {
                    return new LoadOutcome(loader.ConfigType, null, e);
                }
                catch (Exception e)
                {
                    lastFailure = e;
                }
            }

            return new LoadOutcome(loader.ConfigType, null, new ConfigLoadException(loader.ConfigType, MaxLoadAttempts, lastFailure));
        }

        /// <summary>
        /// The <see cref="IConfig{T}"/> the fake hands out: reads the fake at the moment <see cref="Value"/> is read,
        /// so it is available from the moment the fake holds the config. Throws <see cref="ConfigNotInitializedException"/> before.
        /// </summary>
        public sealed class Provider<T> : IConfig<T> where T : class
        {
            private readonly IConfigsReader _reader;

            /// <summary>Creates the provider. Throws <see cref="ArgumentNullException"/> when <paramref name="reader"/> is null.</summary>
            public Provider(IConfigsReader reader)
            {
                _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            }

            /// <inheritdoc />
            public T Value
            {
                get
                {
                    if (_reader.TryGet<T>(out var config))
                    {
                        return config;
                    }

                    throw new ConfigNotInitializedException(typeof(T));
                }
            }
        }

        private sealed class NoSourceReader : IConfigSourceReader
        {
            public UniTask<T> ReadAsync<T>(ConfigSource source, CancellationToken ct) where T : class
            {
                return UniTask.FromException<T>(new InvalidOperationException("The fake configs service has no source reader."));
            }
        }

        private readonly struct LoadOutcome
        {
            public LoadOutcome(Type configType, object config, Exception error)
            {
                ConfigType = configType;
                Config = config;
                Error = error;
            }

            public Type ConfigType { get; }

            public object Config { get; }

            public Exception Error { get; }
        }
    }
}

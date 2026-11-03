using System;
using System.Collections.Generic;
using System.Threading;
using Biome.Configs.Abstract;
using Biome.Configs.Data;
using Cysharp.Threading.Tasks;

namespace Biome.Configs
{
    /// <summary>
    /// The configs service: lands the base configs at the environment init, then runs every registered loader and
    /// freezes the store. Nothing is published until a step is complete, so a failed or cancelled step leaves no
    /// partial state. Main thread only.
    /// </summary>
    public sealed class ConfigsService : IConfigsService
    {
        /// <summary>D1: how many times one loader runs before its failure surfaces. The retry is immediate.</summary>
        internal const int MaxLoadAttempts = 3;

        private readonly ConfigStore _store;
        private readonly IConfigSourceReader _sourceReader;
        private readonly IReadOnlyList<IConfigLoader> _loaders;
        private readonly IReadOnlyList<object> _baseConfigs;
        private ConfigSource[] _sources = Array.Empty<ConfigSource>();
        private bool _isLoading;

        /// <summary>
        /// Creates the service over its parts. Throws <see cref="ArgumentNullException"/> for a null argument or a
        /// null element of <paramref name="loaders"/> or <paramref name="baseConfigs"/>.
        /// </summary>
        public ConfigsService(
            ConfigStore store,
            IConfigSourceReader sourceReader,
            IReadOnlyList<IConfigLoader> loaders,
            IReadOnlyList<object> baseConfigs)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _sourceReader = sourceReader ?? throw new ArgumentNullException(nameof(sourceReader));
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

        /// <inheritdoc />
        public bool TryGet<T>(out T config) where T : class
        {
            if (_store.TryGet(typeof(T), out var stored))
            {
                config = (T)stored;
                return true;
            }

            config = null;
            return false;
        }

        /// <summary>
        /// Validates the environment against the registered loaders, then lands the base configs. A faulted task
        /// carries <see cref="ArgumentNullException"/> for a null environment, <see cref="InvalidOperationException"/>
        /// on a second init, a duplicate config type in the environment, two loaders or base configs for one type,
        /// or a loader whose config type has no source, and <see cref="OperationCanceledException"/> on a cancelled
        /// token. The service stays not initialized after any of them.
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
        /// Runs every loader at once and waits for all of them; a failing loader is retried up to
        /// <see cref="MaxLoadAttempts"/> times in all. Only when every loader succeeded are the configs put into the
        /// store and the store frozen. A faulted task carries <see cref="InvalidOperationException"/> before the
        /// init, after a finished run or during a run, <see cref="ConfigLoadException"/> for the first loader (in
        /// registration order) that kept failing, and <see cref="OperationCanceledException"/> on a cancelled token.
        /// After a failure the run may be started again.
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
                    _store.Put(outcome.ConfigType, outcome.Config);
                }

                _store.Freeze();
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

            foreach (var baseConfig in _baseConfigs)
            {
                _store.Put(baseConfig.GetType(), baseConfig);
            }

            _sources = sources;
            IsInitialized = true;
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

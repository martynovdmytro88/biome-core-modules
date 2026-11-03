using System;
using System.Threading;
using Biome.Configs.Abstract;
using Cysharp.Threading.Tasks;

namespace Biome.Configs.Fakes
{
    /// <summary>
    /// The scripted fake of <see cref="IConfigLoader"/>: what each attempt does is chosen by the factory that made it
    /// (succeed, fail a number of times, delay, wait for a gate, never finish, return a given value). It counts its
    /// attempts and remembers the source and the reader of the last one. Main thread only.
    /// </summary>
    public sealed class FakeConfigLoader : IConfigLoader
    {
        private readonly Func<int, CancellationToken, UniTask<object>> _behaviour;

        private FakeConfigLoader(Type configType, Func<int, CancellationToken, UniTask<object>> behaviour)
        {
            ConfigType = configType;
            _behaviour = behaviour;
        }

        /// <inheritdoc />
        public Type ConfigType { get; }

        /// <summary>How many times <see cref="LoadAsync"/> was called.</summary>
        public int Attempts { get; private set; }

        /// <summary>The source of the last attempt.</summary>
        public ConfigSource LastSource { get; private set; }

        /// <summary>The reader of the last attempt.</summary>
        public IConfigSourceReader LastReader { get; private set; }

        /// <summary>A loader whose every attempt returns <paramref name="config"/>; its type is the config's runtime type.</summary>
        public static FakeConfigLoader Succeeding(object config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return new FakeConfigLoader(config.GetType(), (attempt, ct) => UniTask.FromResult(config));
        }

        /// <summary>A loader whose first <paramref name="failures"/> attempts throw and whose later ones return <paramref name="config"/>.</summary>
        public static FakeConfigLoader FailingTimes(int failures, object config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return new FakeConfigLoader(config.GetType(), (attempt, ct) => attempt <= failures
                ? UniTask.FromException<object>(new InvalidOperationException("scripted failure " + attempt))
                : UniTask.FromResult(config));
        }

        /// <summary>A loader of <paramref name="configType"/> whose every attempt throws.</summary>
        public static FakeConfigLoader AlwaysFailing(Type configType)
        {
            return new FakeConfigLoader(
                configType ?? throw new ArgumentNullException(nameof(configType)),
                (attempt, ct) => UniTask.FromException<object>(new InvalidOperationException("scripted failure " + attempt)));
        }

        /// <summary>
        /// A loader that returns <paramref name="config"/> after <paramref name="delay"/>. The wait ends with an
        /// <see cref="OperationCanceledException"/> when the token is cancelled first.
        /// </summary>
        public static FakeConfigLoader Delayed(object config, TimeSpan delay)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return new FakeConfigLoader(config.GetType(), async (attempt, ct) =>
            {
                await UniTask.Delay(delay, DelayType.Realtime, PlayerLoopTiming.Update, ct);
                return config;
            });
        }

        /// <summary>A loader of <paramref name="configType"/> that finishes with whatever <paramref name="gate"/> is given.</summary>
        public static FakeConfigLoader Gated(Type configType, UniTaskCompletionSource<object> gate)
        {
            if (gate == null)
            {
                throw new ArgumentNullException(nameof(gate));
            }

            return new FakeConfigLoader(
                configType ?? throw new ArgumentNullException(nameof(configType)),
                (attempt, ct) => gate.Task);
        }

        /// <summary>A loader of <paramref name="configType"/> that never finishes until its token is cancelled.</summary>
        public static FakeConfigLoader UntilCancelled(Type configType)
        {
            return new FakeConfigLoader(
                configType ?? throw new ArgumentNullException(nameof(configType)),
                async (attempt, ct) =>
                {
                    await UniTask.WaitUntilCanceled(ct);
                    ct.ThrowIfCancellationRequested();
                    return null;
                });
        }

        /// <summary>A loader of <paramref name="configType"/> whose every attempt returns <paramref name="result"/>, even null or a wrong type.</summary>
        public static FakeConfigLoader Returning(Type configType, object result)
        {
            return new FakeConfigLoader(
                configType ?? throw new ArgumentNullException(nameof(configType)),
                (attempt, ct) => UniTask.FromResult(result));
        }

        /// <inheritdoc />
        public UniTask<object> LoadAsync(ConfigSource source, IConfigSourceReader reader, CancellationToken ct)
        {
            Attempts++;
            LastSource = source;
            LastReader = reader;
            return _behaviour(Attempts, ct);
        }
    }
}

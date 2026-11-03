using System.Threading;
using Cysharp.Threading.Tasks;

namespace Biome.Configs.Abstract
{
    /// <summary>
    /// The entry contract of the configs module. Configs are immutable after load; there is no re-init within a
    /// session. Main thread only.
    /// </summary>
    public interface IConfigsService : IConfigsReader
    {
        /// <summary>True once the environment init is done and the base configs are readable.</summary>
        bool IsInitialized { get; }

        /// <summary>True once the loaders run is done.</summary>
        bool IsLoaded { get; }

        /// <summary>
        /// The environment init, run once by the App at boot before any init state. Takes the environment and
        /// lands the base configs.
        /// </summary>
        UniTask InitializeAsync(IConfigsEnvironment environment, CancellationToken ct);

        /// <summary>
        /// The loaders run (boot-sequence step 7): runs every registered <see cref="IConfigLoader"/> and awaits all
        /// of them; a failed load is retried, then throws <see cref="ConfigLoadException"/>.
        /// </summary>
        UniTask LoadAllAsync(CancellationToken ct);
    }
}

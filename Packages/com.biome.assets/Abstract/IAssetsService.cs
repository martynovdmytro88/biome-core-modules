namespace Biome.Assets.Abstract
{
    /// <summary>
    /// The entry point of the Assets module: the owner of the context tree. Contexts live with their DI scope — the
    /// scope's disposal unloads them. Main thread only.
    /// </summary>
    public interface IAssetsService
    {
        /// <summary>The app-lifetime context: error windows, loading screens. Unloaded only with the service.</summary>
        IAssetsContext GlobalContext { get; }

        /// <summary>A new top-level context, e.g. one per game state. The caller owns it and unloads it.</summary>
        IAssetsContext CreateContext();
    }
}

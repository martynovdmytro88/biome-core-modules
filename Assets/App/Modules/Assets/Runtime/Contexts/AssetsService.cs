using System;
using System.Collections.Generic;
using Biome.Assets.Abstract;

namespace Biome.Assets
{
    /// <summary>
    /// The owner of the context tree: the global context plus every top-level context it handed out. Disposing it
    /// (the DI scope's disposal) unloads them all.
    /// </summary>
    internal sealed class AssetsService : IAssetsService, IDisposable
    {
        private readonly RefStore _store;
        private readonly IAssetBackend _backend;
        private readonly AssetsContext _global;
        private readonly List<AssetsContext> _contexts = new List<AssetsContext>();

        internal AssetsService(IAssetBackend backend)
        {
            _backend = backend;
            _store = new RefStore(backend);
            _global = new AssetsContext(_store, backend, null);
        }

        public IAssetsContext GlobalContext => _global;

        internal RefStore Store => _store;

        public IAssetsContext CreateContext()
        {
            _contexts.RemoveAll(c => c.IsClosed);
            var context = new AssetsContext(_store, _backend, null);
            _contexts.Add(context);
            return context;
        }

        public void Dispose()
        {
            foreach (AssetsContext context in _contexts.ToArray())
            {
                context.Unload();
            }

            _contexts.Clear();
            _global.Unload();
        }
    }
}

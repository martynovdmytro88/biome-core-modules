using System.Threading;
using Cysharp.Threading.Tasks;

namespace Biome.Configs.Abstract
{
    /// <summary>
    /// The seam that reads a raw asset from a source. One implementation per <see cref="ConfigSourceKind"/>.
    /// </summary>
    public interface IConfigSourceReader
    {
        /// <summary>Reads the asset at <paramref name="source"/> as <typeparamref name="T"/>.</summary>
        UniTask<T> ReadAsync<T>(ConfigSource source, CancellationToken ct) where T : class;
    }
}

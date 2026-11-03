using System.Globalization;
using Newtonsoft.Json;

namespace Biome.LocalStore
{
    /// <summary>
    /// The ONE serializer configuration the module owns — never per-caller settings, or the size validation and the
    /// envelope guarantee nothing. Invariant culture; explicit null/default policy (both written); <c>TypeNameHandling</c>
    /// off and type metadata ignored on read (attack surface + portability hazard); no date auto-parsing so string
    /// payloads round-trip byte-for-byte. Not exposed outside the module.
    /// </summary>
    internal static class LocalStoreJson
    {
        internal static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Culture = CultureInfo.InvariantCulture,
            TypeNameHandling = TypeNameHandling.None,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            NullValueHandling = NullValueHandling.Include,
            DefaultValueHandling = DefaultValueHandling.Include,
            DateParseHandling = DateParseHandling.None,
            Formatting = Formatting.None,
            MaxDepth = 32,
        };

        internal static JsonSerializer CreateSerializer() => JsonSerializer.Create(Settings);
    }
}

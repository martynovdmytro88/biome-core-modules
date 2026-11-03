using System;

namespace Biome.Utils.Commands
{
    /// <summary>
    /// What a callable does. Where it runs is the asmdef the method sits in — never a field here.
    /// </summary>
    public enum CallableKind
    {
        Generate,
        Test,
        Mock,
        Cheat,
        Util,
    }

    /// <summary>
    /// The project attribute on a unity-cli callable: <c>kind</c> only. Sits beside <c>[CliCommand]</c>
    /// on the same static method; the <c>///</c> block over the method is the one documentation source
    /// (missing = a <c>no-doc</c> warning from <c>discover</c>).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class BiomeCallableAttribute : Attribute
    {
        public CallableKind Kind { get; }

        public BiomeCallableAttribute(CallableKind kind)
        {
            Kind = kind;
        }
    }
}

namespace Biome.Configs.Abstract
{
    /// <summary>The guard a service calls before it loads, for an environment that can hold two sources for one type.</summary>
    public interface IConfigsEnvironmentGuard
    {
        /// <summary>True with the first config type name that two or more entries share.</summary>
        bool TryFindDuplicateType(out string configType);
    }
}

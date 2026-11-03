// BuilderScript — drives project builds.
// Lives under an Editor/ special folder because it relies on UnityEditor / BuildPipeline,
// which are editor-only and must not compile into player builds.
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Biome.Builder.Editor
{
    /// <summary>Entry point for project builds. Stub — fill on practice.</summary>
    public static class BuilderScript
    {
        // [MenuItem("Build/Build Player")]
        public static void Build()
        {
            // TODO: define scenes, build target, and output path (repo-root Builds/).
            Debug.Log("BuilderScript.Build — stub.");
        }
    }
}
#endif

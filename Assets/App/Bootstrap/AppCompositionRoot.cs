using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace Biome.App
{
    /// <summary>
    /// The app's composition root. Before the first scene loads, the bootstrap loads this asset from
    /// <c>Resources/</c>, selects a profile (an <see cref="AppProfile"/>) and raises its scope as VContainer's root
    /// scope: one instance in DontDestroyOnLoad, the parent of every scene <see cref="LifetimeScope"/> with no setup,
    /// disposed when it is destroyed (quit / leaving play mode). The <see cref="VContainerSettings"/> it needs is
    /// created in code and held here — the project carries no settings asset. Selection only: it names no module type and
    /// no environment — the profile carries both.
    /// </summary>
    [CreateAssetMenu(menuName = "Biome/App Composition Root", fileName = ResourcePath)]
    public sealed class AppCompositionRoot : ScriptableObject
    {
        public const string ResourcePath = "AppCompositionRoot";

        /// <summary>The boot scene is build index 0 (Build Settings); test-run scenes are not in the build.</summary>
        public const int BootSceneBuildIndex = 0;

#if UNITY_EDITOR
        /// <summary>SessionState key an editor tool sets (through unity-cli) to launch another profile once; the launch that reads it erases it.</summary>
        internal const string EditorProfileIdKey = "Biome.App.ProfileIdOverride";
#endif

        [SerializeField, Tooltip("File name (no extension) of the AppProfile in a Resources folder that the launch raises.")]
        private string _profileId;

        private static VContainerSettings s_Settings;
        private static LifetimeScope s_Retired; // shut down, its Destroy still pending until the end of the frame

        /// <summary>The live root scope; null before the bootstrap and after <see cref="Shutdown"/>.</summary>
        public static LifetimeScope Active { get; private set; }

        /// <summary>Loads the AppProfile named by the root's id from Resources; throws naming the id when it is blank or unknown.</summary>
        public AppProfile SelectProfile()
        {
            var id = TakeProfileId();
            var profile = string.IsNullOrWhiteSpace(id) ? null : Resources.Load<AppProfile>(id);
            if (profile == null)
            {
                throw new InvalidOperationException(
                    $"AppCompositionRoot: no AppProfile with id '{id}' in Resources (the id is the profile's file name).");
            }

            return profile;
        }

        /// <summary>The asset's id; in the Editor a pending override key wins once and is erased, so the next launch is the asset's again.</summary>
        private string TakeProfileId()
        {
#if UNITY_EDITOR
            var overrideId = UnityEditor.SessionState.GetString(EditorProfileIdKey, string.Empty);
            if (overrideId.Length > 0)
            {
                UnityEditor.SessionState.EraseString(EditorProfileIdKey);
                return overrideId;
            }
#endif
            return _profileId;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Active = null;
            s_Settings = null;
            s_Retired = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            var root = Resources.Load<AppCompositionRoot>(ResourcePath);
            if (root == null)
            {
                throw new InvalidOperationException($"AppCompositionRoot: no asset at Resources/{ResourcePath}.");
            }

            var scope = Raise(root.SelectProfile());
            var ct = Application.exitCancellationToken;
            root.RunFlowWhenBootedAsync(scope.Container, ct).Forget();
        }

        /// <summary>
        /// Starts the state flow (whose Init state initialises the configs) only when the game booted from the Boot
        /// scene: the active scene is checked one frame after the boot, so test runs (which start from their own scene)
        /// never start the flow.
        /// </summary>
        internal async UniTask RunFlowWhenBootedAsync(IObjectResolver resolver, CancellationToken ct)
        {
            await UniTask.NextFrame(ct);
            if (SceneManager.GetActiveScene().buildIndex != BootSceneBuildIndex)
            {
                return;
            }

            await resolver.Resolve<StateFlowStarter>().RunAsync(ct);
        }

        /// <summary>
        /// Raises the scope of <paramref name="profile"/> as the root scope and returns the live instance; the scope
        /// receives the profile before it builds. Throws for a null or incomplete profile, while a root is up, and in
        /// the same frame as <see cref="Shutdown"/> (the old instance is destroyed at the end of the frame).
        /// </summary>
        internal static LifetimeScope Raise(AppProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile), "AppCompositionRoot: no profile to raise.");
            }

            profile.Validate();
            if (Active != null)
            {
                throw new InvalidOperationException("AppCompositionRoot: a root scope is already up; Shutdown() first.");
            }

            if (s_Settings == null)
            {
                s_Settings = CreateInstance<VContainerSettings>();
                s_Settings.hideFlags = HideFlags.DontUnloadUnusedAsset;
            }

            s_Settings.RootLifetimeScope = profile.Scope;
            LifetimeScope instance;
            using (AppProfileScope.Handover(profile))
            {
                instance = s_Settings.GetOrCreateRootLifetimeScopeInstance();
            }

            if (ReferenceEquals(instance, s_Retired))
            {
                throw new InvalidOperationException(
                    "AppCompositionRoot: Raise() in the same frame as Shutdown() — the old root is destroyed at the end of the frame.");
            }

            Active = instance;
            return instance;
        }

        /// <summary>Destroys the root; its OnDestroy disposes the container, and with it every scope under its GameObject.</summary>
        internal static void Shutdown()
        {
            if (Active != null)
            {
                s_Retired = Active;
                Object.Destroy(Active.gameObject);
            }

            Active = null;
        }
    }
}

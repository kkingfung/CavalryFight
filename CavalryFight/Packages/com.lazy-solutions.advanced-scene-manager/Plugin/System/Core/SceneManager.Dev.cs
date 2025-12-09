using AdvancedSceneManager.Core;
using AdvancedSceneManager.Models.Interfaces;

namespace AdvancedSceneManager
{

    static partial class SceneManager
    {

#if UNITY_EDITOR
        /// <inheritdoc cref="IAssetsAPIInternal"/>
        internal static IAssetsAPIInternal assetImport => settings.project.assets;
#endif

#if ASM_DEV
        internal static bool isDev => true;
#else
        internal static bool isDev => false;
#endif

        internal static FeatureFlags features => FeatureFlags.instance;

    }

}

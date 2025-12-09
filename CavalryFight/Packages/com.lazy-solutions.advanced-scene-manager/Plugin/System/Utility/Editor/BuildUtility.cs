#if UNITY_EDITOR

using AdvancedSceneManager.Callbacks.Events.Editor;
using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Editor.Utility
{

    /// <summary>Provides functions for building, and build events.</summary>
    /// <remarks>Only available in editor.</remarks>
    public static partial class BuildUtility
    {

        private static bool hasRegisteredHandler;
        internal static void Initialize()
        {

            if (!Application.isPlaying && !hasRegisteredHandler)
            {
                BuildPlayerWindow.RegisterBuildPlayerHandler(e => _ = DoBuild(e));
                hasRegisteredHandler = true;
            }

            EditorBuildSettings.sceneListChanged += OnBuildSettingsChanged;
            SceneManager.events.RegisterCallback<ScenesAvailableForImportChangedEvent>(e => UpdateSceneList());
            UpdateSceneList();

        }

    }

}
#endif

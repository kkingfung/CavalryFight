using AdvancedSceneManager.Utility;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AdvancedSceneManager.Core
{

    /// <summary>Manages application quit processes and callbacks.</summary>
    public partial class App
    {

        internal void Initialize()
        {

            SceneManager.app.isStartupFinished = false;

#if UNITY_EDITOR
            EditorApplication.delayCall += () =>
            {
                if (!shouldRunStartupProcess)
                    TrackScenes();
                else
                    SceneManager.app.StartInternal();
            };
#else
            SceneManager.app.StartInternal();
#endif

        }

        static void TrackScenes()
        {
            foreach (var scene in SceneUtility.GetAllOpenUnityScenes())
                if (SceneManager.assets.scenes.TryFind(scene.path, out var s))
                    SceneManager.runtime.Track(s, scene);
        }

    }

}
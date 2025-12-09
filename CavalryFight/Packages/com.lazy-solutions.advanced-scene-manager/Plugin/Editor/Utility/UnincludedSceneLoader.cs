using AdvancedSceneManager.Core;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Loading;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using System.Collections;
using UnityEngine;
using Scene = AdvancedSceneManager.Models.Scene;
using sceneManager = UnityEngine.SceneManagement.SceneManager;

namespace AdvancedSceneManager.Editor
{

    class UnincludedSceneLoader : SceneLoader
    {

        public override bool isGlobal => true;

        public override bool CanHandleScene(Scene scene) =>
            scene && !scene.isIncludedInBuilds;

        public override IEnumerator LoadScene(Scene scene, SceneLoadArgs e)
        {

            if (!SceneManager.profile)
                yield break;

            AddToStandalone(scene);

            var async = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(scene.path, new(UnityEngine.SceneManagement.LoadSceneMode.Additive));

            if (e.reportProgress)
                yield return async.ReportProgress(SceneOperationKind.Unload, e.operation, scene);

            yield return async;

            var openedScene = e.GetOpenedScene();
            while (!openedScene.IsValid())
            {
                openedScene = e.GetOpenedScene();
                yield return null;
            }

            e.SetCompleted(openedScene);
            yield return null;

        }

        void AddToStandalone(Scene scene)
        {

            if (!SceneManager.profile.standaloneScenes.Contains(scene))
            {
                var sceneLoader = scene.GetSceneLoader();
                if (sceneLoader?.addScenesToBuildSettings ?? false)
                    return;

                SceneManager.profile.standaloneScenes.Add(scene);
                BuildUtility.UpdateSceneList(true);
                Debug.LogWarning($"The scene '{scene.path}' was not included in build. It has been added to standalone.");
            }

        }

        public override IEnumerator UnloadScene(Scene scene, SceneUnloadArgs e)
        {

            var async = sceneManager.UnloadSceneAsync(scene.internalScene.Value);

            if (e.reportProgress)
                yield return async.ReportProgress(SceneOperationKind.Unload, e.operation, scene);

            yield return async;

            e.SetCompleted();

        }

    }

}

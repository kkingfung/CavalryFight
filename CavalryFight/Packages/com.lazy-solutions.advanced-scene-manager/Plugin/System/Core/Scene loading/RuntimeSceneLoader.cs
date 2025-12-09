using AdvancedSceneManager.Loading;
using AdvancedSceneManager.Services;
using AdvancedSceneManager.Utility;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scene = AdvancedSceneManager.Models.Scene;
using sceneManager = UnityEngine.SceneManagement.SceneManager;

namespace AdvancedSceneManager.Core
{

    /// <summary>A scene loader that uses the normal play mode API. 
    /// <para>See <see cref="sceneManager.LoadSceneAsync(string,LoadSceneMode)"/>.</para>
    /// <para>See <see cref="sceneManager.UnloadSceneAsync(UnityEngine.SceneManagement.Scene)"/>.</para>
    /// </summary>
    [RegisterService]
    public class RuntimeSceneLoader : SceneLoader
    {

        /// <inheritdoc />
        public override bool activeOutsideOfPlayMode => false;

        /// <inheritdoc />
        public override bool CanHandleScene(Scene scene) =>
            scene.isIncludedInBuilds;

        /// <inheritdoc />
        public override IEnumerator LoadScene(Scene scene, SceneLoadArgs e)
        {

            if (e.isPreload)
            {

                var async = sceneManager.
                    LoadSceneAsync(scene.path, LoadSceneMode.Additive).
                    Preload(out var activateCallback);

                if (e.reportProgress)
                    yield return async.ReportProgress(SceneOperationKind.Load, e.operation, scene);

                while (!Mathf.Approximately(async.progress, 0.9f))
                    yield return null;

                var openedScene = e.GetOpenedScene();
                while (!openedScene.IsValid())
                {
                    openedScene = e.GetOpenedScene();
                    yield return null;
                }

                e.SetCompleted(openedScene, activateCallback);

            }
            else
            {

                var async = sceneManager.LoadSceneAsync(scene.path, LoadSceneMode.Additive);

                if (e.reportProgress)
                    yield return async.ReportProgress(SceneOperationKind.Load, e.operation, scene);

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

        }

        /// <inheritdoc />
        public override IEnumerator UnloadScene(Scene scene, SceneUnloadArgs e)
        {

            if (scene.internalScene.HasValue)
            {

                var async = sceneManager.UnloadSceneAsync(scene.internalScene.Value);

                if (e.reportProgress)
                    yield return async.ReportProgress(SceneOperationKind.Unload, e.operation, scene);

                yield return async;

            }

            e.SetCompleted();

        }

    }

}

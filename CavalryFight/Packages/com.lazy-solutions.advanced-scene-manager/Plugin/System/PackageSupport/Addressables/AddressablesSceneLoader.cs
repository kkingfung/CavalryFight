#if ADDRESSABLES

using AdvancedSceneManager.Core;
using AdvancedSceneManager.Loading;
using AdvancedSceneManager.Services;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using Scene = AdvancedSceneManager.Models.Scene;

namespace AdvancedSceneManager.PackageSupport.Addressables
{

    [RegisterService]
    class AddressablesSceneLoader : Core.SceneLoader
    {

        public override string sceneToggleText => "Addressable";
        public override Indicator indicator => new() { useFontAwesome = true, text = "" };

        public override bool isGlobal => false;
        public override bool addScenesToBuildSettings => false;

        public override IEnumerator LoadScene(Scene scene, SceneLoadArgs e)
        {

            if (!e.scene.isAddressable)
                yield break;

            AsyncOperationHandle<SceneInstance> async;
            if (Guid.TryParse(e.scene.m_assetReference?.AssetGUID, out _))
                async = scene.m_assetReference.LoadSceneAsync(LoadSceneMode.Additive, activateOnLoad: !e.isPreload);
            else
            {

                var address = scene.address;
                if (string.IsNullOrWhiteSpace(address))
                {
                    Debug.LogError("Could not find address for scene: " + e.scene.name);
                    yield break;
                }

                async = UnityEngine.AddressableAssets.Addressables.LoadSceneAsync(address, loadMode: LoadSceneMode.Additive, activateOnLoad: !e.isPreload);

            }

            if (e.reportProgress)
                yield return async.ReportProgress(SceneOperationKind.Load, scene, e.operation);

            yield return async;

            if (async.OperationException != null)
            {
                Debug.LogError(async.OperationException);
                e.SetCompleted(default);
                yield break;
            }
            else
            {
                e.scene.m_sceneInstance = async.Result;
                if (e.isPreload)
                    e.SetCompleted(async.Result.Scene, ActivatePreloadedScene);
                else
                    e.SetCompleted(async.Result.Scene);
            }

            IEnumerator ActivatePreloadedScene()
            {
                yield return async.Result.ActivateAsync();
            }

        }

        public override IEnumerator UnloadScene(Scene scene, SceneUnloadArgs e)
        {

            if (!e.scene)
                yield break;

            if (e.scene.m_sceneInstance is not SceneInstance sceneInstance)
                yield break;

            if (sceneInstance.Scene.IsValid() && sceneInstance.Scene.isLoaded)
            {

                var async = UnityEngine.AddressableAssets.Addressables.UnloadSceneAsync(e.scene.m_sceneInstance.Value);

                if (e.reportProgress)
                    async.ReportProgress(SceneOperationKind.Unload, scene, e.operation);

                yield return async;

            }

            e.scene.m_sceneInstance = null;
            e.SetCompleted();

        }

    }

}
#endif

using AdvancedSceneManager.Callbacks.Events;
using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.Editor.UI.Views.Popups;
using AdvancedSceneManager.Utility;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views
{

    partial class HeaderView
    {

        Label labelPlay = null!;
        ProgressSpinner progressSpinner = null!;
        Label labelOperations = null!;
        Label labelSceneLoad = null!;
        Label labelViewDiag = null!;
        void OnAdded_DiagInfo()
        {

            labelPlay = view.Q<Label>("label-play");
            progressSpinner = view.Q<ProgressSpinner>();
            labelOperations = view.Q<Label>("text-operation-info");
            labelSceneLoad = view.Q<Label>("text-scene-load-info");
            labelViewDiag = view.Q<Label>("text-view-diag");

            RegisterEvent<SceneManagerBecameBusyEvent>(e => Runtime_startedWorking());
            RegisterEvent<SceneManagerBecameIdleEvent>(e => Runtime_startedWorking());

            RegisterEvent<StartupStartedEvent>(e => App_beforeStartup());
            RegisterEvent<StartupFinishedEvent>(e => App_afterStartup());
            RegisterEvent<StartupCancelledEvent>(e => App_afterStartup());

            RegisterEvent<PlayModeChangedEvent>(e => EditorApplication_playModeStateChanged(e.state));

            UpdateOperationText();
            UpdateSceneLoadText();
            UpdatePlayButton();

            view.Q<Button>("button-diag").clicked += ASMWindow.OpenPopup<DiagPopup>;

        }

        void UpdatePlayButton()
        {
            var isBusy = SceneManager.runtime.isBusy || SceneManager.app.isRunningStartupProcess;
            buttonPlay.SetEnabled(!isBusy);
            SetProgressSpinner(isBusy);
        }

        private void EditorApplication_playModeStateChanged(PlayModeStateChange obj)
        {
            UpdateOperationText();
            UpdateSceneLoadText();
            UpdatePlayButton();
        }

        void UnsetupProgressListener()
        {
            UpdateOperationText();
            UpdateSceneLoadText();
            UpdatePlayButton();
        }

        void Runtime_startedWorking()
        {
            UpdatePlayButton();
            UpdateTexts().StartCoroutine();
        }

        void Runtime_stoppedWorking()
        {
            UpdatePlayButton();
            UpdateOperationText();
            UpdateSceneLoadText();
        }

        void App_beforeStartup()
        {
            UpdatePlayButton();
            UpdateTexts().StartCoroutine();
        }

        void App_afterStartup()
        {
            UpdateOperationText();
            UpdateSceneLoadText();
            UpdatePlayButton();
        }

        void SetProgressSpinner(bool visible)
        {
            progressSpinner.style.opacity = visible ? 1 : 0;
            labelPlay.style.opacity = visible ? 0 : 1;
            buttonPlay.tooltip = visible ? null : "Play as build (hold shift to force open all scenes)";

            if (visible)
                progressSpinner.Start();
            else
                progressSpinner.Stop();
        }

        IEnumerator UpdateTexts()
        {
            while (SceneManager.runtime.isBusy)
            {
                UpdateOperationText();
                UpdateSceneLoadText();
                yield return null;
            }
            UpdateOperationText();
            UpdateSceneLoadText();
        }

        void UpdateOperationText()
        {

            if (labelOperations is not null)
            {
                labelOperations.text = GetString();
                labelOperations.EnableInClassList("visible", !string.IsNullOrEmpty(labelOperations.text));
                UpdateViewDiagText();
            }

            string GetString()
            {

                var runningCount = SceneManager.runtime.runningOperations.Count();
                var queuedCount = SceneManager.runtime.queuedOperations.Except(SceneManager.runtime.runningOperations).Count();

                if (runningCount > 0 && queuedCount > 0)
                    return $"{runningCount} running operations - {queuedCount} waiting";
                else if (runningCount > 0)
                    return $"{runningCount} running operations";
                else if (queuedCount > 0)
                    return $"{queuedCount} queued operations";

                if (SceneManager.app.isRunningStartupProcess)
                    return "Running startup";

                return null;

            }

        }

        void UpdateSceneLoadText()
        {

            if (labelSceneLoad is not null)
            {
                labelSceneLoad.text = GetString();
                labelSceneLoad.EnableInClassList("visible", !string.IsNullOrEmpty(labelSceneLoad.text));
                UpdateViewDiagText();
            }

            string GetString()
            {

                var scenesOpening = SceneManager.runtime.currentOperation?.open?.Count();
                var scenesClosing = SceneManager.runtime.currentOperation?.close?.Count();
                var scenesPreloading = SceneManager.runtime.currentOperation?.preload?.Count();

                if (scenesOpening > 0)
                    return $"{scenesOpening} scenes opening";
                else if (scenesClosing > 0)
                    return $"{scenesClosing} scenes closing";
                else if (scenesPreloading > 0)
                    return $"{scenesPreloading} scenes preloading";

                return null;

            }

        }

        void UpdateViewDiagText()
        {
            labelViewDiag.style.position = labelOperations.ClassListContains("visible") || labelSceneLoad.ClassListContains("visible") ? Position.Absolute : Position.Relative;
            labelViewDiag.visible = !labelOperations.ClassListContains("visible") && !labelSceneLoad.ClassListContains("visible");
        }

    }

}

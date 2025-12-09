using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Services;
using AdvancedSceneManager.Utility;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    class UpdatePopup : ViewModel, IPopup
    {

        [Inject] private readonly IUpdateService updateService;
        public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.update;

        Button downloadButton;
        Button checkButton;
        protected override void OnAdded()
        {

            view.Q<Button>("button-github").RegisterCallback<ClickEvent>(e => ViewOnGithub());
            view.Q<Button>("button-settings").RegisterCallback<ClickEvent>(e => ViewSettings());

            checkButton = view.Q<Button>("button-check");
            checkButton.RegisterCallback<ClickEvent>(e => CheckUpdate());

            downloadButton = view.Q<Button>("button-download");
            downloadButton.clicked += Download;

            RegisterEvent<UpdateCheckedEvent>(e => Refresh());
            Refresh();

            view.Q<ScrollView>().PersistScrollPosition();

        }

        void Refresh()
        {

            downloadButton.SetEnabled(updateService.isUpdateAvailable);

            if (updateService.requiresAssetStoreUpdate)
                view.Q<PopupHeader>().title = "Asset store update is available!";
            else if (updateService.isUpdateAvailable)
                view.Q<PopupHeader>().title = "Patch is available!";
            else
                view.Q<PopupHeader>().title = "No patch available";

            view.Q<Label>("text-patch-notes").text = updateService.latestPatchNotes.Trim();

            view.Q("text-warning-patch-notes").SetVisible(updateService.isUpdateAvailable && !updateService.requiresAssetStoreUpdate);
            view.Q("text-warning-asset-store").SetVisible(updateService.requiresAssetStoreUpdate);

        }

        protected override void OnRemoved()
        {
            token?.Cancel();
            view.Q<ScrollView>().ClearScrollPosition();
        }

        void ViewOnGithub()
        {
            Application.OpenURL("https://github.com/Lazy-Solutions/AdvancedSceneManager/releases");
        }

        async void CheckUpdate()
        {

            checkButton.SetEnabled(false);
            downloadButton.SetEnabled(false);

            await updateService.CheckForUpdatesAsync();

            Refresh();
            checkButton.SetEnabled(true);

        }

        CancellationTokenSource token;

        async void Download()
        {

            try
            {

                token?.Cancel();
                token = new();

                downloadButton.SetEnabled(false);
                await updateService.CheckForUpdatesAsync(true, token.Token);
                if (token?.IsCancellationRequested ?? false)
                    throw new TaskCanceledException();

                if (updateService.isUpdateAvailable)
                    await updateService.ApplyUpdateAsync();
                else
                    ASMWindow.ClosePopup();

                token = null;

            }
            catch (TaskCanceledException)
            {
                downloadButton?.SetEnabled(true);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                downloadButton?.SetEnabled(true);
            }
            finally
            {
                CoroutineUtility.Run(() => downloadButton?.SetEnabled(true), after: TimeSpan.FromSeconds(10));
            }

        }

        void ViewSettings()
        {
            ASMWindow.OpenSettings<SettingsPopup.UpdatesPage>();
        }

    }

}

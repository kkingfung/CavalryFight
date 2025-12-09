using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Services;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    partial class SettingsPopup
    {

        public class UpdatesPage : SubPage
        {

            public override string title => "Updates";
            public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.settings.updates;

            [Inject] private readonly IUpdateService updateService;

            Label statusLabel;
            Label currentVersionLabel;
            Label availableVersionLabel;
            Label assetStoreLabel;

            Button checkButton;
            Button downloadButton;
            protected override void OnAdded()
            {

                view.Q("dropdown-interval").BindToUserSettings();
                view.Q("toggle-default").BindToSettings();

                checkButton = view.Q<Button>("button-check");
                downloadButton = view.Q<Button>("button-download");
                statusLabel = view.Q<Label>("text-status");
                currentVersionLabel = view.Q<Label>("text-version-current");
                availableVersionLabel = view.Q<Label>("text-version-available");
                assetStoreLabel = view.Q<Label>("text-asset-store");

                checkButton.clicked += Check;
                downloadButton.clicked += Download;
                view.Q<Button>("button-view-patches").clicked += ViewPatches;

                Reload();

                RegisterEvent<UpdateCheckedEvent>(e => Reload());

            }

            void Reload()
            {

                currentVersionLabel.text = $"Installed version: <b>{updateService.installedVersion}</b>";
                availableVersionLabel.text = $"Available version: <b>{updateService.latestVersion}</b>";

                availableVersionLabel.SetVisible(updateService.isUpdateAvailable);
                assetStoreLabel.SetVisible(updateService.requiresAssetStoreUpdate);

                checkButton.SetVisible(!updateService.isUpdateAvailable);
                downloadButton.SetVisible(updateService.isUpdateAvailable);

                statusLabel.text = !updateService.isUpdateAvailable
                    ? "You are up to date!"
                    : updateService.requiresAssetStoreUpdate
                        ? "Asset Store update is available!"
                        : "Patch is available!";

            }

            async void Check() =>
                await updateService.CheckForUpdatesAsync(logError: true);

            async void Download()
            {
                if (await updateService.CheckForUpdatesAsync(logError: true) &&
                    updateService.isUpdateAvailable)
                    await updateService.ApplyUpdateAsync();
            }

            void ViewPatches() =>
                updateService.OpenReleasesPage();

        }

    }

}

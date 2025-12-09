using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    partial class SettingsPopup
    {

        public class AssetsPage : SubPage
        {

            public override string title => "Assets";
            public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.settings.assets.root;

            protected override void OnAdded()
            {
                view.BindToSettings();
                SetupAssetMove();

                RegisterEvent<OnWindowEnableEvent>(e =>
                {
                    //There is a bug where block list items will become disabled after domain reload, this fixes that
                    view.Query<BindableElement>().ForEach(e => e.SetEnabled(true));
                });
            }

            void SetupAssetMove()
            {

                var textField = view.Q<TextField>("text-path");
                var cancelButton = view.Q<Button>("button-cancel");
                var applyButton = view.Q<Button>("button-apply");

                textField.value = SceneManager.settings.project.assetPath;
                UpdateEnabledStatus();

                cancelButton.clicked += Cancel;
                applyButton.clicked += Apply;
                textField.RegisterValueChangedCallback(e => UpdateEnabledStatus());

                void Cancel()
                {
                    textField.value = SceneManager.settings.project.assetPath;
                    UpdateEnabledStatus();

                    cancelButton.Hide();
                    applyButton.Hide();
                }

                void Apply()
                {

                    var profilePath = SceneManager.assetImport.GetFolder<Profile>();
                    var scenePath = SceneManager.assetImport.GetFolder<Scene>();

                    if (!AssetDatabaseUtility.CreateFolder(textField.value))
                    {
                        Debug.LogError("An error occurred when creating specified folder.");
                        return;
                    }

                    SceneManager.settings.project.assetPath = textField.value;

                    AssetDatabase.MoveAsset(profilePath, SceneManager.assetImport.GetFolder<Profile>());
                    AssetDatabase.MoveAsset(scenePath, SceneManager.assetImport.GetFolder<Scene>());

                    UpdateEnabledStatus();

                }

                void UpdateEnabledStatus()
                {

                    var isChanged = textField.value != SceneManager.settings.project.assetPath;

                    var isValid =
                        isChanged &&
                        textField.value.ToLower().StartsWith("assets/") &&
                        !string.IsNullOrEmpty(textField.value) &&
                        !Path.GetInvalidPathChars().Any(textField.value.Contains) &&
                        !Path.GetInvalidFileNameChars().Any(textField.value.Replace("/", "").Contains);

                    cancelButton.SetEnabled(isValid);
                    applyButton.SetEnabled(isValid);

                    cancelButton.SetVisible(isChanged);
                    applyButton.SetVisible(isChanged);

                }

            }

        }

    }

}

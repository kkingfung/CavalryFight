using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Services;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    class DynamicCollectionPopup : ViewModel, IPopup
    {

        public override string title => context.dynamicCollection?.title;
        public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.dynamicCollection;

        [Inject] private readonly IDialogService dialogService = null!;

        protected override void OnAdded()
        {

            if (context.dynamicCollection is not DynamicCollection collection)
            {
                ASMWindow.ClosePopup();
                return;
            }

            SetupTitle(collection);
            SetupPath(collection);

        }

        void SetupTitle(DynamicCollection collection)
        {

            view.Q<Label>("label-title").text = title;

            var renameButton = view.Q<Button>("button-rename");

            renameButton.RegisterCallback<ClickEvent>(e =>
            {
                dialogService.PromptName(
                    value: collection.title,
                    onContinue: (text) =>
                    {
                        collection.Rename(text);
                        ASMWindow.OpenPopup<DynamicCollectionPopup>(new(collection));
                    },
                    onCancel: () => ASMWindow.OpenPopup<DynamicCollectionPopup>(new(collection)));
            });

        }

        void SetupPath(DynamicCollection collection)
        {

            var pathField = view.Q<TextField>("text-path");
            pathField.value = collection.path;

            pathField.RegisterCallback<BlurEvent>(e =>
            {
                collection.path = pathField.text;
                collection.ReloadPaths();
                collection.Save();
            });

            view.Q<Button>("button-pick").RegisterCallback<ClickEvent>(e =>
            {

                var path = collection.path;
                if (string.IsNullOrEmpty(path))
                    path = Application.dataPath;

                var folder = EditorUtility.OpenFolderPanel("Pick folder...", path, "");

                if (Directory.Exists(folder))
                {
                    pathField.value = folder;
                    collection.path = AssetDatabaseUtility.MakeRelative(folder);
                    collection.ReloadPaths();
                }

            });

        }

        protected override void OnRemoved()
        {
            context.dynamicCollection.ReloadPaths();
        }

    }

}

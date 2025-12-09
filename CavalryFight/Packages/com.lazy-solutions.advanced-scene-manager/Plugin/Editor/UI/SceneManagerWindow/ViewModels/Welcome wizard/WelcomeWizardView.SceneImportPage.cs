using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views
{

    partial class WelcomeWizardView
    {

        public class SceneImportPage : SubPage
        {

            public override string title => "Scenes";
            public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.welcomeWizard.sceneImport;
            public override bool useScrollView => false;

            List<string> scenesToImport = null!;

            string scenesToImportPersisted
            {
                get => sessionState.GetProperty(string.Empty);
                set => sessionState.SetProperty(value);
            }

            public override VisualElement CreateHeaderGUI()
            {

                var header = view.Q("header");

                var autoImportGroup = header.Q("import-option-field");
                autoImportGroup.BindToSettings();
                autoImportGroup.tooltip =
                    "Manual:\n" +
                    "Manually import each scene.\n\n" +
                    "SceneCreated:\n" +
                    "Import scenes when they are created.";

                header.RemoveFromHierarchy();
                header.Show();

                return header;

            }

            Button importButton = null!;
            public override VisualElement CreateFooterGUI()
            {

                var footer = view.Q("footer");

                importButton = view.Q<Button>("button-import");
                importButton.RegisterCallback<ClickEvent>(e => SceneImportUtility.Import(scenesToImport));

                importButton?.SetEnabled(scenesToImport?.Any() ?? false);

                footer.RemoveFromHierarchy();
                footer.Show();

                return footer;

            }

            void RefreshImportButton()
            {
                importButton?.SetEnabled(scenesToImport.Any());
            }

            protected override void OnAdded()
            {
                var list = view.Q<ASMListView>();
                Reload();

                scenesToImport = scenesToImportPersisted.Split('\n').Where(SceneImportUtility.StringExtensions.IsValidSceneToImport).ToList();

                RegisterEvent<ScenesAvailableForImportChangedEvent>(e => Reload());

                void Reload()
                {
                    var scenes = SceneImportUtility.unimportedScenes.Except(SceneImportUtility.dynamicScenes);

                    list.itemsSource = scenes.ToList();
                    scenesToImport = scenesToImport?.Where(scenes.Contains)?.ToList() ?? new();
                    EditorApplication.delayCall += () => list.SetCheckedItems(scenesToImport);
                    RefreshImportButton();
                }

                list.UseCommonSceneImportContextMenu();
                list.RegisterItemCheckedCallback(e =>
                {
                    scenesToImport = list.checkedItems.OfType<string>().ToList();
                    scenesToImportPersisted = string.Join('\n', scenesToImport);
                    RefreshImportButton();
                });
            }

            protected override void OnRemoved()
            {
                scenesToImport?.Clear();
                scenesToImportPersisted = null!;
            }

        }

    }

}


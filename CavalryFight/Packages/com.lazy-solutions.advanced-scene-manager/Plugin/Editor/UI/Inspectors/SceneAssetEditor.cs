using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Editor.UI.Views.Popups;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using UnityEditor;
using UnityEngine.UIElements;
using static AdvancedSceneManager.Editor.Utility.SceneImportUtility.StringExtensions;

namespace AdvancedSceneManager.Editor
{

    [CustomEditor(typeof(SceneAsset))]
    partial class SceneAssetEditor : UnityEditor.Editor
    {

        SceneAsset sceneAsset;
        Scene scene;
        string path;

        VisualElement rootVisualElement = null!;
        public override VisualElement CreateInspectorGUI()
        {

            rootVisualElement = new VisualElement();

            rootVisualElement.style.marginTop = 4;
            rootVisualElement.style.marginBottom = 4;
            rootVisualElement.style.marginLeft = -8;

            rootVisualElement.RegisterCallbackOnce<AttachToPanelEvent>(e =>
            {
                foreach (var style in ViewLocator.instance.styles.Enumerate())
                    rootVisualElement.styleSheets.Add(style);
            });

            Reload();

            return rootVisualElement;

        }

        void Reload()
        {

            if (rootVisualElement is null)
                return;

            sceneAsset = (SceneAsset)target;
            path = AssetDatabase.GetAssetPath(sceneAsset);
            _ = SceneImportUtility.GetImportedScene(path, out scene);

            rootVisualElement.Clear();

            if (FallbackSceneUtility.GetStartupScene() == path)
                FallbackScene(rootVisualElement);
            else if (!scene)
                Unimported(rootVisualElement);
            else if (scene.isImported)
                ASMScene(rootVisualElement);
            else
                ImportedScene(rootVisualElement);

        }

        void FallbackScene(VisualElement element)
        {
            element.Add(new HelpBox(" This scene is designated as ASM fallback scene.", HelpBoxMessageType.Info));
        }

        void Unimported(VisualElement element)
        {

            if (BlocklistUtility.IsBlacklisted(path))
                element.Add(new HelpBox(" This scene has been blacklisted for import into Advanced Scene Manager. It cannot be imported", HelpBoxMessageType.Info));
            else if (!IsValidSceneToImport(path))
            {

                var isScene = "IsScene: " + IsScene(path);
                var isNotImported = "IsNotImported: " + !IsImported(path);
                var isNotBlacklisted = "IsNotBlacklisted: " + !IsBlacklisted(path);
                var isNotTestScene = "IsNotTestScene: " + !IsTestScene(path);
                var isNotPackageScene = "IsNotPackageScene: " + !IsPackageScene(path);
                var isNotASMScene = "IsNotASMScene: " + !IsASMScene(path);

                var str = string.Join("\n ", isScene, isNotImported, isNotBlacklisted, isNotTestScene, isNotPackageScene, isNotASMScene);

                element.Add(new HelpBox(" This scene is not valid to import in Advanced Scene Manager.\n One of the following filters may indicate why:\n " + str, HelpBoxMessageType.Info));

            }
            else
            {
                var button = new Button() { text = "Import into ASM" };
                button.clicked += () => SceneImportUtility.Import(path);
                button.style.alignSelf = Align.FlexEnd;
                element.Add(button);
            }

        }

        void ASMScene(VisualElement element)
        {

            if (scene == SceneManager.assets.defaults.inGameToolbarScene)
                element.Add(new HelpBox(" " + Scene.InGameToolbarDescription, HelpBoxMessageType.Info));
            else if (scene == SceneManager.assets.defaults.pauseScene)
                element.Add(new HelpBox(" " + Scene.PauseScreenDescription, HelpBoxMessageType.Info));

            ImportedScene(element);

        }

        void ImportedScene(VisualElement element)
        {

            AddSceneField(element);

            if (!SceneManager.profile)
                return;

            var popup = new ScenePopup();
            var view = popup.CreateGUI();
            element.Add(view);

            popup.Add(view, new(scene: scene, customParam: ScenePopup.CustomParams.isInspector));

            // Replace default ScenePopup.MainPage with SceneAssetEditor.MainPage
            popup.stack?.ResetStack();
            popup.stack?.Push<SceneAssetEditor.MainPage>(animate: false);

        }

        void AddSceneField(VisualElement element)
        {

            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.paddingTop = 6;

            var field = new SceneField() { value = scene };
            field.SetObjectPickerEnabled(false);
            field.style.flexGrow = 1;
            field.style.marginRight = 3;

            container.Add(field);

            if (scene && !scene.isDefaultASMScene)
            {

                var button = new Button() { text = "Unimport" };
                button.clicked += () => SceneImportUtility.Unimport(scene);
                button.style.marginLeft = 3;

                container.Add(button);

            }

            element.Add(container);

        }

    }

}

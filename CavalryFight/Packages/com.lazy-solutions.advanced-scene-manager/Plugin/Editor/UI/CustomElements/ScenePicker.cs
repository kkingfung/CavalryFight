using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.Editor.UI;
using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor
{

    [UxmlElement]
    public partial class ScenePicker : BaseField<Object>
    {

        [UxmlAttribute]
        public bool AllowNone { get; set; } = true;

        [UxmlAttribute]
        public PopulateMode Mode { get; set; } = PopulateMode.LoadingScenes;

        const string NullString = "None";
        readonly DropdownField dropdown;
        readonly Button reloadButton;
        readonly ExtendableButtonContainer extendableButtonContainer;

        List<Scene> scenes = new();

        public ScenePicker() : base(null, null)
        {

            InitializeExtendableButtonContainer(out extendableButtonContainer);
            InitializeReloadButton(out reloadButton);
            InitializeDropdownField(out dropdown);
            InitializeContextMenu();

            RegisterCallbackOnce<AttachToPanelEvent>(e =>
            {
                EditorSceneManager.sceneSaved += EditorSceneManager_sceneSaved;
                SceneManager.events.RegisterCallback<ScenesAvailableForImportChangedEvent>(SceneImportUtility_scenesChanged);

                AutoPopulate();
            });

            RegisterCallbackOnce<DetachFromPanelEvent>(e =>
            {
                EditorSceneManager.sceneSaved -= EditorSceneManager_sceneSaved;
                SceneManager.events.UnregisterCallback<ScenesAvailableForImportChangedEvent>(SceneImportUtility_scenesChanged);
            });

        }

        private void EditorSceneManager_sceneSaved(UnityEngine.SceneManagement.Scene scene)
        {
            if (Mode != PopulateMode.Manual)
                AutoPopulate();
        }

        private void SceneImportUtility_scenesChanged(ScenesAvailableForImportChangedEvent e)
        {
            if (Mode != PopulateMode.Manual)
                AutoPopulate();
        }

        #region DropdownField

        void InitializeDropdownField(out DropdownField dropdown)
        {
            dropdown = new();
            Add(dropdown);

            dropdown.value = GetDisplayString(_value);
            dropdown.RegisterValueChangedCallback(e =>
            {
                if (e.newValue == NullString)
                    _value = null;
                else
                    _value = scenes.FirstOrDefault(s => s && s.name == e.newValue);
            });
        }

        string GetDisplayString(Scene scene) =>
            scene ? scene.name : NullString;

        #endregion
        #region ExtendableButtonContainer

        void InitializeExtendableButtonContainer(out ExtendableButtonContainer extendableButtonContainer)
        {

            extendableButtonContainer = new()
            {
                location = UI.ElementLocation.SceneLeft,
                autoInitialize = false,
            };

            extendableButtonContainer.style.flexDirection = FlexDirection.Row;
            extendableButtonContainer.style.marginRight = 6;

            extendableButtonContainer.Initialize(new(scene: _value));

            Add(extendableButtonContainer);

        }

        #endregion
        #region Auto populate

        public enum PopulateMode
        {
            Manual,
            LoadingScenes,
            SplashScenes
        }

        void InitializeReloadButton(out Button button)
        {
            button = new Button(() =>
            {
                LoadingScreenUtility.CacheSpecialScenes();
                AutoPopulate();
            })
            { text = "", tooltip = "Refresh scenes" };

            button.AddToClassList("scene-open-button");
            button.AddToClassList("fontAwesome");
            button.style.marginRight = 3;

            button.SetVisible(Mode != PopulateMode.Manual);
        }

        void AutoPopulate()
        {
            if (Mode == PopulateMode.SplashScenes)
                SetScenes(GetSplashScenes());
            else if (Mode == PopulateMode.LoadingScenes)
                SetScenes(GetLoadingScenes());
            else
                SetScenes();
        }

        IEnumerable<Scene> GetSplashScenes() =>
            SceneManager.assets.scenes.Where(s => s && s.isSplashScreen);

        IEnumerable<Scene> GetLoadingScenes() =>
            SceneManager.assets.scenes.Where(s => s && s.isLoadingScreen);

        #endregion
        #region Context menu

        void InitializeContextMenu() =>
            this.ContextMenu(e =>
            {

                //Unity has its own items, which don't make sense in this context
                e.menu.ClearItems();

                e.menu.AppendAction("View SceneAsset",
                    status: _value ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled,
                    action: (e) =>
                    {
                        EditorGUIUtility.PingObject(_value.sceneAsset);
                        Selection.activeObject = _value.sceneAsset;
                    });

            });

        #endregion

        public void SetReadOnly(bool value)
        {
            dropdown.SetEnabled(!value);
        }

        public void SetScenes(IEnumerable<Scene> scenes = null)
        {

            this.scenes = scenes?.Where(s => s)?.ToList() ?? new List<Scene>();

            if (AllowNone)
                this.scenes.Insert(0, null);

            dropdown.choices = this.scenes.OrderBy(s => s ? s.name : null).Select(s => s ? s.name : NullString).ToList();

        }

        public override void SetValueWithoutNotify(Object newValue)
        {
            base.SetValueWithoutNotify(newValue);

            dropdown.value = GetDisplayString(newValue as Scene);
            extendableButtonContainer.Initialize(new(scene: _value));
        }

        public Scene _value
        {
            get => base.value as Scene;
            set => base.value = value;
        }

    }

}

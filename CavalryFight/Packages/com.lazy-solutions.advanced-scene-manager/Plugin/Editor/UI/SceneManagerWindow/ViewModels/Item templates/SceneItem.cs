using AdvancedSceneManager.DependencyInjection;
using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Editor.UI.Views.Popups;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Interfaces;
using AdvancedSceneManager.Services;
using AdvancedSceneManager.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.ItemTemplates
{

    class SceneItem : ViewModel
    {

        public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.items.scene;

        static readonly List<SceneItem> sceneItems = new();

        ExtendableButtonContainer leftContainer = null!;
        ExtendableButtonContainer rightContainer = null!;

        [Inject] private readonly ISelectionService selectionService = null!;
        [Inject] private readonly ISearchService searchService = null!;

        public SceneItem()
        { }

        protected override void OnAdded()
        {

            if (context.baseCollection is null)
                throw new InvalidOperationException("SceneItem view model did not recieve a collection.");

            if (!context.sceneIndex.HasValue)
                throw new InvalidOperationException("SceneItem view model did not recieve a scene.");

            sceneItems.Add(this);

            leftContainer = view.Q<ExtendableButtonContainer>("extendable-button-container-left");
            rightContainer = view.Q<ExtendableButtonContainer>("extendable-button-container-right");

            leftContainer.Initialize(context);
            rightContainer.Initialize(context);

            context.baseCollection.PropertyChanged += Collection_PropertyChanged;

            if (context.dynamicCollection)
                SetupSceneAssetField();
            else
                ChangeScene(context.scene);

            SetupRemove();
            SetupMenu();
            SetupContextMenu();
            SetupSelection();

            if (context.collection)
                view.SetupLockBindings(context.collection);

            view.Q("button-remove").SetVisible(context.baseCollection is IEditableCollection);
            view.Q("label-reorder-scene")?.SetVisible(context.baseCollection is IEditableCollection && !searchService.isSearching);

            searchService.RefreshSearchDelayed();

        }

        protected override void OnRemoved()
        {
            sceneItems.Remove(this);
            if (context.baseCollection is not null)
                context.baseCollection.PropertyChanged -= Collection_PropertyChanged;
        }

        void ChangeScene(Scene scene)
        {

            context = new(context.baseCollection, scene, context.sceneIndex);

            if (scene)
                view.Bind(new(scene));
            else
                view.Unbind();

            SetupSceneField();

            view.Q<Button>("button-menu").SetEnabled(scene);
            SetupMenu();
            CheckDuplicate();

            leftContainer.Initialize(context);
            rightContainer.Initialize(context);

        }

        void CheckDuplicate()
        {
            if (context.collection)
                view.Q("scene").EnableInClassList("duplicate", context.collection.scenes.NonNull().Count(s => s == context.scene) > 1);
        }

        void Collection_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {

            leftContainer.Initialize(context);
            rightContainer.Initialize(context);

            CheckDuplicate();

        }

        void SetupRemove()
        {
            if (context.baseCollection is IEditableCollection c)
                view.Q<Button>("button-remove").clicked += () =>
                {
                    if (context.sceneIndex.HasValue)
                        c.RemoveAt(context.sceneIndex.Value);
                };
        }

        SceneField field;
        EventCallback<ChangeEvent<Scene>> callback;

        void SetupSceneField()
        {

            field = view.Q<SceneField>("field-scene");
            field.SetVisible(true);
            field.SetValueWithoutNotify(context.scene);
            field.SetObjectPickerEnabled(context.baseCollection is IEditableCollection);

            if (context.baseCollection is IEditableCollection c)
            {

                if (callback is not null)
                    field.UnregisterValueChangedCallback(callback);
                field.RegisterValueChangedCallback(callback = OnFieldChanged);

                void OnFieldChanged(ChangeEvent<Scene> e)
                {

                    if (!context.sceneIndex.HasValue)
                        return;

                    c.Replace(context.sceneIndex.Value, e.newValue);
                    ChangeScene(e.newValue);

                    foreach (var item in sceneItems)
                        item.CheckDuplicate();

                }

            }

        }

        void SetupSceneAssetField()
        {

            var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(context.OfType<string>());

            var field = view.Q<ObjectField>("field-sceneAsset");
            field.SetVisible(true);
            field.SetEnabled(false);
            field.SetValueWithoutNotify(asset);

        }

        void SetupMenu()
        {

            var buttonMenu = view.Q<Button>("button-menu");
            var buttonCreate = view.Q<Button>("button-create");

            buttonMenu.SetVisible(false);
            buttonCreate.SetVisible(false);

            if (context.baseCollection is not SceneCollection and not StandaloneCollection)
                return;

            if (context.scene)
            {
                buttonMenu.clickable = new(OpenPopup);
                buttonMenu.SetVisible(true);
            }
            else
            {
                buttonCreate.clickable = new(CreateScene);
                buttonCreate.SetVisible(true);
            }

            void OpenPopup() =>
                ASMWindow.OpenPopup<ScenePopup>(context);

            void CreateScene()
            {

                DependencyInjectionUtility.GetService<IDialogService>().PromptName(async value =>
                {
                    var scene = SceneUtility.CreateAndImport($"{GetCurrentFolderInProjectWindow()}/{value}.unity");
                    if (field is not null)
                        field.value = scene;
                    await Task.Delay(250);
                    ASMWindow.ClosePopup();
                },
                onCancel: () => ASMWindow.ClosePopup());

            }

        }

        string GetCurrentFolderInProjectWindow()
        {
            var projectWindowUtilType = typeof(ProjectWindowUtil);
            var getActiveFolderPath = projectWindowUtilType.GetMethod("GetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);
            return getActiveFolderPath.Invoke(null, null) as string;
        }

        void SetupContextMenu()
        {

            if (!context.collection)
                return;

            view.RegisterCallback<ContextClickEvent>(e =>
            {

                if ((e.target as VisualElement)?.parent?.GetType() == typeof(ExtendableButtonContainer))
                    return;

                var menu = new GenericMenu();
                e.StopPropagation();

                var wasAdded = selectionService.IsSelected(this);
                var scenes = selectionService.scenes.ToList();
                if (!wasAdded)
                    scenes.Add(new() { collection = context.collection, sceneIndex = context.sceneIndex!.Value });

                var distinctScenes = scenes.
                    Select(c => ((SceneCollection)c.collection).scenes.ElementAtOrDefault(c.sceneIndex)).
                    NonNull().
                    Where(s => s).
                    ToArray();

                GenerateSceneHeader(scenes);

                var asset = context.scene ? (SceneAsset)context.scene : null;
                if (asset)
                    menu.AddItem(new("View in project view..."), false, () => ContextualActions.ViewInProjectView(asset));
                else
                    menu.AddDisabledItem(new("View in project view..."));

                menu.AddSeparator("");

                if (context.scene)
                    menu.AddItem(new("Open..."), false, () => ContextualActions.Open(distinctScenes, additive: false));
                else
                    menu.AddDisabledItem(new("Open..."));

                if (context.scene)
                    menu.AddItem(new("Open additive..."), false, () => ContextualActions.Open(distinctScenes, additive: true));
                else
                    menu.AddDisabledItem(new("Open additive..."));

                menu.AddSeparator("");
                menu.AddItem(new("Remove..."), false, () => ContextualActions.Remove(scenes));

                if (distinctScenes.Length > 1)
                {

                    menu.AddSeparator("");

                    menu.AddItem(new("Merge scenes..."), false, () => ContextualActions.MergeScenes(distinctScenes));
                    menu.AddItem(new("Bake lightmaps..."), false, () => ContextualActions.BakeLightmaps(distinctScenes));

                }

                menu.ShowAsContext();

                void GenerateSceneHeader(IEnumerable<CollectionScenePair> items)
                {

                    var groupedItems = items.GroupBy(i => i.collection).Select(g => (collection: g.Key, scenes: g.Select(i => i.sceneIndex).ToArray()));

                    foreach (var c in groupedItems)
                    {
                        menu.AddDisabledItem(new(c.collection.title));
                        foreach (var index in c.scenes)
                        {
                            var scene = ((SceneCollection)c.collection).ElementAtOrDefault(index);
                            menu.AddDisabledItem(new(index + ": " + (scene ? scene.name : "none")), false);
                        }
                        menu.AddSeparator("");
                    }
                }

            });

        }

        void SetupSelection()
        {

            var container = view.Q("scene");
            var sceneField = view.Q<SceneField>();

            sceneField.OnClickCallback(e =>
            {

                if (e.button == 0 && (e.ctrlKey || e.commandKey))
                {
                    e.StopPropagation();
                    e.StopImmediatePropagation();
                    selectionService.ToggleSelection(this);
                    UpdateSelection();
                }

            });

            UpdateSelection();
            void UpdateSelection() =>
                container.EnableInClassList("selected", selectionService.IsSelected(this));

        }

    }

}

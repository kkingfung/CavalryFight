using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Enums;
using AdvancedSceneManager.Services;
using System;
using System.Linq;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    partial class ScenePopup
    {

        public class MainPage : SubPage
        {

            public override string title => context.scene ? context.scene.name : null;
            public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.scene.main;

            [Inject] private readonly IDialogService dialogService = null!;

            protected override void OnAdded()
            {

                base.OnAdded();

                view.Q<SceneLoaderPicker>().Initialize(context.scene);

                SetupHalfPersistent();
                SetupEditorOptions();
                SetupLoadPriority();
                SetupAutoScenes();

                view.Q("navigate-standalone").SetVisible(context.standaloneCollection);

            }

            #region Header

            public override VisualElement CreateHeaderGUI()
            {
                var header = view.Q("header");

                SetupRenameButton(header.Q<Button>("button-rename"));
                SetupCollectionTitle(header.Q<Label>("label-collection-title"));
                SetupOpenWithToggle(header.Q<Toggle>("toggle-open-with-collection"));

                header.RemoveFromHierarchy();
                header.Show();

                return header;
            }

            void SetupRenameButton(Button renameButton)
            {

                renameButton.SetVisible(context.customParam is not CustomParams.isInspector);

                renameButton.RegisterCallback<ClickEvent>(e =>
                {
                    var collection = context.baseCollection;
                    var scene = context.scene;
                    dialogService.PromptName(
                        value: scene.name,
                        onContinue: (title) =>
                        {
                            scene.Rename(title);
                            ASMWindow.OpenPopup<ScenePopup>(new(collection, scene));
                        },
                        onCancel: () => ASMWindow.OpenPopup<ScenePopup>(new(collection, scene)));
                });

            }

            void SetupCollectionTitle(Label collectionTitleLabel)
            {
                collectionTitleLabel.text = context.baseCollection?.title;
                collectionTitleLabel.tooltip = $"You're modifying this scene in the context of the '{collectionTitleLabel?.text}' collection, some properties only apply to the collection they are opened by.";
            }

            void SetupOpenWithToggle(Toggle openWithCollectionToggle)
            {

                openWithCollectionToggle.Hide();

                if (context.collection)
                {
                    openWithCollectionToggle.Show();
                    openWithCollectionToggle.label = $"Open with collection";
                    openWithCollectionToggle.tooltip = openWithCollectionToggle.tooltip.Replace("parent", $"'{context.collection.title}'");
                    openWithCollectionToggle.value = context.collection.ShouldAutoOpen(context.scene);
                    openWithCollectionToggle.RegisterValueChangedCallback(e => context.collection.SetAutoOpen(context.scene, e.newValue));
                }

            }

            #endregion
            #region Scene

            void SetupHalfPersistent()
            {

                const int ReopenIndex = 0;
                const int RemainOpenIndex = 1;

                var dropdown = view.Q<DropdownField>("dropdown-half-persistent");
                dropdown.index = context.scene.keepOpenWhenNewCollectionWouldReopen ? RemainOpenIndex : ReopenIndex;
                dropdown.RegisterValueChangedCallback(e => context.scene.keepOpenWhenNewCollectionWouldReopen = dropdown.index == RemainOpenIndex);

            }

            void SetupLoadPriority()
            {

                var descriptionExpanded = view.Q("description");

                bool isVisible = false;
                var button = view.Q<Button>("button-loading-priority-description");
                button.clicked += () => Toggle();

                void Toggle()
                {
                    isVisible = !isVisible;
                    descriptionExpanded.SetVisible(isVisible);
                    button.text = isVisible ? "Read less" : "Read more";

                    if (context.customParam is not CustomParams.isInspector)
                        view.schedule.Execute(() =>
                        {
                            if (view.GetAncestor<ScrollView>() is ScrollView scroll)
                                scroll.verticalScroller.value = scroll.verticalScroller.highValue;
                        }).ExecuteLater(5);
                }

            }

            void SetupAutoScenes()
            {
                var list = view.Q<ListView>("list-auto-scenes");

                list.onAdd = _ =>
                {
                    context.scene.m_autoScenes.Add(new() { option = AutoSceneOption.Never });
                    context.scene.Save();
                    Reload();

                    BuildUtility.UpdateSceneList();
                };

                list.onRemove = _ =>
                {
                    if (list.selectedIndex == -1)
                        return;

                    context.scene.m_autoScenes.RemoveAt(list.selectedIndex);
                    context.scene.Save();
                    Reload();

                    BuildUtility.UpdateSceneList();
                };

                Reload();
                void Reload()
                {
                    var items = context.scene.m_autoScenes.Where(e => e.option.HasValue).ToList();
                    list.itemsSource = items;
                }

                list.bindItem = (element, index) =>
                {
                    var entry = (AutoSceneEntry)list.itemsSource[index]; //InvalidCast?
                    var sceneField = element.Q<SceneField>();
                    var enumField = element.Q<EnumField>();

                    sceneField.SetValueWithoutNotify(entry.scene);
                    enumField.SetValueWithoutNotify(entry.option);

                    var sceneChangeCallback = new EventCallback<ChangeEvent<Scene>>(e =>
                    {
                        entry.scene = e.newValue;
                        context.scene.Save();
                        BuildUtility.UpdateSceneList();
                    });

                    var enumChangeCallback = new EventCallback<ChangeEvent<Enum>>(e =>
                    {
                        entry.option = (AutoSceneOption)e.newValue;
                        context.scene.Save();
                        BuildUtility.UpdateSceneList();
                    });

                    sceneField.RegisterValueChangedCallback(sceneChangeCallback);
                    enumField.RegisterValueChangedCallback(enumChangeCallback);

                    element.userData = (sceneChangeCallback, enumChangeCallback);
                };

                list.unbindItem = (element, index) =>
                {

                    if (element.userData is not (EventCallback<ChangeEvent<Scene>> sceneChangeCallback, EventCallback<ChangeEvent<Enum>> enumChangeCallback))
                        return;

                    var sceneField = element.Q<SceneField>();
                    var enumField = element.Q<EnumField>();

                    sceneField.UnregisterValueChangedCallback(sceneChangeCallback);
                    enumField.UnregisterValueChangedCallback(enumChangeCallback);
                    element.userData = null;
                };


            }

            #endregion
            #region Editor

            void SetupEditorOptions()
            {

                var list = view.Q<ListView>("list-auto-open-scenes");
                var enumField = view.Q<EnumField>("enum-auto-open-in-editor");
                list.makeItem = () => new SceneField();

                list.bindItem = (element, i) =>
                {

                    var field = (SceneField)element;

                    if (context.scene.autoOpenInEditorScenes.ElementAtOrDefault(i) is Scene s && s)
                        field.SetValueWithoutNotify(s);
                    else
                        field.SetValueWithoutNotify(null);

                    field.RegisterValueChangedCallback(e => context.scene.autoOpenInEditorScenes[i] = e.newValue);

                };

                enumField.RegisterValueChangedCallback(e => UpdateListVisible());

                UpdateListVisible();
                void UpdateListVisible() =>
                    list.SetVisible(context.scene.autoOpenInEditor == Models.Enums.EditorPersistentOption.WhenAnyOfTheFollowingScenesAreOpened);

            }

            #endregion

        }

    }
}


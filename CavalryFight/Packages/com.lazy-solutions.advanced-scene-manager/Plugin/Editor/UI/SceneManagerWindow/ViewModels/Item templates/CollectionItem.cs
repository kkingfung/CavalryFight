using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Editor.UI.Views.Popups;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Interfaces;
using AdvancedSceneManager.Services;
using AdvancedSceneManager.Utility;
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.ItemTemplates
{

    class CollectionItem : ViewModel
    {

        public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.items.collection;

        ExtendableButtonContainer leftContainer = null!;
        ExtendableButtonContainer rightContainer = null!;

        [Inject] private readonly ISelectionService selectionService = null!;
        [Inject] private readonly ISearchService searchService = null!;

        protected override void OnAdded()
        {

            if (context.baseCollection is null)
            {
                view.Hide();
                return;
            }

            base.OnAdded();

            leftContainer = view.Q<ExtendableButtonContainer>("extendable-button-container-left");
            rightContainer = view.Q<ExtendableButtonContainer>("extendable-button-container-right");

            if (context.collection)
            {
                leftContainer.Initialize(context);
                rightContainer.Initialize(context);
            }

            SetupHeader();
            SetupContent();

            view.Q("label-reorder-collection")?.SetVisible(context.collection && !searchService.isSearching);
            view.Q("button-add-scene")?.SetVisible(context.baseCollection is IEditableCollection);
            view.Q("button-remove")?.SetVisible(context.baseCollection is SceneCollection or DynamicCollection or DefaultASMScenesCollection);
            view.Q("button-menu")?.SetVisible(context.baseCollection is SceneCollection or DynamicCollection);

            view.Q("collection").EnableInClassList("last", SceneManager.profile.collections.LastOrDefault() == context.collection);

            if (context.baseCollection is ScriptableObject obj && obj)
                view.Bind(new(obj));

            searchService.RefreshSearchDelayed();

        }

        protected override void OnRemoved()
        {
            RemoveScenes();
        }

        bool isExpanded
        {
            get => SceneManager.settings.user.m_expandedCollections.Contains(context.baseCollection!.id);
            set
            {

                if (value == SceneManager.settings.user.m_expandedCollections.Contains(context.baseCollection!.id))
                    return;

                SceneManager.settings.user.m_expandedCollections.Remove(context.baseCollection.id);
                if (value == true)
                    SceneManager.settings.user.m_expandedCollections.Add(context.baseCollection.id);

                SceneManager.settings.user.Save();
                UpdateExpanded();

            }
        }

        #region Header

        void SetupHeader()
        {

            SetupContextMenu();

            SetupExpander();
            SetupCollectionDrag();
            SetupSceneHeaderDrop();
            SetupStartupIndicator();

            SetupMenu();
            SetupRemove();
            SetupAdd();

            if (context.collection)
                view.SetupLockBindings(context.collection);

        }

        void SetupContextMenu()
        {

            if (!context.collection)
                return;

            view.Q("button-header").ContextMenu(e =>
            {

                e.StopPropagation();

                var collections = selectionService.collections.Concat(context.collection).ToArray();
                GenerateCollectionHeader(collections);

                var isSingleVisibility = collections.Length == 1 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;

                e.menu.AppendAction("View in project view...", e => ContextualActions.ViewInProjectView(context.collection), isSingleVisibility);
                e.menu.AppendAction("Create template...", e => ContextualActions.CreateTemplate(context.collection), isSingleVisibility);

                e.menu.AppendSeparator(); e.menu.AppendSeparator();
                e.menu.AppendAction("Remove...", e => ContextualActions.Remove(collections));

                void GenerateCollectionHeader(params ISceneCollection[] collections)
                {
                    foreach (var c in collections)
                        e.menu.AppendAction(c.title, e => { }, DropdownMenuAction.Status.Disabled);
                    e.menu.AppendSeparator();
                }

            });

        }

        #region Middle

        void SetupExpander()
        {

            var header = view.Q("collection-header");
            var expander = view.Q<Button>("button-header");
            var list = view.GetAncestor<ListView>();

            UpdateExpanded();
            UpdateSelection();

            expander.clickable = null;
            expander.clickable = new(() => { });
            expander.clickable.activators.Add(new() { modifiers = EventModifiers.Control });
            expander.clickable.clickedWithEventInfo += (_e) =>
            {

                if (_e.IsCtrlKeyHeld() || _e.IsCommandKeyHeld())
                {

                    selectionService.ToggleSelection(this);

                    var i = SceneManager.profile.IndexOf(context.collection);
                    if (i == -1)
                        return;

                    if (list.selectedIndices.Contains(i))
                        list.RemoveFromSelection(i);
                    else
                        list.AddToSelection(i);

                    UpdateSelection();

                }
                else
                    ToggleExpanded();

            };

            void UpdateSelection() =>
                header.EnableInClassList("selected", selectionService.IsSelected(this));

        }

        void ToggleExpanded()
        {
            isExpanded = !isExpanded;
            UpdateExpanded();
        }

        Label expandedStatus;
        void UpdateExpanded()
        {

            view.Q("collection").EnableInClassList("expanded", isExpanded);
            expandedStatus ??= view.Q<Label>("label-expanded-status");

            expandedStatus.text = isExpanded ? "" : "";
            expandedStatus.style.marginTop = isExpanded ? 0 : 1;

            if (isExpanded)
            {
                if (!hasScenes)
                    AddScenes();
            }
            else if (!SceneManager.settings.user.keepSceneUIInMemoryWhenCollectionCollapsed)
            {
                RemoveScenes();
            }

        }

        void SetupCollectionDrag()
        {

            if (!context.collection)
                return;

            var header = view.Q("button-header");

            header.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button == 0)
                    header.CapturePointer(e.pointerId);
            }, TrickleDown.TrickleDown);

            header.RegisterCallback<PointerUpEvent>(e =>
            {
                header.ReleasePointer(e.pointerId);
            }, TrickleDown.TrickleDown);

            header.RegisterCallback<PointerLeaveEvent>(e =>
            {

                var isDragging = DragAndDrop.objectReferences.Length == 1 && DragAndDrop.objectReferences[0] == context.collection;
                if (header.HasPointerCapture(e.pointerId) && !isDragging)
                {
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.objectReferences = new[] { context.collection };
                    DragAndDrop.StartDrag("Collection drag: " + context.collection.name);
                }

                header.ReleasePointer(e.pointerId);

            });

        }

        void SetupSceneHeaderDrop()
        {

            if (context.baseCollection is not IEditableCollection collection)
                return;

            var header = view.Q("button-header");

            header.RegisterCallback<DragUpdatedEvent>(e =>
            {
                e.StopPropagation();
                var scenes = SceneField.GetDragDropScenes().ToArray();
                DragAndDrop.visualMode = scenes.Length > 0 ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;
            });

            header.RegisterCallback<DragPerformEvent>(e =>
            {

                var scenes = SceneField.GetDragDropScenes();
                if (scenes.Any())
                    collection.Add(scenes.ToArray());

            });

        }

        void SetupStartupIndicator()
        {
            if (context.collection)
                view.Q("label-startup").BindVisibility(context.collection, nameof(context.collection.isStartupCollection));
            else
                view.Q("label-startup").SetVisible(false);
        }

        #endregion
        #region Right

        void SetupMenu()
        {
            view.Q<Button>("button-menu").clickable = new Clickable(() =>
            {

                if (context.baseCollection is SceneCollection sc)
                    ASMWindow.OpenPopup<CollectionPopup>(new(sc));

                else if (context.baseCollection is DynamicCollection dc)
                    ASMWindow.OpenPopup<DynamicCollectionPopup>(new(dc));

            });
        }

        void SetupRemove() =>
            view.Q<Button>("button-remove").clickable = new Clickable(() =>
            {
                ContextualActions.Remove(context.baseCollection);
            });

        void SetupAdd()
        {
            view.Q<Button>("button-add-scene").clickable = new Clickable(() =>
            {
                (context.baseCollection as IEditableCollection)?.AddEmptyScene();
                isExpanded = true;
            });
        }

        #endregion

        #endregion
        #region Content

        void SetupContent()
        {
            SetupDescription();
            SetupSceneDropZone();
        }

        void SetupDescription()
        {

            if (context.baseCollection is null)
                return;

            var label = view.Q<Label>("label-description");
            label.text = context.baseCollection.description;
            label.BindVisibility(context.baseCollection, propertyPath: nameof(context.baseCollection.description));

            if (context.defaultASMCollection is not null)
            {
                var button = view.Q<Button>("button-import-default-scenes");
                button.clickable = new(DefaultASMScenesCollection.ImportScenes);
                button.SetVisible(true);
            }

        }

        bool hasScenes;
        void AddScenes()
        {
            hasScenes = true;

            var list = view.Q<ListView>("list");
            list.makeNoneElement = () => new Label("No scenes added.");

            // Always create a root without a bindingPath
            list.makeItem = () =>
            {
                var element = SceneManagerWindow.window!.viewLocator.items.scene.Instantiate();
                element.bindingPath = null; // prevent bogus auto-binding
                return element;
            };

            list.bindItem = (element, index) =>
            {
                if (list.itemsSource?[index] is not SerializedProperty prop)
                    return;

                var viewModel = new SceneItem();

                if (context.dynamicCollection)
                {
                    string path = prop.stringValue;
                    viewModel.Add(element, new(context.baseCollection, null, index, path));
                }
                else
                {
                    var scene = (Scene)prop.objectReferenceValue;
                    viewModel.Add(element, new(context.baseCollection, scene, index));
                }

                element.userData = viewModel;
            };

            list.unbindItem = (element, index) =>
            {
                if (element.userData is SceneItem viewModel)
                    _ = viewModel.Remove(ignoreView: true);
            };

            (SerializedObject so, string propertyName)? binding = context switch
            {
                { collection: { } col } => (new SerializedObject(col), nameof(SceneCollection.m_scenes)),
                { dynamicCollection: { } dyn } => (new SerializedObject(dyn), nameof(DynamicCollection.m_cachedPaths)),
                { standaloneCollection: { } s } => (new SerializedObject(s), nameof(StandaloneCollection.m_standaloneScenes)),
                { defaultASMCollection: { } d } => (new SerializedObject(d), nameof(DefaultASMScenesCollection.m_scenes)),
                _ => null
            };

            if (binding is { } b)
            {
                list.bindingPath = b.propertyName;
                list.Bind(b.so);
            }

        }

        void RemoveScenes()
        {
            var list = view.Q<ListView>("list");
            list.bindingPath = null;
            list.bindItem = null;
            list.itemsSource = null;
            list.ClearBindings();

            hasScenes = false;
        }

        void SetupSceneDropZone()
        {

            if (context.baseCollection is not IEditableCollection collection)
                return;

            var zone = view.Q("scene-drop-zone");

            view.RegisterCallback<DragEnterEvent>(e =>
            {
                if (IsSceneDrag())
                    SetVisible(true);
            }, TrickleDown.TrickleDown);

            view.RegisterCallback<PointerEnterEvent>(e =>
            {
                if (!IsSceneDrag())
                    SetVisible(false);
            }, TrickleDown.TrickleDown);

            view.RegisterCallback<DragExitedEvent>(e =>
            {
                SetVisible(false);
            });

            view.RegisterCallback<DragLeaveEvent>(e =>
            {
                SetVisible(false);
            });

            void SetVisible(bool visible) =>
                zone.EnableInClassList("isDragging", visible);

            zone.RegisterCallback<DragUpdatedEvent>(e =>
            {
                e.StopPropagation();
                e.StopImmediatePropagation();
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                DragAndDrop.AcceptDrag();
            });

            zone.RegisterCallback<DragPerformEvent>(e =>
            {
                var scenes = SceneField.GetDragDropScenes();
                collection.Add(SceneField.GetDragDropScenes().ToArray());
                SetVisible(false);
            });

            bool IsSceneDrag()
            {

                if (DragAndDrop.objectReferences.Length == 0)
                    return false;

                var scenes = SceneField.GetDragDropScenes();
                scenes = scenes.Except((collection as ISceneCollection<Scene>)?.scenes);
                return scenes.Any();

            }

        }

        #endregion

    }

}

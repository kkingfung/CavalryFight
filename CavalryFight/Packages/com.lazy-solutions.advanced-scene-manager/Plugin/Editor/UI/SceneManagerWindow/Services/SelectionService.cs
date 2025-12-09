using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.DependencyInjection;
using AdvancedSceneManager.Editor.UI.Views.ItemTemplates;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Interfaces;
using AdvancedSceneManager.Services;
using AdvancedSceneManager.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace AdvancedSceneManager.Editor.UI
{

    [RegisterService(typeof(SelectionService))]
    interface ISelectionService : DependencyInjectionUtility.IInjectable
    {
        IEnumerable<ISceneCollection> collections { get; }
        IEnumerable<CollectionScenePair> scenes { get; }
        IEnumerable<Scene> actionableScenes { get; }

        void Add(CollectionItem item);
        void Add(SceneItem item);
        void Clear();
        bool IsSelected(CollectionItem item);
        bool IsSelected(SceneItem item);
        void Remove(CollectionItem item);
        void Remove(ISceneCollection collection);
        void Remove(ISceneCollection collection, int sceneIndex);
        void Remove(SceneItem item);
        void SetSelection(CollectionItem item, bool value);
        void SetSelection(SceneItem item, bool value);
        void ToggleSelection(CollectionItem item);
        void ToggleSelection(SceneItem item);
    }

    class SelectionService : ServiceBase, ISelectionService
    {

        List<CollectionScenePair> selectedCollections => SceneManager.settings.user.m_selectedCollections ??= new();
        List<CollectionScenePair> selectedScenes => SceneManager.settings.user.m_selectedScenes ??= new();

        public IEnumerable<Scene> actionableScenes => selectedScenes.Select(s => s.scene).NonNull();
        public IEnumerable<CollectionScenePair> scenes => selectedScenes;
        public IEnumerable<ISceneCollection> collections => selectedCollections.OfType<CollectionScenePair>().Select(c => c.collection);

        bool hasSelection => selectedCollections.Count > 0 || selectedScenes.Count > 0;

        protected override void OnInitialize()
        {
            RegisterEvent<CollectionRemovedEvent>(e => Remove(e.collection));
            RegisterEvent<CollectionDeletedEvent>(e => Remove(e.collection));
        }

        void Save() =>
            SceneManager.settings.user.Save();

        void InvokeEvent() =>
            SceneManager.events.InvokeCallback(new CollectionViewSelectionChangedEvent(collections, selectedScenes));

        public void Add(SceneItem item) => SetSelection(item, true);
        public void Add(CollectionItem item) => SetSelection(item, true);

        public void Remove(SceneItem item) => SetSelection(item, false);
        public void Remove(CollectionItem item) => SetSelection(item, false);

        public void Remove(ISceneCollection collection)
        {
            selectedCollections.RemoveAll(c => c.collection.id == collection.id);
            Save();
            InvokeEvent();
        }

        public void Remove(ISceneCollection collection, int sceneIndex)
        {
            selectedScenes.RemoveAll(s => s.collection == collection && s.sceneIndex == sceneIndex);
            Save();
            InvokeEvent();
        }

        public void SetSelection(SceneItem item, bool value)
        {
            if (IsSelected(item) != value)
                ToggleSelection(item);
        }

        public void SetSelection(CollectionItem item, bool value)
        {
            if (IsSelected(item) != value)
                ToggleSelection(item);
        }

        public void ToggleSelection(SceneItem item)
        {

            if (item.context.baseCollection is not ISelectableCollection collection)
                return;

            var existingItem = selectedScenes.FirstOrDefault(i => i.collection == collection && i.sceneIndex == item.context.sceneIndex);
            if (!selectedScenes.Remove(existingItem))
                selectedScenes.Add(new() { collection = collection, sceneIndex = item.context.sceneIndex ?? 0 });

            Save();
            InvokeEvent();

        }

        public void ToggleSelection(CollectionItem item)
        {

            if (item.context.baseCollection is not ISelectableCollection collection)
                return;

            var existingItem = selectedCollections.FirstOrDefault(i => i.collection == collection);
            if (!selectedCollections.Remove(existingItem))
                selectedCollections.Add(new() { collection = collection });

            Save();
            InvokeEvent();

        }

        public bool IsSelected(SceneItem item) =>
            selectedScenes.Any(i => i.collection?.id == item.context.baseCollection?.id && i.sceneIndex == item.context.sceneIndex);

        public bool IsSelected(CollectionItem item) =>
            selectedCollections.Any(c => c.collection?.id == item.context.baseCollection?.id);

        public void Clear()
        {

            if (!selectedCollections.Any() && !selectedScenes.Any())
                return;

            selectedCollections.Clear();
            selectedScenes.Clear();
            Save();

            GetService<ICollectionViewService>().Reload();
            InvokeEvent();

        }

    }

}

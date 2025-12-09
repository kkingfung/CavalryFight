using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.Editor.UI.Views;
using AdvancedSceneManager.Editor.UI.Views.ItemTemplates;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Services;
using AdvancedSceneManager.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    [RegisterService(typeof(SearchService))]
    interface ISearchService
    {
        SerializableDictionary<SceneCollection, Scene[]> savedSearch { get; }
        string lastSearch { get; set; }
        bool isSearching { get; }

        void Search(string query);
        void ClearSearch();
        void RefreshSearch();
        void RefreshSearchDelayed();
    }

    class SearchService : ServiceBase, ISearchService
    {

        public SerializableDictionary<SceneCollection, Scene[]> savedSearch { get; private set; } = new();

        public string lastSearch
        {
            get => sessionState.GetProperty(string.Empty);
            set => sessionState.SetProperty(value);
        }

        public bool isSearching => !string.IsNullOrEmpty(lastSearch);

        protected override void OnInitialize()
        {
            RegisterEvent<CollectionAddedEvent>(e => RefreshSearch());
            RegisterEvent<CollectionRemovedEvent>(e => RefreshSearch());
        }

        public void RefreshSearchDelayed()
        {
            EditorApplication.delayCall -= RefreshSearch;
            EditorApplication.delayCall += RefreshSearch;
        }

        public void RefreshSearch()
        {
            if (isSearching)
                Search(lastSearch);
        }

        readonly List<ViewModel> filteredItems = new();

        IEnumerable<ViewModel> GetItems()
        {

            if (!SceneManagerWindow.window)
                return Enumerable.Empty<ViewModel>();

            if (SceneManagerWindow.window.mainView.GetViewModel<CollectionView>() is not CollectionView collectionView)
                return Enumerable.Empty<ViewModel>();

            var collections = collectionView.view.Query("CollectionItem").ToList().Select(view => view.userData).OfType<ViewModel>();
            var scenes = collectionView.view.Query("SceneItem").ToList().Select(view => view.userData).OfType<ViewModel>();

            return collections.Concat(scenes);

        }

        public void Search(string query)
        {

            ClearSearch();
            lastSearch = query;

            if (string.IsNullOrWhiteSpace(query))
                return;

            foreach (var item in GetItems().ToList())
            {

                var isMatch = item switch
                {
                    CollectionItem collection when collection.context.baseCollection is not null => collection.context.baseCollection?.title?.Contains(query, System.StringComparison.OrdinalIgnoreCase) ?? false,
                    SceneItem scene when scene.context.scene => scene.context.scene && (scene.context.scene.name?.Contains(query, System.StringComparison.OrdinalIgnoreCase) ?? false),
                    _ => false
                };

                ToggleFilteredOut(item.view, !isMatch);
                if (isMatch)
                    filteredItems.Add(item);

            }

        }

        public void ClearSearch()
        {

            lastSearch = string.Empty;
            savedSearch.Clear();

            foreach (var item in GetItems().ToList())
                ToggleFilteredOut(item.view, false);

            filteredItems.Clear();

        }

        void ToggleFilteredOut(VisualElement element, bool filteredOut)
        {
            element?.EnableInClassList("filtered-out", filteredOut);
        }
    }
}

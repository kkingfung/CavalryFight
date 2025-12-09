using AdvancedSceneManager.Callbacks.Events;
using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.Services;
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views
{

    class SearchView : ViewModel
    {

        public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.main.search;

        [Inject] private readonly IProfileBindingsService profileBindingService = null!;
        [Inject] private readonly ISearchService searchService = null!;

        protected override void OnAdded()
        {

            searchGroup = view.Q<GroupBox>("group-search");
            searchField = view.Q<TextField>("text-search");
            saveButton = view.Q<Button>("button-save-search");
            list = view.Q("list-saved");
            searchButton = rootVisualElement.Q<Button>("button-search");

            searchGroup.SetVisible(shouldAlwaysDisplaySearch || searchService.isSearching);

            if (searchService.isSearching && searchService.savedSearch == null)
                searchService.Search(searchService.lastSearch);

            SetupSave();

            SetupSearchBox();
            SetupGroup();

            UpdateSearchButton();

            profileBindingService.BindEnabledToProfile(rootVisualElement.Q<Button>("button-search"));

            RegisterEvent<ToggleSearchViewEvent>(e =>
            {
                if (e.display ?? !isOpen)
                    Open();
                else
                    Hide();
            });

            RegisterEvent<ASMSettingsChangedEvent>(e =>
            {
                searchGroup.SetVisible(shouldAlwaysDisplaySearch);
                UpdateSearchButton();
            });

        }

        GroupBox searchGroup = null!;
        TextField searchField = null!;
        static Button searchButton;

        Button saveButton = null!;
        VisualElement list = null!;

        public bool shouldAlwaysDisplaySearch => SceneManager.profile && SceneManager.settings.user.alwaysDisplaySearch;

        #region Triggers

        record ToggleSearchViewEvent(bool? display = null) : EventCallbackBase;

        [ASMWindowElement(ElementLocation.Header, isVisibleByDefault: true)]
        [ASMWindowElement(ElementLocation.Footer)]
        static VisualElement OpenSearch()
        {

            searchButton = new Button(() => ToggleOpen())
            {
                name = "button-search",
                text = "",
                tooltip = "Search collections and scenes (ctrl+f can also be used)",
            };
            searchButton.UseFontAwesome(solid: true);

            return searchButton;

        }

        [Shortcut("ASM/Search", typeof(SceneManagerWindow), defaultShortcutModifiers: ShortcutModifiers.Control, defaultKeyCode: KeyCode.F)]
        static void HotKey()
        {

            if (!ASMWindow.IsPopupOpen())
                ToggleOpen();

        }

        static void ToggleOpen(bool? display = null)
        {
            SceneManager.events.InvokeCallbackSync(new ToggleSearchViewEvent(display));
        }

        public bool isOpen => searchGroup.IsVisible();

        public void Open()
        {
            searchGroup.Show();
            UpdateSaved();
            searchField.Focus();
            UpdateSearchButton();
        }

        public void Hide()
        {

            if (shouldAlwaysDisplaySearch)
                return;

            searchService.ClearSearch();

            searchField.SetValueWithoutNotify(string.Empty);
            searchGroup.Hide();
            //collectionViewService.Reload();
            UpdateSearchButton();

        }

        #endregion
        #region Setup

        void SetupSave()
        {

            UpdateSaved();
            UpdateSaveButton();

            saveButton.clickable = null;
            saveButton.clickable = new(() =>
            {

                if (SceneManager.settings.user.savedSearches.Contains(searchField.text))
                    ArrayUtility.Remove(ref SceneManager.settings.user.savedSearches, searchField.text);
                else
                    ArrayUtility.Add(ref SceneManager.settings.user.savedSearches, searchField.text);

                SceneManager.settings.user.Save();
                UpdateSaved();
                UpdateSaveButton();

            });

            searchField.RegisterValueChangedCallback(e => UpdateSaveButton());

        }

        void SetupSearchBox()
        {

            searchField.value = searchService.lastSearch;

            searchField.RegisterCallback<KeyUpEvent>(e =>
            {
                UpdateSearch(searchField.text);
                UpdateSaveButton();
            });

        }

        void SetupGroup()
        {

            searchGroup.RegisterCallback<PointerDownEvent>(e =>
            {
                if (searchField.panel.focusController.focusedElement != searchField)
                    RefocusSearch(e.clickCount > 2);
            });

            void RefocusSearch(bool isDoubleClick = false)
            {

                searchField.Focus();

                if (isDoubleClick)
                    searchField.SelectAll();
                else
                    searchField.SelectRange(searchField.text.Length, searchField.text.Length);

            }

        }

        #endregion
        #region Search

        void UpdateSearch(string q)
        {
            searchService.Search(q);
        }

        #endregion
        #region Update UI

        void UpdateSearchButton()
        {

            if (searchButton is null)
                return;

            searchButton.text = shouldAlwaysDisplaySearch ? "" : "";
            searchButton.tooltip = shouldAlwaysDisplaySearch ? "Clear search" : "Search collections and scenes";

        }

        void UpdateSaveButton()
        {

            saveButton.SetVisible(!string.IsNullOrEmpty(searchField.text));
            if (SceneManager.settings.user.savedSearches?.Contains(searchField.text) ?? false)
            {
                saveButton.UseFontAwesome(solid: true);
                saveButton.text = "\uf02e";
            }
            else
            {
                saveButton.UseFontAwesome(regular: true);
                saveButton.text = "\uf02e";
            }

        }

        void UpdateSaved()
        {
            list.Clear();
            foreach (var item in SceneManager.settings.user.savedSearches ?? Array.Empty<string>())
                list.Add(new Button(() => { searchField.value = item; UpdateSearch(item); UpdateSaveButton(); }) { text = item, name = "button-saved-search" });
        }

        #endregion

    }

}

using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Models.Internal;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    abstract class ListPopup<T> : ViewModel, IPopup where T : ASMModelBase
    {

        public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.list;
        VisualTreeAsset listItem => ((SceneManagerWindow)window).viewLocator.items.list;

        public abstract void OnAdd();
        public abstract void OnSelected(T item);

        public virtual void OnRemove(T item) { }
        public virtual void OnRename(T item) { }
        public virtual void OnDuplicate(T item) { }

        public virtual bool displayRenameButton { get; }
        public virtual bool displayRemoveButton { get; }
        public virtual bool displayDuplicateButton { get; }
        public virtual bool displaySortButton { get; }

        public virtual IEnumerable<MenuButtonItem> CustomMenuItems(T item) => Enumerable.Empty<MenuButtonItem>();

        public abstract string noItemsText { get; }
        public abstract string headerText { get; }

        public abstract IEnumerable<T> items { get; }

        public virtual IEnumerable<T> Sort(IEnumerable<T> items, ListSortDirection sortDirection) =>
            items;

        T[] list;

        VisualElement container;
        Button sortButton;
        PopupHeader header;

        protected override void OnAdded()
        {

            base.OnAdded();

            this.container = view;

            container.BindToSettings();

            header = view.Q<PopupHeader>();

            header.title = headerText;

            container.Q<Label>("text-no-items").text = noItemsText;
            container.Q<Button>("button-add").clicked += OnAdd;

            var list = container.Q<ListView>();

            list.makeItem = () =>
            {
                var element = ViewUtility.Instantiate(listItem);
                OnMakeItem(element);
                return element;
            };

            list.unbindItem = Unbind;
            list.bindItem = Bind;

            sortButton = view.Q<Button>("button-sort");
            sortButton.SetVisible(displaySortButton);
            sortButton.text = SceneManager.settings.user.m_sortDirection == ListSortDirection.Ascending ? "" : "";
            sortButton.tooltip = SceneManager.settings.user.m_sortDirection == ListSortDirection.Ascending ? "Sort by: Descending" : "Sort by: Ascending";

            sortButton.RegisterCallback<ClickEvent>(e =>
            {
                SceneManager.settings.user.m_sortDirection = SceneManager.settings.user.m_sortDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
                SceneManager.settings.user.Save();
                sortButton.text = SceneManager.settings.user.m_sortDirection == ListSortDirection.Ascending ? "" : "";
                sortButton.tooltip = SceneManager.settings.user.m_sortDirection == ListSortDirection.Ascending ? "Sort by: Descending" : "Sort by: Ascending";
                Reload();
            });

            Reload();

            view.Q<ScrollView>().PersistScrollPosition();

        }

        public void Reload()
        {
            list = Sort(items.Where(o => o), SceneManager.settings.user.m_sortDirection).ToArray();
            container.Q("text-no-items").SetVisible(!list.Any());
            container.Q<ListView>().itemsSource = list;
            container.Q<ListView>().Rebuild();
        }

        void Unbind(VisualElement element, int index)
        {

            var nameButton = element.Q<Button>("button-main");
            var menuButton = element.Q<Button>("button-menu");
            nameButton.userData = null;

            nameButton.UnregisterCallback<ClickEvent>(OnSelect);

        }

        void OnSelect(ClickEvent e)
        {
            if (e.target is Button button && button.userData is T t)
                OnSelected(t);
        }

        void Bind(VisualElement element, int index)
        {

            var item = list.ElementAt(index);
            var text = element.Q<Label>("label-text");
            var nameButton = element.Q<Button>("button-main");
            var menuButton = element.Q<Button>("button-menu");
            element.Q<Toggle>().Hide();

            text.text = item.name;

            nameButton.RegisterCallback<ClickEvent>(OnSelect);

            var l = new List<MenuButtonItem>
            {
                new("Rename", () => OnRename(item), displayRenameButton),
                new("Duplicate", () => OnDuplicate(item), displayDuplicateButton),
                new("Remove", () => OnRemove(item), displayRemoveButton)
            };

            l.AddRange(CustomMenuItems(item));
            menuButton.SetupMenuButton(l);

            menuButton.SetVisible(displayRenameButton || displayDuplicateButton || displayRemoveButton);

            nameButton.userData = item;
            element.userData = item;

        }

        public virtual void OnMakeItem(VisualElement element)
        { }

    }

}

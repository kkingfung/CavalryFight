using AdvancedSceneManager.Editor.UI;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor
{

    [UxmlElement]
    public partial class ASMListView : GroupBox
    {

        class CheckableItem
        {
            public object item = null!;
            public bool isChecked;

            public override string ToString() => item.ToString();
        }

        [UxmlAttribute]
        public bool useCheckboxes { get; set; }

        [UxmlAttribute]
        public string noItemsText
        {
            get => m_noItemsText!;
            set
            {
                m_noItemsText = value;
                if (noItemsLabel is not null)
                    noItemsLabel.text = value;
            }
        }

        public Func<object, string> displayString { get; set; }

        string m_noItemsText;

        readonly ListView list = new();
        readonly Toggle allToggle;

        readonly List<VisualElement> elements = new();

        IList m_itemsSource;
        List<CheckableItem> items;
        public IList itemsSource
        {
            get => m_itemsSource;
            set
            {
                m_itemsSource = value;
                list.itemsSource = items = m_itemsSource.OfType<object>().Select(e => new CheckableItem() { item = e, isChecked = checkedItems?.Contains(e.ToString()) ?? false }).ToList();
            }
        }

        public IEnumerable<object> checkedItems => items?.Where(e => e.isChecked)?.Select(e => e.item) ?? Enumerable.Empty<object>();

        public void SetCheckedItems(IEnumerable<object> items)
        {

            if (this.items is null || items is null)
                return;

            foreach (var item in this.items)
                item.isChecked = items.Contains(item.item);

            foreach (var element in elements)
                element?.Q<Toggle>()?.SetValueWithoutNotify(((CheckableItem)element.userData)?.isChecked ?? false);

            UpdateAllToggle();

        }

        Label noItemsLabel;
        public ASMListView()
        {
            list.allowAdd = false;
            list.allowRemove = false;
            list.showBoundCollectionSize = false;
            list.fixedItemHeight = 28;
            list.itemsSource = new List<object>();

            list.makeItem = MakeItem;
            list.bindItem = BindItem;
            list.unbindItem = UnbindItem;
            list.destroyItem = DestroyItem;
            list.makeNoneElement = () => noItemsLabel = new Label(noItemsText ?? "No items") { name = "label-no-items" };

            allToggle = new Toggle() { name = "toggle-all", text = "(toggle all)", tooltip = "Toggle all" };
            allToggle.RegisterValueChangedCallback(e => OnAllToggle());
            UpdateAllToggle();

            Add(allToggle);
            Add(list);
        }

        VisualElement MakeItem()
        {

            var element = SceneManagerWindow.window!.viewLocator.items.list.Instantiate();

            var button = element.Q<Button>("button-main");
            var toggle = element.Q<Toggle>("toggle-check");
            var textLabel = element.Q("label-text");
            var menuButton = element.Q("button-menu");

            toggle.RegisterValueChangedCallback(OnToggle);
            button.RegisterCallback<ClickEvent>(OnClick);
            menuButton.RegisterCallback<ClickEvent>(OnMenu);

            toggle.SetVisible(useCheckboxes);

            elements.Add(element);

            return element;

        }

        void DestroyItem(VisualElement element)
        {
            elements.Remove(element);
        }

        void BindItem(VisualElement element, int index)
        {
            var item = items.ElementAtOrDefault(index);
            if (item is null)
                return;

            var menuButton = element.Q("button-menu");
            var toggle = element.Q<Toggle>();

            element.userData = item;
            toggle.userData = item;
            element.Q("button-main").userData = item;
            menuButton.userData = item;

            element.Q<Label>("label-text").text = displayString?.Invoke(item) ?? item.ToString();
            toggle.SetValueWithoutNotify(item.isChecked);

            menuButton.RegisterCallback<ClickEvent>(OnMenu);
            element.AddManipulator(new ContextualMenuManipulator(OnContextMenu));
            menuButton.SetVisible(contextMenuCallbacks.Any());

            UpdateAllToggle();
        }

        void UnbindItem(VisualElement element, int index)
        {
            element.userData = null;
            element.Q<Label>("label-text").text = null;
        }

        #region Toggle

        void ToggleItem(CheckableItem item)
        {
            item.isChecked = !item.isChecked;
            elements.FirstOrDefault(e => e.userData == item)?.Q<Toggle>()?.SetValueWithoutNotify(item.isChecked);
            UpdateAllToggle();
            OnItemChecked(item);
        }

        void OnToggle(ChangeEvent<bool> e)
        {
            var item = (CheckableItem)((VisualElement)e.target).userData;
            if (item is null)
                return;

            item.isChecked = e.newValue;
            UpdateAllToggle();
            OnItemChecked(item);
        }

        void UpdateAllToggle()
        {
            var isAllChecked = items?.All(i => i.isChecked) ?? false;
            var isAllUnchecked = items?.All(i => !i.isChecked) ?? true;

            allToggle.SetValueWithoutNotify(isAllChecked);
            allToggle.showMixedValue = !isAllChecked && !isAllUnchecked;
            allToggle.SetVisible(useCheckboxes && items?.Count > 1);
        }

        void OnAllToggle()
        {
            if (items is not null)
                foreach (var item in items)
                {
                    item.isChecked = allToggle.value;
                    OnItemChecked(item);
                }

            foreach (var element in elements)
                element.Q<Toggle>()?.SetValueWithoutNotify(allToggle.value);

            UpdateAllToggle();
        }

        readonly List<EventCallback<object>> itemCheckedCallbacks = new();

        public void RegisterItemCheckedCallback(EventCallback<object> callback) =>
            itemCheckedCallbacks.Add(callback);

        public void UnregisterItemCheckedCallback(EventCallback<object> callback) =>
            itemCheckedCallbacks.Remove(callback);

        void OnItemChecked(object item)
        {
            foreach (var callback in itemCheckedCallbacks)
                callback(item);
        }

        #endregion
        #region Menu

        readonly List<Action<GenericDropdownMenu, Vector2, object>> contextMenuCallbacks = new();

        public void RegisterContextMenu(Action<GenericDropdownMenu, Vector2, object> callback)
        {
            contextMenuCallbacks.Add(callback);
            foreach (var element in elements)
                element.Q("button-menu").SetVisible(true);
        }

        public void UnregisterContextMenu(Action<GenericDropdownMenu, Vector2, object> callback)
        {
            if (contextMenuCallbacks.Remove(callback))
                foreach (var element in elements)
                    element.Q("button-menu").SetVisible(contextMenuCallbacks.Any());
        }

        void OnContextMenu(ContextualMenuPopulateEvent e)
        {
            var item = (CheckableItem)((VisualElement)e.target).userData;
            ShowContextMenu((VisualElement)e.target, e.mousePosition, item.item);
        }

        void OnMenu(ClickEvent e)
        {

            if (e.button != 0)
                return;

            var button = (VisualElement)e.target;
            var item = (CheckableItem)button.userData;

            var position = new Vector2(button.worldBound.x - button.worldBound.width, button.worldBound.y + button.worldBound.height);
            ShowContextMenu(button, position, item.item);

            e.StopPropagation();

        }

        void ShowContextMenu(VisualElement target, Vector2 position, object item)
        {
            var menu = new GenericDropdownMenu();

            foreach (var callback in contextMenuCallbacks)
                callback(menu, position, item);

            CloseAllContextMenus();
            menu.DropDown(new Rect(position, Vector2.zero), target, anchored: false);
        }

        void CloseAllContextMenus()
        {
            //Fixes issue where context menu might get stuck, and won't close
            panel.visualTree.Query(className: "unity-base-dropdown").ForEach(element => element.RemoveFromHierarchy());
        }

        public void UseCommonSceneImportContextMenu()
        {
            RegisterContextMenu((e, position, item) =>
            {

                var scenePath = (string)item;

                e.AddItem("View SceneAsset", isChecked: false, Ping);
                e.AddSeparator("");
                e.AddItem("Add to blacklist...", isChecked: false, () => ShowBlocklistMenu("blacklist", BlocklistUtility.blacklist, position));
                e.AddItem("Add to whitelist...", isChecked: false, () => ShowBlocklistMenu("whitelist", BlocklistUtility.whitelist, position));

                void Ping() =>
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath));

                void ShowBlocklistMenu(string menuName, Blocklist blocklist, Vector2 pos)
                {
                    var menu = new GenericDropdownMenu();
                    menu.contentContainer.RegisterCallback<BlurEvent>(e => CloseAllContextMenus());

                    menu.AddDisabledItem($"Add to {menuName}:", isChecked: false);
                    menu.AddSeparator("");

                    var zeroWidthSpace = "​";
                    var segments = scenePath.Split("/");
                    var paths = segments.Select((s, i) => $"{string.Join("/", segments.Take(i))}").Skip(2);

                    foreach (var path in paths.Distinct())
                        menu.AddItem("/" + path.Replace("/Assets", ""), isChecked: false, () => Add(path));

                    menu.AddItem($"/{scenePath.Replace("Assets/", "")}/{zeroWidthSpace}", isChecked: false, () => Add(scenePath));

                    CloseAllContextMenus();
                    menu.DropDown(new Rect(pos, Vector2.zero), list, anchored: false);

                    void Add(string path) =>
                        blocklist.Add(path);
                }

            });
        }

        #endregion
        #region Click

        void OnClick(ClickEvent e)
        {
            if (e.button != 0)
                return;

            var item = (CheckableItem)((VisualElement)e.target).userData;

            if (clickCallbacks.Any())
            {
                foreach (var callback in clickCallbacks)
                    callback(item.item);
            }
            else if (useCheckboxes)
            {
                ToggleItem(item);
            }
        }

        readonly List<EventCallback<object>> clickCallbacks = new();
        public void RegisterClickCallback(EventCallback<object> onClick)
        {
            clickCallbacks.Add(onClick);
        }

        public void UnregisterClickCallback(EventCallback<object> onClick)
        {
            clickCallbacks.Remove(onClick);
        }

        #endregion

    }

}

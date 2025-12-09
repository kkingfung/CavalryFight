using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    partial class DiagPopup
    {

        class ServicesPage : SubPage
        {

            public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.diag.services;
            public override string title => "Services";
            public override bool useScrollView => false;

            bool servicesFilterASM
            {
                get => sessionState.GetProperty(false);
                set => sessionState.SetProperty(value);
            }

            Toggle filterASMToggle;

            public override VisualElement CreateHeaderGUI()
            {
                var header = view.Q("header");
                filterASMToggle = header.Q<Toggle>("toggle-filter-asm");

                filterASMToggle.value = servicesFilterASM;
                filterASMToggle.RegisterValueChangedCallback(e =>
                {
                    servicesFilterASM = e.newValue;
                    RefreshList();
                });

                header.SetVisible(true);
                header.RemoveFromHierarchy();

                return header;
            }

            Column implementationColumn = null;
            MultiColumnListView list;
            List<ServiceEntry> allEntries;
            List<ServiceEntry> viewEntries = new();

            VisualElement progressSpinner;

            protected override void OnAdded()
            {

                progressSpinner = view.Q("progress-spinner");

                list = view.Q<MultiColumnListView>("list-services");
                list.makeNoneElement = () =>
                {
                    var label = new Label("No registered services found.");
                    label.style.marginLeft = label.style.marginRight = label.style.marginTop = label.style.marginBottom = 10;
                    label.style.unityTextAlign = TextAnchor.MiddleCenter;
                    return label;
                };

                // Source data
                var allServices = ServiceUtility.GetAll(); // IReadOnlyDictionary<Type, object>
                allEntries = allServices.Select(kvp => new ServiceEntry(kvp.Key, kvp.Value)).ToList();

                SetupColumns(out _, out implementationColumn);
                RefreshList();

                list.fixedItemHeight = 22;
                list.style.flexGrow = 1;
                list.style.minWidth = 300;
                implementationColumn.width = 300;

                void SetupColumns(out Column keyColumn, out Column implColumn)
                {
                    keyColumn = new Column()
                    {
                        title = "Key",
                        makeCell = () => MakeLabel(link: true),
                        bindCell = (e, i) =>
                        {
                            var entry = viewEntries[i];
                            var label = (Label)e;
                            label.text = entry.KeyTypeName;
                            label.tooltip = entry.KeyAssemblyName + "\n" + entry.KeyFullName;
                            label.userData = entry.KeyType;
                        },
                        sortable = true,
                        width = 200
                    };

                    implColumn = new Column()
                    {
                        title = "Implementation",
                        makeCell = () => MakeLabel(link: true),
                        bindCell = (e, i) =>
                        {
                            var entry = viewEntries[i];
                            var label = (Label)e;
                            label.text = entry.ImplTypeName;
                            label.tooltip = entry.ImplAssemblyName + "\n" + entry.ImplFullName;
                            label.userData = entry.ImplType;
                        },
                        sortable = true,
                        stretchable = true
                    };

                    list.columns.Add(keyColumn);
                    list.columns.Add(implColumn);

                    list.sortingMode = ColumnSortingMode.Custom;
                    list.columnSortingChanged += RefreshList;

                    list.sortColumnDescriptions.Add(new(0, SortDirection.Ascending));
                }

                static VisualElement MakeLabel(bool link)
                {
                    var label = new Label();

                    label.style.overflow = Overflow.Hidden;
                    label.style.whiteSpace = WhiteSpace.NoWrap;
                    label.style.textOverflow = TextOverflow.Ellipsis;
                    label.style.height = 22;

                    label.EnableInClassList("cursor-link", link);
                    label.EnableInClassList("label-link", link);

                    if (link)
                    {
                        label.RegisterCallback<ClickEvent>(e =>
                        {
                            if (e.button != 0) return;
                            if (label.userData is Type t)
                                t.OpenInCodeEditor();
                        });
                    }

                    return label;
                }
            }

            public override void OnAddAnimationComplete()
            {
                view.schedule.Execute(() => list.itemsSource = viewEntries);
                RefreshList();
            }

            void RefreshList()
            {
                progressSpinner.Show(fade: true);

                var sortInfo = list.sortedColumns.FirstOrDefault();
                var column = sortInfo?.column;
                var direction = sortInfo?.direction ?? SortDirection.Ascending;

                IEnumerable<ServiceEntry> filteredList = allEntries;

                if (!servicesFilterASM)
                    filteredList = filteredList.Where(e => !e.IsASM);

                if (column == implementationColumn)
                {
                    filteredList = (direction == SortDirection.Ascending
                        ? filteredList.OrderBy(e => e.ImplTypeName)
                        : filteredList.OrderByDescending(e => e.ImplTypeName));
                }
                else
                {
                    filteredList = (direction == SortDirection.Ascending
                        ? filteredList.OrderBy(e => e.KeyTypeName)
                        : filteredList.OrderByDescending(e => e.KeyTypeName));
                }

                // 🔑 Don't replace the list object — mutate it
                viewEntries.Clear();
                viewEntries.AddRange(filteredList);

                // Rebuild on next frame to avoid recursive layout
                view.schedule.Execute(() =>
                {
                    list.Rebuild();

                    view.schedule.Execute(() => progressSpinner.Hide(fade: true)).StartingIn(500);
                }).StartingIn(0);
            }

            sealed class ServiceEntry
            {
                public ServiceEntry(Type keyType, object instance)
                {
                    KeyType = keyType;
                    Instance = instance;
                }

                public Type KeyType { get; }
                public object Instance { get; }

                public string KeyTypeName => KeyType?.Name ?? "null";
                public string KeyFullName => KeyType?.FullName ?? "null";
                public string KeyAssemblyName => KeyType?.Assembly?.GetName().Name ?? "Unknown";

                public Type ImplType => Instance?.GetType();
                public string ImplTypeName => ImplType?.Name ?? "null";
                public string ImplFullName => ImplType?.FullName ?? "null";
                public string ImplAssemblyName => ImplType?.Assembly?.GetName().Name ?? "Unknown";

                public bool IsASM =>
                    (KeyAssemblyName?.StartsWith("AdvancedSceneManager") ?? false) ||
                    (ImplAssemblyName?.StartsWith("AdvancedSceneManager") ?? false) ||
                    (KeyType?.Namespace?.StartsWith("AdvancedSceneManager") ?? false) ||
                    (ImplType?.Namespace?.StartsWith("AdvancedSceneManager") ?? false);
            }

        }

    }

}

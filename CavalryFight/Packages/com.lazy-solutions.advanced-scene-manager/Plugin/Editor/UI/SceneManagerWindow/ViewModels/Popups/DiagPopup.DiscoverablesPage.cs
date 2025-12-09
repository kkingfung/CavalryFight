using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Utility.Discoverability;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    partial class DiagPopup
    {

        class DiscoverablesPage : SubPage
        {
            public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.diag.discoverables;
            public override string title => "Discoverables";
            public override bool useScrollView => false;

            bool discoverablesFilterASM
            {
                get => sessionState.GetProperty(false);
                set => sessionState.SetProperty(value);
            }

            Type discoverablesFilterType
            {
                get => sessionState.GetProperty<Type>(null);
                set => sessionState.SetProperty(value);
            }

            private Toggle filterASMToggle;
            private DropdownField filterType;

            public override VisualElement CreateHeaderGUI()
            {
                var header = view.Q("header");
                filterASMToggle = header.Q<Toggle>("toggle-filter-asm");
                filterType = header.Q<DropdownField>("dropdown-filter");

                filterASMToggle.value = discoverablesFilterASM;
                filterASMToggle.RegisterValueChangedCallback(e =>
                {
                    discoverablesFilterASM = e.newValue;
                    RefreshList();
                });

                var types = allDiscoverables.Select(d => d.attribute.GetType()).Distinct().ToList();
                var options = types.Select(t => t.Name).ToList();
                options.Insert(0, "All");

                filterType.choices = options;
                filterType.index = 0;

                filterType.RegisterValueChangedCallback(e =>
                {

                    if (e.newValue == "All")
                        discoverablesFilterType = null;
                    else
                        discoverablesFilterType = types[options.IndexOf(e.newValue) - 1];

                    RefreshList();

                });

                header.SetVisible(true);
                header.RemoveFromHierarchy();

                return header;
            }

            Column targetColumn = null;
            MultiColumnListView list;
            List<DiscoveredMember> allDiscoverables;
            List<DiscoveredMember> discoverables = new();

            VisualElement progressSpinner;

            public override void OnAddAnimationComplete()
            {
                RefreshList();
            }

            protected override void OnAdded()
            {

                progressSpinner = view.Q("progress-spinner");

                list = view.Q<MultiColumnListView>("list-discoverables");
                list.makeNoneElement = () =>
                {
                    var label = new Label("No discoverables found.");
                    label.style.marginLeft = label.style.marginRight = label.style.marginTop = label.style.marginBottom = 10;
                    label.style.unityTextAlign = TextAnchor.MiddleCenter;
                    return label;
                };

                allDiscoverables = DiscoverabilityUtility.GetMembers().ToList();

                SetupColumns(out _, out targetColumn);

                list.fixedItemHeight = 22;
                list.style.flexGrow = 1;
                list.style.minWidth = 300;
                targetColumn.width = 300;

                void SetupColumns(out Column attributeColumn, out Column targetColumn)
                {

                    attributeColumn = new Column()
                    {
                        title = "Attribute",
                        makeCell = () => MakeLabel(link: false),
                        bindCell = (e, i) =>
                        {
                            ((Label)e).text = GetAttributeName(discoverables[i]);
                            ((Label)e).tooltip = discoverables[i].attribute.friendlyDescription;
                        },
                        sortable = true,
                        width = 200,
                    };

                    targetColumn = new Column()
                    {
                        title = "Target",
                        makeCell = () => MakeLabel(link: true),
                        bindCell = (e, i) =>
                        {
                            ((Label)e).text = "<u>" + discoverables[i].ToString() + "</u>";
                            ((Label)e).tooltip = discoverables[i].ToString();
                            ((Label)e).userData = discoverables[i];
                        },
                        sortable = true,
                        stretchable = true
                    };

                    list.columns.Add(attributeColumn);
                    list.columns.Add(targetColumn);

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

                    label.RegisterCallback<ClickEvent>(e =>
                    {

                        if (e.button != 0)
                            return;

                        if (e.target is Label l && l.userData is DiscoveredMember discoverable)
                            discoverable.member.OpenInCodeEditor();

                    });

                    return label;

                }

            }

            void RefreshList()
            {

                progressSpinner.Show(fade: true);

                var sortInfo = list.sortedColumns.FirstOrDefault();

                var column = sortInfo?.column;
                var direction = sortInfo?.direction;

                IEnumerable<DiscoveredMember> filteredList;

                if (column == targetColumn)
                {
                    filteredList = (direction == SortDirection.Ascending
                        ? allDiscoverables.OrderBy(d => d.GetIdentifier())
                        : allDiscoverables.OrderByDescending(d => d.GetIdentifier()));

                }
                else
                {
                    filteredList = (direction == SortDirection.Ascending
                        ? allDiscoverables.OrderBy(GetAttributeName)
                        : allDiscoverables.OrderByDescending(GetAttributeName));
                }

                if (!discoverablesFilterASM)
                    filteredList = filteredList.Where(d => !GetAssembly(d).StartsWith("AdvancedSceneManager"));

                if (discoverablesFilterType is Type type)
                    filteredList = filteredList.Where(d => d.attribute.GetType() == type);

                discoverables = filteredList.ToList();
                view.schedule.Execute(() =>
                {
                    list.itemsSource = discoverables;

                    view.schedule.Execute(() => progressSpinner.Hide(fade: true)).StartingIn(500);
                }).StartingIn(0);

            }

            static string GetAttributeName(DiscoveredMember discoverable)
                => $"[{discoverable.attribute.GetType().Name.Replace("Attribute", "")}]";

            string GetAssembly(DiscoveredMember discoverable)
            {

                if (discoverable.member is Type t)
                    return t.Assembly.FullName;
                else if (discoverable.member.DeclaringType is Type t2)
                    return t2.Assembly.FullName;

                return "Unknown";

            }

        }

    }

}

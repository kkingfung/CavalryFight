using AdvancedSceneManager.Editor.UI;
using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor
{

    #region NavigateTo property drawer

    [Serializable]
    public struct ViewModelType
    {

        public string typeName;

        public readonly void Navigate(VisualElement element = null)
        {
            if (!string.IsNullOrEmpty(typeName) && Type.GetType(typeName, throwOnError: false) is Type type && type.IsViewModel())
            {
                if (element?.GetTopAncestor<PageStackView>() is PageStackView stack)
                    stack.Push(type);
                else if (type.IsSettingsPage())
                    ASMWindow.OpenSettings(type);
                else
                    ASMWindow.OpenPopup(type);
            }
        }

    }

    [CustomPropertyDrawer(typeof(ViewModelType))]
    class ViewModelTypeDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var typeProp = property.FindPropertyRelative("typeName");

            // All non-abstract subclasses of ViewModel
            var allViewModels = TypeCache.GetTypesDerivedFrom<ViewModel>()
                .Where(t => !t.IsAbstract)
                .ToList();

            // Group by declaring type (nested)
            var nestedGroups = allViewModels
                .Where(t => t.DeclaringType != null)
                .GroupBy(t => t.DeclaringType)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Catch-all group: top-level subclasses with no declaring type
            var rootGroup = allViewModels
                .Where(t => t.DeclaringType == null)
                .ToList();

            // Combine into dictionary: category = declaring type OR catch-all
            var viewKinds = new Dictionary<string, List<Type>>();
            foreach (var g in nestedGroups)
                viewKinds[g.Key.Name] = g.Value;
            if (rootGroup.Count > 0)
                viewKinds["ViewModel"] = rootGroup;

            // Sort categories: ViewModel first, then rest alphabetically
            var categoryNames = viewKinds.Keys
                .Where(k => k != "ViewModel")
                .OrderBy(k => k)
                .Prepend("ViewModel")
                .ToList();

            // Dropdowns
            var viewKindDropdown = new PopupField<string>("Category", categoryNames, 0);
            var viewDropdown = new PopupField<string>("Navigate To", new List<string>(), 0);

            var currentTypes = new List<Type>();

            void RefreshTypeDropdown(string category)
            {
                currentTypes = viewKinds[category]
                    .Cast<Type>()
                    .ToList();

                currentTypes.Insert(0, null);

                var options = currentTypes.Select(GetName).ToList();
                viewDropdown.choices = options;

                string currentValue = typeProp.stringValue;
                int selectedIndex = currentTypes.FindIndex(t => t?.AssemblyQualifiedName == currentValue);
                if (selectedIndex == -1) selectedIndex = 0;

                viewDropdown.SetValueWithoutNotify(options[selectedIndex]);
            }

            viewKindDropdown.RegisterValueChangedCallback(evt =>
            {
                RefreshTypeDropdown(evt.newValue);
            });

            viewDropdown.RegisterValueChangedCallback(evt =>
            {
                var selectedType = currentTypes[viewDropdown.index];
                typeProp.stringValue = selectedType?.AssemblyQualifiedName ?? string.Empty;
                property.serializedObject.ApplyModifiedProperties();
            });

            // Restore selection
            var currentValue = typeProp.stringValue;
            var currentType = !string.IsNullOrEmpty(currentValue)
                ? Type.GetType(currentValue)
                : null;

            string matchedCategory = "ViewModel";
            if (currentType != null)
            {
                if (currentType.DeclaringType != null && viewKinds.ContainsKey(currentType.DeclaringType.Name))
                    matchedCategory = currentType.DeclaringType.Name;
                else if (viewKinds.ContainsKey("ViewModel") && viewKinds["ViewModel"].Contains(currentType))
                    matchedCategory = "ViewModel";
            }

            viewKindDropdown.SetValueWithoutNotify(matchedCategory);
            RefreshTypeDropdown(matchedCategory);

            var container = new VisualElement();
            container.Add(viewKindDropdown);
            container.Add(viewDropdown);
            return container;
        }

        string GetName(Type type) => type?.Name ?? "None";
    }

    public class ViewModelTypeConverter : UxmlAttributeConverter<ViewModelType>
    {
        public override ViewModelType FromString(string value)
            => new() { typeName = XmlEscapeUtility.Unescape(value) };

        public override string ToString(ViewModelType value)
            => XmlEscapeUtility.Escape(value.typeName) ?? string.Empty;
    }

    #endregion

    [UxmlElement]
    public partial class NavigationButton : Button
    {

        [UxmlAttribute(name = "text")]
        public string labelText
        {
            get => textLabel.text;
            set => textLabel.text = value;
        }

        [UxmlAttribute]
        public string iconLeft
        {
            get => iconLeftLabel?.text ?? string.Empty;
            set => iconLeftLabel.text = value;
        }

        [UxmlAttribute]
        public string iconRight
        {
            get => iconRightLabel?.text ?? string.Empty;
            set => iconRightLabel.text = value;
        }

        private string m_iconLeftFontAwesomeClassName = "fontAwesome";
        [UxmlAttribute]
        public string iconLeftFontAwesomeClassName
        {
            get => m_iconLeftFontAwesomeClassName;
            set
            {
                iconLeftLabel?.RemoveFromClassList(m_iconLeftFontAwesomeClassName);
                m_iconLeftFontAwesomeClassName = value;
                iconLeftLabel?.AddToClassList(m_iconLeftFontAwesomeClassName);
            }
        }

        private string m_iconRightFontAwesomeClassName = "fontAwesome";
        [UxmlAttribute]
        public string iconRightFontAwesomeClassName
        {
            get => m_iconRightFontAwesomeClassName;
            set
            {
                iconRightLabel?.RemoveFromClassList(m_iconRightFontAwesomeClassName);
                m_iconRightFontAwesomeClassName = value;
                iconRightLabel?.AddToClassList(m_iconRightFontAwesomeClassName);
            }
        }

        [UxmlAttribute]
        public ViewModelType navigateTo { get; set; }

        readonly Label iconLeftLabel;
        readonly Label textLabel;
        readonly Label iconRightLabel;

        public NavigationButton(Action onClick) : this()
        {
            RegisterCallback<ClickEvent>((e) => onClick?.Invoke());
        }

        public NavigationButton()
        {
            style.flexDirection = FlexDirection.Row;
            style.marginBottom = 4;
            style.marginTop = 4;
            style.marginLeft = 4;
            style.marginRight = 4;
            style.paddingTop = 12;
            style.paddingBottom = 12;
            style.paddingLeft = 12;
            style.paddingRight = 12;
            style.justifyContent = Justify.Center;
            style.borderBottomLeftRadius = 8;
            style.borderBottomRightRadius = 8;
            style.borderTopLeftRadius = 8;
            style.borderTopRightRadius = 8;
            style.height = 42;

            Add(CreateIcon(out iconLeftLabel, iconLeftFontAwesomeClassName));
            Add(CreateLabel(out textLabel));
            Add(CreateArrow(out iconRightLabel, iconRightFontAwesomeClassName));

            iconLeft = "";
            iconRight = "\uf061";

            RegisterCallback<ClickEvent>(e => navigateTo.Navigate(this));
        }

        VisualElement CreateIcon(out Label label, string fontAwesomeClassName)
        {
            var iconContainer = new VisualElement
            {
                name = "icon-container",
                pickingMode = PickingMode.Ignore
            };

            iconContainer.style.flexGrow = 0;
            iconContainer.style.backgroundColor = new StyleColor(new Color(0, 0, 0, 0));
            iconContainer.style.justifyContent = Justify.Center;
            iconContainer.style.alignItems = Align.Center;
            iconContainer.style.width = 24;

            label = new Label(iconLeft)
            {
                name = "icon",
                pickingMode = PickingMode.Ignore,
                tabIndex = -1,
                displayTooltipWhenElided = false,
                parseEscapeSequences = true,
            };
            label.AddToClassList(fontAwesomeClassName);

            label.style.marginBottom = 0;
            label.style.marginTop = 0;
            label.style.marginLeft = 0;
            label.style.marginRight = 0;
            label.style.paddingBottom = 0;
            label.style.paddingTop = 0;
            label.style.paddingLeft = 0;
            label.style.paddingRight = 0;

            iconContainer.Add(label);
            return iconContainer;
        }

        Label CreateLabel(out Label label)
        {
            label = new Label(text)
            {
                pickingMode = PickingMode.Ignore,
                tabIndex = -1,
                displayTooltipWhenElided = true
            };

            label.style.flexGrow = 1;
            label.style.fontSize = 15;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;

            return label;
        }

        VisualElement CreateArrow(out Label label, string fontAwesomeClassName)
        {
            label = new Label(iconRight)
            {
                name = "arrow-right",
                pickingMode = PickingMode.Ignore,
                tabIndex = -1,
                displayTooltipWhenElided = false,
                parseEscapeSequences = true,
            };

            label.AddToClassList(fontAwesomeClassName);

            label.style.marginBottom = 0;
            label.style.marginTop = 0;
            label.style.marginLeft = 0;
            label.style.marginRight = 0;

            return label;
        }

    }

}

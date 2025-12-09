#if UNITY_EDITOR

using AdvancedSceneManager.Utility.Discoverability;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    /// <summary>Specifies location for a custom element in the ASM window.</summary>
    public enum ElementLocation
    {
        /// <summary>Specifies that the custom element should be located in the header of the ASM window.</summary>
        Header = 0,
        /// <summary>Specifies that the custom element should be located in the footer of the ASM window.</summary>
        Footer = 1,
        /// <summary>Specifies that the custom element should be located in the collection headers the ASM window, on the right side.</summary>
        Collection = 2,
        /// <summary>Specifies that the custom element should be located in the scene fields of the ASM window, on the right side.</summary>
        Scene = 3,

        /// <summary>Specifies that the custom element should be located in the collection fields of the ASM window, on the left side.</summary>
        CollectionLeft = 4,

        /// <summary>Specifies that the custom element should be located in the scene fields of the ASM window, on the left side.</summary>
        SceneLeft = 5,

        /// <summary>Specifies that the custom element should be considered a settings page. It will be accessible as a category in the main settings page.</summary>
        Settings = 6,

        /// <inheritdoc cref="Collection"/>
        CollectionRight = Collection,

        /// <inheritdoc cref="Scene"/>
        SceneRight = Scene,

    }

    /// <summary>Specifies a method or view model class that should be used as a callback to insert a visual element into the ASM window.</summary>
    /// <remarks>When specified on a class it should inherit <see cref="ViewModel"/>.</remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class ASMWindowElementAttribute : DiscoverableAttribute
    {

        /// <summary>Gets if this element is hosted within the settings page.</summary>
        public static bool IsHostedWithinSettingsPage(VisualElement element) =>
            element.ClassListContains("settings");

        /// <summary>Gets if this element is hosted within the standalone collection.</summary>
        public static bool IsStandalone(VisualElement element) =>
            element.ClassListContains("standalone");

        /// <summary>Gets if this element is hosted within the default ASM scenes collection.</summary>
        public static bool IsDefaultASMScene(VisualElement element) =>
            element.ClassListContains("asm");

        /// <summary>Gets the location of this element.</summary>
        public ElementLocation location { get; }

        /// <summary>Gets if this element is visible by default.</summary>
        public bool isVisibleByDefault { get; }

        /// <summary>Gets if this element can be toggled visible or hidden.</summary>
        public bool canToggleVisible { get; } = true;

        /// <summary>A name to distinguish this from other attributes on the same method.</summary>
        public string name { get; }

        /// <summary>Specifies default order.</summary>
        public int defaultOrder { get; }

        /// <summary>Defines a new ASM window element.</summary>
        /// <param name="location">Specifies the location of this element.</param>
        /// <param name="isVisibleByDefault">Specifies whatever this element should be visible by default. Ignored for location: Settings.</param>
        /// <param name="canToggleVisible">Specifies if this element can be toggled visible or hidden. Ignored for location: Settings.</param>
        /// <param name="name">Specifies the name to use in case attribute is used mulitple times for a singular callback.</param>
        /// <param name="defaultOrder">Specifies the default order.</param>
        /// <remarks><i>Note</i>: <see cref="IsHostedWithinSettingsPage(VisualElement)"/> can be used to determine if element is constructed to be displayed in the appearance settings.</remarks>
        public ASMWindowElementAttribute(ElementLocation location, bool isVisibleByDefault = false, bool canToggleVisible = true, string name = null, int defaultOrder = int.MaxValue)
        {
            this.location = location;
            this.isVisibleByDefault = isVisibleByDefault;
            this.canToggleVisible = canToggleVisible;
            this.name = name;
            this.defaultOrder = defaultOrder;
        }

        /// <inheritdoc />
        public override bool IsValidTarget(MemberInfo member)
        {

            var isType = member.IsType();
            var isMethod = member.IsMethod();
            var correctType = isType ? member.IsType<ViewModel>() : member.IsMethodAndReturns<VisualElement>();

            // Disallow methods for settings pages
            if (location is ElementLocation.Settings && isMethod)
            {
                LogError(member, $"{ElementLocation.Settings} is not a valid target for method-based callbacks.");
                return false;
            }

            if (isType && !correctType)
            {
                LogError(member, "Type must inherit from 'AdvancedSceneManager.Editor.UI.ViewModel'.");
                return false;
            }

            if (isMethod && !correctType)
            {
                LogError(member, "Method callback must return VisualElement.");
                return false;
            }

            return correctType;

        }

        /// <inheritdoc />
        public override string friendlyDescription =>
            "Specifies a method or view model class that should be used as a callback to insert a visual element into the ASM window.";

    }

    /// <summary>Provides utility methods for working with <see cref="VisualElement"/>.</summary>
    public static class UIElementUtility
    {

        /// <summary>Applies font awesome <i>free</i> to the <see cref="VisualElement"/>.</summary>
        /// <remarks>Note that not all icons are available (no clue why, ask unity).</remarks>
        /// <param name="button">The button to apply font awesome to.</param>
        /// <param name="solid">Applies solid style. Default.</param>
        /// <param name="regular">Applies regular style.</param>
        /// <param name="brands">Applies brands style.</param>
        public static void UseFontAwesome(this VisualElement button, bool? solid = null, bool? regular = null, bool? brands = null)
        {

            var count = new[] { regular, solid, brands }.Count(b => b.HasValue);
            if (count == 0)
                solid = true;
            else if (count > 1)
                throw new ArgumentException("Only one flag may be set.");

            button.EnableInClassList("fontAwesome", solid is true);
            button.EnableInClassList("fontAwesomeRegular", regular is true);
            button.EnableInClassList("fontAwesomeBrands", brands is true);

        }

    }

}
#endif

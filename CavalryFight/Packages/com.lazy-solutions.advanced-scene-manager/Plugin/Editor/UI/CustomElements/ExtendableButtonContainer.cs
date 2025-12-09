using AdvancedSceneManager.Editor.UI;
using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Editor.UI.Views.Popups;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility.Discoverability;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor
{

    [UxmlElement]
    public partial class ExtendableButtonContainer : VisualElement
    {

        [UxmlAttribute]
        public ElementLocation location { get; set; }

        [UxmlAttribute]
        public bool autoInitialize { get; set; }

        [UxmlAttribute]
        public bool isSettingsPage { get; set; }

        public static Scene lastScene { get; private set; }
        public static SceneCollection lastCollection { get; private set; }

        readonly static List<ExtendableButtonContainer> containers = new();

        #region Static

        public record Callback(string name, Func<ElementLocation, ViewModelContext, VisualElement> elementCallback, ASMWindowElementAttribute attribute);

        static readonly Dictionary<ElementLocation, List<Callback>> elements = new()
        {
            { ElementLocation.Header, new List<Callback>() },
            { ElementLocation.CollectionLeft, new List<Callback>() },
            { ElementLocation.CollectionRight, new List<Callback>() },
            { ElementLocation.SceneLeft, new List<Callback>() },
            { ElementLocation.SceneRight, new List<Callback>() },
            { ElementLocation.Footer, new List<Callback>() },
        };

        protected static void GetData(Callback callback, out int index, out bool isVisible)
        {
            var data = SceneManager.settings.user.m_extendableButtons.FirstOrDefault(b => b.location == callback.attribute.location && b.name == callback.name);
            index = data?.index ?? callback.attribute.defaultOrder;
            isVisible = data?.isVisible ?? callback.attribute.isVisibleByDefault;
        }

        protected static void SetData(Callback callback, int? newIndex = null, bool? newIsVisible = null, bool save = true)
        {
            var data = SceneManager.settings.user.m_extendableButtons.FirstOrDefault(b => b.location == callback.attribute.location && b.name == callback.name);
            if (data is null)
            {
                data = new() { name = callback.name, location = callback.attribute.location, index = newIndex ?? -1, isVisible = newIsVisible ?? callback.attribute.isVisibleByDefault };
                SceneManager.settings.user.m_extendableButtons.Add(data);
            }
            else
            {
                data.index = newIndex ?? data.index;
                data.isVisible = newIsVisible ?? data.isVisible;
            }

            if (save)
                SceneManager.settings.user.Save();
        }

        public static IEnumerable<Callback> Enumerate(ElementLocation location) =>
            elements[location].OrderBy(callback =>
            {
                GetData(callback, out var index, out var isVisible);
                return index;
            });

        [DiscoverabilityCacheInvalidated]
        static void InitializeExtendableButtons()
        {
            ReloadCache();
            ResetAll();
        }

        static void ReloadCache()
        {

            elements[ElementLocation.Header].Clear();
            elements[ElementLocation.CollectionLeft].Clear();
            elements[ElementLocation.CollectionRight].Clear();
            elements[ElementLocation.SceneLeft].Clear();
            elements[ElementLocation.SceneRight].Clear();
            elements[ElementLocation.Footer].Clear();

            var callbacks = DiscoverabilityUtility.GetMembers<ASMWindowElementAttribute>()
                .OfType<ASMWindowElementAttribute, MemberInfo>()
                .Distinct()
                .ToList();

            foreach (var (attribute, member) in callbacks)
            {

                if (attribute.location == ElementLocation.Settings)
                    continue;

                var name = $"{attribute.location}+{attribute.name}:{(member is Type t ? t.FullName : member.DeclaringType.FullName)}+{member.Name}";
                elements[attribute.location].Add(new Callback(name, Invoke, attribute));

                VisualElement Invoke(ElementLocation location, ViewModelContext context)
                {
                    if (member is MethodInfo method)
                        return InvokeMethod(method, location, context);
                    else if (member is Type type && typeof(ViewModel).IsAssignableFrom(type))
                        return InvokeViewModel(type, location, context);

                    return null;
                }

                VisualElement InvokeMethod(MethodInfo method, ElementLocation location, ViewModelContext context)
                {
                    // Get the method info
                    var parameters = method.GetParameters();

                    // Prepare arguments based on the parameter types
                    var args = new object[parameters.Length];

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var param = parameters[i];

                        // Check the parameter type and set the appropriate argument
                        if (param.ParameterType == typeof(ViewModelContext))
                        {
                            args[i] = context;
                        }
                        else if (param.ParameterType == typeof(ElementLocation))
                        {
                            args[i] = location;
                        }
                        else
                        {
                            throw new InvalidOperationException($"Unsupported parameter type: {param.ParameterType}");
                        }
                    }

                    // Invoke the method with the prepared arguments
                    return (VisualElement)method.Invoke(null, args);
                }

                VisualElement InvokeViewModel(Type type, ElementLocation location, ViewModelContext context)
                {

                    if (!ViewModel.Instantiate(type, out var viewModel, out var view))
                        return null;

                    view.RegisterCallback<AttachToPanelEvent>(e => viewModel?.Add(view, context));
                    view.RegisterCallback<DetachFromPanelEvent>(e => viewModel?.Remove());

                    return view;

                }

            }

        }

        protected static void ResetAll()
        {
            foreach (var container in containers)
                container.Reset();
        }

        protected static void ResetAll(ElementLocation location)
        {
            var containersToReset = containers.Where(c => c.location == location);
            foreach (var container in containersToReset)
                container.Reset();
        }

        #endregion

        public ViewModelContext context { get; private set; }

        public bool isReversed => style.flexDirection == FlexDirection.RowReverse || style.flexDirection == FlexDirection.ColumnReverse;

        public ExtendableButtonContainer()
        {

            RegisterCallbackOnce<AttachToPanelEvent>(e =>
            {

                containers.Add(this);

                ContextMenu(this);

                if (autoInitialize) //Auto initialize
                    Reset();

            });

            RegisterCallbackOnce<DetachFromPanelEvent>(e => containers.Remove(this));

        }

        public void Initialize(ViewModelContext context)
        {
            this.context = context;
            Reset();
        }

        protected virtual void Reset()
        {

            Clear();

            var callbacks = Enumerate(location).Where(IsElementVisible);
            if (isReversed)
                callbacks = callbacks.Reverse();

            foreach (var callback in callbacks.ToList())
            {
                try
                {
                    var element = callback.elementCallback?.Invoke(location, context);
                    if (element is not null)
                    {

                        element.AddToClassList("extendable-element");

                        if (context.baseCollection is StandaloneCollection)
                            element.AddToClassList("standalone");

                        if (context.baseCollection is DefaultASMScenesCollection)
                            element.AddToClassList("asm");

                        if (isSettingsPage)
                            element.AddToClassList("settings");

                        element.viewDataKey = callback.name;
                        element.userData = callback;
                        Add(Wrap(element, callback));

                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

        }

        protected virtual bool IsElementVisible(Callback callback)
        {

            //Dynamic collection is not a valid target
            if (location is ElementLocation.CollectionLeft or ElementLocation.CollectionRight && context.collection is null)
                return false;

            if (!callback.attribute.canToggleVisible)
                return true;

            GetData(callback, out _, out var isVisible);
            return isVisible;

        }

        protected virtual VisualElement Wrap(VisualElement element, Callback callback)
        {

            element.RegisterCallback<PointerDownEvent>(e =>
            {
                lastCollection = context.collection as SceneCollection;
                lastScene = context.scene;
            }, TrickleDown.TrickleDown);

            ContextMenu(element, callback);

            return element;

        }

        #region Context menu

        void ContextMenu(VisualElement element, Callback callback = null)
        {
            if (GetType() != typeof(ExtendableButtonContainer))
                return;

            if (location is (ElementLocation.SceneLeft or ElementLocation.SceneRight or ElementLocation.CollectionLeft or ElementLocation.CollectionRight))
                return;

            var list = SceneManager.settings.user.m_extendableButtons;

            element.ContextMenu(e =>
            {

                e.menu.AppendAction("Customize...", OpenSettings);

                if (callback is not null)
                {
                    e.menu.AppendSeparator("");
                    e.menu.AppendAction("Hide", HideButton);
                }

            });

            void OpenSettings(DropdownMenuAction e) =>
                ASMWindow.OpenSettings<SettingsPopup.ExtendableUIPage>();

            void HideButton(DropdownMenuAction e)
            {
                var item = list.FirstOrDefault(b => b.isVisible && b.location == location && b.name == callback.name);
                item.isVisible = false;
                SceneManager.settings.user.Save();
                Reset();
            }

        }

        #endregion

    }

}

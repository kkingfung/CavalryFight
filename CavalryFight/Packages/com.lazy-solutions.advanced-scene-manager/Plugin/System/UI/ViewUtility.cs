#if UNITY_EDITOR

using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Utility;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    static class ViewUtility
    {

        /// <summary>Clones the visual tree asset without the <see cref="TemplateContainer"/>.</summary>
        public static VisualElement Clone(this VisualTreeAsset template)
        {
            var root = template.CloneTree();
            if (root is not null && root.childCount == 1)
                return root[0];

            if (root is not null && root.childCount > 1)
            {
                var wrapper = new VisualElement();
                foreach (var child in root.Children().ToList())
                    wrapper.Add(child);
                return wrapper;
            }

            return root!;
        }

        /// <summary>Hides the element using <see cref="DisplayStyle.None"/>.</summary>
        /// <remarks>Supports fade animation.</remarks>
        public static void Hide(this VisualElement element, bool fade = false)
        {

            if (!fade)
            {
                element.style.display = DisplayStyle.None;
                return;
            }

            element.AddToClassList("fade-out");

            element.RegisterCallbackOnce<TransitionEndEvent>(e =>
            {
                if (e.AffectsProperty("opacity"))
                {
                    element.RemoveFromClassList("fade-out");
                    element.style.display = DisplayStyle.None;
                }
            });

        }

        /// <summary>Shows the element using <see cref="DisplayStyle.Flex"/>.</summary>
        /// <remarks>Supports fade animation.</remarks>
        public static void Show(this VisualElement element, bool fade = false)
        {

            element.style.display = DisplayStyle.Flex;

            if (!fade)
                return;

            element.AddToClassList("fade-in");

            element.RegisterCallbackOnce<TransitionEndEvent>(e =>
            {
                if (e.AffectsProperty("opacity"))
                    element.RemoveFromClassList("fade-in");
            });

        }

        public static void SetVisible(this VisualElement element, bool visible) =>
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        public static bool IsVisible(this VisualElement element) =>
            element.style.display == DisplayStyle.Flex;

        [HideInCallstack]
        public static void InvokeView(this VisualElement element, Action action, string name)
        {
            try
            {
                action.Invoke();
            }
            catch (Exception ex)
            {

                if (element.userData is HelpBox errorElement)
                    element.userData = ExceptionBox(ex, element.style.flexGrow, element.style.flexShrink, $"Could not invoke {GetMethodName(action)} for '{name}':", errorElement);
                else
                {
                    element.Clear();
                    element.userData = errorElement = ExceptionBox(ex, element.style.flexGrow, element.style.flexShrink, $"Could not invoke {GetMethodName(action)} for '{name}':");
                    errorElement.RemoveFromHierarchy();
                    element.Add(errorElement);
                }

                Log.Exception(ex);

            }
        }

        [HideInCallstack]
        public static T InvokeView<T>(this VisualElement element, Func<T> action, string name)
        {
            try
            {
                return action.Invoke();
            }
            catch (Exception ex)
            {

                if (element.userData is HelpBox errorElement)
                    element.userData = ExceptionBox(ex, element.style.flexGrow, element.style.flexShrink, $"Could not invoke {GetMethodName(action)} for '{name}':", errorElement);
                else
                {
                    element.Clear();
                    element.userData = errorElement = ExceptionBox(ex, element.style.flexGrow, element.style.flexShrink, $"Could not invoke {GetMethodName(action)} for '{name}':");
                    errorElement.RemoveFromHierarchy();
                    element.Add(errorElement);
                }

                Log.Exception(ex);
                return default!;

            }
        }

        [HideInCallstack]
        public static void InvokeView<T>(this T viewModel, Action action) where T : ViewModel
        {
            try
            {
                action.Invoke();
            }
            catch (Exception ex)
            {

                if (viewModel?.view is VisualElement element)
                {

                    if (element.userData is HelpBox errorElement)
                        element.userData = ExceptionBox(ex, element.style.flexGrow, element.style.flexShrink, $"Could not invoke {GetMethodName(action)} for '{viewModel.GetType().Name}':", errorElement);
                    else
                    {
                        element.Hide();
                        element.userData = errorElement = ExceptionBox(ex, element.style.flexGrow, element.style.flexShrink, $"Could not invoke {GetMethodName(action)} for '{viewModel.GetType().Name}':");
                        errorElement.RemoveFromHierarchy();
                        var index = element.parent.IndexOf(element);
                        element.parent.Insert(index, errorElement);
                    }

                }

                Log.Exception(ex);

            }
        }

        [HideInCallstack]
        public static T2 InvokeView<T, T2>(this T viewModel, Func<T2> action) where T : ViewModel
        {
            try
            {
                return action.Invoke();
            }
            catch (Exception ex)
            {

                if (viewModel?.view is VisualElement element)
                {

                    if (element.userData is HelpBox errorElement)
                        element.userData = ExceptionBox(ex, element.style.flexGrow, element.style.flexShrink, $"Could not invoke {GetMethodName(action)} for '{viewModel.GetType().Name}':", errorElement);
                    else
                    {
                        element.Hide();
                        element.userData = errorElement = ExceptionBox(ex, element.style.flexGrow, element.style.flexShrink, $"Could not invoke {GetMethodName(action)} for '{viewModel.GetType().Name}':");
                        errorElement.RemoveFromHierarchy();
                        var index = element.parent.IndexOf(element);
                        element.parent.Insert(index, errorElement);
                    }

                }

                Log.Exception(ex);
                return default!;

            }
        }

        [HideInCallstack]
        public static TemplateContainer Instantiate(VisualTreeAsset view)
        {

            try
            {
                if (!view)
                    throw new NullReferenceException("Could not instantiate the view, as it could not be found. Please try triggering a recompile, restart unity, re-import or re-install of ASM.");

                var template = view.Instantiate();
                return template;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                var template = new TemplateContainer();
                template.Add(ExceptionBox(ex, 0, 0));
                return template;
            }

        }

        public static HelpBox ExceptionBox(Exception ex, StyleFloat flexGrow, StyleFloat flexShrink, string header = null, HelpBox existingBox = null)
        {

            var box = existingBox ??= new HelpBox() { name = "Error", messageType = HelpBoxMessageType.Error };

            box.text = header + "\n" + ex.Message;

            box.style.flexGrow = flexGrow;
            box.style.flexShrink = flexShrink;
            box.style.marginLeft = 6;
            box.style.marginTop = 6;
            box.style.marginRight = 6;
            box.style.marginBottom = 6;

            box.AddToClassList("cursor-link");

            box.Children().ForEach(e => e.pickingMode = PickingMode.Ignore);

            if (box.userData is EventCallback<ClickEvent> callback)
                box.UnregisterCallback(callback);

            box.userData = callback = new(e => ex.OpenInCodeEditor());
            box.RegisterCallback(callback);

            return box;

        }

        static string GetMethodName(Delegate del) =>
             del.Method.Name;

    }

}
#endif

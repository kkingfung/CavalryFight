using AdvancedSceneManager.Callbacks.Events;
using AdvancedSceneManager.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    partial class DiagPopup
    {

        class Coroutines : SubPage
        {

            public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.diag.coroutines;
            public override string title => "Coroutines";
            public override bool useScrollView => false;

            protected override void OnAdded()
            {
                Reload();
                RegisterEvent<GlobalCoroutinesChanged>(e => EditorApplication.delayCall += Reload);
            }

            protected override void OnRemoved()
            {
                items.Clear();
            }

            readonly Queue<GlobalCoroutine> items = new();

            void Reload()
            {
                var list = view.Q<ScrollView>();
                list.Clear();

                // Add any new coroutines
                var newItems = CoroutineUtility.coroutines.Where(c => !items.Contains(c) && !string.IsNullOrEmpty(c.description)).ToList();
                foreach (var item in newItems)
                {
                    if (items.Count == 100)
                        items.Dequeue(); // remove oldest
                    items.Enqueue(item); // add newest
                }

                // Render items (newest last in this case)
                foreach (var item in items)
                {
                    var label = new Label
                    {
                        tooltip = GetFriendlyMethodName(item.caller.method)
                    };

                    if (item.wasCancelled)
                        label.text = "<color=#888888>[Cancelled]</color>\t ";
                    else if (item.isComplete)
                        label.text = "<color=#888888>[Complete]</color>\t ";
                    else if (item.isPaused)
                        label.text = "<color=#888888>[Paused]</color>\t ";
                    else if (item.isRunning)
                        label.text = "<color=#888888>[Running]</color>\t ";

                    label.text += item.description;

                    label.AddToClassList("cursor-link");
                    label.AddToClassList("label-link");

                    label.RegisterCallback<ClickEvent>(e => item.ViewCallerInCodeEditor());

                    list.Add(label);
                }

                // Fallback
                if (list.childCount == 0)
                {
                    var label = new Label("No coroutines active");
                    label.userData = "no-items";
                    label.style.unityFontStyleAndWeight = FontStyle.Italic;
                    label.style.opacity = 0.75f;
                    list.Add(label);
                }
            }

            static string GetFriendlyName(Type type)
            {
                if (type.IsGenericType)
                {
                    var name = type.Name;
                    var tickIndex = name.IndexOf('`');
                    if (tickIndex > 0)
                        name = name[..tickIndex];

                    var args = type.GetGenericArguments()
                        .Select(GetFriendlyName);
                    return $"{name}<{string.Join(", ", args)}>";
                }

                if (type.IsNested)
                    return $"{GetFriendlyName(type.DeclaringType)}+{type.Name}";

                return type.Name;
            }

            static string GetFriendlyMethodName(MethodBase method)
            {
                var typeName = method.DeclaringType != null
                    ? $"{method.DeclaringType.Namespace}.{GetFriendlyName(method.DeclaringType)}"
                    : "";

                return $"{typeName}+{method.Name}()";
            }

        }

    }

}

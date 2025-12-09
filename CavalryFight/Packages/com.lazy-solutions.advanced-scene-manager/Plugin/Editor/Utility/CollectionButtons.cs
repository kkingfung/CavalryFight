using AdvancedSceneManager.Callbacks.Events;
using AdvancedSceneManager.Editor.UI;
using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Utility;
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static AdvancedSceneManager.Editor.Utility.SceneButtons;

#if !COROUTINES
using AdvancedSceneManager.Utility.Services;
#endif

namespace AdvancedSceneManager.Editor.Utility
{

    static class CollectionButtons
    {

        [ASMWindowElement(ElementLocation.CollectionLeft, defaultOrder: int.MinValue + 100)]
        static VisualElement IncludeToggle(ViewModelContext context)
        {

            var toggle = Toggle(context,
                tooltip: "Specifies whatever this collection will be included in build");

            toggle.bindingPath = "m_isIncluded";
            if (context.collection)
                toggle.Bind(new SerializedObject(context.collection));
            else
                toggle.Unbind();

            return toggle;

        }

        [ASMWindowElement(ElementLocation.CollectionLeft, isVisibleByDefault: true, defaultOrder: int.MinValue + 100 + 1)]
        static VisualElement Play(ViewModelContext context) =>
            Button(context,
                text: "",
                tooltip: "Open collection in play mode (hold shift to force open all scenes)",
                onClick: (shift) => Play(shift, context));

        [ASMWindowElement(ElementLocation.CollectionLeft, isVisibleByDefault: true)]
        static VisualElement OpenNonAdditive(ViewModelContext context) =>
            Button(context,
                   text: "",
                   tooltip: "Open collection (hold shift to force open all scenes)",
                   onPropertyChanged: (button) =>
                   {
#if !COROUTINES
                       button.SetEnabled(false);
#endif
                   },
                   onClick: async (shift) => await OpenNonAdditive(shift, context));

        [ASMWindowElement(ElementLocation.CollectionLeft, isVisibleByDefault: true)]
        static VisualElement OpenAdditive(ViewModelContext context) =>
            Button(context,
                  text: "",
                  tooltip: "Open collection additively (hold shift to force open all scenes)",
                  onPropertyChanged: (button) =>
                  {
                      button.text = context.collection && context.collection.isOpen ? "" : "";
#if !COROUTINES
                      button.SetEnabled(false);
#endif
                  },
                  onClick: (shift) => OpenAdditive(shift, context));

        static Button Button(ViewModelContext context, string text = null, string tooltip = null, bool useFontAwesome = true, Action<Button> onPropertyChanged = null, Action<bool> onClick = null)
        {
            var element = SceneButtons.Button(context, text, tooltip, useFontAwesome, onPropertyChanged, null);
            Setup(element, onClick);
            return element;
        }

        static Toggle Toggle(ViewModelContext context, string tooltip = null, Action<Toggle> onPropertyChanged = null)
        {
            var element = Element(context, null, tooltip, false, onPropertyChanged);
            return element;
        }

        static void Setup(Button button, Action<bool> action)
        {

            button.clickable = new(() => { });
            button.clickable.activators.Add(new() { modifiers = EventModifiers.Shift });
            button.clickable.clickedWithEventInfo += (e) =>
            {
#if COROUTINES
                action.Invoke(e.IsShiftKeyHeld());
#else
                ServiceUtility.Get<UI.Notifications.EditorCoroutinesNotification>()?.Show();
#endif
            };

#if !COROUTINES
            button.tooltip = "Editor coroutines is needed to use this feature.";
#endif

        }

        static void Play(bool openAll, ViewModelContext context)
        {
            if (context.collection)
                SceneManager.app.Restart(new() { openCollection = context.collection, forceOpenAllScenesOnCollection = openAll });
        }

        static async Awaitable OpenNonAdditive(bool openAll, ViewModelContext context)
        {
            if (context.collection)
            {
                await SceneManager.runtime.Open(context.collection, openAll).CloseAll().RegisterCallback<SceneClosePhaseEvent>(e =>
                     {
                         var unimportedScenes = SceneUtility.GetAllOpenUnityScenes().Except(FallbackSceneUtility.GetScene()).Except(context.collection.effectiveLoadingScene).ToList(); //Only unimported scenes should be left

                         e.WaitFor(async () =>
                         {
                             foreach (var scene in unimportedScenes)
                             {
                                 if (Application.isPlaying)
                                     await UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
                                 else
                                     UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, removeScene: true);
                             }
                         });
                     }, When.After);
            }
        }

        static void OpenAdditive(bool openAll, ViewModelContext context)
        {

            if (!context.collection)
                return;

            if (context.collection.isOpen)
                SceneManager.runtime.Close(context.collection);
            else
                SceneManager.runtime.OpenAdditive(context.collection, openAll);

        }

    }

}

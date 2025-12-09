using AdvancedSceneManager.Core;
using AdvancedSceneManager.Editor.UI;
using AdvancedSceneManager.Models;
using System;
using System.ComponentModel;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using LoadSceneMode = UnityEngine.SceneManagement.LoadSceneMode;
using OpenSceneMode = UnityEditor.SceneManagement.OpenSceneMode;
using Scene = AdvancedSceneManager.Models.Scene;

#if COROUTINES
using AdvancedSceneManager.Callbacks.Events;
using AdvancedSceneManager.Utility;
#else
using AdvancedSceneManager.Utility.Services;
#endif

namespace AdvancedSceneManager.Editor.Utility
{

    static class SceneButtons
    {

        [ASMWindowElement(ElementLocation.SceneLeft, isVisibleByDefault: true)]
        static VisualElement OpenNonAdditive(ViewModelContext context)
        {
            Button button = null;
            button = Button(context,
                   text: "",
                   tooltip: "Open scene",
                   onClick: () => OpenScene(additive: false, context, button));

            return button;
        }

        [ASMWindowElement(ElementLocation.SceneLeft, isVisibleByDefault: true)]
        static VisualElement OpenAdditive(ViewModelContext context)
        {
            Button button = null;
            button = Button(context,
                  text: "",
                  tooltip: "Open scene additively",
                  onPropertyChanged: (button) => button.text = IsOpen(context) ? "" : "",
                  onClick: () => OpenScene(additive: true, context, button));

            return button;
        }

        static bool IsOpen(ViewModelContext context)
        {
            if (TryGetScene(context, out var scene))
            {
                return scene.isOpenInHierarchy;
            }
            else if (TryGetSceneAsset(context, out var path))
            {
                var uscene = Application.isPlaying
                    ? UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path)
                    : UnityEditor.SceneManagement.EditorSceneManager.GetSceneByPath(path);

                return uscene.isLoaded;
            }

            return false;
        }

        static async void OpenScene(bool additive, ViewModelContext context, Button button)
        {
#if COROUTINES

            if (TryGetScene(context, out var scene))
            {

                var operation = additive
                    ? SceneManager.runtime.ToggleOpen(scene).With(context.collection)
                    : SceneManager.runtime.CloseAll(exceptLoadingScreens: false, exceptUnimported: false).Open(scene).With(context.collection).RegisterCallback<SceneClosePhaseEvent>(e =>
                    {
                        var unimportedScenes = SceneUtility.GetAllOpenUnityScenes().Except(FallbackSceneUtility.GetScene()).ToList(); //Only unimported scenes should be left

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

                if (ASMWindowElementAttribute.IsStandalone(button))
                    _ = operation.IsStandaloneScene();
                else if (ASMWindowElementAttribute.IsDefaultASMScene(button))
                    _ = operation.IsDefaultASMScene();

            }
            else if (TryGetSceneAsset(context, out var path))
            {

                FallbackSceneUtility.EnsureOpen();

                var uscene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);

                if (uscene.isLoaded)
                {
                    if (Application.isPlaying)
                        await UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(uscene);
                    else
                        UnityEditor.SceneManagement.EditorSceneManager.CloseScene(uscene, removeScene: true);
                }
                else
                {
                    if (Application.isPlaying)
                        await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(path, additive ? LoadSceneMode.Additive : LoadSceneMode.Single);
                    else
                        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, additive ? OpenSceneMode.Additive : OpenSceneMode.Single);
                }

                if (!Application.isPlaying)
                    FallbackSceneUtility.Close();

            }

#else
            ServiceUtility.Get<UI.Notifications.EditorCoroutinesNotification>()?.Show();
#endif
        }

        static bool TryGetScene(ViewModelContext context, out Scene scene)
        {
            scene = context.scene;
            var path = context.customParam as string;
            if (!scene && path is not null)
                scene = Scene.Find(path);
            return scene;
        }

        static bool TryGetSceneAsset(ViewModelContext context, out string scenePath)
        {
            var path = context.customParam as string;
            scenePath = path;
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        }

        [ASMWindowElement(ElementLocation.SceneRight, isVisibleByDefault: true)]
        static VisualElement LoaderIndicator(ViewModelContext context) =>
            Button(context,
                  onPropertyChanged: (button) =>
                  {

                      if (ASMWindowElementAttribute.IsHostedWithinSettingsPage(button))
                      {
                          button.text = "";
                          button.tooltip = "This indicator will display the enabled scene loader, if any.";
                          return;
                      }

                      button.text = "";
                      button.tooltip = "";

                      button.SetVisible(false);

                      if (!context.scene)
                          return;

                      if (context.scene.GetSceneLoader() is SceneLoader loader)
                      {

                          if (loader.indicator.icon?.Invoke() is Texture2D icon)
                          {
                              button.style.backgroundImage = icon;
                          }
                          else if (!string.IsNullOrWhiteSpace(loader.indicator.text))
                          {

                              button.text = loader.indicator.text;
                              button.EnableInClassList("fontAwesome", loader.indicator.useFontAwesome);
                              button.EnableInClassList("fontAwesomeBrands", loader.indicator.useFontAwesomeBrands);
                              button.style.color = loader.indicator.color ?? Color.white;

                          }

                          button.tooltip = string.IsNullOrWhiteSpace(loader.indicator.tooltip) ? loader.sceneToggleText : loader.indicator.tooltip;
                          button.SetVisible(true);

                          if (loader.indicator.onClick is null)
                              button.SetEnabled(false);
                          else
                          {
                              button.SetEnabled(true);
                              button.clicked += () => loader.indicator.onClick(context.scene);
                          }

                      }
                  });

        [ASMWindowElement(ElementLocation.SceneRight, isVisibleByDefault: true)]
        static VisualElement InputBindingIndicator(ViewModelContext context) =>
            Label(context,
                onPropertyChanged: (label) =>
                {

                    label.Hide();

                    if (ASMWindowElementAttribute.IsHostedWithinSettingsPage(label))
                    {
                        label.text = "Tab";
                        label.tooltip = "This indicator will display the first input a standalone scene is bound to, if any.";
                        label.Show();
                    }
                    else if (context.scene && context.scene.inputBindings.FirstOrDefault() is InputBinding binding)
                    {
                        label.text = string.Join("+", binding.buttons.Select(b => ObjectNames.NicifyVariableName(b.name)));
                        label.Show();
                    }

                });

        [ASMWindowElement(ElementLocation.SceneRight, isVisibleByDefault: true)]
        static VisualElement DoNotOpenIndicator(ViewModelContext context) =>
            Label(context,
                  text: "",
                  tooltip: "This scene will not open automatically when collection opens",
                  onPropertyChanged: (button) =>
                  {

                      if (ASMWindowElementAttribute.IsHostedWithinSettingsPage(button))
                      {
                          button.SetVisible(true);
                          return;
                      }

                      var isDoNotOpen = context.scene && context.collection && context.collection.scenesThatShouldNotAutomaticallyOpen.Contains(context.scene);
                      button.SetVisible(isDoNotOpen);

                  });

        [ASMWindowElement(ElementLocation.SceneRight, isVisibleByDefault: true)]
        static VisualElement PersistentIndicator(ViewModelContext context) =>
            Label(context,
                  text: "",
                  tooltip: "This scene will open as persistent",
                  onPropertyChanged: (button) =>
                  {

                      if (ASMWindowElementAttribute.IsHostedWithinSettingsPage(button))
                      {
                          button.SetVisible(true);
                          return;
                      }

                      var isPersistent = context.scene && (context.scene.keepOpenWhenCollectionsClose || context.scene.EvalOpenAsPersistent(context.collection, null));
                      button.SetVisible(isPersistent);

                  });

        public static Button Button(ViewModelContext context, string text = null, string tooltip = null, bool useFontAwesome = true, Action<Button> onPropertyChanged = null, Action onClick = null)
        {

            var element = Element(context, text, tooltip, useFontAwesome, onPropertyChanged);
            element.clickable = new(onClick);
            element.AddToClassList("scene-open-button");

            return element;

        }

        public static Label Label(ViewModelContext context, string text = null, string tooltip = null, bool useFontAwesome = true, Action<Label> onPropertyChanged = null)
        {
            return Element(context, text, tooltip, useFontAwesome, onPropertyChanged);
        }

        public static T Element<T>(ViewModelContext context, string text = null, string tooltip = null, bool useFontAwesome = true, Action<T> onPropertyChanged = null) where T : VisualElement, new()
        {

            var element = new T()
            {
                tooltip = tooltip
            };

            if (element is TextElement e)
                e.text = text;

            if (useFontAwesome)
                element.UseFontAwesome();

            if (context.scene)
            {
                context.scene.PropertyChanged += PropertyChanged;
                context.scene.onDestroy += Scene_onDestroy;
            }

            if (context.collection)
            {
                context.collection.PropertyChanged += PropertyChanged;
                context.collection.onDestroy += Collection_onDestroy;
            }

            SceneManager.runtime.sceneOpened += Runtime_sceneOpened;
            SceneManager.runtime.sceneClosed += Runtime_sceneClosed;

            UnityEngine.SceneManagement.SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;

            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += EditorSceneManager_sceneOpened;
            UnityEditor.SceneManagement.EditorSceneManager.sceneClosed += EditorSceneManager_sceneClosed;

            element.RegisterCallbackOnce<DetachFromPanelEvent>(e =>
            {

                if (context.scene)
                {
                    context.scene.PropertyChanged -= PropertyChanged;
                    context.scene.onDestroy -= Scene_onDestroy;
                }

                if (context.collection)
                {
                    context.collection.PropertyChanged -= PropertyChanged;
                    context.collection.onDestroy -= Collection_onDestroy;
                }

                SceneManager.runtime.sceneOpened -= Runtime_sceneOpened;
                SceneManager.runtime.sceneClosed -= Runtime_sceneClosed;

                UnityEngine.SceneManagement.SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
                UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= SceneManager_sceneUnloaded;

                UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= EditorSceneManager_sceneOpened;
                UnityEditor.SceneManagement.EditorSceneManager.sceneClosed -= EditorSceneManager_sceneClosed;

            });

            element.RegisterCallbackOnce<AttachToPanelEvent>(e =>
            {
                onPropertyChanged?.Invoke(element);
            });

            return element;

            void PropertyChanged(object sender, PropertyChangedEventArgs e) => onPropertyChanged?.Invoke(element);
            void Runtime_sceneOpened(Scene obj) => onPropertyChanged?.Invoke(element);
            void Runtime_sceneClosed(Scene obj) => onPropertyChanged?.Invoke(element);

            void Scene_onDestroy()
            {
                context.scene.PropertyChanged -= PropertyChanged;
                context.scene.onDestroy -= Scene_onDestroy;
            }

            void Collection_onDestroy()
            {
                context.collection.PropertyChanged -= PropertyChanged;
                context.collection.onDestroy -= Collection_onDestroy;
            }

            void SceneManager_sceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode) => onPropertyChanged?.Invoke(element);
            void SceneManager_sceneUnloaded(UnityEngine.SceneManagement.Scene scene) => onPropertyChanged?.Invoke(element);
            void EditorSceneManager_sceneClosed(UnityEngine.SceneManagement.Scene scene) => onPropertyChanged?.Invoke(element);
            void EditorSceneManager_sceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode) => onPropertyChanged?.Invoke(element);
        }

    }

}

using AdvancedSceneManager.Callbacks.Events;
using AdvancedSceneManager.Core;
using AdvancedSceneManager.Loading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static AdvancedSceneManager.SceneManager;
using scene = UnityEngine.SceneManagement.Scene;
using Scene = AdvancedSceneManager.Models.Scene;

namespace AdvancedSceneManager.Utility
{

    /// <summary>Manager for loading screens.</summary>
    public static class LoadingScreenUtility
    {

        #region Load progress listeners

        static readonly List<ILoadProgressListener> m_loadProgressListeners = new List<ILoadProgressListener>();

        /// <summary>The currently open loading screens.</summary>
        public static IEnumerable<ILoadProgressListener> loadProgressListeners => m_loadProgressListeners;

        /// <summary>The currently open loading screens.</summary>
        public static IEnumerable<LoadingScreenBase> loadingScreens => loadProgressListeners.OfType<LoadingScreenBase>();

        /// <summary>Registers a <see cref="ILoadProgressListener"/> that will receive callbacks when progress is reported from ASM.</summary>
        public static void RegisterLoadProgressListener(ILoadProgressListener listener)
        {
            if (!m_loadProgressListeners.Contains(listener))
                m_loadProgressListeners.Add(listener);
        }

        /// <summary>Unregisters a <see cref="ILoadProgressListener"/> that was registered using <see cref="RegisterLoadProgressListener(ILoadProgressListener)"/>.</summary>
        public static void UnregisterLoadProgressListener(ILoadProgressListener listener) =>
          m_loadProgressListeners.Remove(listener);

        /// <summary>Report progress.</summary>
        public static void ReportProgress(ILoadProgressData progress)
        {
            foreach (var listener in loadProgressListeners)
                listener.OnProgressChanged(progress);
        }

        #endregion
        #region Methods

        /// <summary>Gets if this scene is a loading screen.</summary>
        public static bool IsLoadingScreenOpen(Scene scene) =>
            loadingScreens.Any(l => scene && l && l.gameObject && (scene.path == l.gameObject.scene.path));

        /// <summary>Gets if any loading screens are open.</summary>
        public static bool isAnyLoadingScreenOpen =>
            loadingScreens.Where(l => l && l.gameObject).Any();

        static Scene GetLoadingScreen(SceneOperation operation)
        {
            if (operation?.loadingScene)
                return operation.loadingScene;
            else if (operation?.collection && operation.collection.effectiveLoadingScene)
                return operation.collection.effectiveLoadingScene;
            else
                return null;
        }

        /// <inheritdoc cref="OpenLoadingScreen{T}(Scene, SceneOperation, Action{T})"/>
        public static Async<LoadingScreen> OpenLoadingScreen(SceneOperation operation, Action<LoadingScreen> callbackBeforeBegin = null) =>
            OpenLoadingScreen(GetLoadingScreen(operation), operation, callbackBeforeBegin);

        /// <inheritdoc cref="OpenLoadingScreen{T}(Scene, SceneOperation, Action{T})"/>
        public static Async<LoadingScreen> OpenLoadingScreen(Scene loadingScene, SceneOperation operation = null, Action<LoadingScreen> callbackBeforeBegin = null) =>
            OpenLoadingScreen<LoadingScreen>(loadingScene, operation, callbackBeforeBegin);

        /// <inheritdoc cref="OpenLoadingScreen{T}(Scene, SceneOperation, Action{T})"/>
        public static Async<T> OpenLoadingScreen<T>(SceneOperation operation, Action<T> callbackBeforeBegin = null) where T : LoadingScreenBase =>
            OpenLoadingScreen(GetLoadingScreen(operation), operation, callbackBeforeBegin);

        /// <summary>Shows a loading screen.</summary>
        public static Async<T> OpenLoadingScreen<T>(Scene loadingScene, SceneOperation operation = null, Action<T> callbackBeforeBegin = null) where T : LoadingScreenBase
        {

            if (!loadingScene)
                return Async<T>.complete;

            T value = default;
            return new(Coroutine().StartCoroutine(description: $"OpenLoadingScreen: {loadingScene.name}"), () => value);

            IEnumerator Coroutine()
            {

                yield return loadingScene.Load(reportsProgress: false);

                if (!loadingScene.internalScene.HasValue || !loadingScene.internalScene.Value.IsValid())
                    yield return OnError($"Loaded scene was not valid.");
                else if (!loadingScene.FindObject<T>(out var loadingScreen))
                    yield return OnError($"No {typeof(T).Name} script could be found in '{loadingScene.name}.'");
                else
                {

                    if (loadingScreen is LoadingScreen l && l)
                        l.operation = operation;

                    callbackBeforeBegin?.Invoke(loadingScreen);
                    loadingScreen.SetState(isOpening: true);
                    yield return loadingScreen.OnOpen();
                    loadingScreen.SetState(isOpen: true);
                    value = loadingScreen;

                }

                IEnumerator OnError(string message)
                {
                    Debug.LogError(message);
                    yield return loadingScene.Unload();
                }

            }

        }

        /// <inheritdoc cref="CloseLoadingScreenScene(Scene)"/>
        public static IEnumerator CloseLoadingScreen(Scene scene) =>
            CloseLoadingScreen(loadingScreens.LastOrDefault(l => scene.Equals(l.ASMScene())));

        /// <summary>Hide the loading screen.</summary>
        /// <param name="loadingScreen">The loading screen to hide.</param>
        /// <param name="closeScene">Specifies whatever the scene should be closed afterwards. Use <see cref="CloseLoadingScreenScene(Scene)"/> if <see langword="false"/>.</param>
        public static IEnumerator CloseLoadingScreen(LoadingScreenBase loadingScreen, bool closeScene = true)
        {

            if (!loadingScreen)
                yield break;

            loadingScreen.SetState(isClosing: true);
            yield return loadingScreen.OnClose();

            if (closeScene && loadingScreen.ASMScene(out var scene))
                yield return CloseLoadingScreenScene(scene);

        }

        /// <summary>Close the scene that contained a loading screen.</summary>
        public static IEnumerator CloseLoadingScreenScene(Scene scene)
        {
            yield return scene.Unload();
        }

        /// <summary>Hide all loading screens.</summary>
        public static IEnumerator CloseAll()
        {
            foreach (var loadingScreen in loadingScreens.ToArray())
                yield return CloseLoadingScreen(loadingScreen);
        }

        #endregion
        #region DoAction utility

        #region Fade

        /// <summary>Finds the default fade loading screen. Can be set through project settings, or in scene loading section of the settings popup.</summary>
        public static Scene fade
        {
            get
            {
                if (settings.project.fadeScene)
                    return settings.project.fadeScene;
                else
                {
                    Log.Warning("No fade loading screen assigned. Assign one in ASM Settings → Scene Loading → Default fade screen to avoid this warning.");
                    return null;
                }
            }
        }

        /// <summary>Fades out the screen.</summary>
        public static Async<LoadingScreen> FadeOut(float duration = 1, Color? color = null) =>
            OpenLoadingScreen<LoadingScreen>(fade, null, callbackBeforeBegin: l => SetFadeProps(l, duration, color));

        /// <summary>Fades in the screen.</summary>
        public static IEnumerator FadeIn(LoadingScreenBase loadingScreen, float duration = 1, Color? color = null)
        {
            SetFadeProps(loadingScreen, duration, color);
            return CloseLoadingScreen(loadingScreen);
        }

        static void SetFadeProps(LoadingScreenBase loadingScreen, float duration, Color? color)
        {
            if (loadingScreen is IFadeLoadingScreen fade)
            {
                fade.fadeDuration = duration;
                fade.color = color ?? Color.black;
            }
        }

        #endregion

        /// <inheritdoc cref="DoAction(Scene, Func{IEnumerator}, Action{LoadingScreenBase})"/>
        public static SceneOperation DoAction(Scene scene, Action action, Action<LoadingScreenBase> loadingScreenCallback = null) =>
            DoAction(scene, coroutine: RunAction(action), loadingScreenCallback);

        /// <summary>Opens loading screen, performs action and hides loading screen again.</summary>
        /// <param name="scene">The loading screen scene.</param>
        /// <param name="coroutine">To coroutine to execute.</param>
        /// <param name="loadingScreenCallback">The callback to perform when loading script is loaded, but before ASM has called <see cref="LoadingScreenBase.OnOpen()"/>.</param>
        public static SceneOperation DoAction(Scene scene, Func<IEnumerator> coroutine, Action<LoadingScreenBase> loadingScreenCallback = null) =>
            SceneOperation.
                Start().
                With(scene).
                With(loadingScreenCallback).
                RegisterCallback<SceneOpenPhaseEvent>(e => e.WaitFor(coroutine), When.After);

        static Func<IEnumerator> RunAction(Action action)
        {
            return () => Run();
            IEnumerator Run()
            {
                action?.Invoke();
                yield break;
            }
        }

        #endregion

        /// <summary>Gets the current default loading screen.</summary>
        public static Scene defaultLoadingScreen =>
            profile ? profile.loadingScene : null;

#if UNITY_EDITOR
        internal static DateTime? lastRefresh;
        internal static void CacheSpecialScenes()
        {
            if (lastRefresh.HasValue && (DateTime.Now - lastRefresh.Value).TotalSeconds < 1)
                return;

            lastRefresh = DateTime.Now;
            foreach (var scene in assets.scenes.Where(s => s.CheckIfSpecialScene()))
                scene.Save();
        }
#endif

        /// <summary>Returns a coroutine that returns when <see cref="AsyncOperation.isDone"/> becomes <see langword="true"/>.</summary>
        public static GlobalCoroutine ReportProgress(this AsyncOperation asyncOperation, SceneOperationKind kind, SceneOperation operation, Scene scene = null)
        {

            return Coroutine().StartCoroutine();

            IEnumerator Coroutine()
            {

                if (asyncOperation == null)
                {
                    Log.Warning("Could not report progress due to null async operation.", logStackTrace: true);
                    yield break;
                }

                float lastProgress = -1f;

                while (!IsDone())
                {

                    float progress = asyncOperation.progress;

                    if (!Mathf.Approximately(lastProgress, progress))
                    {
                        lastProgress = progress;

                        if (operation is not null && !operation.progressScope.isRegistered)
                            operation.progressScope.SetSubProgress(kind, scene, asyncOperation.progress);

                        ReportProgress(new SceneLoadProgressData(asyncOperation.progress, kind, operation, scene));
                    }

                    yield return null;

                }

                if (operation is not null && !operation.progressScope.isRegistered)
                    operation.progressScope.SetSubProgress(kind, scene, asyncOperation.progress);

                ReportProgress(new SceneLoadProgressData(1, kind, operation, scene));

            }

            bool IsDone() =>
                (asyncOperation.isDone || Mathf.Approximately(asyncOperation.progress, 1f)) ||
                (!asyncOperation.allowSceneActivation && Mathf.Approximately(asyncOperation.progress, 0.9f));

        }

        /// <summary>Sets <see cref="AsyncOperation.allowSceneActivation"/> to <see langword="false"/>.</summary>
        public static AsyncOperation Preload(this AsyncOperation asyncOperation, out Func<IEnumerator> activateCallback)
        {

            asyncOperation.allowSceneActivation = false;
            activateCallback = Activate;

            return asyncOperation;

            IEnumerator Activate()
            {
                asyncOperation.allowSceneActivation = true;
                yield return asyncOperation;
            }

        }

    }

}

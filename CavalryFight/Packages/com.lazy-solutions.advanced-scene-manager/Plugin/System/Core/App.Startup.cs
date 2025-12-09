using System;
using System.Collections;
using System.Linq;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using UnityEngine;
using AdvancedSceneManager.Loading;
using AdvancedSceneManager.Callbacks.Events;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Callbacks.Events.Editor;
#endif

namespace AdvancedSceneManager.Core
{

    /// <summary>Manages startup and quit processes.</summary>
    /// <remarks>Usage: <see cref="SceneManager.app"/>.</remarks>
    public partial class App : DependencyInjection.IApp
    {

        #region Properties

        /// <summary>An object that persists start properties across domain reload, which is needed when configurable enter play mode is set to reload domain on enter play mode.</summary>
        [Serializable]
        public class StartupProps
        {

            /// <summary>Initializes a new instance of startup properties.</summary>
            public StartupProps()
            { }

            /// <summary>Creates a new props, from the specified props, copying its values.</summary>
            public StartupProps(StartupProps props)
            {
                forceOpenAllScenesOnCollection = props.forceOpenAllScenesOnCollection;
                fadeColor = props.fadeColor;
                openCollection = props.openCollection;
                m_runStartupProcessWhenPlayingCollection = props.m_runStartupProcessWhenPlayingCollection;
                softSkipSplashScreen = props.softSkipSplashScreen;
            }

            /// <summary>Specifies whatever splash screen should open, but be skipped.</summary>
            /// <remarks>Used by ASMSplashScreen.</remarks>
            [NonSerialized] public bool softSkipSplashScreen;

            /// <summary>Specifies whatever all scenes on <see cref="openCollection"/> should be opened.</summary>
            public bool forceOpenAllScenesOnCollection;

            /// <summary>The color for the fade out.</summary>
            /// <remarks>Unity splash screen color will be used if <see langword="null"/>.</remarks>
            public Color? fadeColor;

            [SerializeField] private bool? m_runStartupProcessWhenPlayingCollection;

            /// <summary>Specifies whatever startup process should run before <see cref="openCollection"/> is opened.</summary>
            public bool runStartupProcessWhenPlayingCollection
            {
#if UNITY_EDITOR
                get => m_runStartupProcessWhenPlayingCollection ?? SceneManager.settings.user.startupProcessOnCollectionPlay;
#else
                get => m_runStartupProcessWhenPlayingCollection ?? false;
#endif
                set => m_runStartupProcessWhenPlayingCollection = value;
            }

            /// <summary>Gets if startup process should run.</summary>
            public bool runStartupProcess =>
                openCollection
                ? runStartupProcessWhenPlayingCollection
                : true;

            /// <summary>Specifies a collection to be opened after startup process is done.</summary>
            public SceneCollection openCollection;

#if UNITY_EDITOR
            /// <summary>Gets the effective fade animation color, uses <see cref="fadeColor"/> if specified. Otherwise <see cref="PlayerSettings.SplashScreen.backgroundColor"/> will be used during first startup. On subsequent restarts <see cref="Color.black"/> will be used (ASM restart, not application restart!).</summary>
#endif
            public Color effectiveFadeColor => fadeColor ?? (SceneManager.app.isRestart ? Color.black : SceneManager.settings.project.buildUnitySplashScreenColor);

        }

        /// <summary>Gets the props that should be used for startup process.</summary>
        public StartupProps startupProps
        {
            get => SessionStateUtility.Get<StartupProps>(null, $"ASM.App.{nameof(startupProps)}");
            set => SessionStateUtility.Set(value, $"ASM.App.{nameof(startupProps)}");
        }

        /// <summary>Gets whatever we're currently in ASM play mode.</summary>
        /// <remarks>This is <see langword="true"/> when in build or when ASM play button in ASM window is pressed. Also <see langword="true"/> when any start or restart method in app class is called.</remarks>
#if UNITY_EDITOR
        public bool isASMPlay
        {
            get => SessionStateUtility.Get(false, $"ASM.App.{nameof(isASMPlay)}");
            internal set => SessionStateUtility.Set(value, $"ASM.App.{nameof(isASMPlay)}");
        }
#else
        public bool isASMPlay => true;
#endif

        /// <summary>Gets if startup process is finished.</summary>
        public bool isStartupFinished { get; private set; }

        /// <summary>Gets if ASM has been restarted, or is currently restarting.</summary>
        public bool isRestart { get; private set; }

#if UNITY_EDITOR

        static bool shouldRunStartupProcess
        {
            get => SessionStateUtility.Get(false, $"ASM.App.{nameof(shouldRunStartupProcess)}");
            set => SessionStateUtility.Set(value, $"ASM.App.{nameof(shouldRunStartupProcess)}");
        }

#endif

        #endregion
        #region Internal start

        void StartInternal()
        {

            ResetQuitStatus();

            if (isASMPlay)
                Restart();

        }

        #endregion
        #region Start / Restart

        GlobalCoroutine coroutine;

        /// <summary>Enters play mode, and runs ASM startup process. If already inside play mode, then startup process will be run again.</summary>
        /// <remarks>Proxy for <see cref="Restart(StartupProps)"/>.</remarks>
        public void Play(StartupProps props = null) =>
            Restart(props);

        /// <inheritdoc cref="RestartInternal(StartupProps)"/>
        public void Restart(StartupProps props = null) =>
            RestartInternal(props);

        /// <inheritdoc cref="RestartInternal(StartupProps)"/>
        public Async<bool> RestartAsync(StartupProps props = null) =>
            RestartInternal(props);

        Async<bool> currentProcess;
        /// <summary>Restarts the ASM startup process.</summary>
        Async<bool> RestartInternal(StartupProps props = null)
        {

            if (currentProcess is not null)
                return currentProcess;

            CancelStartup();

            if (props is not null)
                startupProps = props;

            startupProps ??= new();

            coroutine?.Stop();

#if UNITY_EDITOR
            if (!Application.isPlaying)
                if (!TryEnterPlayMode(props))
                    return Async<bool>.FromResult(false);
#endif

            coroutine = DoStartupProcess(startupProps).StartCoroutine(description: "ASM Startup", onComplete: () =>
            {
                currentProcess = null;
                startupProgressScope = null;
                isStartupFinished = true;
                isRunningStartupProcess = false;
            });

            currentProcess = new(coroutine, () => isStartupFinished);
            return currentProcess;

        }

        /// <summary>Cancels startup process.</summary>
        public void CancelStartup()
        {

            if (isRunningStartupProcess)
            {
                SceneManager.events.InvokeCallback<StartupCancelledEvent>();
            }

            currentProcess = null;
            startupProgressScope = null;
            isStartupFinished = true;
            isRunningStartupProcess = false;

            coroutine?.Stop();

        }

#if UNITY_EDITOR

        /// <summary>Tries to enter play mode. Returns <see langword="false"/> if user denies to save modified scenes.</summary>
        /// <remarks>Prompt to save modifies scenes can be overriden, see <see cref="ASMUserSettings.alwaysSaveScenesWhenEnteringPlayMode"/>.</remarks>
        bool TryEnterPlayMode(StartupProps props)
        {

            if (SceneManager.settings.user.alwaysSaveScenesWhenEnteringPlayMode)
                EditorSceneManager.SaveOpenScenes();

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return false;

            shouldRunStartupProcess = true;
            isASMPlay = true;

            SceneManager.events.InvokeCallbackSync(new BeforeASMPlayModeEvent(props));

            EditorApplication.EnterPlaymode();

            return true; //Unity does not support checking if playmode is entered or not, so we have to check at a later point

        }

#endif

        #endregion
        #region Startup process

        /// <summary>Gets the progress scope used during startup.</summary>
        /// <remarks><see langword="null"/> unless startup process is currently running.</remarks>
        public ProgressScope startupProgressScope { get; private set; }

        /// <summary>Gets if ASM startup process is running.</summary>
        public bool isRunningStartupProcess { get; private set; }

        SplashScreen splashScreen;

        IEnumerator DoStartupProcess(StartupProps props)
        {

            if (!Application.isPlaying)
                yield break;

            using var timer = Log.Duration("ASM startup took {0}s.");

            isRunningStartupProcess = true;
            isRestart = isStartupFinished;
            isStartupFinished = false;

            //Fixes issue where first scene cannot be opened when user are not using configurable enter play mode
            yield return null;

#if UNITY_EDITOR

            LogUtility.LogStartupBegin();
            if (!SceneManager.profile)
            {
                Log.Error("No profile set.");
                yield break;
            }

#endif

            startupProgressScope = new ProgressScope().
                Expect(SceneOperationKind.Load, SceneManager.profile.startupScenes.Distinct()).
                Expect(SceneOperationKind.Load, props.openCollection, openAll: props.forceOpenAllScenesOnCollection);

            foreach (var collection in SceneManager.profile.startupCollections)
            {
                startupProgressScope.Expect(SceneOperationKind.Load, collection);
            }

            QueueUtility<SceneOperation>.StopAll();
            SceneManager.events.InvokeCallback(new StartupStartedEvent(props));

            yield return OpenSplashScreen(props);
            yield return CloseAllScenes(props);

            yield return OpenScenes(props, true);
            yield return OpenCollections(props);
            yield return OpenCollection(props);
            yield return OpenScenes(props, false);

            startupProgressScope?.StopListener();

            yield return CloseSplashScreen();

            if (!SceneManager.openScenes.Any())
                Log.Warning("No scenes opened during startup.");

            if (currentProcess == null) //CancelStartup() was called
                yield break;

#if UNITY_EDITOR
            shouldRunStartupProcess = false;
#endif

            isStartupFinished = true;
            isRunningStartupProcess = false;

            SceneManager.events.InvokeCallback(new StartupFinishedEvent(props));
            LogUtility.LogStartupEnd();

            startupProgressScope = null;

        }

        IEnumerator CloseAllScenes(StartupProps _)
        {

            SceneManager.runtime.Reset();
            if (splashScreen)
                SceneManager.runtime.Track(splashScreen.ASMScene());

            var scenes = SceneUtility.GetAllOpenUnityScenes().Where(SceneFilter).ToArray();

            static bool SceneFilter(UnityEngine.SceneManagement.Scene s) =>
                !FallbackSceneUtility.IsFallbackScene(s) &&
                (!SceneManager.profile.startupScene || SceneManager.profile.startupScene.name != s.name);

            if (scenes.Count() <= 0)
                yield break;

            foreach (var scene in scenes)
            {

                yield return FallbackSceneUtility.EnsureOpenAsync();

                if (FallbackSceneUtility.IsFallbackScene(scene))
                    continue;

                if (!scene.IsValid())
                    continue;

                if (splashScreen && scene == splashScreen.gameObject.scene)
                    continue;

#if UNITY_EDITOR
                if (SceneImportUtility.StringExtensions.IsTestScene(scene.path))
                    continue;
#endif
                if (scene.ASMScene() == SceneManager.assets.defaults.inGameToolbarScene)
                    continue;

                yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene).ReportProgress(SceneOperationKind.Unload, null, scene);

            }
        }

        IEnumerator OpenSplashScreen(StartupProps props)
        {

            if (SceneManager.profile && SceneManager.profile.splashScene)
            {

                yield return EnsureClosed();

#if UNITY_EDITOR
                if (!SceneManager.settings.user.splashDisplayInEditor)
                    yield break;
#endif

                var async = LoadingScreenUtility.OpenLoadingScreen<SplashScreen>(SceneManager.profile.splashScene);
                yield return async;

                splashScreen = async.value;

                if (splashScreen)
                    splashScreen.ASMScene().Activate();

            }

            static IEnumerator EnsureClosed()
            {

                var scenes = SceneUtility.GetAllOpenUnityScenes().Where(s => s.path == SceneManager.profile.splashScene.path);

                foreach (var scene in scenes)
                    yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);

            }

        }

        IEnumerator CloseSplashScreen()
        {
            if (splashScreen)
                yield return LoadingScreenUtility.CloseLoadingScreen(splashScreen);
        }

        IEnumerator OpenCollections(StartupProps props)
        {

            if (props.runStartupProcess)
            {

                var collections = SceneManager.profile.startupCollections.ToArray();
                var progress = collections.ToDictionary(c => c, c => 0f);

                foreach (var collection in collections)
                    yield return collection.Open().WithoutLoadingScreen();

            }

        }

        IEnumerator OpenScenes(StartupProps props, bool persistent)
        {

            var scenes = SceneManager.profile.startupScenes.Where(s => persistent == s.keepOpenWhenCollectionsClose).ToList();
            var progress = scenes.ToDictionary(c => c, c => 0f);

            foreach (var scene in scenes)
                yield return scene.Open();

        }

        IEnumerator OpenCollection(StartupProps props)
        {

            var collection = props.openCollection;
            if (collection)
                yield return collection.Open(openAll: props.forceOpenAllScenesOnCollection).WithoutLoadingScreen();

        }

        #endregion

    }

}

using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Callbacks.Events;
using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.Core;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Enums;
using AdvancedSceneManager.Utility;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AdvancedSceneManager.Editor
{

    sealed class EditorSceneLoader : SceneLoader
    {

        public override string sceneToggleText { get; }
        public override bool activeOutsideOfPlayMode => true;
        public override bool activeInPlayMode => false;

        #region Register

        static readonly ProgressListener progressListener = new();

        [OnLoad]
        static void Initialize()
        {

            //Sets up PlayModeChangedEvent
            EditorApplication.playModeStateChanged += state => SceneManager.events.InvokeCallbackSync(new PlayModeChangedEvent(state));

            SceneManager.events.RegisterCallback<PlayModeChangedEvent>(OnPlayModeChanged);

            SceneManager.runtime.AddSceneLoader<UnincludedSceneLoader>();
            LoadingScreenUtility.RegisterLoadProgressListener(progressListener);

            SceneManager.events.RegisterCallback<StartPhaseEvent>(e => BeforeStart(e.operation));
            SceneManager.events.RegisterCallback<EndPhaseEvent>(e =>
            {
                if (!Application.isPlaying)
                    FallbackSceneUtility.Close();
            });

            SetupSelectionPersistance();

            //This is important since user might have domain reload before play enabled
            if (!Application.isPlaying)
            {
                //Frames are staggered in the editor, and editor may produce a new frame unless something actually happens,
                //like mouse move for example. This makes sure coroutines run without moving mouse.
                SceneManager.events.RegisterCallback<SceneManagerBecameBusyEvent>(e => EditorApplication.update += ForceRepaint);
                SceneManager.events.RegisterCallback<SceneManagerBecameIdleEvent>(e => EditorApplication.update -= ForceRepaint);

                EditorApplication.delayCall += OnEnable;
            }

        }

        static void SetupSelectionPersistance()
        {

            var selectedObjects = System.Array.Empty<GlobalObjectId>();

            SceneManager.events.RegisterCallback<BeforeASMPlayModeEvent>(e => SaveSelection());
            SceneManager.events.RegisterCallback<StartupFinishedEvent>(e => RestoreSelection());

            SceneManager.events.RegisterCallback<PlayModeChangedEvent>(PlayModeChanged);

            void SaveSelection() =>
                selectedObjects = Selection.objects.NonNull().Select(o => GlobalObjectId.GetGlobalObjectIdSlow(o)).ToArray();

            void RestoreSelection()
            {
                EditorApplication.delayCall += () =>
                {
                    //Don't restore if user has paused game and selected something before this (or managed to click something as things were loading)
                    if (Application.isPlaying && Selection.objects.Length > 0)
                        return;

                    if (selectedObjects.Length == 0)
                        return;

                    var obj = new Object[selectedObjects.Length];
                    GlobalObjectId.GlobalObjectIdentifiersToObjectsSlow(selectedObjects, obj);

                    Selection.objects = obj;
                };
            }

            void PlayModeChanged(PlayModeChangedEvent e)
            {
                if (e.state == PlayModeStateChange.EnteredEditMode)
                {
                    RestoreSelection();
                }
                else if (e.state == PlayModeStateChange.ExitingPlayMode && !SceneManager.app.isASMPlay)
                {
                    selectedObjects = System.Array.Empty<GlobalObjectId>();
                }
            }

        }

        static void OnPlayModeChanged(PlayModeChangedEvent e)
        {

            if (e.state == PlayModeStateChange.EnteredEditMode)
            {
                OnEnable();
                SceneManager.app.isASMPlay = false;
            }
            else if (e.state == PlayModeStateChange.ExitingEditMode)
            {

                QueueUtility<SceneOperation>.StopAll();

                if (SceneImportUtility.untrackedScenes.Count() != 0)
                {
                    Debug.LogError("Note: You have imported scenes that are, for whatever reason, not tracked by ASM. Please track them by opening the ASM window and pressing the related notification.");
                }

                EditorSceneManager.playModeStartScene =
                    SceneManager.app.isASMPlay
                    ? AssetDatabase.LoadAssetAtPath<SceneAsset>(FallbackSceneUtility.GetPath())
                    : null;

            }
            else if (e.state == PlayModeStateChange.EnteredPlayMode)
            {
                OnDisable();
                OpenPlayModeScenes();
                EditorApplication.delayCall += TrackScenes;
            }
            else if (e.state == PlayModeStateChange.ExitingPlayMode)
            {
                SceneManager.app.CancelStartup();
                foreach (var operation in SceneManager.runtime.runningOperations)
                {
                    operation.Cancel();
                }
                SceneManager.app.isASMPlay = false;
            }

        }

        static void ForceRepaint()
        {
            if (!Application.isBatchMode)
                Resources.FindObjectsOfTypeAll<EditorWindow>().First().Repaint();
        }

        static void OnEnable()
        {

            OnDisable();

            EditorSceneManager.sceneManagerSetupRestored += SceneSetupRestored;
            EditorApplication.playModeStateChanged += PlayModeChanged;
            BuildUtility.postBuild += OnPostBuild;

            SceneManager.runtime.AddSceneLoader<EditorSceneLoader>();

            SetupTracking();
            Reload();

            BuildUtility.UpdateSceneList();

        }

        static void OpenPlayModeScenes()
        {
            if (Application.isPlaying && !SceneManager.app.isASMPlay && SceneManager.profile)
                foreach (var scene in SceneManager.profile.standaloneScenes.Where(s => s && s.openOnPlayMode && !s.isOpenInHierarchy))
                    scene.Open();
        }

        static void OnDisable()
        {

            EditorSceneManager.sceneManagerSetupRestored -= SceneSetupRestored;
            EditorApplication.playModeStateChanged -= PlayModeChanged;
            BuildUtility.postBuild -= OnPostBuild;

            SceneManager.runtime.RemoveSceneLoader<EditorSceneLoader>();

            if (SceneManager.settings.user.clearCollectionWhenEnteringPlayMode)
                SceneManager.runtime.UntrackCollections();

        }

        static void SceneSetupRestored(UnityEngine.SceneManagement.Scene[] scenes) =>
            scenes.ForEach(Track);

        static void PlayModeChanged(PlayModeStateChange state)
        {
            EditorApplication.delayCall += Reload;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {

                if (!SceneManager.app.isASMPlay)
                    foreach (var scene in SceneManager.openScenes)
                        _ = CallbackUtility.DoSceneOpenCallbacks(scene).StartCoroutine();

            }
        }

        static void OnPostBuild(BuildUtility.PostBuildEventArgs _) =>
            EditorApplication.delayCall += Reload;

        static void Reload() =>
            TrackScenes();

        static void BeforeStart(SceneOperation operation)
        {

            if (Application.isPlaying)
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                operation.Cancel();
                return;
            }

            var unsavedScenes = SceneUtility.GetAllOpenUnityScenes().Where(s => s.isDirty);
            foreach (var scene in unsavedScenes)
            {
                FallbackSceneUtility.EnsureOpen();
                EditorSceneManager.CloseScene(scene, true);
            }

            var untitledScenes = SceneUtility.GetAllOpenUnityScenes().Where(s => !s.isDirty && !AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path)).ToArray();
            foreach (var scene in untitledScenes)
            {
                FallbackSceneUtility.EnsureOpen();
                EditorSceneManager.CloseScene(scene, true);
            }

            if (operation.isFrozen)
                return;

            operation.WithoutLoadingScreen();

            var scenes = operation.open.Any() ? operation.open : SceneManager.openScenes;
            foreach (var scene in scenes)
            {

                var autoScenes = FindAutoOpenScenes(scene);
                operation.Open(autoScenes);

                foreach (var autoScene in autoScenes)
                    operation.RegisterCallback<SceneOpenPhaseEvent>(e => SceneUtility.MoveToTop(autoScene), When.After);

            }

        }

        static IEnumerable<Scene> FindAutoOpenScenes(Scene scene)
        {

            return SceneManager.profile.scenes.Where(ShouldOpen);

            bool ShouldOpen(Scene s)
            {

                if (s.autoOpenInEditor is EditorPersistentOption.AnySceneOpened)
                    return true;
                else if (s.autoOpenInEditor is EditorPersistentOption.WhenAnyOfTheFollowingScenesAreOpened && s.autoOpenInEditorScenes.Contains(scene))
                    return true;

                return false;

            }

        }

        #endregion
        #region Tracking

        static void TrackScenes()
        {

            //Make sure open scenes are tracked
            foreach (var scene in SceneUtility.GetAllOpenUnityScenes().ToArray())
            {

                if (!scene.ASMScene(out var s))
                    continue;

                //Persist preloaded scene
                if (!scene.isLoaded)
                    SceneManager.runtime.TrackPreload(s, () => FinishPreloadCallback(scene));

                Track(s, scene);

            }

        }

        static void Track(Scene scene, UnityEngine.SceneManagement.Scene unityScene) => SceneManager.runtime.Track(scene, unityScene);
        static void Track(Scene scene) => SceneManager.runtime.Track(scene);
        static void Untrack(Scene scene) => SceneManager.runtime.Untrack(scene);

        static void SetupTracking()
        {

            TrackScenes();

            EditorSceneManager.sceneOpened += (e, _) => Track(e);
            EditorSceneManager.sceneClosed += (e) => Untrack(e);

        }

        static void Track(UnityEngine.SceneManagement.Scene scene)
        {
            if (scene.ASMScene(out var s))
                Track(s, scene);
        }

        static void Untrack(UnityEngine.SceneManagement.Scene scene)
        {
            if (scene.ASMScene(out var s))
                Untrack(s);
        }

        #endregion
        #region Load / unload

        public override IEnumerator LoadScene(Scene scene, SceneLoadArgs e)
        {
            if (!Application.isPlaying)
            {

                if (!e.isPreload)
                {
                    var uScene = EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Additive);
                    e.SetCompleted(uScene);
                }
                else
                {
                    var uScene = EditorSceneManager.OpenScene(scene.path, OpenSceneMode.AdditiveWithoutLoading);
                    e.SetCompleted(uScene, () => FinishPreloadCallback(e.scene));
                }

                FallbackSceneUtility.Close();
                yield break;

            }
        }

        static IEnumerator FinishPreloadCallback(Scene scene)
        {
            var uScene = EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Additive);
            scene.internalScene = uScene;
            yield break;
        }

        public override IEnumerator UnloadScene(Scene scene, SceneUnloadArgs e)
        {

            if (e.scene.internalScene.HasValue)
            {
                FallbackSceneUtility.EnsureOpen();
                EditorSceneManager.CloseScene(e.scene.internalScene.Value, true);
                FallbackSceneUtility.Close();
                e.SetCompleted();
            }

            yield break;

        }

        #endregion
        #region SceneAsset open

        [OnOpenAsset]
        static bool OnOpen(int instanceID)
        {

            var sceneAsset = EditorUtility.InstanceIDToObject(instanceID) as SceneAsset;
            if (!sceneAsset || !sceneAsset.ASMScene(out var scene))
                return false;

#if !COROUTINES
            return false;
#else

            if (Application.isPlaying)
            {
                scene.Open();
                return true;
            }
            else
            {

                if (SceneManager.settings.user.openCollectionOnSceneAssetOpen && scene.FindCollection(out var collection))
                {
                    collection.Open();
                    return true;
                }

                SceneManager.runtime.UntrackCollections();
                SceneManager.runtime.CloseAll(false, false).Open(scene);

                return true;

            }
#endif

        }

        #endregion

    }

}

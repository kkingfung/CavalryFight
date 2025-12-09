using AdvancedSceneManager.Callbacks.Events;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Enums;
using AdvancedSceneManager.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdvancedSceneManager.Core
{

    partial class Runtime
    {

        readonly List<Scene> m_openScenes = new();

        /// <summary>Gets the scenes that are currently open.</summary>
        public IEnumerable<Scene> openScenes => m_openScenes.NonNull();

        #region Checks

        bool IsValid(Scene scene) => scene;
        bool IsClosed(Scene scene) => !openScenes.Contains(scene);
        bool IsOpen(Scene scene) => scene && scene.isOpen;
        bool CanOpen(Scene scene, SceneCollection collection, bool openAllScenes) =>
            openAllScenes || !collection.scenesThatShouldNotAutomaticallyOpen.Contains(scene);

        bool LoadingScreen(Scene scene) => LoadingScreenUtility.IsLoadingScreenOpen(scene);

        bool IsPersistent(Scene scene, SceneCollection closeCollection = null, SceneCollection nextCollection = null)
        {
            if (!Application.isPlaying)
                return false;

            return
                scene.isPersistent
                || (scene.keepOpenWhenNewCollectionWouldReopen && nextCollection && nextCollection.Contains(scene));
        }

        bool NotPersistent(Scene scene, SceneCollection closeCollection = null, SceneCollection nextCollection = null) =>
            !IsPersistent(scene, closeCollection, nextCollection);

        bool NotPersistent(Scene scene, SceneCollection closeCollection = null) =>
            !IsPersistent(scene, closeCollection);

        bool NotLoadingScreen(Scene scene) =>
            !LoadingScreen(scene);

        #endregion
        #region Open

        /// <inheritdoc cref="Scene.Open"/>
        public SceneOperation Open(Scene scene) =>
            Open(scenes: scene);

        /// <inheritdoc cref="Scene.OpenAndActivate"/>
        public SceneOperation OpenAndActivate(Scene scene) =>
            SceneOperation.Queue().OpenAndActivate(scene);

        /// <inheritdoc cref="Open(IEnumerable{Scene})"/>
        public SceneOperation Open(params Scene[] scenes) =>
            Open((IEnumerable<Scene>)scenes);

        /// <inheritdoc cref="Scene.Open()"/>
        /// <remarks>Scenes that are already open will not be reopened; close them first.</remarks>
        public SceneOperation Open(IEnumerable<Scene> scenes)
        {
            scenes = scenes
                .NonNull()
                .Where(IsValid)
                .Where(IsClosed);

            if (!scenes.Any())
                return SceneOperation.done;

            if (SceneManager.runtime.currentOperation?.acceptsSubOperations ?? false)
            {
                // User is attempting to open a scene in an open callback; queue it as a sub-operation
                var operation = SceneOperation.Start().Open(scenes);
                SceneManager.runtime.currentOperation.WaitFor(operation);
                return operation;
            }
            else
                return SceneOperation.Queue().Open(scenes);
        }

        /// <inheritdoc cref="Scene.OpenWithLoadingScreen(Scene)"/>
        public SceneOperation OpenWithLoadingScreen(Scene scene, Scene loadingScreen) =>
            Open(scene).With(loadingScreen);

        /// <inheritdoc cref="Scene.OpenWithLoadingScreen(Scene)"/>
        public SceneOperation OpenWithLoadingScreen(IEnumerable<Scene> scene, Scene loadingScreen) =>
            Open(scene).With(loadingScreen);

        #endregion
        #region Close

        /// <inheritdoc cref="Scene.Close"/>
        public SceneOperation Close(Scene scene) =>
            Close(scenes: scene);

        /// <inheritdoc cref="Close(IEnumerable{Scene})"/>
        public SceneOperation Close(params Scene[] scenes) =>
            Close((IEnumerable<Scene>)scenes);

        /// <inheritdoc cref="Scene.Close"/>
        /// <remarks>Also closes persistent scenes.</remarks>
        public SceneOperation Close(IEnumerable<Scene> scenes) =>
            Close(scenes, skipEmptySceneCheck: false);

        /// <inheritdoc cref="Close(IEnumerable{Scene})"/>
        public SceneOperation Close(IEnumerable<Scene> scenes, bool skipEmptySceneCheck = false)
        {
            scenes = scenes
                .NonNull()
                .Where(IsValid)
                .Where(IsOpen);

            var list = scenes.ToList();

            if (!skipEmptySceneCheck && !list.Any())
                return SceneOperation.done;

            return SceneOperation.Queue().Close(scenes);
        }

        /// <inheritdoc cref="Scene.CloseWithLoadingScreen(Scene)"/>
        public SceneOperation CloseWithLoadingScreen(Scene scene, Scene loadingScreen) =>
            Close(scene).With(loadingScreen);

        /// <inheritdoc cref="Scene.CloseWithLoadingScreen(Scene)"/>
        public SceneOperation CloseWithLoadingScreen(IEnumerable<Scene> scene, Scene loadingScreen) =>
            Close(scene).With(loadingScreen);

        /// <summary>Closes all scenes and collections.</summary>
        /// <param name="exceptLoadingScreens">If true, loading screens remain open.</param>
        /// <param name="exceptUnimported">If true, unimported scenes remain open.</param>
        /// <param name="except">Optional specific scenes to exclude.</param>
        public SceneOperation CloseAll(bool exceptLoadingScreens = true, bool exceptUnimported = true, params Scene[] except)
        {
            var scenes = openScenes;
            if (exceptLoadingScreens)
                scenes = scenes.Where(s => !s.isLoadingScreen && !except.Contains(s));

            if (SceneManager.settings.project.reverseUnloadOrderOnCollectionClose)
                scenes = scenes.Reverse();

            var operation = Close(scenes, skipEmptySceneCheck: true)
                .UntrackAllCollectionsCallback()
                .RegisterCallback<LoadingScreenClosePhaseEvent>(e => UntrackPreload(), When.Before);

            if (!exceptUnimported)
                operation.RegisterCallback<SceneClosePhaseEvent>(e => e.WaitFor(CloseUnimportedScenes), When.After);

            return operation;

            IEnumerator CloseUnimportedScenes()
            {
                var scenes = SceneUtility.GetAllOpenUnityScenes()
                    .Where(s => !s.ASMScene() && !FallbackSceneUtility.IsFallbackScene(s))
                    .ToArray();

                foreach (var scene in scenes)
                    yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
            }
        }

        #endregion
        #region Preload

        /// <inheritdoc cref="Scene.Preload(Action)"/>
        public SceneOperation Preload(Scene scene, Action onPreloaded = null) =>
            Preload(onPreloaded: (s) => onPreloaded?.Invoke(), new[] { scene });

        /// <summary>Preloads the specified scenes.</summary>
        public SceneOperation Preload(Action<Scene> onPreloaded = null, params Scene[] scenes) =>
            Preload(scenes: scenes, onPreloaded);

        /// <summary>Preloads the specified scenes.</summary>
        public SceneOperation Preload(params Scene[] scenes) =>
            Preload(scenes, onPreloaded: null);

        #endregion
        #region Toggle

        /// <inheritdoc cref="Scene.ToggleOpen"/>
        public SceneOperation ToggleOpen(Scene scene) =>
            IsOpen(scene)
                ? Close(scene)
                : Open(scene);

        #endregion
        #region Active

        /// <summary>Gets the currently active scene, assuming it has been imported into ASM.</summary>
        public Scene activeScene =>
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().ASMScene();

        /// <inheritdoc cref="Scene.Activate"/>
        public void Activate(Scene scene)
        {
            if (!scene || !scene.isOpen)
                return;

            if (scene.internalScene.HasValue && scene.internalScene.Value.isLoaded)
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene.internalScene.Value);
            else
                Debug.LogError("Could not set active scene since internalScene not valid.");
        }

        #endregion
        #region Reopen

        /// <inheritdoc cref="Scene.Reopen"/>
        public SceneOperation Reopen(Scene scene)
        {
            if (IsClosed(scene))
                return SceneOperation.done;

            return SceneOperation.Queue().Close(scene).Open(scene);
        }

        /// <inheritdoc cref="Reopen(Scene)"/>
        public SceneOperation Reopen(IEnumerable<Scene> scene)
        {
            scene = scene
                .NonNull()
                .Where(IsValid)
                .Where(IsOpen);

            if (!scene.Any())
                return SceneOperation.done;

            return SceneOperation.Queue().Close(scene).Open(scene);
        }

        #endregion
        #region SceneState

        /// <inheritdoc cref="Scene.state"/>
        public SceneState GetState(Scene scene)
        {
            if (!scene)
                return SceneState.Unknown;

            if (!scene.internalScene.HasValue)
                return SceneState.NotOpen;

            if (FallbackSceneUtility.IsFallbackScene(scene.internalScene.Value))
                throw new InvalidOperationException("Fallback scene is tracked by a Scene, this should not happen.");

            var isPreloaded = scene.internalScene.HasValue && !scene.internalScene.Value.isLoaded;
            var isOpen = openScenes.Contains(scene);
            var isQueued =
                QueueUtility<SceneOperation>.queue.Any(o => o.open?.Contains(scene) ?? false) ||
                QueueUtility<SceneOperation>.running.Any(o => o.open?.Contains(scene) ?? false);

            var isOpening = SceneOperation.currentLoadingScene == scene;
            var isPreloading = preloadedScenes.Contains(scene) || (SceneOperation.currentLoadingScene == scene && SceneOperation.isCurrentLoadingScenePreload);

            if (isPreloaded) return SceneState.Preloaded;
            else if (isPreloading) return SceneState.Preloading;
            else if (isOpen) return SceneState.Open;
            else if (isOpening) return SceneState.Opening;
            else if (isQueued) return SceneState.Queued;
            else return SceneState.NotOpen;
        }

        #endregion
        #region Unimported scenes

        /// <summary>Gets all open Unity scenes that are not imported into ASM.</summary>
        public IEnumerable<UnityEngine.SceneManagement.Scene> unimportedScenes =>
            SceneUtility.GetAllOpenUnityScenes()
            .Where(s => !FallbackSceneUtility.IsFallbackScene(s))
            .Where(s => !s.ASMScene());

        /// <summary>Closes all open Unity scenes that are not imported into ASM.</summary>
        public IEnumerator CloseUnimportedScenes()
        {
            foreach (var scene in unimportedScenes.ToArray())
                yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene.path);
        }

        #endregion

    }

}

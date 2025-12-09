using AdvancedSceneManager.Callbacks.Events;
using AdvancedSceneManager.Models;
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

        private readonly Dictionary<Scene, Func<IEnumerator>> m_preloadedScenes = new();

        /// <summary>Gets the scenes that are preloaded.</summary>
        public IEnumerable<Scene> preloadedScenes => m_preloadedScenes.Keys.NonNull();

        /// <summary>Gets the currently preloaded collection.</summary>
        public SceneCollection preloadedCollection { get; private set; }

        /// <summary>Gets if <see cref="preloadedCollection"/> is additive.</summary>
        public bool isPreloadedCollectionAdditive { get; private set; }

        #region Scene

        /// <summary>Preloads the scenes.</summary>
        public SceneOperation Preload(IEnumerable<Scene> scenes, Action<Scene> onPreloaded = null)
        {

            scenes = scenes.NonNull().Where(s => !preloadedScenes.Contains(s) && !s.isOpen).ToArray();

            return scenes.Any()
                ? SceneOperation.Queue().Preload(scenes).RegisterCallback<LoadingScreenClosePhaseEvent>(e => Callbacks(), When.Before)
                : SceneOperation.done;

            void Callbacks()
            {
                foreach (var scene in scenes)
                    onPreloaded?.Invoke(scene);
            }

        }

        internal void TrackPreload(Scene scene, Func<IEnumerator> preloadCallback)
        {

            if (m_preloadedScenes.ContainsKey(scene))
                return;

            m_preloadedScenes.Add(scene, () => Coroutine(preloadCallback));

            IEnumerator Coroutine(Func<IEnumerator> preloadCallback)
            {

                yield return preloadCallback();

                scenePreloadFinished?.Invoke(scene);
                scene.events.OnPreloadFinished?.Invoke(scene);

                m_preloadedScenes.Remove(scene);

            }

            if (scene)
            {
                scenePreloaded?.Invoke(scene);
                scene.events.OnPreload?.Invoke(scene);
            }

        }

        internal void UntrackPreload(Scene scene) =>
            m_preloadedScenes.Remove(scene);

        #endregion
        #region Collection

        private SceneOperation PreloadInternal(SceneCollection collection, bool openAll = false, bool isAdditive = false)
        {

            if (!collection)
                return SceneOperation.done;

            if (preloadedCollection)
            {
                Debug.LogError("Cannot preload multiple collections at once.");
                return SceneOperation.done;
            }

            preloadedCollection = collection;
            isPreloadedCollectionAdditive = isAdditive;

            return SceneOperation.Queue().
                With(collection, false).
                WithoutLoadingScreen().
                Preload(collection.scenes.
                    Where(IsValid).
                    Where(IsClosed).
                    Where(s => CanOpen(s, collection, openAll)));

        }

        SceneOperation FinishPreload(SceneCollection collection)
        {

            if (!collection)
                return SceneOperation.done;

            var operation = SceneOperation.Start().
                WithoutLoadingScreen().
                With(collection, collection.setActiveSceneWhenOpenedAsAdditive || !isPreloadedCollectionAdditive).
                TrackCollectionCallback(collection, isPreloadedCollectionAdditive);

            if (!isPreloadedCollectionAdditive)
                operation.Close(EvalScenesToClose(nextCollection: collection));

            return operation;

        }

        #endregion

        /// <summary>Finish loading preloaded scenes.</summary>
        /// <remarks>If a collection is preloaded, then scenes that would have normally closed when opening collection, will be closed when calling this. Scene will also be set as active.</remarks>
        public SceneOperation FinishPreload()
        {

            return preloadedScenes.Any()
                ? SceneOperation.Start().RegisterCallback<LoadingScreenOpenPhaseEvent>(e => e.WaitFor(Coroutine), When.After)
                : SceneOperation.done;

            IEnumerator Coroutine()
            {

                foreach (var scene in preloadedScenes.ToArray())
                {
                    if (m_preloadedScenes.TryGetValue(scene, out var callback))
                        yield return callback.Invoke();
                    UntrackPreload(scene);
                }

                if (preloadedCollection)
                    yield return FinishPreload(preloadedCollection);

                UntrackPreload();

            }

        }

        /// <summary>Cancels the preload. All preloaded scenes will be fully loaded (limitation by Unity), then closed. No ASM scene callbacks will be called.</summary>
        public SceneOperation CancelPreload()
        {

            return SceneOperation.Start().RegisterCallback<LoadingScreenOpenPhaseEvent>(e => e.WaitFor(Coroutine), When.After);

            IEnumerator Coroutine()
            {

                var scenes = preloadedScenes.ToArray();
                foreach (var scene in scenes)
                {
                    if (m_preloadedScenes.TryGetValue(scene, out var callback))
                    {
                        //Debug.Log(scene);
                        yield return callback.Invoke();
                    }
                }

                yield return SceneOperation.Start().Close(scenes);

                UntrackPreload();

            }

        }

        internal void UntrackPreload()
        {
            m_preloadedScenes.Clear();
            preloadedCollection = null;
            isPreloadedCollectionAdditive = false;
        }

    }

}

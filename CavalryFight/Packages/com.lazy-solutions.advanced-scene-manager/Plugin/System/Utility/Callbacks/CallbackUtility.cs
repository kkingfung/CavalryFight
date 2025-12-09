using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace AdvancedSceneManager.Callbacks
{

    /// <summary>
    /// Provides utilities for discovering and invoking scene and collection callbacks.
    /// Handles interfaces derived from <see cref="ISceneCallbacks"/> and their coroutine/awaitable counterparts.
    /// </summary>
    public static class CallbackUtility
    {

        #region Find objects

        [SuppressMessage("TypeSafety", "UNT0014:Invalid type for call to GetComponent", Justification = nameof(ISceneCallbacks) + " is an interface meant to be implemented on " + nameof(MonoBehaviour) + ".")]
        static IEnumerable<T> Get<T>(Object obj) where T : ISceneCallbacks
        {
            if (obj is ScriptableObject so && so is T t)
            {
                yield return t;
            }
            else if (obj is Scene scene)
            {
                foreach (var component in scene.FindObjects<T>())
                    yield return component;
            }
            else if (obj is GameObject go)
            {
                foreach (var item in go.GetComponentsInChildren<T>())
                    yield return item;
            }
        }

        static readonly Dictionary<Object, ISceneCallbacks[]> cache = new();

        /// <summary>Gets all cached callback instances of type <typeparamref name="T"/> for a specific object.</summary>
        static IEnumerable<T> GetCached<T>(Object obj) where T : ISceneCallbacks
        {
            // Clear unloaded objects
            foreach (var key in cache.Keys.Where(k => !k).ToArray())
                cache.Remove(key);

            if (!obj || (obj is Scene scene && !scene.isOpenInHierarchy))
                return Enumerable.Empty<T>();

            if (cache.TryGetValue(obj, out var callbacks))
                return callbacks.OfType<T>().ToArray();

            var items = Get<ISceneCallbacks>(obj).ToArray();
            cache[obj] = items;
            return items.OfType<T>();
        }

#if UNITY_EDITOR
        [OnLoad]
        static void InitializeCache()
        {
            cache.Clear();
            EditorSceneManager.sceneDirtied += SceneCallback;
            EditorSceneManager.sceneSaved += SceneCallback;
        }
#endif

        [OnLoad]
        static void IntitializeCacheRuntime()
        {
            cache.Clear();
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += SceneCallback;
        }

        private static void SceneCallback(UnityEngine.SceneManagement.Scene scene)
        {
            if (scene.ASMScene(out var s))
                _ = cache.Remove(s);
        }

        #endregion
        #region Known callbacks

        delegate IEnumerator Callback(object obj, object param);

        /// <summary>Holds known callback mappings for each supported <see cref="ISceneCallbacks"/> type.</summary>
        static readonly Dictionary<Type, Callback> knownCallbacks = new()
        {
            { typeof(ISceneOpen),                (o, p) => Call(() => ((ISceneOpen)o).OnSceneOpen()) },
            { typeof(ISceneClose),               (o, p) => Call(() => ((ISceneClose)o).OnSceneClose()) },
            { typeof(ICollectionOpen),           (o, p) => Call(() => ((ICollectionOpen)o).OnCollectionOpen(p as SceneCollection)) },
            { typeof(ICollectionClose),          (o, p) => Call(() => ((ICollectionClose)o).OnCollectionClose(p as SceneCollection)) },

            { typeof(ISceneOpenCoroutine),       (o, p) =>  ((ISceneOpenCoroutine)o).OnSceneOpen() },
            { typeof(ISceneCloseCoroutine),      (o, p) =>  ((ISceneCloseCoroutine)o).OnSceneClose() },
            { typeof(ICollectionOpenCoroutine),  (o, p) =>  ((ICollectionOpenCoroutine)o).OnCollectionOpen(p as SceneCollection) },
            { typeof(ICollectionCloseCoroutine), (o, p) =>  ((ICollectionCloseCoroutine)o).OnCollectionClose(p as SceneCollection) },

            { typeof(ISceneOpenAwaitable),       (o, p) =>  ((ISceneOpenAwaitable)o).OnSceneOpen() },
            { typeof(ISceneCloseAwaitable),      (o, p) =>  ((ISceneCloseAwaitable)o).OnSceneClose() },
            { typeof(ICollectionOpenAwaitable),  (o, p) =>  ((ICollectionOpenAwaitable)o).OnCollectionOpen(p as SceneCollection) },
            { typeof(ICollectionCloseAwaitable), (o, p) =>  ((ICollectionCloseAwaitable)o).OnCollectionClose(p as SceneCollection) },
        };

        static IEnumerator Call(Action action)
        {
            action.LogInvoke();
            yield break;
        }

        static IEnumerator KnownCallback(Type t, object obj, object param = null) =>
            typeof(ISceneCallbacks).IsAssignableFrom(t)
                ? knownCallbacks.GetValue(t)?.Invoke(obj, param)
                : null;

        /// <summary>Invokes all scene open callbacks on the specified <paramref name="scene"/>.</summary>
        public static IEnumerator DoSceneOpenCallbacks(Scene scene) =>
            CoroutineUtility.WaitAll("Invoking scene open callbacks",
                () => Invoke<ISceneOpen>().On(scene),
                () => Invoke<ISceneOpenCoroutine>().On(scene),
                () => Invoke<ISceneOpenAwaitable>().On(scene)
                );

        /// <summary>Invokes all scene close callbacks on the specified <paramref name="scene"/>.</summary>
        public static IEnumerator DoSceneCloseCallbacks(Scene scene) =>
            CoroutineUtility.WaitAll("Invoking scene close callbacks",
                () => Invoke<ISceneClose>().On(scene),
                () => Invoke<ISceneCloseCoroutine>().On(scene),
                () => Invoke<ISceneCloseAwaitable>().On(scene)
                );

        /// <summary>Invokes all collection open callbacks on the specified <paramref name="collection"/>.</summary>
        public static IEnumerator DoCollectionOpenCallbacks(SceneCollection collection)
        {
            if (collection && collection.userData)
                yield return CoroutineUtility.WaitAll("Invoking collection open callbacks",
                    () => Invoke<ICollectionOpen>().WithParam(collection).On(collection.userData),
                    () => Invoke<ICollectionOpenCoroutine>().WithParam(collection).On(collection.userData),
                    () => Invoke<ICollectionOpenAwaitable>().WithParam(collection).On(collection.userData)
                    );

            if (collection)
                yield return CoroutineUtility.WaitAll("Invoking collection open callbacks",
                    () => Invoke<ICollectionOpen>().WithParam(collection).On(collection),
                    () => Invoke<ICollectionOpenCoroutine>().WithParam(collection).On(collection),
                    () => Invoke<ICollectionOpenAwaitable>().WithParam(collection).On(collection)
                    );
        }

        /// <summary>Invokes all collection close callbacks on the specified <paramref name="collection"/>.</summary>
        public static IEnumerator DoCollectionCloseCallbacks(SceneCollection collection)
        {
            if (collection && collection.userData)
                yield return CoroutineUtility.WaitAll("Invoking collection close callbacks",
                    () => Invoke<ICollectionClose>().WithParam(collection).On(collection.userData),
                    () => Invoke<ICollectionCloseCoroutine>().WithParam(collection).On(collection.userData),
                    () => Invoke<ICollectionCloseAwaitable>().WithParam(collection).On(collection.userData)
                    );

            if (collection)
                yield return CoroutineUtility.WaitAll("Invoking collection close callbacks",
                    () => Invoke<ICollectionClose>().WithParam(collection).On(collection),
                    () => Invoke<ICollectionCloseCoroutine>().WithParam(collection).On(collection),
                    () => Invoke<ICollectionCloseAwaitable>().WithParam(collection).On(collection)
                    );
        }

        #endregion
        #region Discoverable callbacks

        /// <summary>
        /// Invokes all callbacks of type <typeparamref name="T"/> defined via <see cref="SceneCallbackAttribute"/> for a specific <see cref="Scene"/>.
        /// </summary>
        /// <remarks>Currently not implemented.</remarks>
        [SuppressMessage("CodeQuality", "IDE0060:Remove unused parameter", Justification = "Not yet implemented.")]
        [SuppressMessage("Roslynator", "RCS1079:Throwing of new NotImplementedException.", Justification = "Suppressed until implemented.")]
        internal static IEnumerator Invoke<T>(Scene scene) where T : SceneCallbackAttribute
        {
            yield break; // TODO: Implement discoverable scene callbacks.
        }

        /// <summary>
        /// Invokes all callbacks of type <typeparamref name="T"/> defined via <see cref="SceneCallbackAttribute"/> for a specific <see cref="SceneCollection"/>.
        /// </summary>
        /// <remarks>Currently not implemented.</remarks>
        [SuppressMessage("CodeQuality", "IDE0060:Remove unused parameter", Justification = "Not yet implemented.")]
        [SuppressMessage("Roslynator", "RCS1079:Throwing of new NotImplementedException.", Justification = "Suppressed until implemented.")]
        internal static IEnumerator Invoke<T>(SceneCollection collection) where T : SceneCallbackAttribute
        {
            yield break; // TODO: Implement discoverable collection callbacks.
        }

        #endregion
        #region Invoke

        /// <summary>Creates a fluent callback invocation API for the specified callback type.</summary>
        public static FluentInvokeAPI<T> Invoke<T>() where T : ISceneCallbacks =>
            new();

        static IEnumerator Invoke<T>(FluentInvokeAPI<T>.Callback invoke, object param, params Object[] obj) where T : ISceneCallbacks
        {
            var callbackObjects = obj.SelectMany(o => GetCached<T>(o)).ToArray();
            if (!callbackObjects.Any())
                yield break;

            foreach (var callback in callbackObjects)
                yield return Add(callback);

            IEnumerator Add(T callback)
            {
                var isEnabled = (callback is MonoBehaviour mb && mb && mb.isActiveAndEnabled) || callback is ScriptableObject;
                yield return invoke.Invoke(callback, isEnabled);
            }
        }

        /// <summary>Provides a fluent API for invoking callbacks of type <typeparamref name="T"/>.</summary>
        public sealed class FluentInvokeAPI<T> where T : ISceneCallbacks
        {

            /// <summary>Represents a coroutine callback delegate.</summary>
            public delegate IEnumerator Callback(T obj, bool isEnabled);

            Callback callback;
            object param;

            /// <summary>Gets whether <typeparamref name="T"/> has a default callback mapping.</summary>
            public bool hasDefaultCallback =>
                knownCallbacks.ContainsKey(typeof(T));

            /// <summary>Specifies a custom callback to invoke for <typeparamref name="T"/>.</summary>
            public FluentInvokeAPI<T> WithCallback(Callback callback) =>
                Set(() => this.callback = callback);

            /// <summary>Specifies an optional parameter passed to the invoked callback.</summary>
            public FluentInvokeAPI<T> WithParam(object param) =>
                Set(() => this.param = param);

            /// <summary>Invokes the callback on all scenes in the specified <paramref name="collection"/>.</summary>
            public IEnumerator On(SceneCollection collection, params Scene[] additionalScenes) =>
                On(collection.scenes.Concat(additionalScenes).ToArray());

            /// <summary>Invokes the callback on all currently open scenes.</summary>
            public IEnumerator OnAllOpenScenes() =>
                On(SceneManager.runtime.openScenes.ToArray());

            /// <summary>Invokes the callback on the specified <paramref name="scenes"/>.</summary>
            public IEnumerator On(params Scene[] scenes)
            {
                scenes = scenes.NonNull().ToArray();
                if (scenes.Length == 0)
                    yield break;

                if (hasDefaultCallback && callback is null)
                    callback = (c, isEnabled) => KnownCallback(typeof(T), c, param);

                if (callback is null)
                {
                    Debug.LogError($"No callback specified for callback type '{typeof(T).Name}'.");
                    yield break;
                }

                yield return Invoke(callback, param, scenes);
            }

            /// <summary>Invokes the callback on the specified <paramref name="scriptableObjects"/>.</summary>
            public IEnumerator On(params ScriptableObject[] scriptableObjects)
            {
                scriptableObjects = scriptableObjects.Where(s => s).ToArray();
                if (scriptableObjects.Length == 0)
                    yield break;

                if (hasDefaultCallback && callback is null)
                    callback = (c, isEnabled) => KnownCallback(typeof(T), c, param);

                if (callback is null)
                {
                    Debug.LogError($"No callback specified for callback type '{typeof(T).Name}'.");
                    yield break;
                }

                yield return Invoke(callback, param, scriptableObjects);
            }

            FluentInvokeAPI<T> Set(Action action)
            {
                action?.Invoke();
                return this;
            }

        }

        #endregion

    }

}

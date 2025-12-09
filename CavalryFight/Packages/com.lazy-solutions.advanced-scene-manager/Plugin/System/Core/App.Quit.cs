using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Callbacks.Events;
using AdvancedSceneManager.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Core
{

    public partial class App
    {

        readonly List<Func<IEnumerator>> callbacks = new();

        /// <summary>Register a callback to be called before quit.</summary>
        public void RegisterQuitCallback(Func<IEnumerator> coroutine) => callbacks.Add(coroutine);

        /// <summary>Unregister a callback that was to be called before quit.</summary>
        public void UnregisterQuitCallback(Func<IEnumerator> coroutine) => callbacks.Remove(coroutine);

        IEnumerator CallSceneCloseCallbacks()
        {
            yield return CallbackUtility.Invoke<ISceneClose>().OnAllOpenScenes();

            foreach (var scene in SceneManager.runtime.openScenes.ToArray())
                yield return CallbackUtility.Invoke<OnSceneCloseAttribute>(scene);
        }

        IEnumerator CallCollectionCloseCallbacks()
        {
            if (SceneManager.openCollection)
            {
                yield return CallbackUtility.Invoke<ICollectionClose>().WithParam(SceneManager.openCollection).OnAllOpenScenes();
                yield return CallbackUtility.Invoke<OnCollectionCloseAttribute>(SceneManager.openCollection);
            }
        }

        IEnumerator CallAddedCallbacks()
        {
            yield return callbacks.WaitAll(isCancelled: () => cancelQuit, description: "Invoking quit callback");
        }

        IEnumerator CallQuitEvent()
        {
            SceneManager.events.InvokeCallbackInternal(new QuitEvent(), when: When.Unspecified, out var waitFor);
            yield return waitFor.WaitAll(isCancelled: () => cancelQuit, description: "Invoking quit callback");
        }

        internal void ResetQuitStatus()
        {
            isQuitting = false;
            cancelQuit = false;
        }

        /// <summary>Gets whatever ASM is currently in the process of quitting the game.</summary>
        public bool isQuitting { get; private set; }

        bool cancelQuit;

        /// <summary>Cancels the current quit process.</summary>
        /// <remarks>Only usable during a <see cref="RegisterQuitCallback(Func{IEnumerator})"/> or while <see cref="isQuitting"/> is true.</remarks>
        public void CancelQuit()
        {
            if (isQuitting)
                cancelQuit = true;
        }

        /// <summary>Quits the application with optional fade effect.</summary>
        public void Quit(bool fade = true, Color? fadeColor = null, float fadeDuration = 1)
        {

            Coroutine().StartCoroutine();
            IEnumerator Coroutine()
            {

                QueueUtility<SceneOperation>.StopAll();

                isQuitting = true;
                cancelQuit = false;

                var wait = new List<Func<IEnumerator>>();

                var async = LoadingScreenUtility.FadeOut(fadeDuration, fadeColor);
                yield return async;
                wait.Add(() => new WaitForSecondsRealtime(0.5f));

                wait.Add(() => CallAddedCallbacks());
                wait.Add(() => CallQuitEvent());
                wait.Add(() => CallCollectionCloseCallbacks());
                wait.Add(() => CallSceneCloseCallbacks());

                yield return wait.WaitAll(isCancelled: () => cancelQuit, description: "Invoking quit callbacks");

                if (cancelQuit)
                {
                    cancelQuit = false;
                    isQuitting = false;
                    if (async?.value)
                        yield return LoadingScreenUtility.CloseLoadingScreen(async.value);
                    yield break;
                }

                Exit();

            }

        }

        /// <summary>Exits the application immediately.</summary>
        /// <remarks>No callbacks will be called, and no fade out will occur.</remarks>
        public void Exit()
        {
#if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }

    }

}
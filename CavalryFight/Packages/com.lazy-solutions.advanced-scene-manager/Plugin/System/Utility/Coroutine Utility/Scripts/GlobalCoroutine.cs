using AdvancedSceneManager.Callbacks.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AdvancedSceneManager.Utility
{

    /// <summary>Represents a <see cref="IEnumerator"/> coroutine started using <see cref="CoroutineUtility"/>.</summary>
    public class GlobalCoroutine : CustomYieldInstruction
    {

        #region Pooled construction

        /// <summary>Gets <see cref="GlobalCoroutine"/> from pool.</summary>
        internal static GlobalCoroutine Get(Action onComplete, (MethodBase method, string file, int line) caller, string description) =>
            GlobalCoroutinePool.Get(onComplete, caller, description);

        /// <summary>Don't use this, <see cref="GlobalCoroutine"/> is pooled using <see cref="GlobalCoroutinePool"/>. Use <see cref="Get"/> instead.</summary>
        internal GlobalCoroutine()
        { }

        /// <summary>Clears out the fields of this <see cref="GlobalCoroutine"/>, used to prepare before returning to <see cref="GlobalCoroutinePool"/>.</summary>
        internal void Clear() =>
            ConstructInternal(null, default, null, isDestroy: true);

        /// <summary>'Constructs' an instance of <see cref="GlobalCoroutine"/>, <see cref="GlobalCoroutine"/> is pooled using <see cref="GlobalCoroutinePool"/>, this means the instances are recycled, so instead of using constructor, we call this.</summary>
        internal void Construct(Action onComplete, (MethodBase method, string file, int line) caller, string description) =>
            ConstructInternal(onComplete, caller, description, isDestroy: false);

        void ConstructInternal(Action onComplete, (MethodBase method, string file, int line) caller, string description, bool isDestroy)
        {

            this.onComplete.Clear();
            this.onComplete.Add(onComplete);
            isPaused = false;
            isComplete = false;
            wasCancelled = false;
            this.caller = caller;
            this.description = description;

            SceneManager.events.InvokeCallbackSync(new GlobalCoroutinesChanged(CoroutineUtility.coroutines));

        }

        /// <summary />
        ~GlobalCoroutine()
        {
            Clear();
            GlobalCoroutinePool.ReturnToPool(this);
        }

        #endregion

        List<Action> onComplete = new();

        /// <summary>Gets whatever this coroutine is paused.</summary>
        public bool isPaused { get; private set; }

        /// <summary>Gets whatever this coroutine is completed.</summary>
        public bool isComplete { get; private set; }

        /// <summary>Gets whatever this coroutine is currently running. This will still return <see langword="true"/> when paused.</summary>
        public bool isRunning { get; private set; }

        /// <summary>Gets whatever this coroutine was cancelled.</summary>
        public bool wasCancelled { get; private set; }

        /// <summary>Gets the caller info of this coroutine.</summary>
        public (MethodBase method, string file, int line) caller { get; private set; }

        /// <summary>Gets the user defined message that was associated with this coroutine.</summary>
        public string description { get; set; }

        /// <summary><see cref="CustomYieldInstruction.keepWaiting"/>, this is how unity knows if this coroutine is done or not.</summary>
        public override bool keepWaiting => !isComplete;

        /// <summary>Pauses the coroutine. Make sure to not use this from within a coroutine, unless you also make sure to unpause it from outside. No effect if already paused.</summary>
        public void Pause()
        {
            if (!isPaused)
            {
                isPaused = true;
                SceneManager.events.InvokeCallbackSync(new GlobalCoroutinesChanged(CoroutineUtility.coroutines));
            }
        }

        /// <summary>Resumes a paused coroutine. No effect if not paused.</summary>
        public void Resume()
        {
            if (isPaused)
            {
                isPaused = false;
                SceneManager.events.InvokeCallbackSync(new GlobalCoroutinesChanged(CoroutineUtility.coroutines));
            }
        }

        internal void OnStart()
        {
            isRunning = true;
            SceneManager.events.InvokeCallbackSync(new GlobalCoroutinesChanged(CoroutineUtility.coroutines));
        }

        /// <summary>Stops the coroutine.</summary>
        public void Stop() =>
            Stop(isCancel: true);

        /// <summary>Stops the coroutine.</summary>
        internal void Stop(bool isCancel)
        {

            SceneManager.events.InvokeCallbackSync(new GlobalCoroutinesChanged(CoroutineUtility.coroutines));

            if (isComplete)
                return;

            if (CoroutineUtility.m_runner)
                CoroutineUtility.m_runner.Stop(this);

            wasCancelled = isCancel;
            isComplete = true;
            isRunning = false;

            foreach (var callback in onComplete)
                callback?.Invoke();

        }

        /// <inheritdoc cref="Object.ToString"/>/>
        public override string ToString() =>
            caller.ToString();

        /// <summary>Adds a callback to be invoked when the coroutine completes.</summary>
        public void OnComplete(Action callback) =>
            onComplete.Add(callback);

#if UNITY_EDITOR
        /// <summary>View caller in code editor, only accessible in editor.</summary>
        public void ViewCallerInCodeEditor()
        {

            var relativePath =
                caller.file.Contains("/Packages/")
                ? caller.file.Substring(caller.file.IndexOf("/Packages/") + 1)
                : "Assets" + caller.file.Replace(Application.dataPath, "");

            if (UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(relativePath))
                _ = UnityEditor.AssetDatabase.OpenAsset(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.MonoScript>(relativePath), caller.line, 0);
            else
                Debug.LogError($"Could not find '{relativePath}'");

        }
#endif

    }

}

using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Callbacks.Events;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdvancedSceneManager.Core
{

    partial class Runtime
    {

        void InitializeQueue()
        {

            QueueUtility<SceneOperation>.queueFilled += () =>
            {
                startedWorking?.Invoke();
                SceneManager.events.InvokeCallback<SceneManagerBecameBusyEvent>();
            };

            QueueUtility<SceneOperation>.queueEmpty += () =>
            {

                stoppedWorking?.Invoke();
                SceneManager.events.InvokeCallback<SceneManagerBecameIdleEvent>();
                CheckAllScenesClosed();

            };

#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged += (state) =>
            {
                if (state == UnityEditor.PlayModeStateChange.EnteredPlayMode)
                    CheckAllScenesClosed();
            };
#endif

        }

        [OnLoad]
        static void CheckAllScenesClosed()
        {

            if (Application.isPlaying && SceneUtility.unitySceneCount == 1 && FallbackSceneUtility.isOpen)
            {
                SceneManager.runtime.onAllScenesClosed?.Invoke();
                SceneManager.events.InvokeCallback<AllScenesClosedEvent>();
            }

        }

        /// <summary>Gets whatever ASM is busy with any scene operations.</summary>
        public bool isBusy => QueueUtility<SceneOperation>.isBusy || SceneManager.app.isRunningStartupProcess;

        /// <summary>The currently running scene operations.</summary>
        public IEnumerable<SceneOperation> runningOperations =>
            QueueUtility<SceneOperation>.running;

        /// <summary>Gets the current scene operation queue.</summary>
        public IEnumerable<SceneOperation> queuedOperations =>
            QueueUtility<SceneOperation>.queue;

        /// <summary>Gets the current active operation in the queue.</summary>
        public SceneOperation currentOperation =>
            QueueUtility<SceneOperation>.queue.FirstOrDefault();

        /// <summary>Gets if this collection is currently queued to be opened.</summary>
        public bool IsQueued(SceneCollection collection) =>
            SceneManager.runtime.runningOperations.Concat(SceneManager.runtime.queuedOperations).Any(o => o.collection == collection && !o.isCollectionCloseOperation);

        /// <summary>Gets if this scene is queued to be opened.</summary>
        public bool IsQueued(Scene scene) =>
            SceneManager.runtime.runningOperations.Concat(SceneManager.runtime.queuedOperations).SelectMany(o => o.open.Concat(o.preload)).Contains(scene);

    }

}

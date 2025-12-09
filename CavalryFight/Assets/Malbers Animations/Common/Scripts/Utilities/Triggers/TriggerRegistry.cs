using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MalbersAnimations.Utilities
{
    public static class TriggerRegistry
    {
        private static readonly HashSet<TriggerTarget> AllTargets = new();
        private static readonly Dictionary<Collider, TriggerTarget> ColliderMap = new();
        private static readonly List<Collider> KeysToRemove = new(32);

        static TriggerRegistry()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        }

        public static void RegisterTarget(TriggerTarget target)
        {
            if (target)
                AllTargets.Add(target);
        }

        public static void UnregisterTarget(TriggerTarget target)
        {
            if (target)
                AllTargets.Remove(target);
        }

        public static void RegisterCollider(Collider collider, TriggerTarget target)
        {
            if (collider && target)
                ColliderMap[collider] = target;
        }

        public static void UnregisterCollider(Collider collider)
        {
            if (collider && ColliderMap.ContainsKey(collider))
                ColliderMap.Remove(collider);
        }

        public static TriggerTarget GetTargetForCollider(Collider collider)
        {
            TriggerTarget target = null;
            if (collider)
                ColliderMap.TryGetValue(collider, out target);
            return target;
        }

        public static void RemoveInvalidReferences()
        {
            KeysToRemove.Clear();

            foreach (var entry in ColliderMap)
            {
                if (entry.Key == null || entry.Value == null ||
                    !entry.Key.gameObject || !entry.Value.gameObject)
                {
                    KeysToRemove.Add(entry.Key);
                }
            }

            foreach (var key in KeysToRemove)
            {
                ColliderMap.Remove(key);
            }

            AllTargets.RemoveWhere(target => target == null || !target.gameObject);

            if (KeysToRemove.Count > 0)
                Debug.Log($"[TriggerRegistry] Removed {KeysToRemove.Count} invalid references");
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single)
            {
                RemoveInvalidReferences();
                Debug.Log($"[TriggerRegistry] Cleanup performed on scene load: {scene.name}");
            }
        }

#if UNITY_EDITOR
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                AllTargets.Clear();
                ColliderMap.Clear();
                // Debug.Log("[TriggerRegistry] Registry cleared on exit play mode");
            }
        }
#endif
    }
}
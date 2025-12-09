#if UNITY_EDITOR

using AdvancedSceneManager.Callbacks.Events;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Utility;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Editor
{

    [CustomEditor(typeof(CoroutineRunner))]
    internal class CoroutineRunnerEditor : UnityEditor.Editor
    {

        void OnEnable()
        {
            SceneManager.events.RegisterCallback<GlobalCoroutinesChanged>(OnCoroutinesChanged);
        }

        void OnDisable()
        {
            SceneManager.events.UnregisterCallback<GlobalCoroutinesChanged>(OnCoroutinesChanged);
        }

        void OnCoroutinesChanged(GlobalCoroutinesChanged e) =>
            Repaint();

        static readonly Dictionary<GlobalCoroutine, bool> expanded = new();

        public override void OnInspectorGUI()
        {

            UnfocusOnClick();

            EditorGUILayout.Space();

            foreach (var coroutine in CoroutineUtility.coroutines)
                DrawCoroutine(coroutine);

        }

        void DrawCoroutine(GlobalCoroutine coroutine)
        {

            if (!expanded.ContainsKey(coroutine))
                expanded.Add(coroutine, false);

            var header = string.IsNullOrWhiteSpace(coroutine.description)
                    ? coroutine.caller.method?.Name
                    : coroutine.description;

            if (coroutine.isPaused)
                header += " [Paused]";

            _ = EditorGUILayout.BeginHorizontal();
            expanded[coroutine] = EditorGUILayout.BeginFoldoutHeaderGroup(
                foldout: expanded[coroutine],
                content: header);

            if (expanded[coroutine])
            {

                if (!coroutine.isPaused && GUILayout.Button("Pause", GUILayout.ExpandWidth(false)))
                    coroutine.Pause();
                else if (coroutine.isPaused && GUILayout.Button("Resume", GUILayout.ExpandWidth(false)))
                    coroutine.Resume();

                if (GUILayout.Button("View in code editor", GUILayout.ExpandWidth(false)))
                    coroutine.ViewCallerInCodeEditor();
                EditorGUILayout.EndHorizontal();

                var file = coroutine.caller.file.MakeRelative(includeAssetsFolder: false, prefixWithAssetsIfNecessary: false);

                EditorGUILayout.LabelField("Method:", (coroutine.caller.method?.DeclaringType?.FullName ?? "[Unknown caller type]") + "." + (coroutine.caller.method?.Name ?? "[Unknown caller method]") + "()");
                EditorGUILayout.LabelField("File:", file, new GUIStyle(EditorStyles.label) { wordWrap = true });
                EditorGUILayout.LabelField("Line:", coroutine.caller.line.ToString());

                EditorGUILayout.Space();

            }
            else
                EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndFoldoutHeaderGroup();

        }

        void UnfocusOnClick()
        {
            if (Event.current.type == EventType.MouseDown)
                GUI.FocusControl("");
        }

    }

}
#endif

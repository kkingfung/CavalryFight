#if UNITY_EDITOR

using System;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Editor.Utility
{

    /// <summary>Provides utility methods for opening the code editor on a method.</summary>
    public static class CodeEditorUtility
    {

        /// <summary>Opens the code editor to the top frame of a given exception.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static void OpenInCodeEditor(this Exception exception)
        {
            var trace = new System.Diagnostics.StackTrace(exception, true);
            var frame = trace.GetFrame(0);
            if (frame == null)
            {
                Debug.LogWarning("No stack frame found.");
                return;
            }

            string file = frame.GetFileName();
            int line = frame.GetFileLineNumber();

            if (string.IsNullOrEmpty(file) || line == 0)
            {
                Debug.LogWarning("Missing file or line info from stack frame.");
                return;
            }

            file = file.Replace("\\", "/");
            string relativePath = file.Contains("/Packages/")
                ? file.Substring(file.IndexOf("/Packages/") + 1)
                : "Assets" + file.Replace(Application.dataPath, "");

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativePath);
            if (asset)
                AssetDatabase.OpenAsset(asset, line);
            else
                Debug.LogError($"Could not find '{relativePath}'");
        }

        /// <summary>Opens the code editor to a specific member (e.g., method, property, or type).</summary>
        /// <remarks>Only available in editor.</remarks>
        public static void OpenInCodeEditor(this MemberInfo member)
        {
            if (member == null)
            {
                Debug.LogWarning("Member is null.");
                return;
            }

            Type typeToFind = member as Type ?? member.ReflectedType ?? member.DeclaringType;
            if (typeToFind == null)
            {
                Debug.LogWarning("Could not determine declaring type.");
                return;
            }

            string path = FindScriptAssetPath(typeToFind);
            if (path == null)
            {
                Debug.LogWarning($"Script for type {typeToFind.FullName} not found.");
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (!asset)
            {
                Debug.LogWarning("Could not load MonoScript.");
                return;
            }

            int line = FindLineNumber(asset, member.Name);
            AssetDatabase.OpenAsset(asset, line);
        }

        private static string FindScriptAssetPath(Type type)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:MonoScript"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script?.GetClass() == type)
                    return path;
            }
            return null;
        }

        private static int FindLineNumber(MonoScript script, string memberName)
        {
            var path = AssetDatabase.GetAssetPath(script);
            var lines = System.IO.File.ReadAllLines(path);
            var pattern = $@"\b{Regex.Escape(memberName)}\b";

            for (int i = 0; i < lines.Length; i++)
            {
                if (Regex.IsMatch(lines[i], pattern))
                    return i + 1;
            }

            return 1; // fallback to line 1
        }

    }

}
#endif

#if UNITY_EDITOR

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Editor.Utility
{

    /// <summary>Contains utility functions for working with the asset database.</summary>
    /// <remarks>Only available in the editor.</remarks>
    public static class AssetDatabaseUtility
    {

        const char BackSlash = '\\';
        const char ForwardSlash = '/';
        const string AssetsPath = "Assets/";

        /// <inheritdoc cref="CreateFolder(string, out string)"/>
        public static bool CreateFolder(string folder) =>
            CreateFolder(folder, out _);

        /// <summary>Creates the specified folder.</summary>
        /// <param name="path">The path to create folder at. Supports absolute paths (on same drive as project). Attempts to detect if path is file, and will then create containing folder.</param>
        /// <param name="createdFolder">The created folder.</param>
        /// <returns><see langword="true"/> if folder already exists, or if folder was created.</returns>
        public static bool CreateFolder(string path, [NotNullWhen(true)] out string createdFolder)
        {

            createdFolder = null;
            path = path?.ConvertToUnixPath()?.Trim();
            if (string.IsNullOrEmpty(path?.Trim('/')))
                return false;

            if (Path.IsPathRooted(path))
                path = Path.GetRelativePath(Application.dataPath, path).ConvertToUnixPath();

            if (path.StartsWith("../"))
                return false;

            var folder = Application.dataPath + ForwardSlash + path.Trim();
            folder = folder.Replace("/Assets/Assets", "/Assets/");

            createdFolder = Directory.CreateDirectory(folder).FullName.MakeRelative();
            createdFolder = createdFolder.TrimEnd(ForwardSlash) + ForwardSlash;
            AssetDatabase.ImportAsset(createdFolder);

            return AssetDatabase.IsValidFolder(createdFolder);

        }

        /// <summary>Converts the path separators to use forward slash.</summary>
        public static string ConvertToUnixPath(this string path) =>
              path?.Replace(BackSlash, ForwardSlash);

        /// <summary>Makes the path absolute. Converts path to unix style.</summary>
        /// <remarks>Only works for same disk as <see cref="Application.dataPath"/> is on.</remarks>
        public static string MakeRelative(this string path, bool includeAssetsFolder = true, bool prefixWithAssetsIfNecessary = true)
        {

            path = path.ConvertToUnixPath();

            if (path.StartsWith(Application.dataPath))
                path = path.Remove(0, Application.dataPath.Length);

            var assetsPath = AssetsPath.TrimEnd(ForwardSlash);
            var startsWithAssets = (path.StartsWith(AssetsPath) || path == assetsPath);

            if (!includeAssetsFolder && startsWithAssets)
                path = path.Remove(0, assetsPath.Length).TrimStart(ForwardSlash);

            else if (includeAssetsFolder && !startsWithAssets && prefixWithAssetsIfNecessary)
                path = AssetsPath + path.TrimStart(ForwardSlash);

            return path;

        }

        /// <summary>Finds all assets of type <typeparamref name="T"/>.</summary>
        public static IEnumerable<T> FindAssets<T>() where T : Object =>
            FindAssets<T>(System.Array.Empty<string>());

        /// <summary>Finds all assets of type <typeparamref name="T"/>.</summary>
        public static IEnumerable<T> FindAssets<T>(params string[] searchInFolders) where T : Object =>
            AssetDatabase.FindAssets($"t:{typeof(T).Name}", searchInFolders)
            .Select(AssetDatabase.GUIDToAssetPath)
            .SelectMany(AssetDatabase.LoadAllAssetsAtPath)
            .OfType<T>()
            .Distinct();

        /// <summary>Finds all assets of type <typeparamref name="T"/>.</summary>
        public static IEnumerable<string> FindAssetPaths<T>(params string[] searchInFolders) where T : Object =>
            AssetDatabase.FindAssets($"t:{typeof(T).Name}", searchInFolders)
            .Select(AssetDatabase.GUIDToAssetPath)
            .Distinct();

        #region Open folder in project view

        /// <summary>Shows the folder and selects the asset.</summary>
        public static void ShowFolder(string path) =>
            ShowFolder(AssetDatabase.LoadAssetAtPath<Object>(path));

        /// <summary>Shows the folder and selects the asset.</summary>
        public static void ShowFolder(Object obj)
        {
            if (!obj)
                throw new System.ArgumentNullException(nameof(obj));

            ShowFolder(obj.GetInstanceID());
            Selection.activeObject = obj;
        }

        /// <summary>
        /// Selects a folder in the project window and shows its content.
        /// Opens a new project window, if none is open yet.
        /// </summary>
        /// <param name="folderInstanceID">The instance of the folder asset to open.</param>
        static void ShowFolder(int folderInstanceID)
        {

            // Find the internal ProjectBrowser class in the editor assembly.
            var editorAssembly = typeof(EditorApplication).Assembly;
            var projectBrowserType = editorAssembly.GetType("UnityEditor.ProjectBrowser");

            // This is the internal method, which performs the desired action.
            // Should only be called if the project window is in two column mode.
            var showFolderContents = projectBrowserType.GetMethod("ShowFolderContents", BindingFlags.Instance | BindingFlags.NonPublic);

            // Find any open project browser windows.
            var projectBrowserInstances = Resources.FindObjectsOfTypeAll(projectBrowserType);

            if (projectBrowserInstances.Length > 0)
            {
                for (int i = 0; i < projectBrowserInstances.Length; i++)
                    ShowFolderInternal(projectBrowserInstances[i], showFolderContents, folderInstanceID);
            }
            else
            {
                var projectBrowser = OpenNewProjectBrowser(projectBrowserType);
                ShowFolderInternal(projectBrowser, showFolderContents, folderInstanceID);
            }

        }

        static void ShowFolderInternal(Object projectBrowser, MethodInfo showFolderContents, int folderInstanceID)
        {

            // Sadly, there is no method to check for the view mode.
            // We can use the serialized object to find the private property.
            var serializedObject = new SerializedObject(projectBrowser);
            var inTwoColumnMode = serializedObject.FindProperty("m_ViewMode").enumValueIndex == 1;

            if (!inTwoColumnMode)
            {
                // If the browser is not in two column mode, we must set it to show the folder contents.
                var setTwoColumns = projectBrowser.GetType().GetMethod("SetTwoColumns", BindingFlags.Instance | BindingFlags.NonPublic);
                setTwoColumns.Invoke(projectBrowser, null);
            }

            var revealAndFrameInFolderTree = true;
            showFolderContents.Invoke(projectBrowser, new object[] { folderInstanceID, revealAndFrameInFolderTree });

        }

        static EditorWindow OpenNewProjectBrowser(System.Type projectBrowserType)
        {

            var projectBrowser = EditorWindow.GetWindow(projectBrowserType);
            projectBrowser.Show();

            // Unity does some special initialization logic, which we must call,
            // before we can use the ShowFolderContents method (else we get a NullReferenceException).
            var init = projectBrowserType.GetMethod("Init", BindingFlags.Instance | BindingFlags.Public);
            init.Invoke(projectBrowser, null);

            return projectBrowser;

        }

        #endregion

    }

}

#endif

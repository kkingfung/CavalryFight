#if UNITY_EDITOR

using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Enums;
using AdvancedSceneManager.Models.Internal;
using AdvancedSceneManager.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static AdvancedSceneManager.Editor.Utility.SceneImportUtility.StringExtensions;

namespace AdvancedSceneManager.Editor.Utility
{

    /// <summary>Contains utility functions for importing / un-importing scenes.</summary>
    public partial class SceneImportUtility : AssetPostprocessor
    {

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {

            importedAssets = importedAssets.Where(IsScene).ToArray();
            deletedAssets = deletedAssets.Where(IsScene).ToArray();
            movedAssets = movedAssets.Where(IsScene).ToArray();
            movedFromAssetPaths = movedFromAssetPaths.Where(IsScene).ToArray();

            if (importedAssets.Length + deletedAssets.Length + movedAssets.Length == 0)
                return;

            SceneImportUtility.importedAssets.AddRange(importedAssets);
            SceneImportUtility.deletedAssets.AddRange(deletedAssets);
            SceneImportUtility.movedAssets.AddRange(movedAssets);
            SceneImportUtility.movedFromAssetPaths.AddRange(movedFromAssetPaths);

            EditorApplication.delayCall -= TrackChangedAssets;
            EditorApplication.delayCall += TrackChangedAssets;

        }

        readonly static List<string> movedAssets = new();
        readonly static List<string> movedFromAssetPaths = new();
        readonly static List<string> importedAssets = new();
        readonly static List<string> deletedAssets = new();
        static void TrackChangedAssets()
        {

            using var timer = Log.Duration("Tracking SceneAsset changes took {0}s.");

            MoveAssets(movedAssets, movedFromAssetPaths);
            ImportAssets(importedAssets);
            DeleteAssets(deletedAssets);

            movedAssets.Clear();
            movedFromAssetPaths.Clear();
            importedAssets.Clear();
            deletedAssets.Clear();

            Notify();

        }

        #region Import

        static void ImportAssets(IList<string> importedAssets)
        {
            var scenesToImport = importedAssets.Where(IsValidSceneToImport).ToArray();

            if (scenesToImport.Any() && SceneManager.settings.project.sceneImportOption is SceneImportOption.SceneCreated)
                _ = Import(scenesToImport);
        }

        /// <summary>Imports the specified scenes.</summary>
        /// <param name="sceneAssetPaths">Paths to scene assets to import.</param>
        /// <param name="notify">Whether to invoke <see cref="ScenesAvailableForImportChangedEvent"/>.</param>
        public static IEnumerable<Scene> Import(IEnumerable<string> sceneAssetPaths, bool notify = true) =>
            Import(sceneAssetPaths, SceneManager.settings.project.assetPath, notify);

        /// <summary>Imports the specified scenes into the given folder.</summary>
        /// <param name="sceneAssetPaths">Paths to scene assets to import.</param>
        /// <param name="importFolder">Folder where imported scenes will be placed.</param>
        /// <param name="notify">Whether to invoke <see cref="ScenesAvailableForImportChangedEvent"/>.</param>
        /// <returns>A collection of imported <see cref="Scene"/> instances.</returns>
        public static IEnumerable<Scene> Import(IEnumerable<string> sceneAssetPaths, string importFolder, bool notify = true)
        {
            var list = new List<Scene>();
            var paths = sceneAssetPaths.Where(path => !IsImported(path)).ToArray();

            try
            {
                foreach (var path in paths)
                {
                    try
                    {
                        list.Add(Import(path, importFolder, notify: false, track: false, skipImportedCheck: false));
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
            finally
            { }

            foreach (var item in list)
                SceneManager.assetImport.Add(item, importFolder);

            LogUtility.LogImported(list);

            if (notify && list.Any())
                Notify();

            return list;
        }

        /// <summary>Imports a single scene asset.</summary>
        /// <param name="sceneAssetPath">The path to the scene asset.</param>
        /// <param name="notify">Whether to invoke <see cref="ScenesAvailableForImportChangedEvent"/>.</param>
        /// <param name="track">Whether to add the scene to the asset tracking system.</param>
        public static Scene Import(string sceneAssetPath, bool notify = true, bool track = true) =>
            Import(sceneAssetPath, SceneManager.settings.project.assetPath, notify, track);

        /// <summary>Imports a single scene asset into the given folder.</summary>
        /// <param name="sceneAssetPath">The path to the scene asset.</param>
        /// <param name="importFolder">Folder where the imported scene will be placed.</param>
        /// <param name="notify">Whether to invoke <see cref="ScenesAvailableForImportChangedEvent"/>.</param>
        /// <param name="track">Whether to add the scene to the asset tracking system.</param>
        /// <param name="skipImportedCheck">Whether to skip checking if the scene is already imported.</param>
        /// <returns>The imported <see cref="Scene"/> instance.</returns>
        public static Scene Import(string sceneAssetPath, string importFolder, bool notify = true, bool track = true, bool skipImportedCheck = false)
        {

            if (!IsValidSceneToImport(sceneAssetPath))
                return null;

            if (!skipImportedCheck && IsImported(sceneAssetPath))
                throw new InvalidOperationException("Cannot import a scene that is already imported!");

            var scene = ScriptableObject.CreateInstance<Scene>();
            ((ScriptableObject)scene).name = Path.GetFileNameWithoutExtension(sceneAssetPath);
            scene.m_id = ASMModelBase.GenerateID();

            SceneManager.assetImport.SetSceneAssetPath(scene, sceneAssetPath, false);
            scene.CheckIfSpecialScene();

            if (track)
                SceneManager.assetImport.Add(scene, importFolder);

            if (notify)
            {
                Notify();
                LogUtility.LogImported(sceneAssetPath);
            }

            SceneManager.events.InvokeCallback(new SceneImportedEvent(scene));

            return scene;

        }

        #endregion
        #region Unimport

        static void DeleteAssets(IList<string> deletedAssets) =>
            Unimport(GetImportedScenes(deletedAssets));

        /// <summary>Unimports the specified scenes.</summary>
        /// <param name="scenes">Paths to scenes to unimport.</param>
        /// <param name="notify">Whether to invoke <see cref="ScenesAvailableForImportChangedEvent"/>.</param>
        public static void Unimport(IEnumerable<string> scenes, bool notify = true) =>
            Unimport(GetImportedScenes(scenes).NonNull(), notify);

        /// <summary>Unimports the specified scenes.</summary>
        /// <param name="scenes">Scenes to unimport.</param>
        /// <param name="notify">Whether to invoke <see cref="ScenesAvailableForImportChangedEvent"/>.</param>
        public static void Unimport(IEnumerable<Scene> scenes, bool notify = true)
        {
            var list = scenes.ToArray();
            if (list.Any())
            {
                var progressID = Progress.Start("Unimporting scenes...");

                try
                {
                    for (int i = 0; i < list.Length; i++)
                    {
                        Progress.Report(progressID, (float)i / list.Length, list[i].path);
                        Unimport(list[i], false);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                finally
                {
                    Progress.Remove(progressID);
                }

                SceneManager.settings.project.Save();
                LogUtility.LogUnimported(list);

                if (notify)
                    Notify();
            }
        }

        /// <summary>Unimports the specified scene.</summary>
        /// <param name="scene">The scene to unimport.</param>
        /// <param name="notify">Whether to invoke <see cref="ScenesAvailableForImportChangedEvent"/>.</param>
        public static void Unimport(Scene scene, bool notify = true)
        {
            if (!scene)
                throw new ArgumentNullException(nameof(scene));

            var path = scene.path;
            SceneManager.assetImport.Remove(scene);

            if (notify)
            {
                Notify();
                LogUtility.LogUnimported(path);
            }

            SceneManager.events.InvokeCallback(new SceneUnimportedEvent(scene));
        }

        #endregion
        #region Move

        static void MoveAssets(IList<string> movedAssets, IList<string> movedFromAssetPaths)
        {
            for (int i = 0; i < movedFromAssetPaths.Count; i++)
            {
                if (GetImportedScene(movedFromAssetPaths[i], out var scene))
                {
                    SceneManager.assetImport.SetSceneAssetPath(scene, movedAssets[i]);
                    scene.Rename(Path.GetFileNameWithoutExtension(movedAssets[i]));
                }
            }
        }

        #endregion
        #region Get imported scenes

        /// <summary>Gets the imported scene asset by its own asset path.</summary>
        /// <param name="path">The path to the scene asset file.</param>
        /// <returns>The imported <see cref="Scene"/> if found; otherwise, <see langword="null"/>.</returns>
        public static Scene GetImportedSceneByItsOwnPath(string path)
        {
            if (path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) &&
                path.IndexOf(SceneManager.assetImport.assetPath, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return AssetDatabase.LoadAssetAtPath<Scene>(path);
            }
            return null;
        }

        /// <summary>Attempts to get the imported scene matching the specified scene asset path.</summary>
        /// <param name="sceneAssetPath">The path to the scene asset.</param>
        /// <param name="scene">When this method returns, contains the imported <see cref="Scene"/> if found; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the scene was found; otherwise, <see langword="false"/>.</returns>
        public static bool GetImportedScene(string sceneAssetPath, out Scene scene) =>
            scene = SceneManager.assets.scenes.FirstOrDefault(
                s => s && string.Equals(s.path, sceneAssetPath, StringComparison.OrdinalIgnoreCase));

        /// <summary>Gets imported scenes matching the specified scene asset paths.</summary>
        /// <param name="sceneAssetPaths">The paths to the scene assets.</param>
        /// <returns>An enumerable collection of imported <see cref="Scene"/> instances.</returns>
        public static IEnumerable<Scene> GetImportedScenes(IEnumerable<string> sceneAssetPaths)
        {
            foreach (var path in sceneAssetPaths)
                if (GetImportedScene(path, out var scene))
                    yield return scene;
        }

        #endregion

        static bool hasNotifiedThisFrame;

        /// <summary>Notifies immediately, but only once per frame. Further calls in the same frame are ignored.</summary>
        internal static void Notify()
        {
            if (hasNotifiedThisFrame)
                return;

            hasNotifiedThisFrame = true;
            EditorApplication.delayCall += () => hasNotifiedThisFrame = false;

            NotifyNow();
        }

        internal static void NotifyNow()
        {
            SceneManager.events.InvokeCallbackSync<ScenesAvailableForImportChangedEvent>();
        }

        /// <summary>Gets the list of unimported scenes in the project, that are available for import.</summary>
        public static IEnumerable<string> unimportedScenes
        {
            get
            {
                if (!SceneManager.isInitialized)
                    return Enumerable.Empty<string>();

                var untracked = untrackedScenes.Select(s => s.path).ToArray();
                var invalid = scenesWithBadPath.Select(s => AssetDatabase.GetAssetPath(s.sceneAsset)).ToArray();

                return AssetDatabase.FindAssets("t:SceneAsset")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(IsValidSceneToImport)
                    .Except(untracked, StringComparer.OrdinalIgnoreCase)
                    .Except(invalid, StringComparer.OrdinalIgnoreCase)
                    .Distinct(StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>Gets the list of dynamic scenes in the current profile.</summary>
        public static IEnumerable<string> dynamicScenes =>
            SceneManager.profile
            ? SceneManager.profile.dynamicCollections.SelectMany(c => c.scenePaths)
            : Enumerable.Empty<string>();

        /// <summary>Gets the duplicate imported scenes.</summary>
        public static IEnumerable<IGrouping<string, Scene>> duplicateScenes =>
            SceneManager.assets.scenes
                .GroupBy(s => s.path, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1);

        /// <summary>Gets the list of imported scenes in the project.</summary>
        public static IEnumerable<string> importedScenes =>
            SceneManager.assets.scenes.Where(s => s).Select(s => s.path);

        /// <summary>Gets the list of imported scenes that do not have an associated scene asset.</summary>
        public static IEnumerable<Scene> invalidScenes =>
            SceneManager.assets.scenes.Where(s => s && !s.hasSceneAsset);

        /// <summary>Gets the list of imported scenes that do not match their asset path.</summary>
        public static IEnumerable<Scene> scenesWithBadPath =>
            SceneManager.assets.scenes.Where(s =>
                s && s.sceneAsset &&
                !IsPackageScene(s.path) &&
                !string.Equals(s.path, AssetDatabase.GetAssetPath(s.sceneAsset), StringComparison.OrdinalIgnoreCase));

        /// <summary>Gets the list of imported scenes that are blacklisted.</summary>
        public static IEnumerable<Scene> importedBlacklistedScenes =>
            SceneManager.assets.scenes.Where(s => s && IsBlacklisted(s.path));

        /// <summary>Gets the list of scenes that are imported, but are, for whatever reason, not tracked by AssetRef.</summary>
        public static IEnumerable<Scene> untrackedScenes
        {
            get
            {
                if (!SceneManager.isInitialized)
                    return Enumerable.Empty<Scene>();

                return AssetDatabase.FindAssets(Scene.AssetSearchString)
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<Scene>)
                    .NonNull()
                    .Where(s => !SceneManager.assets.scenes.Contains(s));
            }
        }

    }

}
#endif

using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using AdvancedSceneManager.Utility;
using AdvancedSceneManager.Models.Internal;
using AdvancedSceneManager.Models.Interfaces;
using AdvancedSceneManager.Core;

#if UNITY_EDITOR
using UnityEditor;
using AdvancedSceneManager.Editor.Utility;
#endif

namespace AdvancedSceneManager.Models
{

    partial class ASMSettings
    {

        //Fields from 3.0, needed for upgrade to 3.1
        [SerializeField, HideInInspector] internal List<Profile> m_profiles;
        [SerializeField, HideInInspector] internal List<Scene> m_scenes;
        [SerializeField, HideInInspector] internal List<SceneCollectionTemplate> m_collectionTemplates;

        [SerializeField, HideInInspector] internal ASMSceneHelper m_sceneHelper;
        [SerializeField, HideInInspector] internal FeatureFlags m_featureFlags;

        internal ASMAssetsCache assets => ASMAssetsCache.instance;

    }

    [ASMFilePath("ProjectSettings/AdvancedSceneManager.AssetsCache.asset")]
    internal class ASMAssetsCache : ASMScriptableSingleton<ASMAssetsCache>, IAssetsAPI, IAssetsAPIDefaultScenes, IAssetsAPIInternal/*, ISerializationCallbackReceiver*/
    {

        public ASMSceneHelper m_sceneHelper;
        public List<Profile> m_profiles = new();
        public List<Scene> m_scenes = new();
        public List<SceneCollectionTemplate> m_collectionTemplates = new();

        #region Serialization

#if UNITY_EDITOR
        void OnEnable()
        {
            Cleanup();
        }

        void Cleanup()
        {

            if (Application.isPlaying || EditorApplication.isUpdating)
                return;

            EditorApplication.delayCall -= CleanupInternal;
            EditorApplication.delayCall += CleanupInternal;

            void CleanupInternal()
            {

                if (Application.isPlaying || EditorApplication.isUpdating)
                    return;

                EnsureAssetsAdded<Profile>();
                EnsureAssetsAdded<Scene>();
                EnsureAssetsAdded<SceneCollectionTemplate>();

                m_profiles = m_profiles.NonNull().OrderBy(e => e.id).ToList();
                m_scenes = m_scenes.NonNull().OrderBy(e => e.id).ToList();
                m_collectionTemplates = m_collectionTemplates.NonNull().OrderBy(e => e.id).ToList();

                CleanupFolder(SceneManager.assetImport.GetFolder<Profile>());
                CleanupFolder(SceneManager.assetImport.GetFolder<Scene>());
                CleanupFolder(SceneManager.assetImport.GetFolder<SceneCollectionTemplate>());

            }

        }

        void CleanupFolder(string folder)
        {

            var emptySceneFolders = AssetDatabase.GetSubFolders(folder);

            foreach (var subfolder in emptySceneFolders)
            {

                var path = Application.dataPath + subfolder.Replace("Assets", "");

                if (Directory.Exists(path) && Directory.GetFileSystemEntries(path).Length == 0)
                    AssetDatabase.DeleteAsset(subfolder);

            }

        }

        void EnsureAssetsAdded<T>() where T : ASMModelBase
        {

            var list = GetList<T>();

            var existingPaths = list.Select(AssetDatabase.GetAssetPath).ToArray();
            var paths = AssetDatabaseUtility.FindAssetPaths<T>().Where(path => !existingPaths.Contains(path));

            var assets = paths.Select(AssetDatabase.LoadAssetAtPath<T>).NonNull().Where(m => m.hasID).ToArray();

            foreach (var asset in assets)
                list.Add(asset);

        }

#endif
        #endregion
        #region IAssetsAPI

        IEnumerable<Profile> IAssetsAPI.profiles => m_profiles.NonHidden();
        IEnumerable<Scene> IAssetsAPI.scenes => m_scenes.NonHidden();
        IEnumerable<SceneCollectionTemplate> IAssetsAPI.collectionTemplates => m_collectionTemplates.NonHidden();
        ASMSceneHelper IAssetsAPI.sceneHelper => m_sceneHelper;

        IAssetsAPIDefaultScenes IAssetsAPI.defaults => this;

        IEnumerable<T> IAssetsAPI.Enumerate<T>() => GetList<T>();

        IEnumerable<IASMModel> IAssetsAPI.Enumerate() =>
            m_profiles.OfType<IASMModel>().Concat(m_scenes).Concat(m_collectionTemplates);

        #region Fallback scene

        string IAssetsAPI.fallbackScenePath => GetFallbackScenePath();

        static string GetFallbackScenePath()
        {
            if (SceneManager.profile && SceneManager.profile.startupScene)
                return SceneManager.profile.startupScene.path;
            else
#if UNITY_EDITOR
                return $"{SceneManager.package.folder}/Plugin/Assets/{FallbackSceneUtility.Name}.unity";
#else
                return $"Packages/com.lazy-solutions.advanced-scene-manager/Plugin/Assets/{FallbackSceneUtility.Name}.unity";
#endif

        }

        #endregion

        #endregion
        #region IAssetsAPIInternal

#if UNITY_EDITOR

        string IAssetsAPIInternal.assetPath => SceneManager.settings.project ? SceneManager.settings.project.assetPath : string.Empty;

        #region Import

        void IAssetsAPIInternal.Add<T>(T asset, string rootFolder, bool save) => AddAsset(asset, rootFolder, save);
        void IAssetsAPIInternal.Remove<T>(T asset, bool save) => RemoveAsset(asset, save);
        bool IAssetsAPIInternal.IsIDTaken<T>(string id) => IsIDTaken<T>(id);
        string IAssetsAPIInternal.GetPath(ASMModelBase asset, string rootFolder) => GetAssetPath(asset, rootFolder);
        string IAssetsAPIInternal.GetFolder(ASMModelBase asset, string rootFolder) => GetAssetFolder(asset, rootFolder);
        string IAssetsAPIInternal.GetFolder<T>() => GetAssetFolder<T>();
        string IAssetsAPIInternal.GetFolder<T>(string id) => GetAssetFolder<T>(id);
        string IAssetsAPIInternal.GetPath<T>(string id, string name) => GetAssetPath<T>(id, name);
        bool IAssetsAPIInternal.Contains<T>(T asset) => ContainsAsset(asset);

        /// <remarks>Only available in editor.</remarks>
        internal void AddAsset<T>(T asset, string rootFolder = null, bool save = true) where T : ASMModelBase
        {

            if (!asset)
                return;

            rootFolder ??= SceneManager.settings.project.assetPath;

            // Ensure it exists in the right folder
            EnsureAssetPersistent(asset, rootFolder);

            // Add to settings list
            var list = GetList<T>();
            if (!list.Contains(asset))
            {

                list.Add(asset);

                if (save)
                {
                    Cleanup();
                    Save();
                }

            }

        }

        /// <remarks>Only available in editor.</remarks>
        internal void RemoveAsset<T>(T asset, bool save = true) where T : ASMModelBase
        {

            if (!asset)
                return;

            var list = GetList<T>();
            if (!list.Remove(asset))
                save = false;

            var path = AssetDatabase.GetAssetPath(asset);
            if (!string.IsNullOrEmpty(path))
                AssetDatabase.DeleteAsset(path);

            if (save)
            {
                Cleanup();
                Save();
            }

        }

        /// <remarks>Only available in editor.</remarks>
        internal bool IsIDTaken<T>(string id) where T : ASMModelBase, new() =>
            AssetDatabase.IsValidFolder(GetAssetFolder<T>(id));

        /// <remarks>Only available in editor.</remarks>
        internal bool ContainsAsset<T>(T asset) where T : ASMModelBase, new() =>
            GetList<T>().Contains(asset);

        // --- Persistence helpers ---

        /// <remarks>Only available in editor.</remarks>
        internal void EnsureAssetPersistent<T>(T asset, string rootFolder) where T : ASMModelBase
        {
            var path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path))
            {
                CreateAsset(asset, rootFolder);
            }
            else if (!path.StartsWith(rootFolder, StringComparison.OrdinalIgnoreCase))
            {
                MoveAsset(asset, rootFolder);
            }
        }

        void CreateAsset(ASMModelBase asset, string rootFolder)
        {
            var path = GetAssetPath(asset, rootFolder);
            Directory.GetParent(path).Create();
            AssetDatabase.CreateAsset(asset, path);
        }

        void MoveAsset(ASMModelBase asset, string rootFolder)
        {
            var newPath = GetAssetPath(asset, rootFolder);
            AssetDatabaseUtility.CreateFolder(Path.GetDirectoryName(newPath));
            var error = AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(asset), newPath);
            if (!string.IsNullOrEmpty(error))
                Log.Error(error);
        }

        #endregion
        #region Scene paths

#if UNITY_EDITOR

        /// <summary>Sets scene path and asset.</summary>
        /// <remarks>Only available in editor.</remarks>
        void IAssetsAPIInternal.SetSceneAssetPath(Scene scene, string path, bool save)
        {

            if (!scene)
                return;

            var didChange = false;
            if (scene.path.ToLower() != path.ToLower())
            {
                scene.path = path;
                didChange = true;
            }

            if (!scene.isDefaultASMScene)
            {

                var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                if (scene.sceneAsset != asset)
                {
                    scene.sceneAsset = asset;
                    didChange = true;
                }

                var guid = AssetDatabase.GUIDFromAssetPath(path).ToString();
                if (scene.m_sceneAssetGUID != guid)
                {
                    scene.m_sceneAssetGUID = guid;
                    didChange = true;
                }

            }

            if (didChange && save)
                scene.SaveNow();

        }

#endif
        #endregion

        /// <remarks>Only available in editor.</remarks>
        internal string GetAssetPath(ASMModelBase asset, string rootFolder) =>
            $"{GetAssetFolder(asset, rootFolder)}/{asset.name}.asset";

        /// <remarks>Only available in editor.</remarks>
        internal string GetAssetFolder(ASMModelBase asset, string rootFolder) =>
            $"{rootFolder}/{asset.GetType().Name}/{asset.id}";

        /// <remarks>Only available in editor.</remarks>
        internal string GetAssetFolder<T>() where T : ASMModelBase, new() =>
            $"{SceneManager.settings.project.assetPath}/{typeof(T).Name}";

        /// <remarks>Only available in editor.</remarks>
        internal string GetAssetFolder<T>(string id) where T : ASMModelBase, new() =>
            $"{GetAssetFolder<T>()}/{id}";

        /// <remarks>Only available in editor.</remarks>
        internal string GetAssetPath<T>(string id, string name) where T : ASMModelBase, new() =>
            $"{GetAssetFolder<T>(id)}/{name}.asset";

#endif

        public List<T> GetList<T>()
        {
            if (typeof(T) == typeof(Profile)) return (List<T>)(object)m_profiles;
            if (typeof(T) == typeof(Scene)) return (List<T>)(object)m_scenes;
            if (typeof(T) == typeof(SceneCollectionTemplate)) return (List<T>)(object)m_collectionTemplates;
            throw new InvalidOperationException($"No list for {typeof(T).Name}");
        }

        #endregion
        #region IAssetsAPIDefaultScenes

        Scene Find(string name) =>
           ((IAssetsAPIDefaultScenes)this).Enumerate().Find(name);

        /// <summary>Gets the default ASM splash scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        Scene IAssetsAPIDefaultScenes.splashASMScene => Find("Splash ASM");

        /// <summary>Gets the default fade splash scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        Scene IAssetsAPIDefaultScenes.splashFadeScene => Find("Splash Fade");

        /// <summary>Gets the default fade loading scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        Scene IAssetsAPIDefaultScenes.fadeScene => Find("Fade");

        /// <summary>Gets the default progress bar loading scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        Scene IAssetsAPIDefaultScenes.progressBarScene => Find("ProgressBar");

        /// <summary>Gets the default progress bar loading scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        Scene IAssetsAPIDefaultScenes.totalProgressBarScene => Find("TotalProgressBar");

        /// <summary>Gets the default icon bounce loading scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        Scene IAssetsAPIDefaultScenes.iconBounceScene => Find("IconBounce");

        /// <summary>Gets the default press any button loading scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        Scene IAssetsAPIDefaultScenes.pressAnyKeyScene => Find("PressAnyKey");

        /// <summary>Gets the default quote loading scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        Scene IAssetsAPIDefaultScenes.quoteScene => Find("Quote");

        /// <summary>Gets the default pause scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        Scene IAssetsAPIDefaultScenes.pauseScene => Find("Pause");

        /// <summary>Gets the default in-game-toolbar scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        Scene IAssetsAPIDefaultScenes.inGameToolbarScene => Find("InGameToolbar");

        /// <summary>Enumerates all imported default scenes.</summary>
        IEnumerable<Scene> IAssetsAPIDefaultScenes.Enumerate() =>
           SceneManager.assets.scenes.NonNull().Where(s => s.isDefaultASMScene);

        #endregion

    }

}

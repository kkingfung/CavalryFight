using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Models.Utility;
using AdvancedSceneManager.Services;
using AdvancedSceneManager.Utility;
using AdvancedSceneManager.Utility.Discoverability;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using AdvancedSceneManager.Editor.Utility;
#endif

namespace AdvancedSceneManager.Models
{

    partial class ASMSettings
    {

        static readonly List<Action> callbacks = new();

        internal static bool isInitialized;

        internal static double initializationTimeOverall { get; private set; }
        internal static double initializationTimeDiscoverables { get; private set; }
        internal static double initializationTimeServices { get; private set; }
        internal static double initializationTimeCallbacks { get; private set; }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod]
        static void OnLoad()
        {
            _ = instance;
        }

        /// <summary>Runs the callback when ASMSettings has initialized.</summary>
        public static void OnInitialized(Action action)
        {
            if (isInitialized)
                action.Invoke();
            else
                callbacks.Add(action);
        }

        void OnEnable_Initialization()
        {

            isInitialized = true;

            using var timer = Log.Duration("ASM initialization took {0}s.");

#if UNITY_EDITOR
            EditorApplication.delayCall += CheckDefaultAssets;
            AfterUpdateUtility.OnBeforeInitializeDone();
            ASMScriptableSingletonBuildStep.Cleanup();
#endif

            InitializeProfile();
            InitializeDiscoverables();
            InitializeServices();
            InvokeOnInitializeCallbacks();

            initializationTimeOverall = timer.Elapsed.TotalSeconds;

            SceneManager.app.Initialize();

        }

        static void InitializeProfile()
        {

#if UNITY_EDITOR

            ProfileUtility.SetProfile(GetProfile(), updateBuildSettings: !Application.isBatchMode);

            if (!Application.isBatchMode)
                BuildUtility.Initialize();

            static Profile GetProfile()
            {

                if (Application.isBatchMode)
                    return ProfileUtility.buildProfile;

                return GetFirstNonNull(
                    ProfileUtility.forceProfile,
                    SceneManager.settings.user.activeProfile,
                    ProfileUtility.defaultProfile,
                    ProfileUtility.buildProfile,
                    SceneManager.assets.profiles.Count() == 1 ? SceneManager.assets.profiles.ElementAt(0) : null);

            }

            static Profile GetFirstNonNull(params Profile[] profiles) =>
                profiles?.NonNull()?.FirstOrDefault() ?? null;

#else
            Log.Info("Set profile: " + SceneManager.profile);
#endif

        }

        void InitializeDiscoverables()
        {
            using var timer = Log.Duration("ASM initialization (discoverables) took {0}s.");
            DiscoverabilityUtility.Initialize();
            initializationTimeDiscoverables = timer.Elapsed.TotalSeconds;
        }

        void InitializeServices()
        {
            using var timer = Log.Duration("ASM initialization (services) took {0}s.");
            ServiceUtility.Initialize();
            initializationTimeServices = timer.Elapsed.TotalSeconds;
        }

        void InvokeOnInitializeCallbacks()
        {

            using var timer = Log.Duration("ASM initialization (callbacks) took {0}s.");

            DiscoverabilityUtility.Invoke<OnLoadAttribute>();

            foreach (var callback in callbacks)
                callback.Invoke();
            callbacks.Clear();

            initializationTimeCallbacks = timer.Elapsed.TotalSeconds;

        }

        string DefaultAssetsFolder => "Packages/com.lazy-solutions.advanced-scene-manager/Plugin/Assets";

        void CheckDefaultAssets()
        {

#if UNITY_EDITOR

            if (UnityEditor.MPE.ProcessService.level == UnityEditor.MPE.ProcessLevel.Secondary)
                return;

            CheckFallbackScene();
            if (CheckDefaultAsset(ref m_sceneHelper) | TrackASMScenes())
                Save();

#endif

        }

        bool CheckDefaultAsset<T>(ref T obj) where T : ScriptableObject
        {

            var needsSave = false;
            var path = $"{DefaultAssetsFolder}/{typeof(T).Name}.asset";

#if UNITY_EDITOR

            if (!obj)
            {
                obj = AssetDatabase.LoadAssetAtPath<T>(path);
                needsSave = true;
            }

#if ASM_DEV
            if (!obj)
            {
                AssetDatabase.CreateAsset(CreateInstance<T>(), path);
                obj = AssetDatabase.LoadAssetAtPath<T>(path);
                needsSave = true;
            }
#endif

#endif

            if (!obj)
                Log.Error($"Could not find {path}. You may have to re-install ASM.");

            return needsSave;

        }

        void CheckFallbackScene()
        {

#if UNITY_EDITOR

            var path = $"{DefaultAssetsFolder}/{FallbackSceneUtility.Name}.unity";
            var obj = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);

#if ASM_DEV
            if (!obj)
                obj = SceneUtility.Create(path);
#endif

            if (!obj)
                Log.Error($"Could not find {path}. You may have to re-install ASM.");

#endif

        }

        bool TrackASMScenes()
        {

            bool hasChanges = false;

#if UNITY_EDITOR

            var scenesToImport = SceneManager.assets.defaults.Enumerate().Where(s => !s.isImported);
            if (scenesToImport.Any())
            {
                foreach (var scene in scenesToImport)
                    SceneManager.assetImport.Add(scene, save: false);
                hasChanges = true;
            }

            if (SceneManager.assets.defaults.fadeScene && !SceneManager.settings.project.fadeScene)
            {
                SceneManager.settings.project.fadeScene = SceneManager.assets.defaults.fadeScene;
                hasChanges = true;
            }

#endif

            return hasChanges;

        }

    }

}

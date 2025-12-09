using UnityEngine;
using AdvancedSceneManager.Utility;
using System.ComponentModel;
using AdvancedSceneManager.Models.Enums;
using System.Collections.Generic;
using System.Collections;
using System;
using AdvancedSceneManager.DependencyInjection;
using AdvancedSceneManager.Callbacks;

#if UNITY_EDITOR
using UnityEditor;
using AdvancedSceneManager.Editor.Utility;
#endif

namespace AdvancedSceneManager.Models
{

    /// <summary>Contains the project wide ASM settings.</summary>
    /// <remarks>Manages initialization, as this <see cref="ScriptableObject"/> is core to ASM, and nothing works without it.</remarks>
    [ASMFilePath("ProjectSettings/AdvancedSceneManager.asset")]
    public partial class ASMSettings : ASMScriptableSingleton<ASMSettings>, INotifyPropertyChanged, IProjectSettings
    {

        void OnEnable()
        {
            _ = ASMAssetsCache.instance;
            _ = DiscoverablesCache.instance;
            OnEnable_Initialization();
        }

        /// <inheritdoc />
        public override void SaveNow()
        {
            assets.SaveNow();
            discoverablesCache.SaveNow();
            base.SaveNow();
        }

        #region Properties

        /// <summary>Represents a serializable dictionary for storing custom data.</summary>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        [Serializable]
        public class CustomDataDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
        {

            [SerializeField] private SerializableDictionary<TKey, TValue> dict = new();

            /// <summary>Gets or sets the value associated with the specified key.</summary>
            public TValue this[TKey key]
            {
                get => dict[key];
                set => dict[key] = value;
            }

            /// <summary>Gets custom data.</summary>
            /// <param name="key">The key of the data to get.</param>
            /// <param name="value">When this method returns, contains the value associated with the key, if found.</param>
            /// <returns><see langword="true"/> if the key was found; otherwise, <see langword="false"/>.</returns>
            public bool Get(TKey key, out TValue value) =>
                dict.TryGetValue(key, out value);

            /// <summary>Gets custom data.</summary>
            /// <param name="key">The key of the data to get.</param>
            /// <returns>The value associated with the key, or the default value if not found.</returns>
            public TValue Get(TKey key) =>
                dict.ContainsKey(key)
                ? dict[key]
                : default;

            /// <summary>Sets custom data.</summary>
            /// <param name="key">The key of the data to set.</param>
            /// <param name="value">The value to set.</param>
            public void Set(TKey key, TValue value)
            {
                _ = dict.Set(key, value);
                SceneManager.settings.project.Save();
            }

            /// <summary>Clears custom data for the specified key.</summary>
            /// <param name="key">The key of the data to clear.</param>
            public void Clear(TKey key)
            {
                if (dict.Remove(key))
                    SceneManager.settings.project.Save();
            }

            /// <summary>Determines whether the specified key exists.</summary>
            /// <param name="key">The key to check.</param>
            public bool ContainsKey(TKey key) =>
                dict.ContainsKey(key);

            /// <inheritdoc/>
            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => dict.GetEnumerator();

            /// <inheritdoc/>
            IEnumerator IEnumerable.GetEnumerator() => dict.GetEnumerator();

        }

        /// <summary>Represents a collection of string-based custom data entries.</summary>
        [Serializable]
        public class CustomData : CustomDataDictionary<string, string>
        { }

        /// <summary>Represents a collection of scene-specific custom data entries.</summary>
        [Serializable]
        public class SceneData : CustomDataDictionary<string, CustomData>
        { }

        [SerializeField] internal SceneData sceneData = new();

        #region Profiles

        [Header("Profiles")]
        [SerializeField] internal Profile m_defaultProfile;
        [SerializeField] internal Profile m_forceProfile;

        [SerializeField] internal Profile m_buildProfile;

        /// <summary>The profile to use when none is set.</summary>
        public Profile defaultProfile
        {
            get => m_defaultProfile;
            set { m_defaultProfile = value; OnPropertyChanged(); }
        }

        /// <summary>The profile to force everyone in this project to use.</summary>
        public Profile forceProfile
        {
            get => m_forceProfile;
            set { m_forceProfile = value; OnPropertyChanged(); }
        }

        /// <summary>The profile to use during build.</summary>
        public Profile buildProfile => m_buildProfile;

#if UNITY_EDITOR

        /// <summary>Sets the build profile.</summary>
        public void SetBuildProfile(Profile profile)
        {
            if (m_buildProfile != profile)
            {
                m_buildProfile = profile;
                SaveNow();
            }
        }

#endif

        #endregion
        #region Spam check

        [Header("Spam Check")]
        [SerializeField] private bool m_checkForDuplicateSceneOperations = true;
        [SerializeField] private bool m_preventSpammingEventMethods = true;
        [SerializeField] private float m_spamCheckCooldown = 0.5f;

        /// <summary>By default, ASM checks for duplicate scene operations, since this is usually caused by mistake, but this will disable that.</summary>
        public bool checkForDuplicateSceneOperations
        {
            get => m_checkForDuplicateSceneOperations;
            set { m_checkForDuplicateSceneOperations = value; OnPropertyChanged(); }
        }

        /// <summary>By default, ASM will prevent spam calling event methods (i.e. calling Scene.Open() from a button press), but this will disable that.</summary>
        public bool preventSpammingEventMethods
        {
            get => m_preventSpammingEventMethods;
            set { m_preventSpammingEventMethods = value; OnPropertyChanged(); }
        }

        /// <summary>Sets the default cooldown for <see cref="SpamCheck"/>.</summary>
        public float spamCheckCooldown
        {
            get => m_spamCheckCooldown;
            set { m_spamCheckCooldown = value; OnPropertyChanged(); }
        }

        #endregion
        #region Netcode

#if NETCODE

        [Header("Netcode")]
        [SerializeField] private bool m_isNetcodeValidationEnabled = true;

        /// <summary>Specifies whatever ASM should validate scenes in netcode.</summary>
        public bool isNetcodeValidationEnabled
        {
            get => m_isNetcodeValidationEnabled;
            set { m_isNetcodeValidationEnabled = value; OnPropertyChanged(); }
        }

#endif

        #endregion
        #region Scenes

        [Header("Scenes")]
        [SerializeField] internal Blocklist m_blacklist = new();
        [SerializeField] internal Blocklist m_whitelist = new();

        [SerializeField] private bool m_enableCrossSceneReferences;
        [SerializeField] private bool m_enableGUIDReferences = true;
        [SerializeField] private SceneImportOption m_sceneImportOption = SceneImportOption.Manual;
        [SerializeField] private bool m_reverseUnloadOrderOnCollectionClose = true;

        /// <summary>Gets or sets whatever cross-scene references should be enabled.</summary>
        /// <remarks>Experimental.</remarks>
        public bool enableCrossSceneReferences
        {
            get => m_enableCrossSceneReferences;
            set { m_enableCrossSceneReferences = value; OnPropertyChanged(); }
        }

        /// <summary>Gets or sets whatever GUID references should be enabled.</summary>
        public bool enableGUIDReferences
        {
            get => m_enableGUIDReferences;
            set { m_enableGUIDReferences = value; OnPropertyChanged(); }
        }

        /// <summary>Gets or sets when to automatically import scenes.</summary>
        public SceneImportOption sceneImportOption
        {
            get => m_sceneImportOption;
            set { m_sceneImportOption = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies whatever collections should unload scenes in the reverse order.</summary>
        public bool reverseUnloadOrderOnCollectionClose
        {
            get => m_reverseUnloadOrderOnCollectionClose;
            set { m_reverseUnloadOrderOnCollectionClose = value; OnPropertyChanged(); }
        }

        #endregion
        #region ASM info

        [Header("ASM")]
        [SerializeField] private string m_assetPath = "Assets/Settings/AdvancedSceneManager";
        [SerializeField] private CustomData m_customData = new();

        /// <summary>Specifies the path where profiles and imported scenes should be generated to.</summary>
        public string assetPath
        {
            get => m_assetPath;
            set { m_assetPath = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies custom data.</summary>
        public CustomData customData => m_customData;

        #endregion
        #region Splash screen color

        [Header("Splash screen")]
        [SerializeField] private Color m_unitySplashScreenColor = Color.black;

        /// <summary>This is the color of the unity splash screen, used to make the transition from unity splash screen to ASM smooth, this is set before building. <see cref="Color.black"/> is used when the unity splash screen is disabled.</summary>
        public Color buildUnitySplashScreenColor => m_unitySplashScreenColor;

#if UNITY_EDITOR

        [OnLoad]
        static void UpdateSplashScreen() =>
            BuildUtility.preBuild += (e) => SceneManager.settings.project.UpdateSplashScreenColor();

        void UpdateSplashScreenColor()
        {

            var color =
                PlayerSettings.SplashScreen.show
                ? PlayerSettings.SplashScreen.backgroundColor
                : Color.black;

            if (color != m_unitySplashScreenColor)
            {
                m_unitySplashScreenColor = color;
                SaveNow();
            }

        }

#endif

        #endregion
        #region Locking

        [Header("Locking")]
        [SerializeField] private bool m_allowSceneLocking = true;
        [SerializeField] private bool m_allowCollectionLocking = true;

        /// <summary>Specifies whatever asm will allow locking scenes.</summary>
        public bool allowSceneLocking
        {
            get => m_allowSceneLocking;
            set { m_allowSceneLocking = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies whatever asm will allow locking collections.</summary>
        public bool allowCollectionLocking
        {
            get => m_allowCollectionLocking;
            set { m_allowCollectionLocking = value; OnPropertyChanged(); }
        }

        #endregion
        #region Runtime

        [Header("Runtime")]
        [SerializeField] private SceneCollection m_openCollection;
        [SerializeField] private List<SceneCollection> m_additiveCollections = new();

        internal SceneCollection openCollection
        {
            get
            {
#if UNITY_EDITOR
                return SceneManager.settings.user.m_openCollection;
#else
                return m_openCollection;
#endif
            }
            set
            {
#if UNITY_EDITOR             

                if (!m_openCollection)
                {
                    m_openCollection = null;
                    Save();
                }

                SceneManager.settings.user.m_openCollection = value;
                SceneManager.settings.user.Save();

#else
                m_openCollection = value; 
                Save(); 
#endif
                OnPropertyChanged();
            }
        }

        internal IEnumerable<SceneCollection> openAdditiveCollections
        {
            get
            {
#if UNITY_EDITOR
                return SceneManager.settings.user.m_additiveCollections;
#else
                return m_additiveCollections;
#endif
            }
        }

        internal void AddAdditiveCollection(SceneCollection collection)
        {
#if UNITY_EDITOR
            SceneManager.settings.user.m_additiveCollections.Add(collection);
            SceneManager.settings.user.m_additiveCollections.RemoveAll(c => !c);
            SceneManager.settings.user.Save();
#else
            m_additiveCollections.Add(collection);
#endif
        }

        internal void RemoveAdditiveCollection(SceneCollection collection)
        {
#if UNITY_EDITOR
            SceneManager.settings.user.m_additiveCollections.Remove(collection);
            SceneManager.settings.user.m_additiveCollections.RemoveAll(c => !c);
            SceneManager.settings.user.Save();
#else
            m_additiveCollections.Remove(collection);
#endif
        }

        internal void ClearAdditiveCollections()
        {
#if UNITY_EDITOR
            SceneManager.settings.user.m_additiveCollections.Clear();
            SceneManager.settings.user.Save();
#else
            m_additiveCollections.Clear();
#endif
        }

        #endregion
        #region Fade scene

        [SerializeField] private Scene m_fadeScene;

        /// <summary>Specifies the scene to use for certain methods, i.e. <see cref="LoadingScreenUtility.FadeOut(float, Color?)"/>.</summary>
        public Scene fadeScene
        {
            get => m_fadeScene;
            set { m_fadeScene = value; OnPropertyChanged(); }
        }

        #endregion
        #region Allow loading scenes in parallel

        [SerializeField] private bool m_allowLoadingScenesInParallel;

        /// <summary>Specifies if scenes should be loaded in parallel, rather than sequentially.</summary>
        public bool allowLoadingScenesInParallel
        {
            get => m_allowLoadingScenesInParallel;
            set { m_allowLoadingScenesInParallel = value; OnPropertyChanged(); }
        }

        #endregion
        #region Updates

        [SerializeField] private bool m_allowUpdateCheck = true;

        /// <summary>Gets or sets whether ASM is allowed to check for updates.</summary>
        public bool allowUpdateCheck
        {
            get => m_allowUpdateCheck;
            set { m_allowUpdateCheck = value; OnPropertyChanged(); }
        }

        #endregion
        #region Discoverable cache

        internal DiscoverablesCache discoverablesCache => DiscoverablesCache.instance;

        #endregion
        #region ASM Toolbar

        [SerializeField] private bool m_toolbarButtonVisible = true;
        [SerializeField] private bool m_toolbarEnabled = true;
        [SerializeField] private InputBinding[] m_toolbarBindings;

        /// <summary>Specifies whatever the ASM toolbar button should be visible.</summary>
        public bool toolbarButtonVisible
        {
            get => m_toolbarButtonVisible;
            set { m_toolbarButtonVisible = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies whatever the ASM toolbar should be enabled.</summary>
        public bool toolbarEnabled
        {
            get => m_toolbarEnabled;
            set { m_toolbarEnabled = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies the bindings to open the ASM toolbar, if enabled.</summary>
        public InputBinding[] toolbarBindings
        {
            get => m_toolbarBindings;
            set { m_toolbarBindings = value; OnPropertyChanged(); }
        }

        #endregion

        #endregion

    }

}

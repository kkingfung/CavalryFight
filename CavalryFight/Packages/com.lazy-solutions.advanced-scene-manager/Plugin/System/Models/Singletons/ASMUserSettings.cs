#if UNITY_EDITOR

using AdvancedSceneManager.DependencyInjection.Editor;
using AdvancedSceneManager.Editor.UI;
using AdvancedSceneManager.Models.Interfaces;
using AdvancedSceneManager.Models.Internal;
using AdvancedSceneManager.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Serialization;

namespace AdvancedSceneManager.Models
{

    /// <summary>Specifies an interval for how often to check for ASM updates.</summary>
    public enum UpdateInterval
    {
        /// <summary>Automatically determine update interval.</summary>
        Auto,

        /// <summary>Never check for updates.</summary>
        Never,

        /// <summary>Check for updates every hour.</summary>
        EveryHour,

        /// <summary>Check for updates every 3 hours.</summary>
        Every3Hours,

        /// <summary>Check for updates every 6 hours.</summary>
        Every6Hours,

        /// <summary>Check for updates every 12 hours.</summary>
        Every12Hours,

        /// <summary>Check for updates every 24 hours.</summary>
        Every24Hours,

        /// <summary>Check for updates every 48 hours.</summary>
        Every48Hours,

        /// <summary>Check for updates every week.</summary>
        EveryWeek
    }

    /// <summary>Represents a pair of a scene collection and an index, used to reference a specific scene relative to its parent collection.</summary>
    [Serializable]
    public class CollectionScenePair
    {
        [SerializeField] private string m_collectionID;
        [SerializeField] private string m_sceneID;

        [NonSerialized] private ISceneCollection _collection;
        [NonSerialized] private Scene _scene;

        /// <summary>Gets or sets the scene collection.</summary>
        public ISceneCollection collection
        {
            get
            {
                if (_collection == null && !string.IsNullOrEmpty(m_collectionID))
                    _collection = SceneCollection.FindCollectionAll(m_collectionID, activeProfile: false);
                return _collection;
            }
            set
            {
                _collection = value;
                m_collectionID = value?.id;
            }
        }

        /// <summary>Gets or sets the scene.</summary>
        public Scene scene
        {
            get
            {
                if (!_scene && !string.IsNullOrEmpty(m_sceneID))
                    _scene = Scene.Find(m_sceneID);
                return _scene;
            }
            set
            {
                _scene = value;
                m_sceneID = value ? value.id : null;
            }
        }

        /// <summary>Gets or sets the index of the scene within its collection.</summary>
        public int sceneIndex;
    }

    /// <summary>Contains settings that are stored locally, that aren't synced to source control.</summary>
    /// <remarks>Only available in editor.</remarks>
    [ASMFilePath("UserSettings/AdvancedSceneManager.asset")]
    public class ASMUserSettings : ASMScriptableSingleton<ASMUserSettings>, INotifyPropertyChanged, IUserSettings, ISerializationCallbackReceiver
    {

        /// <inheritdoc />
        public override bool editorOnly => true;

        #region ISerializationCallbackReceiver

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            FixupExtendableButtons();
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            UpdateExtendableButtons();
        }

        #endregion
        #region Menu popup

        [Header("Menu Popup")]
        [SerializeField] private string m_quickBuildPath;
        [SerializeField] private bool m_quickBuildUseProfiler;

        internal string quickBuildPath
        {
            get => m_quickBuildPath;
            set { m_quickBuildPath = value; OnPropertyChanged(); }
        }

        internal bool quickBuildUseProfiler
        {
            get => m_quickBuildUseProfiler;
            set { m_quickBuildUseProfiler = value; OnPropertyChanged(); }
        }

        #endregion
        #region Startup

        [Header("Startup")]
        [SerializeField] private Profile m_activeProfile;
        [SerializeField] private bool m_startupProcessOnCollectionPlay = true;
        [SerializeField] private bool m_splashDisplayInEditor = true;

        /// <summary>Specifies the active profile in editor.</summary>
        public Profile activeProfile
        {
            get => m_activeProfile;
            set { m_activeProfile = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies whatever startup process should run when pressing collection play button.</summary>
        public bool startupProcessOnCollectionPlay
        {
            get => m_startupProcessOnCollectionPlay;
            set { m_startupProcessOnCollectionPlay = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies whatever splash scene should be displayed in the editor.</summary>
        public bool splashDisplayInEditor
        {
            get => m_splashDisplayInEditor;
            set { m_splashDisplayInEditor = value; OnPropertyChanged(); }
        }

        #endregion
        #region Logging

        [Header("Logging")]
        [SerializeField] private bool m_logImport;
        [SerializeField] private bool m_logTracking;
        [SerializeField] private bool m_logLoading;
        [SerializeField] private bool m_logStartup;
        [SerializeField] private bool m_logOperation;
        [SerializeField] private bool m_logBuildScenes;

        /// <summary>Specifies whatever ASM should log when a <see cref="ASMModelBase"/> is imported.</summary>
        public bool logImport
        {
            get => m_logImport;
            set { m_logImport = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies whatever ASM should log when a scene is tracked after loaded.</summary>
        public bool logTracking
        {
            get => m_logTracking;
            set { m_logTracking = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies whatever ASM should log when a scene is loaded.</summary>
        public bool logLoading
        {
            get => m_logLoading;
            set { m_logLoading = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies whatever ASM should log during startup.</summary>
        public bool logStartup
        {
            get => m_logStartup;
            set { m_logStartup = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies whatever ASM should log during scene operations.</summary>
        public bool logOperation
        {
            get => m_logOperation;
            set { m_logOperation = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies whatever ASM should log when build scene list is updated.</summary>
        public bool logBuildScenes
        {
            get => m_logBuildScenes;
            set { m_logBuildScenes = value; OnPropertyChanged(); }
        }

        #endregion
        #region Netcode

#pragma warning disable CS0414
        [Header("Netcode")]
        [SerializeField] private bool m_displaySyncedIndicator = true;
#pragma warning restore CS0414

#if NETCODE

        /// <summary>Specifies that the 'synced' hierarchy indicator should be shown for synced scenes when using netcode.</summary>
        public bool displaySyncedIndicator
        {
            get => m_displaySyncedIndicator;
            set { m_displaySyncedIndicator = value; OnPropertyChanged(); }
        }

#endif

        #endregion
        #region Runtime

        [SerializeField] private bool m_clearCollectionWhenEnteringPlayMode = true;
        [SerializeField] internal SceneCollection m_openCollection;
        [SerializeField] internal List<SceneCollection> m_additiveCollections = new();

        /// <summary>Specifies whatever ASM should clear open collection when entering play mode.</summary>
        public bool clearCollectionWhenEnteringPlayMode
        {
            get => m_clearCollectionWhenEnteringPlayMode;
            set => m_clearCollectionWhenEnteringPlayMode = value;
        }

        #endregion
        #region Collection overlay

        [SerializeField, FormerlySerializedAs("pinnedOverlayCollections")] private List<SceneCollection> m_pinnedOverlayCollections = new();

        /// <summary>Enumerates the pinned collections in the collection overlay.</summary>
        public IEnumerable<SceneCollection> pinnedOverlayCollections => m_pinnedOverlayCollections;

        /// <summary>Pins a collection to the collection overlay.</summary>
        public void PinCollectionToOverlay(SceneCollection collection, int? index = null)
        {
            m_pinnedOverlayCollections.Remove(collection);
            if (index.HasValue)
                m_pinnedOverlayCollections.Insert(Math.Clamp(index.Value, 0, m_pinnedOverlayCollections.Count - 1), collection);
            else
                m_pinnedOverlayCollections.Add(collection);
            Save();
            OnPropertyChanged(nameof(pinnedOverlayCollections));
        }

        /// <summary>Unpins a collection from the collection overlay.</summary>
        public void UnpinCollectionFromOverlay(SceneCollection collection)
        {
            m_pinnedOverlayCollections.Remove(collection);
            Save();
            OnPropertyChanged(nameof(pinnedOverlayCollections));
        }

        #endregion
        #region Always save scenes when entering play mode

        [SerializeField] private bool m_alwaysSaveScenesWhenEnteringPlayMode;

        /// <summary>Specifies whatever scenes should always auto save when entering play mode using ASM play button.</summary>
        public bool alwaysSaveScenesWhenEnteringPlayMode
        {
            get => m_alwaysSaveScenesWhenEnteringPlayMode;
            set { m_alwaysSaveScenesWhenEnteringPlayMode = value; OnPropertyChanged(); }
        }

        #endregion
        #region Indicators

        [SerializeField] private bool m_displayHierarchyIndicators = true;
        [SerializeField] private float m_hierarchyIndicatorsOffset = 0;

        [SerializeField] internal bool m_defaultSceneIndicator = true;
        [SerializeField] internal bool m_sceneLoaderIndicator = true;
        [SerializeField] internal bool m_collectionIndicator = true;
        [SerializeField] internal bool m_persistentIndicator = true;
        [SerializeField] internal bool m_untrackedIndicator = true;
        [SerializeField] internal bool m_unimportedIndicator = true;
        [SerializeField] internal bool m_testIndicator = true;
        [SerializeField] internal bool m_lockIndicator = true;

        /// <summary>Specifies whatever the hierarchy indicators should be visible.</summary>
        public bool displayHierarchyIndicators
        {
            get => m_displayHierarchyIndicators;
            set { m_displayHierarchyIndicators = value; OnPropertyChanged(); }
        }

        /// <summary>Gets or sets the offset ASM will use for hierarchy indicators.</summary>
        public float hierarchyIndicatorsOffset
        {
            get => m_hierarchyIndicatorsOffset;
            set { m_hierarchyIndicatorsOffset = value; OnPropertyChanged(); }
        }

        #endregion
        #region Scene manager window

        #region Appearance

        /// <summary>Represents data for a button in the ASM window.</summary>
        [Serializable]
        public class ButtonData
        {

            /// <summary>Gets or sets the name of the button.</summary>
            public string name;

            /// <summary>Gets or sets the location of the button.</summary>
            public ElementLocation location;

            /// <summary>Gets or sets the index of the button.</summary>
            public int index = int.MaxValue;

            /// <summary>Gets or sets whether the button is visible.</summary>
            public bool isVisible = true;

            /// <inheritdoc />
            public override string ToString()
            {
                return $"{{name = {name}, index = {index}, isVisible = {isVisible}}}";
            }

        }

        [Header("Appearance")]
        [SerializeField] private bool m_displaySceneTooltips = true;
        [SerializeField] private int m_toolbarButtonCount = 1;
        [SerializeField] private float m_toolbarPlayButtonOffset = 0;
        [SerializeField] private SerializableDictionary<int, SceneCollection> m_toolbarButtonActions = new();
        [SerializeField] private SerializableDictionary<int, bool> m_toolbarButtonActions2 = new();
        [SerializeField] internal List<ButtonData> m_extendableButtons = new();

        [SerializeField] private bool m_displayCollectionAddButton = true;
        [SerializeField] private bool m_displayCollectionTemplatesButton = true;

        [SerializeField] private bool m_displayDynamicCollectionAddButton = true;
        [SerializeField] private bool m_displayDynamicCollectionMenuButton = true;

#if ASM_DEV
        //Fix for dev, upgrade works in one direction but not the other. Switching to patch branch is annoying when button config disappears.
        [SerializeField, FormerlySerializedAs("m_extendableButtons")] internal List<ButtonData> m_extendableButtonsNew = new();
#endif

        /// <summary>Specifies whatever SceneField will display tooltips.</summary>
        public bool displaySceneTooltips
        {
            get => m_displaySceneTooltips;
            set { m_displaySceneTooltips = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies the collection add hover button should be displayed.</summary>
        public bool displayCollectionAddButton
        {
            get => m_displayCollectionAddButton;
            set { m_displayCollectionAddButton = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies the collection templates hover button should be displayed.</summary>
        public bool displayCollectionTemplatesButton
        {
            get => m_displayCollectionTemplatesButton;
            set { m_displayCollectionTemplatesButton = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies the dynamic collection add hover button should be displayed.</summary>
        public bool displayDynamicCollectionAddButton
        {
            get => m_displayDynamicCollectionAddButton;
            set { m_displayDynamicCollectionAddButton = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies the dynamic collection menu hover button should be displayed.</summary>
        public bool displayDynamicCollectionMenuButton
        {
            get => m_displayDynamicCollectionMenuButton;
            set { m_displayDynamicCollectionMenuButton = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies how many buttons should be placed in toolbar.</summary>
        /// <remarks>Only has an effect if https://github.com/marijnz/unity-toolbar-extender is installed.</remarks>
        public int toolbarButtonCount
        {
            get => m_toolbarButtonCount;
            set { m_toolbarButtonCount = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies offset for toolbar play buttons.</summary>
        /// <remarks>Only has an effect if https://github.com/marijnz/unity-toolbar-extender is installed.</remarks>
        public float toolbarPlayButtonOffset
        {
            get => m_toolbarPlayButtonOffset;
            set { m_toolbarPlayButtonOffset = value; OnPropertyChanged(); }
        }

        /// <summary>Sets the scene collection to open for the specified toolbar button, if any.</summary>
        /// <remarks>Only has an effect if https://github.com/marijnz/unity-toolbar-extender is installed.</remarks>
        public void ToolbarAction(int i, out SceneCollection collection, out bool runStartupProcess)
        {
            collection = m_toolbarButtonActions?.GetValueOrDefault(i);
            runStartupProcess = m_toolbarButtonActions2?.GetValueOrDefault(i) ?? true;
        }

        /// <summary>Sets the scene collection to open for the specified toolbar button, if any.</summary>
        /// <remarks>Only has an effect if https://github.com/marijnz/unity-toolbar-extender is installed.</remarks>
        public void ToolbarAction(int i, SceneCollection collection, bool runStartupProcess)
        {

            if (m_toolbarButtonActions == null)
                m_toolbarButtonActions = new SerializableDictionary<int, SceneCollection>();

            if (m_toolbarButtonActions2 == null)
                m_toolbarButtonActions2 = new SerializableDictionary<int, bool>();

            m_toolbarButtonActions.Set(i, collection);
            m_toolbarButtonActions2.Set(i, runStartupProcess);

            this.Save();

        }

        void FixupExtendableButtons()
        {
            m_extendableButtons.RemoveAll(b => string.IsNullOrEmpty(b.name));
        }

        #region Update

        [SerializeField]
        [FormerlySerializedAs("m_extendableButtons")]
        internal SerializableDictionary<string, bool> m_extendableButtonsOld;

        [SerializeField]
        [FormerlySerializedAs("m_extendableButtonOrder")]
        internal SerializableDictionary<ElementLocation, string[]> m_extendableButtonOrderOld;

        void UpdateExtendableButtons()
        {
            if ((m_extendableButtonsOld == null || m_extendableButtonsOld.Count == 0) &&
                (m_extendableButtonOrderOld == null || m_extendableButtonOrderOld.Count == 0))
                return;

            Dictionary<string, (ElementLocation location, int index)> nameToLocationAndIndex = new();

            // Build map from button name → (location, index)
            if (m_extendableButtonOrderOld != null)
            {
                foreach (var kvp in m_extendableButtonOrderOld)
                {
                    var location = kvp.Key;
                    var names = kvp.Value;

                    for (int i = 0; i < names.Length; i++)
                    {
                        nameToLocationAndIndex[names[i]] = (location, i);
                    }
                }
            }

            // Unlikely that user already has data in new format, so lets just ignore that possibility
            m_extendableButtons.Clear();

            if (m_extendableButtonsOld != null)
            {
                foreach (var kvp in m_extendableButtonsOld)
                {
                    string name = kvp.Key;
                    bool isVisible = kvp.Value;

                    nameToLocationAndIndex.TryGetValue(name, out var locData);

                    var button = new ButtonData
                    {
                        name = name,
                        isVisible = isVisible,
                        location = locData.location,
                        index = locData.index != 0 ? locData.index : int.MaxValue
                    };

                    m_extendableButtons.Add(button);
                }
            }

            // Clear old data
            m_extendableButtonsOld = null;
            m_extendableButtonOrderOld = null;

            Save();
        }

        #endregion

        #endregion

        [SerializeField] private bool m_alwaysDisplaySearch;
        [SerializeField] private bool m_keepSceneUIInMemoryWhenCollectionCollapsed;

        [SerializeField] internal bool m_hideDocsNotification;
        [SerializeField] internal bool m_hideGitIgnoreNotification;
        [SerializeField] internal List<string> m_mutedNotifications = new();
        [SerializeField] internal List<CollectionScenePair> m_selectedScenes = new();
        [SerializeField] internal List<CollectionScenePair> m_selectedCollections = new();
        [SerializeField] internal List<string> m_expandedCollections = new();
        [SerializeField] internal ListSortDirection m_sortDirection;

        /// <summary>The saved searches in scene manager window.</summary>
        [SerializeField] internal string[] savedSearches;

        /// <summary>Determines whatever search should always be displayed, and not just when actively searching.</summary>
        public bool alwaysDisplaySearch
        {
            get => m_alwaysDisplaySearch;
            set { m_alwaysDisplaySearch = value; OnPropertyChanged(); }
        }

        /// <summary>Whether to keep scene UI elements in memory when a collection is collapsed.</summary>
        /// <remarks>Uses more memory when enabled but reduces delay when expanding collections.</remarks>
        public bool keepSceneUIInMemoryWhenCollectionCollapsed
        {
            get => m_keepSceneUIInMemoryWhenCollectionCollapsed;
            set { m_keepSceneUIInMemoryWhenCollectionCollapsed = value; OnPropertyChanged(); }
        }

        [SerializeField] internal SerializableDictionary<string, float> scrollPositions = new();
        [SerializeField] internal bool hasCompletedWelcomeWizard;

        #endregion
        #region Updates

        [SerializeField] private UpdateInterval m_updateInterval;
        [SerializeField] private string m_lastPatchWhenNotified;
        [SerializeField] private string m_cachedLatestVersion;
        [SerializeField] private string m_cachedPatchNotes;
        [SerializeField] private string m_lastUpdateCheck;

        /// <summary>Gets or sets the interval for checking ASM updates.</summary>
        public UpdateInterval updateInterval
        {
            get => m_updateInterval;
            set { m_updateInterval = value; OnPropertyChanged(); }
        }

        /// <summary>Gets or sets the last patch version when user was notified.</summary>
        public string lastPatchWhenNotified
        {
            get => m_lastPatchWhenNotified;
            set { m_lastPatchWhenNotified = value; OnPropertyChanged(); }
        }

        /// <summary>Gets or sets the cached latest version information.</summary>
        public string cachedLatestVersion
        {
            get => m_cachedLatestVersion;
            set { m_cachedLatestVersion = value; OnPropertyChanged(); }
        }

        /// <summary>Gets or sets the cached patch notes.</summary>
        public string cachedPatchNotes
        {
            get => m_cachedPatchNotes;
            set { m_cachedPatchNotes = value; OnPropertyChanged(); }
        }

        /// <summary>Gets or sets the timestamp of the last update check.</summary>
        public string lastUpdateCheck
        {
            get => m_lastUpdateCheck;
            set { m_lastUpdateCheck = value; OnPropertyChanged(); }
        }

        #endregion

        [Header("Misc")]
        [SerializeField] private bool m_openCollectionOnSceneAssetOpen;

        /// <summary>When <see langword="true"/>: opens the first found collection that a scene is contained in when opening an SceneAsset in editor.</summary>
        public bool openCollectionOnSceneAssetOpen
        {
            get => m_openCollectionOnSceneAssetOpen;
            set { m_openCollectionOnSceneAssetOpen = value; OnPropertyChanged(); }
        }

    }

}
#endif

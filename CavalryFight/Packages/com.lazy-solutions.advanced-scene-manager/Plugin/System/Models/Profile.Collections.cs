using AdvancedSceneManager.Models.Enums;
using AdvancedSceneManager.Models.Interfaces;
using AdvancedSceneManager.Models.Utility;
using AdvancedSceneManager.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using AdvancedSceneManager.Callbacks.Events.Editor;
#endif

namespace AdvancedSceneManager.Models
{

    partial class Profile
    {

        void OnEnable_Collections()
        {
#if UNITY_EDITOR

            SceneManager.events.RegisterCallback<ScenesAvailableForImportChangedEvent>(ReloadDynamicCollectionPaths);

            if (EditorUtility.IsPersistent(this))
            {
                if (!standaloneScenes)
                    AddCollection(CreateInstance<StandaloneCollection>());
            }

#endif
        }

        void OnDisable_Collections()
        {
#if UNITY_EDITOR
            SceneManager.events.UnregisterCallback<ScenesAvailableForImportChangedEvent>(ReloadDynamicCollectionPaths);
#endif
        }

        #region Pre-3.0 fields

        //Fields kept so we can upgrade cleanly

#if UNITY_EDITOR
        [SerializeField, FormerlySerializedAs("m_dynamicCollections")] internal List<AfterUpdateUtility.OldDynamicCollection> m_oldDynamicCollections;
        [SerializeField, FormerlySerializedAs("m_standaloneDynamicCollection")] internal AfterUpdateUtility.OldStandaloneCollection m_oldStandaloneCollection;
#else
        [System.NonSerialized] internal object m_oldDynamicCollections;
        [System.NonSerialized] internal object m_oldStandaloneCollection;
#endif

        #endregion

        [SerializeField] internal List<ScriptableObject> m_removedCollections = new();

        [SerializeField] internal List<SceneCollection> m_collections = new();
        [SerializeField] internal List<DynamicCollection> m_dynamicCollectionsList = new();
        [SerializeField] internal StandaloneCollection m_standaloneCollection;

        [SerializeField] internal DefaultASMScenesCollection m_defaultASMScenes;

        [SerializeField] private bool m_unloadUnusedAssetsForStandalone = true;

        /// <summary>Enable or disable ASM calling <see cref="Resources.UnloadUnusedAssets"/> after standalone scenes has been opened or closed.</summary>
        public bool unloadUnusedAssetsForStandalone
        {
            get => m_unloadUnusedAssetsForStandalone;
            set { m_unloadUnusedAssetsForStandalone = value; OnPropertyChanged(); }
        }

        /// <summary>Gets the collections that will be opened on startup.</summary>
        /// <remarks>If no collection is explicitly defined to be opened during startup, then the first available collection in list will be returned.</remarks>
        public IEnumerable<SceneCollection> startupCollections
        {
            get
            {

                var collections = m_collections.Where(c => c && c.isIncluded && c.startupOption == CollectionStartupOption.Open).ToArray();
                if (collections.Length > 0)
                    foreach (var c in collections)
                        yield return c;

                else if (this.collections.FirstOrDefault(c => c && c.isIncluded && c.startupOption == CollectionStartupOption.Auto) is SceneCollection collection && collection)
                    yield return collection;

            }
        }

        bool IsIncluded(ISceneCollection collection)
        {
            if (!Contains(collection))
                return false;

            if (collection is SceneCollection c)
                return c.isIncluded;

            return true;
        }

        /// <summary>Gets the collections contained within this profile.</summary>
        public IEnumerable<SceneCollection> collections => m_collections;

        /// <summary>Gets the dynamic collections contained within this profile.</summary>
        public IEnumerable<DynamicCollection> dynamicCollections => m_dynamicCollectionsList.NonNull();

        /// <summary>Gets the standalone scenes contained within this profile.</summary>
        public StandaloneCollection standaloneScenes => m_standaloneCollection;

        /// <summary>Gets the default asm scenes collection contained within this profile.</summary>
        /// <remarks>May be automatically re-added after remove, if samples are manually imported.</remarks>
        public DefaultASMScenesCollection defaultASMScenes => m_defaultASMScenes;

        /// <summary>Gets the scenes flagged to open on startup.</summary>
        public IEnumerable<Scene> startupScenes => standaloneScenes ? standaloneScenes.NonNull().Where(s => s.openOnStartup) : Enumerable.Empty<Scene>();

        /// <summary>Gets all removed collections in this profile.</summary>
        /// <remarks>Removed collections still exist until deleted, and may be manually opened, but they will not be listed in <see cref="collections"/> or <see cref="dynamicCollections"/>.</remarks>
        public IEnumerable<ISceneCollection> removedCollections => m_removedCollections.OfType<ISceneCollection>();

        /// <summary>Gets <see cref="collections"/>, <see cref="standaloneScenes"/>, <see cref="defaultASMScenes"/>, <see cref="dynamicCollections"/>.</summary>
        public IEnumerable<ISceneCollection> allCollections => GetAllCollections().NonNull();

        IEnumerable<ISceneCollection> GetAllCollections()
        {

            foreach (var collection in m_collections)
                yield return collection;

            yield return m_standaloneCollection;
            yield return m_defaultASMScenes;

            foreach (var collection in m_dynamicCollectionsList)
                yield return collection;

        }

#if UNITY_EDITOR
        void ReloadDynamicCollectionPaths(ScenesAvailableForImportChangedEvent e)
        {
            foreach (var collection in dynamicCollections)
                collection.ReloadPaths();
        }
#endif

        /// <summary>Gets if the specified collection is a startup collection.</summary>
        public bool IsStartupCollection(SceneCollection collection) =>
            startupCollections.Contains(collection);

        /// <summary>Gets the index of the specified collection.</summary>
        public int IndexOf(SceneCollection collection) => m_collections.IndexOf(collection);

        /// <summary>Gets the index of the specified collection.</summary>
        public int IndexOf(DynamicCollection collection) => m_dynamicCollectionsList.IndexOf(collection);

        /// <summary>Gets whatever this profile contains the specified collection.</summary>
        public bool Contains(ISceneCollection collection, bool checkRemoved = false) =>
            allCollections.Contains(collection) || (checkRemoved && removedCollections.Contains(collection));

        /// <summary>Finds all collection that the scene is included in. Includes dynamic collections.</summary>
        public IEnumerable<ISceneCollection> FindCollections(Scene scene) =>
            scene
            ? allCollections.Where(c => c.Contains(scene))
            : Enumerable.Empty<ISceneCollection>();

#if UNITY_EDITOR
        /// <summary>Gets all collections saved as sub assets of this profile, that are not referenced in it.</summary>
        /// <remarks>Only available in editor.</remarks>
        public IEnumerable<ISceneCollection> FindUntrackedCollections()
        {
            var collections = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(this)).OfType<ISceneCollection>().Where(c => !Contains(c) && !removedCollections.Contains(c)).ToList();
            return collections;
        }
#endif

    }

}

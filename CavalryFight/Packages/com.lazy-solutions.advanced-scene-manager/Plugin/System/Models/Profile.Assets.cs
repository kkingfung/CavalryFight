using AdvancedSceneManager.Models.Enums;
using AdvancedSceneManager.Models.Interfaces;
using AdvancedSceneManager.Models.Internal;
using AdvancedSceneManager.Models.Utility;
using AdvancedSceneManager.Utility;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
using AdvancedSceneManager.Callbacks.Events.Editor;
#endif

namespace AdvancedSceneManager.Models
{

    partial class Profile
    {

#if UNITY_EDITOR

        #region Profile

        internal void SetDefaults()
        {
            loadingScene = SceneManager.assets.defaults.fadeScene;
            splashScene = SceneManager.assets.defaults.splashASMScene;
        }

        void AddDefaultCollections()
        {
            this.AddCollection(CreateInstance<DefaultASMScenesCollection>());
            AddCollection("Startup (persistent)", true);
            AddCollection("Main menu");

            void AddCollection(string title, bool openAsPersistent = false)
            {
                var collection = CreateCollection(title);
                collection.startupOption = CollectionStartupOption.Open;
                collection.openAsPersistent = openAsPersistent;
                collection.Save();
            }
        }

        /// <summary>Creates a new profile, with default scenes and collections.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static Profile Create(string name)
        {

            var profile = CreateInternal<Profile>(name);
            SceneManager.assetImport.Add(profile);

            try
            {

                profile.suppressSave = true;

                profile.SetDefaults();
                profile.AddDefaultCollections();
                profile.AddCollection(CreateInstance<StandaloneCollection>());

            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
            finally
            {
                profile.suppressSave = false;
            }

            return profile;

        }

        /// <summary>Creates a new empty profile.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static Profile CreateEmpty(string name, bool useDefaultSpecialScenes = true)
        {

            var profile = CreateInternal<Profile>(name);
            SceneManager.assetImport.Add(profile);

            try
            {

                profile.suppressSave = true;

                if (useDefaultSpecialScenes)
                    profile.SetDefaults();

                profile.AddCollection(CreateInstance<StandaloneCollection>());

            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
            finally
            {
                profile.suppressSave = false;
            }

            return profile;

        }

        /// <summary>Deletes the specified profile.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static void Delete(Profile profile)
        {

            if (ProfileUtility.active == profile)
                ProfileUtility.SetProfile(null);

            SceneManager.assetImport.Remove(profile);

        }

        /// <summary>Duplicate the specified profile.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static void Duplicate(Profile profile)
        {

            var p = Instantiate(profile);

            p.m_removedCollections.Clear();
            p.m_collections.Clear();
            p.m_dynamicCollectionsList.Clear();
            p.m_defaultASMScenes = null;
            p.m_standaloneCollection = null;

            p.SaveNow();

            try
            {

                p.suppressSave = true;
                p.m_id = GenerateID();

                SceneManager.assetImport.Add(p);

                foreach (var collection in profile.collections)
                    Copy(collection);

                foreach (var collection in profile.dynamicCollections)
                    Copy(collection);

                Copy(profile.standaloneScenes);
                Copy(profile.defaultASMScenes);

                void Copy(ISceneCollection collection)
                {

                    if (collection is not ScriptableObject obj || !obj)
                        return;

                    var c = Instantiate(obj);
                    p.AddCollection((ISceneCollection)c);
                    ((ASMModelBase)c).SetID(GenerateID());

                }

                p.Save();

            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
            finally
            {
                p.suppressSave = false;
            }

        }

        #endregion
        #region Create collection

        /// <summary>Creates a new collection with title 'New collection'.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void CreateCollection() =>
            CreateCollection(out _);

        /// <summary>Creates a new collection with title 'New collection'.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void CreateCollection(out SceneCollection collection) =>
            collection = CreateCollection("New collection");

        /// <summary>Create a collection and add it to this profile.</summary>
        /// <remarks>Only available in editor.</remarks>
        public SceneCollection CreateCollection(string title)
        {

            var collection = CreateInternal<SceneCollection>(title);
            collection.SetTitleAfterCreation(prefix, title);

            if (!collection)
                throw new InvalidOperationException("Something went wrong creating collection.");

            AddCollection(collection);

            return collection;

        }

        /// <summary>Create a collection from a template.</summary>
        /// <remarks>Only available in editor.</remarks>
        public SceneCollection CreateCollection(SceneCollectionTemplate template)
        {

            if (!template)
                throw new ArgumentNullException(nameof(template));

            var collection = CreateInternal<SceneCollection>(template.title);
            var id = collection.id;
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(template), collection);
            collection.m_id = id;

            AddCollection(collection);

            return collection;

        }

        /// <summary>Creates a dynamic collection with default values.</summary>
        public void CreateDynamicCollection() =>
            CreateDynamicCollection("", "New dynamic collection");

        /// <summary>Creates a dynamic collection with the specified path and optional title.</summary>
        public DynamicCollection CreateDynamicCollection(string path, string title)
        {

            var collection = CreateInternal<DynamicCollection>(title);
            collection.path = path;
            collection.m_title = title;

            if (!collection)
                throw new InvalidOperationException("Something went wrong creating collection.");

            AddCollection(collection);

            return collection;

        }

        #endregion
        #region Collection

        void AddCollectionInternal(ISceneCollection collection, bool isRestore = false)
        {

            if (!EditorUtility.IsPersistent((ScriptableObject)collection))
                AssetDatabase.AddObjectToAsset((ScriptableObject)collection, this);

            if (collection is SceneCollection c)
                m_collections.Add(c);
            else if (collection is DynamicCollection dc)
                m_dynamicCollectionsList.Add(dc);
            else if (collection is StandaloneCollection sc)
                m_standaloneCollection = sc;
            else if (collection is DefaultASMScenesCollection asm)
            {
                m_defaultASMScenes = asm;
                OnPropertyChanged(nameof(defaultASMScenes));
            }

            if (collection is not SceneCollection)
                ((ScriptableObject)collection).hideFlags = HideFlags.HideInHierarchy;

            Save();

            if (isRestore)
                SceneManager.events.InvokeCallbackSync(new CollectionRestoredEvent(collection));
            else
                SceneManager.events.InvokeCallbackSync(new CollectionAddedEvent(collection));

        }

        void DeleteCollectionInternal(ISceneCollection collection)
        {

            if (collection is SceneCollection c)
                m_collections.Remove(c);
            else if (collection is DynamicCollection dc)
                m_dynamicCollectionsList.Remove(dc);
            else if (collection is DefaultASMScenesCollection)
            {
                m_defaultASMScenes = null;
                OnPropertyChanged(nameof(defaultASMScenes));
            }
            else if (collection is StandaloneCollection)
                throw new InvalidOperationException("Cannot remove the standalone collection.");

            m_removedCollections.Remove((ScriptableObject)collection);

            if (EditorUtility.IsPersistent((ScriptableObject)collection))
                AssetDatabase.RemoveObjectFromAsset((ScriptableObject)collection);

            Save();

            SceneManager.events.InvokeCallbackSync(new CollectionDeletedEvent(collection));

        }

        /// <summary>Adds a collection.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void AddCollection(ISceneCollection collection)
        {
            AddCollectionInternal(collection);
        }

        /// <summary>Removes a collection. Prompts undo.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void Remove(ISceneCollection collection)
        {

            if (collection is SceneCollection c && m_collections.Remove(c))
            {
                m_removedCollections.Add(c);
            }
            else if (collection is DynamicCollection dc && m_dynamicCollectionsList.Remove(dc))
            {
                m_removedCollections.Add(dc);
            }
            else if (collection is DefaultASMScenesCollection asm)
            {
                m_defaultASMScenes = null;
                m_removedCollections.Add(asm);
                OnPropertyChanged(nameof(defaultASMScenes));
            }
            else if (collection is StandaloneCollection)
                throw new InvalidOperationException("Cannot remove the standalone collection.");

            Save();

            SceneManager.events.InvokeCallbackSync(new CollectionRemovedEvent(collection));

        }

        /// <summary>Restores a collection that has been removed.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void Restore(ISceneCollection collection)
        {
            if (m_removedCollections.Remove((ScriptableObject)collection))
                AddCollectionInternal(collection, isRestore: true);
        }

        /// <summary>Deletes a collection. Does not prompt undo.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void Delete(ISceneCollection collection)
        {
            DeleteCollectionInternal(collection);
        }

        /// <summary>Clear removed collections.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void ClearRemovedCollections()
        {
            foreach (var collection in removedCollections.OfType<ISceneCollection>())
                DeleteCollectionInternal(collection);
        }

        /// <summary>Clear <see cref="collections"/>, <see cref="dynamicCollections"/>, <see cref="removedCollections"/>. Does not prompt undo.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void ClearCollections()
        {

            foreach (var collection in collections)
            {
                DeleteCollectionInternal(collection);
            }

            foreach (var collection in dynamicCollections)
            {
                DeleteCollectionInternal(collection);
            }

        }

        internal void RemoveDefaultASMScenes()
        {
            Remove(defaultASMScenes);
        }

        internal void AddDefaultASMScenes()
        {
            AddCollection(CreateInstance<DefaultASMScenesCollection>());
        }

        #endregion

#endif

    }

}

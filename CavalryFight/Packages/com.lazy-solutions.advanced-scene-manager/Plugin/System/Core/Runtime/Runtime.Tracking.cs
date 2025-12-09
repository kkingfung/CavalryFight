using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using System;
using System.Linq;

namespace AdvancedSceneManager.Core
{
    partial class Runtime
    {

        void InitializeTracking()
        {
            sceneClosed += Runtime_sceneClosed;

            foreach (var scene in SceneUtility.GetAllOpenUnityScenes())
            {
                if (scene.ASMScene(out var s))
                {
                    s.internalScene = s;
                    Track(scene, s);
                }
            }

        }

        void Runtime_sceneClosed(Scene scene)
        {
            var collections = openAdditiveCollections.Where(c => !c.scenes.Any(s => s && s.isOpen));
            foreach (var collection in collections.ToArray())
                Untrack(collection, isAdditive: true);
        }

        #region Scenes

        /// <summary>Tracks the specified scene as open.</summary>
        /// <remarks>Does not open scene.</remarks>
        public void Track(Scene scene, UnityEngine.SceneManagement.Scene unityScene)
        {

            if (!scene)
                return;

            if (!FallbackSceneUtility.IsFallbackScene(unityScene))
                scene.internalScene = unityScene;

            Track(scene);

        }

        /// <inheritdoc cref="Track(Scene, UnityEngine.SceneManagement.Scene)"/>
        public void Track(Scene scene)
        {

            if (!scene)
                return;

            if (!scene.internalScene.HasValue)
                FindAssociatedScene(scene);

            if (!scene.internalScene.HasValue)
                return;

            if (FallbackSceneUtility.IsFallbackScene(scene.internalScene ?? default))
            {
                scene.internalScene = null;
                return;
            }

            if (!m_openScenes.Contains(scene))
            {

                m_openScenes.Add(scene);
                scene.OnPropertyChanged(nameof(Scene.isOpen));
                sceneOpened?.Invoke(scene);
                scene.events.OnOpen?.Invoke(scene);
                scene.events.OnOpenChanged?.Invoke(true);

                LogUtility.LogTracked(scene);

            }

        }

        /// <summary>Untracks the specified scene as open.</summary>
        /// <remarks>Does not close scene.</remarks>
        public bool Untrack(Scene scene)
        {

            if (scene && m_openScenes.Remove(scene))
            {

                UntrackPreload(scene);

                scene.internalScene = null;

                scene.OnPropertyChanged(nameof(Scene.isOpen));
                sceneClosed?.Invoke(scene);
                scene.events.OnClose?.Invoke(scene);
                scene.events.OnOpenChanged?.Invoke(false);
                LogUtility.LogUntracked(scene);

                return true;

            }

            return false;

        }

        /// <summary>Untracks all open scenes.</summary>
        /// <remarks>Does not close scenes.</remarks>
        public void UntrackScenes()
        {
            foreach (var scene in m_openScenes.ToArray())
                Untrack(scene);
            m_openScenes.Clear();
        }

        void FindAssociatedScene(Scene scene)
        {
            scene.internalScene = SceneUtility.GetAllOpenUnityScenes().FirstOrDefault(s => s.IsValid() && s.path == scene.path);
            if (!scene.internalScene.HasValue)
                throw new InvalidOperationException("Cannot track scene without a associated unity scene.");
        }

        /// <summary>Tracks a scene that doesn't have a associated unity scene.</summary>
        public void ForceTrack(Scene scene)
        {

            if (!scene)
                return;

            if (FallbackSceneUtility.IsFallbackScene(scene.internalScene ?? default))
            {
                scene.internalScene = null;
                return;
            }

            if (!m_openScenes.Contains(scene))
            {

                m_openScenes.Add(scene);
                scene.OnPropertyChanged(nameof(Scene.isOpen));
                sceneOpened?.Invoke(scene);
                scene.events.OnOpen?.Invoke(scene);

                LogUtility.LogTracked(scene);

            }

        }

        #endregion
        #region Collections

        /// <summary>Tracks the collection as open.</summary>
        /// <remarks>Does not open collection.</remarks>
        public void Track(SceneCollection collection, bool isAdditive = false)
        {

            if (!collection)
                return;

            if (!isAdditive && collection != m_openCollection)
            {
                m_openCollection = collection;
                collection.OnPropertyChanged(nameof(collection.isOpenNonAdditive));
                collection.OnPropertyChanged(nameof(collection.isOpen));

                collectionOpened?.Invoke(collection);
                foreach (var scene in collection.NonNull())
                    scene.events.OnCollectionOpened?.Invoke(scene, collection);
                collection.events.OnOpen?.Invoke(collection);
                collection.events.OnOpenChanged?.Invoke(true);

                LogUtility.LogTracked(collection);
            }
            else if (isAdditive && !openAdditiveCollections.Contains(collection))
            {
                SceneManager.settings.project.AddAdditiveCollection(collection);
                collection.OnPropertyChanged(nameof(collection.isOpenAdditive));
                collection.OnPropertyChanged(nameof(collection.isOpen));
                LogUtility.LogTracked(collection, true);
            }

        }

        /// <summary>Untracks the collection.</summary>
        /// <remarks>Does not close the collection.</remarks>
        public void Untrack(SceneCollection collection, bool isAdditive = false)
        {

            if (!collection)
                return;

            if (!isAdditive && collection == openCollection)
            {

                collection.OnPropertyChanged(nameof(collection.isOpenNonAdditive));
                collection.OnPropertyChanged(nameof(collection.isOpen));

                collectionClosed?.Invoke(collection);
                foreach (var scene in collection.NonNull())
                    scene.events.OnCollectionClosed?.Invoke(scene, collection);
                collection.events.OnClose?.Invoke(collection);
                collection.events.OnOpenChanged?.Invoke(false);

                m_openCollection = null;
                //Debug.Log("collection set to null");

                LogUtility.LogUntracked(collection);

                //Untrack all additive collections
                //openAdditiveCollections.ToArray().ForEach(c => Untrack(c, true));

            }
            else if (isAdditive && openAdditiveCollections.Contains(collection))
            {
                SceneManager.settings.project.RemoveAdditiveCollection(collection);
                collection.OnPropertyChanged(nameof(collection.isOpenAdditive));
                collection.OnPropertyChanged(nameof(collection.isOpen));
                LogUtility.LogUntracked(collection, true);
            }

        }

        /// <summary>Untracks all collections.</summary>
        /// <remarks>Does not close collections.</remarks>
        public void UntrackCollections()
        {
            Untrack(openCollection);
            openAdditiveCollections.ForEach(c => Untrack(c, true));
        }

        #endregion

        /// <summary>Gets whatever this scene is tracked as open.</summary>
        public bool IsTracked(Scene scene)
        {

            if (!scene)
                return false;

            if (FallbackSceneUtility.IsFallbackScene(scene.internalScene ?? default))
                return true;

            if (scene.isDontDestroyOnLoad)
                return true;

            return scene && scene.internalScene.HasValue && scene.internalScene.Value.isLoaded;

        }

        /// <summary>Gets whatever this collection is tracked as open.</summary>
        public bool IsTracked(SceneCollection collection) =>
            openCollection == collection || openAdditiveCollections.Contains(collection);

    }
}

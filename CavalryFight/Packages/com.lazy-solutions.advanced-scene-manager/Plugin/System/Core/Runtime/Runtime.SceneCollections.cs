using AdvancedSceneManager.Callbacks.Events;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdvancedSceneManager.Core
{

    /// <summary>Contains extension methods for <see cref="SceneOperation"/>.</summary>
    static class SceneOperationExtensions
    {

        internal static SceneOperation TrackCollectionCallback(this SceneOperation operation, SceneCollection collection, bool isAdditive = false)
        {

            var openCollection = SceneManager.openCollection;

            operation.RegisterCallback<SceneClosePhaseEvent>(e =>
            {

                if (!openCollection)
                    return;

                SceneManager.runtime.Untrack(openCollection, isAdditive);

                //Make sure additive collection is removed when it is opened non-additively
                if (!isAdditive)
                    SceneManager.runtime.Untrack(openCollection, true);

                if (e.operation is not null)
                    e.WaitFor(e.operation.events.InvokeCallback(new CollectionCloseEvent(openCollection)));

            }, When.After);

            operation.RegisterCallback<SceneOpenPhaseEvent>(e =>
            {
                SceneManager.runtime.Track(collection, isAdditive);
                if (e.operation is not null)
                    e.WaitFor(e.operation.events.InvokeCallback(new CollectionOpenEvent(collection)));
            }, When.After);

            return operation;

        }

        internal static SceneOperation UntrackCollectionCallback(this SceneOperation operation, SceneCollection collection, bool isAdditive = false)
        {
            operation.RegisterCallback<SceneClosePhaseEvent>(e => SceneManager.runtime.Untrack(collection, isAdditive), When.After);
            return operation;
        }

        internal static SceneOperation UntrackAllCollectionsCallback(this SceneOperation operation)
        {
            operation.RegisterCallback<SceneClosePhaseEvent>(e => SceneManager.runtime.UntrackCollections(), When.After);
            return operation;
        }

    }

    partial class Runtime
    {

        private SceneCollection m_openCollection
        {
            get => SceneManager.settings.project.openCollection;
            set => SceneManager.settings.project.openCollection = value;
        }

        /// <summary>Gets the collections that are opened as additive.</summary>
        public IEnumerable<SceneCollection> openAdditiveCollections => SceneManager.settings.project.openAdditiveCollections.NonNull().Distinct();

        /// <summary>Gets the collection that is currently open.</summary>
        public SceneCollection openCollection => m_openCollection;

        #region Checks and scene list evaluation

        /// <summary>Checks if collection is open.</summary>
        /// <param name="collection">The collection to check.</param>
        /// <param name="additive">Checks both if null.</param>
        internal bool IsOpen(SceneCollection collection, bool? additive = null)
        {

            if (!collection || !collection.scenes.Any(IsOpen))
                return false;

            if (additive is null && !IsOpenNonAdditive() && !IsOpenAdditive())
                return false;

            else if (additive == false && !IsOpenNonAdditive())
                return false;

            else if (additive == true && !IsOpenAdditive())
                return false;

            if (!collection.scenes.NonNull().Any(s => s.isOpenInHierarchy))
                return false;

            return true;

            bool IsOpenNonAdditive() => openCollection == collection;
            bool IsOpenAdditive() => openAdditiveCollections.Contains(collection);

        }

        /// <summary>Evaluate the scenes that would close.</summary>
        public IEnumerable<Scene> EvalScenesToClose(SceneCollection closeCollection = null, SceneCollection nextCollection = null, SceneCollection additiveCloseCollection = null)
        {

            var list = additiveCloseCollection
                ? additiveCloseCollection.scenes.Distinct().Where(s => openCollection == null || !openCollection.Contains(s))
                : openScenes.Where(s => !openAdditiveCollections.Any(c => c.Contains(s)));

            list = list.Where(s => IsValid(s) && IsOpen(s) && NotLoadingScreen(s) && NotPersistent(s, closeCollection, nextCollection));

            if (SceneManager.settings.project.reverseUnloadOrderOnCollectionClose)
                list = list.Reverse();

            return list;

        }

        /// <summary>Evaluate the scenes that would open.</summary>
        /// <param name="collection">The collection to evaluate.</param>
        /// <param name="openAll">Opens scenes that have been flagged not to open in the ASM window, or using <see cref="SceneCollection.SetAutoOpen(Scene, bool)"/>.</param>
        public IEnumerable<Scene> EvalScenesToOpen(SceneCollection collection, bool openAll = false) =>
            collection
            ? collection.scenes.Distinct().Where(s => IsValid(s) && IsClosed(s) && CanOpen(s, collection, openAll))
            : Enumerable.Empty<Scene>();

        #endregion
        #region Open

        /// <summary>Opens the collection.</summary>
        /// <param name="collection">The collection to open.</param>
        /// <param name="openAll">Opens scenes that have been flagged not to open in the ASM window, or using <see cref="SceneCollection.SetAutoOpen(Scene, bool)"/>.</param>
        public SceneOperation Open(SceneCollection collection, bool openAll = false) =>
            Open(SceneOperation.Queue(), collection, openAll);

        /// <inheritdoc cref="Open(SceneCollection, bool)"/>
        internal SceneOperation Open(SceneOperation operation, SceneCollection collection, bool openAll = false)
        {

            if (IsOpen(collection))
                return SceneOperation.done;

            var scenesToOpen = EvalScenesToOpen(collection, openAll);
            var scenesToClose = EvalScenesToClose(nextCollection: collection);

            if (!scenesToOpen.Any() && !scenesToClose.Any())
            {
                Track(collection);
                SceneManager.events.InvokeCallback(new CollectionOpenEvent(collection)).StartCoroutine();
                return SceneOperation.done;
            }

            return operation.
                With(collection, true).
                TrackCollectionCallback(collection).
                Close(scenesToClose).
                Open(scenesToOpen);

        }

        /// <summary>Opens the collection without closing existing scenes.</summary>
        /// <param name="collection">The collection to open.</param>
        /// <param name="openAll">Opens scenes that have been flagged not to open in the ASM window, or using <see cref="SceneCollection.SetAutoOpen(Scene, bool)"/>.</param>
        public SceneOperation OpenAdditive(SceneCollection collection, bool openAll = false)
        {

            if (!collection)
                return SceneOperation.done;

            if (m_openCollection == collection)
            {
                Debug.LogError("Cannot open collection as additive if it is already open normally.");
                return SceneOperation.done;
            }

            if (IsOpen(collection))
                return SceneOperation.done;

            var scenesToOpen = EvalScenesToOpen(collection, openAll);

            if (!scenesToOpen.Any())
            {
                Track(collection, isAdditive: true);
                SceneManager.events.InvokeCallback(new CollectionOpenEvent(collection)).StartCoroutine();
                return SceneOperation.done;
            }

            return SceneOperation.Queue().
                With(collection, collection.setActiveSceneWhenOpenedAsAdditive).
                TrackCollectionCallback(collection, true).
                WithoutLoadingScreen().
                Open(scenesToOpen);

        }

        /// <summary>Opens the collection without closing existing scenes.</summary>
        /// <remarks>No effect if no additive collections could be opened. Note that <paramref name="activeCollection"/> will be removed from <paramref name="collections"/> if it is contained within.</remarks>
        public SceneOperation OpenAdditive(IEnumerable<SceneCollection> collections, SceneCollection activeCollection = null, Scene loadingScene = null)
        {

            collections = collections.Where(c => !c.isOpen).Except(activeCollection).NonNull();

            if (!collections.Any())
                return SceneOperation.done;

            var operation = SceneOperation.Queue().
                With(activeCollection, activeCollection.setActiveSceneWhenOpenedAsAdditive).
                With(loadingScene: loadingScene, useLoadingScene: loadingScene).
                Open(collections.SelectMany(c => c.scenes.
                    Distinct().
                    Where(IsValid).
                    Where(IsClosed).
                    Where(s => CanOpen(s, c, false))));

            if (activeCollection)
                operation.TrackCollectionCallback(activeCollection, true);

            return operation;

        }

        #endregion
        #region Close

        /// <summary>Closes <paramref name="collection"/>.</summary>
        public SceneOperation Close(SceneCollection collection) =>
            Close(SceneOperation.Queue(), collection);

        /// <inheritdoc cref="Close(SceneCollection)"/>
        public SceneOperation Close(SceneOperation operation, SceneCollection collection)
        {

            if (!collection)
                return SceneOperation.done;

            var scenes = EvalScenesToClose(collection, additiveCloseCollection: collection.isOpenAdditive ? collection : null);

            if (!scenes.Any())
            {
                if (collection.isOpen)
                {
                    SceneManager.events.InvokeCallback(new CollectionCloseEvent(collection)).StartCoroutine();
                    Untrack(collection, isAdditive: collection.isOpenAdditive);
                }
                return SceneOperation.done;
            }

            return operation.
                With(collection, isCloseOperation: true).
                UntrackCollectionCallback(collection, collection.isOpenAdditive).
                Close(scenes);

        }

        #endregion
        #region Toggle

        /// <summary>Toggles the collection open or closed.</summary>
        /// <param name="collection">The collection to toggle open state.</param>
        /// <param name="openAll">Opens scenes that have been flagged not to open in the ASM window, or using <see cref="SceneCollection.SetAutoOpen(Scene, bool)"/>.</param>
        public SceneOperation ToggleOpen(SceneCollection collection, bool openAll = false) =>
            IsOpen(collection)
            ? Close(collection)
            : Open(collection, openAll);

        #endregion
        #region Preload

        /// <summary>Preloads the collection.</summary>
        /// <param name="collection">The collection to toggle preload.</param>
        /// <param name="openAll">Opens scenes that have been flagged not to open in the ASM window, or using <see cref="SceneCollection.SetAutoOpen(Scene, bool)"/>.</param>
        public SceneOperation Preload(SceneCollection collection, bool openAll = false) =>
            PreloadInternal(collection, openAll, isAdditive: false);

        /// <summary>Preloads the collection as additive.</summary>
        /// <param name="collection">The collection to toggle preload.</param>
        /// <param name="openAll">Opens scenes that have been flagged not to open in the ASM window, or using <see cref="SceneCollection.SetAutoOpen(Scene, bool)"/>.</param>
        public SceneOperation PreloadAdditive(SceneCollection collection, bool openAll = false) =>
            PreloadInternal(collection, openAll, isAdditive: true);

        #endregion
        #region Reopen

        /// <summary>Reopens the collection.</summary>
        /// <param name="collection">The collection to reopen.</param>
        /// <param name="openAll">Opens scenes that have been flagged not to open in the ASM window, or using <see cref="SceneCollection.SetAutoOpen(Scene, bool)"/>.</param>
        public SceneOperation Reopen(SceneCollection collection, bool openAll = false)
        {

            if (collection.isOpenAdditive)
            {
                Debug.LogError("Additive collections cannot currently be reopened. Please close and then open manually.");
                return SceneOperation.done;
            }

            if (!collection.isOpen)
                return Open(collection, openAll);

            var scenes = openAll ? collection.scenesToAutomaticallyOpen : SceneManager.runtime.EvalScenesToClose(collection, collection);

            var close = SceneManager.settings.project.reverseUnloadOrderOnCollectionClose ? scenes.Reverse() : scenes;

            return SceneOperation.Queue().
                With(collection, true).
                Close(close.ToList()).
                Open(scenes.ToList()).
                UntrackCollectionCallback(collection).
                TrackCollectionCallback(collection);

        }

        #endregion

    }

}

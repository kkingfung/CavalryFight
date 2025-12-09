using AdvancedSceneManager.Core;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Enums;
using System;
using System.Collections.Generic;

namespace AdvancedSceneManager.DependencyInjection
{

    /// <inheritdoc cref="SceneManager.runtime"/>
    public interface IRuntime : ISceneManager
    { }

    /// <inheritdoc cref="SceneManager.runtime"/>
    public interface ISceneManager : DependencyInjectionUtility.IInjectable
    {

        /// <inheritdoc cref="Runtime.activeScene"/>
        Scene activeScene { get; }

        /// <inheritdoc cref="Runtime.currentOperation"/>
        SceneOperation currentOperation { get; }

        /// <inheritdoc cref="Runtime.dontDestroyOnLoad"/>
        Scene dontDestroyOnLoad { get; }

        /// <inheritdoc cref="Runtime.isBusy"/>
        bool isBusy { get; }

        /// <inheritdoc cref="Runtime.openAdditiveCollections"/>
        IEnumerable<SceneCollection> openAdditiveCollections { get; }

        /// <inheritdoc cref="Runtime.openCollection"/>
        SceneCollection openCollection { get; }

        /// <inheritdoc cref="Runtime.openScenes"/>
        IEnumerable<Scene> openScenes { get; }

        /// <inheritdoc cref="Runtime.preloadedScenes"/>
        IEnumerable<Scene> preloadedScenes { get; }

        /// <inheritdoc cref="Runtime.queuedOperations"/>
        IEnumerable<SceneOperation> queuedOperations { get; }

        /// <inheritdoc cref="Runtime.runningOperations"/>
        IEnumerable<SceneOperation> runningOperations { get; }

        /// <inheritdoc cref="Runtime.collectionClosed"/>
        event Action<SceneCollection> collectionClosed;

        /// <inheritdoc cref="Runtime.collectionOpened"/>
        event Action<SceneCollection> collectionOpened;

        /// <inheritdoc cref="Runtime.sceneClosed"/>
        event Action<Scene> sceneClosed;

        /// <inheritdoc cref="Runtime.sceneOpened"/>
        event Action<Scene> sceneOpened;

        /// <inheritdoc cref="Runtime.scenePreloaded"/>
        event Action<Scene> scenePreloaded;

        /// <inheritdoc cref="Runtime.scenePreloadFinished"/>
        event Action<Scene> scenePreloadFinished;

        /// <inheritdoc cref="Runtime.startedWorking"/>
        event Action startedWorking;

        /// <inheritdoc cref="Runtime.stoppedWorking"/>
        event Action stoppedWorking;

        /// <inheritdoc cref="Runtime.AddSceneLoader"/>
        void AddSceneLoader<T>() where T : SceneLoader, new();

        /// <inheritdoc cref="Runtime.Close(IEnumerable{Scene})"/>
        SceneOperation Close(IEnumerable<Scene> scenes);

        /// <inheritdoc cref="Runtime.Close(Scene[])"/>
        SceneOperation Close(params Scene[] scenes);

        /// <inheritdoc cref="Runtime.Close(Scene)"/>
        SceneOperation Close(Scene scene);

        /// <inheritdoc cref="Runtime.Close(SceneCollection)"/>
        SceneOperation Close(SceneCollection collection);

        /// <inheritdoc cref="Runtime.CloseAll"/>
        SceneOperation CloseAll(bool exceptLoadingScreens = true, bool exceptUnimported = true, params Scene[] except);

        /// <inheritdoc cref="Runtime.CancelPreload"/>
        SceneOperation CancelPreload();

        /// <inheritdoc cref="Runtime.FinishPreload()"/>
        SceneOperation FinishPreload();

        /// <inheritdoc cref="Runtime.GetLoaderForScene"/>
        SceneLoader GetLoaderForScene(Scene scene, bool useOnlyGlobal = false);

        /// <inheritdoc cref="Runtime.GetState"/>
        SceneState GetState(Scene scene);

        /// <inheritdoc cref="Runtime.GetToggleableSceneLoaders"/>
        IEnumerable<SceneLoader> GetToggleableSceneLoaders();

        /// <inheritdoc cref="Runtime.IsTracked(Scene)"/>
        bool IsTracked(Scene scene);

        /// <inheritdoc cref="Runtime.IsTracked(SceneCollection)"/>
        bool IsTracked(SceneCollection collection);

        /// <inheritdoc cref="Runtime.Open(IEnumerable{Scene})"/>
        SceneOperation Open(IEnumerable<Scene> scenes);

        /// <inheritdoc cref="Runtime.Open(Scene[])"/>
        SceneOperation Open(params Scene[] scenes);

        /// <inheritdoc cref="Runtime.Open(Scene)"/>
        SceneOperation Open(Scene scene);

        /// <inheritdoc cref="Runtime.Open(SceneCollection, bool)"/>
        SceneOperation Open(SceneCollection collection, bool openAll = false);

        /// <inheritdoc cref="Runtime.OpenAdditive(IEnumerable{SceneCollection}, SceneCollection, Scene)"/>
        SceneOperation OpenAdditive(IEnumerable<SceneCollection> collections, SceneCollection activeCollection = null, Scene loadingScene = null);

        /// <inheritdoc cref="Runtime.OpenAdditive(SceneCollection, bool)"/>
        SceneOperation OpenAdditive(SceneCollection collection, bool openAll = false);

        /// <inheritdoc cref="Runtime.OpenAndActivate"/>
        SceneOperation OpenAndActivate(Scene scene);

        /// <inheritdoc cref="Runtime.OpenWithLoadingScreen(IEnumerable{Scene}, Scene)"/>
        SceneOperation OpenWithLoadingScreen(IEnumerable<Scene> scene, Scene loadingScreen);

        /// <inheritdoc cref="Runtime.OpenWithLoadingScreen(Scene,Scene)"/>
        SceneOperation OpenWithLoadingScreen(Scene scene, Scene loadingScreen);

        /// <inheritdoc cref="Runtime.Preload(Scene, Action)"/>
        SceneOperation Preload(Scene scene, Action onPreloaded = null);

        /// <inheritdoc cref="Runtime.RemoveSceneLoader"/>
        void RemoveSceneLoader<T>();

        /// <inheritdoc cref="Runtime.Activate(Scene)"/>
        void Activate(Scene scene);

        /// <inheritdoc cref="Runtime.ToggleOpen(Scene)"/>
        SceneOperation ToggleOpen(Scene scene);

        /// <inheritdoc cref="Runtime.ToggleOpen(SceneCollection, bool)"/>
        SceneOperation ToggleOpen(SceneCollection collection, bool openAll = false);

        /// <inheritdoc cref="Runtime.Track(Scene)"/>
        void Track(Scene scene);

        /// <inheritdoc cref="Runtime.Track(Scene, UnityEngine.SceneManagement.Scene )"/>
        void Track(Scene scene, UnityEngine.SceneManagement.Scene unityScene);

        /// <inheritdoc cref="Runtime.Track(SceneCollection, bool)"/>
        void Track(SceneCollection collection, bool isAdditive = false);

        /// <inheritdoc cref="Runtime.Untrack(Scene)"/>
        bool Untrack(Scene scene);

        /// <inheritdoc cref="Runtime.Untrack(SceneCollection, bool)"/>
        void Untrack(SceneCollection collection, bool isAdditive = false);

        /// <inheritdoc cref="Runtime.UntrackCollections"/>
        void UntrackCollections();

        /// <inheritdoc cref="Runtime.UntrackScenes"/>
        void UntrackScenes();

    }

}

using AdvancedSceneManager.Core;
using AdvancedSceneManager.Models.Interfaces;
using AdvancedSceneManager.Utility;
using System;
using System.Linq;
using UnityEngine;

namespace AdvancedSceneManager.Models
{

    /// <summary>
    /// Provides helper methods for opening, closing, and managing scenes and collections.
    /// Intended for use from <see cref="UnityEngine.Events.UnityEvent"/>.
    /// </summary>
    [AddComponentMenu("")]
    public class ASMSceneHelper : ScriptableObject,
        IOpenableCollection<SceneCollection>,
        IOpenableScene<Scene>
    {

        /// <inheritdoc cref="UnityEngine.Object.name"/>
        public new string name => base.name; // Prevent renaming from UnityEvent

        /// <summary>Gets the global instance of <see cref="ASMSceneHelper"/>.</summary>
        public static ASMSceneHelper instance => SceneManager.assets.sceneHelper;

        #region IOpenableCollection

        /// <inheritdoc/>
        SceneOperation IOpenable<SceneCollection>.Open(SceneCollection collection) => collection.Open();

        /// <inheritdoc/>
        public void Open(SceneCollection collection) => collection.Open();

        /// <summary>Opens the specified collection.</summary>
        /// <param name="collection">The collection to open.</param>
        /// <param name="openAll">Whether to open scenes flagged not to auto-open in the ASM window or via <see cref="SceneCollection.SetAutoOpen(Scene, bool)"/>.</param>
        public SceneOperation Open(SceneCollection collection, bool openAll = false) => collection.Open(openAll);

        /// <inheritdoc cref="Open(SceneCollection, bool)"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _Open(SceneCollection collection) => SpamCheck.EventMethods.Execute(() => collection.Open());

        /// <inheritdoc/>
        SceneOperation IOpenable<SceneCollection>.Reopen(SceneCollection collection) => collection.Reopen();

        /// <inheritdoc/>
        public void Reopen(SceneCollection collection) => collection.Reopen();

        /// <inheritdoc cref="Reopen(SceneCollection)"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _Reopen(SceneCollection collection) => SpamCheck.EventMethods.Execute(() => collection.Reopen());

        /// <summary>Opens the collection as additive.</summary>
        /// <param name="collection">The collection to preload.</param>
        /// <param name="openAll">Whether to open all scenes, including those flagged not to auto-open.</param>
        public SceneOperation OpenAdditive(SceneCollection collection, bool openAll = false) => collection.OpenAdditive(openAll);

        /// <inheritdoc cref="OpenAdditive(SceneCollection, bool)"/>
        public void OpenAdditive(SceneCollection collection) => collection.OpenAdditive();

        /// <inheritdoc cref="OpenAdditive(SceneCollection, bool)"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _OpenAdditive(SceneCollection collection) => SpamCheck.EventMethods.Execute(() => collection.OpenAdditive());

        /// <summary>Preloads the specified collection.</summary>
        /// <param name="collection">The collection to preload.</param>
        /// <param name="openAll">Whether to preload all scenes, including those flagged not to auto-open.</param>
        public SceneOperation Preload(SceneCollection collection, bool openAll = false) => collection.Preload(openAll);

        /// <inheritdoc cref="Preload(SceneCollection, bool)"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _Preload(SceneCollection collection) => SpamCheck.EventMethods.Execute(() => Preload(collection));

        /// <summary>Preloads the collection as additive.</summary>
        /// <param name="collection">The collection to preload.</param>
        /// <param name="openAll">Whether to preload all scenes, including those flagged not to auto-open.</param>
        public SceneOperation PreloadAdditive(SceneCollection collection, bool openAll = false) => collection.PreloadAdditive(openAll);

        /// <inheritdoc cref="PreloadAdditive(SceneCollection, bool)"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _PreloadAdditive(SceneCollection collection) => SpamCheck.EventMethods.Execute(() => PreloadAdditive(collection));

        /// <summary>Toggles the collection open or closed.</summary>
        /// <param name="collection">The collection to toggle open.</param>
        /// <param name="openAll">Whether to include all scenes, including those flagged not to auto-open.</param>
        public SceneOperation ToggleOpen(SceneCollection collection, bool openAll = false) => collection.ToggleOpen(openAll);

        /// <inheritdoc cref="ToggleOpen(SceneCollection, bool)"/>
        public SceneOperation ToggleOpen(SceneCollection collection) => collection.ToggleOpen();

        /// <inheritdoc cref="ToggleOpen(SceneCollection, bool)"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _ToggleOpen(SceneCollection collection) => SpamCheck.EventMethods.Execute(() => collection.ToggleOpen());

        /// <inheritdoc/>
        public SceneOperation Close(SceneCollection collection) => collection.Close();

        /// <inheritdoc cref="Close(SceneCollection)"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _Close(SceneCollection collection) => SpamCheck.EventMethods.Execute(() => collection.Close());

        #endregion
        #region IOpenableScene

        /// <inheritdoc/>
        public SceneOperation Open(Scene scene) => scene.Open();

        /// <inheritdoc cref="Open(Scene)"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _Open(Scene scene) => SpamCheck.EventMethods.Execute(() => Open(scene));

        /// <inheritdoc/>
        public SceneOperation Reopen(Scene scene) => scene.Reopen();

        /// <inheritdoc cref="Reopen(Scene)"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _Reopen(Scene scene) => SpamCheck.EventMethods.Execute(() => Reopen(scene));

        /// <inheritdoc/>
        public SceneOperation OpenAndActivate(Scene scene) => scene.OpenAndActivate();

        /// <inheritdoc cref="OpenAndActivate(Scene)"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _OpenAndActivate(Scene scene) => SpamCheck.EventMethods.Execute(() => OpenAndActivate(scene));

        /// <inheritdoc/>
        public SceneOperation ToggleOpenState(Scene scene) => scene.ToggleOpen();

        /// <inheritdoc/>
        public SceneOperation ToggleOpen(Scene scene) => scene.ToggleOpen();

        /// <inheritdoc cref="ToggleOpen(Scene)"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _ToggleOpen(Scene scene) => SpamCheck.EventMethods.Execute(() => ToggleOpenState(scene));

        /// <inheritdoc/>
        public SceneOperation Close(Scene scene) => scene.Close();

        /// <inheritdoc cref="Close(Scene)"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _Close(Scene scene) => SpamCheck.EventMethods.Execute(() => Close(scene));

        /// <summary>Preloads the scene.</summary>
        /// <param name="scene">The scene to preload.</param>
        /// <param name="onPreloaded">Callback invoked when the scene has finished preloading.</param>
        public SceneOperation Preload(Scene scene, Action onPreloaded = null) => scene.Preload(onPreloaded);

        /// <inheritdoc cref="Preload(Scene, Action)"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _Preload(Scene scene) => SpamCheck.EventMethods.Execute(() => Preload(scene));

        /// <inheritdoc/>
        public void Activate(Scene scene) => scene.Activate();

        /// <inheritdoc cref="Activate(Scene)"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _Activate(Scene scene) => SpamCheck.EventMethods.Execute(() => Activate(scene));

        /// <inheritdoc/>
        public SceneOperation OpenWithLoadingScreen(Scene scene, Scene loadingScene) => scene.OpenWithLoadingScreen(loadingScene);

        /// <inheritdoc/>
        public SceneOperation CloseWithLoadingScreen(Scene scene, Scene loadingScene) => scene.CloseWithLoadingScreen(loadingScene);

        #endregion
        #region Custom

        /// <summary>Opens all scenes whose names start with the specified string.</summary>
        /// <param name="name">The starting substring to match scene names.</param>
        public void OpenWhereNameStartsWith(string name) =>
            SpamCheck.EventMethods.Execute(() => SceneManager.runtime.Open(SceneManager.assets.scenes.Where(s => s.name.StartsWith(name) && s.isIncludedInBuilds).ToArray()));

        /// <inheritdoc cref="App.Quit"/>
        public void Quit() => SceneManager.app.Quit();

        /// <inheritdoc cref="App.Restart"/>
        public void Restart() => SpamCheck.EventMethods.Execute(() => SceneManager.app.Restart());

        /// <summary>Reopens the currently active <see cref="Runtime.openCollection"/>.</summary>
        public void RestartCollection() => SpamCheck.EventMethods.Execute(() => SceneManager.openCollection.Open());

        /// <inheritdoc cref="Runtime.FinishPreload()"/>
        public SceneOperation FinishPreload() => SceneManager.runtime.FinishPreload();

        /// <inheritdoc cref="FinishPreload"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _FinishPreload() => SpamCheck.EventMethods.Execute(() => FinishPreload());

        /// <inheritdoc cref="Runtime.CancelPreload"/>
        public SceneOperation CancelPreload() => SceneManager.runtime.CancelPreload();

        /// <inheritdoc cref="CancelPreload"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _CancelPreload() => SpamCheck.EventMethods.Execute(() => CancelPreload());

        #endregion

    }

}

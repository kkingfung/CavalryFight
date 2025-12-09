using AdvancedSceneManager.Core;
using AdvancedSceneManager.Models.Interfaces;
using AdvancedSceneManager.Utility;
using System;
using UnityEngine;

namespace AdvancedSceneManager.Models.Utility
{

    /// <summary>Represents a <see cref="Scene"/> that changes depending on the active <see cref="Profile"/>.</summary>
    [CreateAssetMenu(menuName = "Advanced Scene Manager/Profile dependent scene", order = SceneUtility.basePriority + 100)]
    public class ProfileDependentScene : ProfileDependent<Scene>, IOpenable
    {

        /// <summary>Converts a <see cref="ProfileDependentScene"/> to its current <see cref="Scene"/>.</summary>
        /// <param name="instance">The profile-dependent scene instance.</param>
        public static implicit operator Scene(ProfileDependentScene instance) =>
            instance.GetModel(out var scene) ? scene : null;

        /// <summary>Gets the <see cref="Scene"/> associated with the currently active <see cref="Profile"/>.</summary>
        public Scene scene => GetModel();

        /// <summary>Gets whether the scene is currently open.</summary>
        public bool isOpen => scene.isOpen;

        /// <summary>Gets whether the scene is queued to be opened or closed.</summary>
        public bool isQueued => scene.isQueued;

        /// <summary>Opens the scene.</summary>
        public SceneOperation Open() => DoAction(s => s.Open());

        /// <inheritdoc cref="Open"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _Open() => Open();

        /// <summary>Reopens the scene.</summary>
        public SceneOperation Reopen() => DoAction(s => s.Reopen());

        /// <inheritdoc cref="Reopen"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _Reopen() => Reopen();

        /// <summary>Opens and activates the scene.</summary>
        public SceneOperation OpenAndActivate() => DoAction(s => s.OpenAndActivate());

        /// <inheritdoc cref="OpenAndActivate"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _OpenAndActivate() => OpenAndActivate();

        /// <summary>Toggles the open state of the scene.</summary>
        public SceneOperation ToggleOpen() => DoAction(s => s.ToggleOpen());

        /// <inheritdoc cref="ToggleOpen"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _ToggleOpenState() => ToggleOpen();

        /// <inheritdoc cref="ToggleOpen"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _ToggleOpen() => ToggleOpen();

        /// <summary>Closes the scene.</summary>
        public SceneOperation Close() => DoAction(s => s.Close());

        /// <inheritdoc cref="Close"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _Close() => Close();

        /// <summary>Preloads the scene.</summary>
        /// <param name="onPreloaded">Callback invoked when preload is complete.</param>
        public SceneOperation Preload(Action onPreloaded = null) => DoAction(s => s.Preload(onPreloaded));

        /// <inheritdoc cref="Preload(Action)"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _Preload() => Preload();

        /// <summary>Finishes preloading the scene.</summary>
        public SceneOperation FinishPreload() => DoAction(s => s.FinishPreload());

        /// <inheritdoc cref="FinishPreload"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _FinishPreload() => FinishPreload();

        /// <summary>Cancels a pending preload operation.</summary>
        public SceneOperation CancelPreload() => DoAction(s => s.CancelPreload());

        /// <inheritdoc cref="CancelPreload"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _CancelPreload() => CancelPreload();

        /// <summary>Opens the scene using a specified loading screen.</summary>
        /// <param name="loadingScreen">The loading screen to display.</param>
        public SceneOperation OpenWithLoadingScreen(Scene loadingScreen) => DoAction(s => s.OpenWithLoadingScreen(loadingScreen));

        /// <inheritdoc cref="OpenWithLoadingScreen(Scene)"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _OpenWithLoadingScreen(Scene loadingScene) => OpenWithLoadingScreen(loadingScene);

        /// <summary>Closes the scene using a specified loading screen.</summary>
        /// <param name="loadingScreen">The loading screen to display.</param>
        public SceneOperation CloseWithLoadingScreen(Scene loadingScreen) => DoAction(s => s.CloseWithLoadingScreen(loadingScreen));

        /// <inheritdoc cref="CloseWithLoadingScreen(Scene)"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _CloseWithLoadingScreen(Scene loadingScene) => CloseWithLoadingScreen(loadingScene);

        /// <summary>Activates the scene.</summary>
        public void Activate() => DoAction(s => s.Activate());

        /// <inheritdoc cref="Activate"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _Activate() => Activate();

    }

}

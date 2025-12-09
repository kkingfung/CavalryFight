using AdvancedSceneManager.Core;
using AdvancedSceneManager.Models.Interfaces;
using AdvancedSceneManager.Utility;
using UnityEngine;

namespace AdvancedSceneManager.Models.Utility
{

    /// <summary>Represents a <see cref="SceneCollection"/> that changes depending on the active <see cref="Profile"/>.</summary>
    [CreateAssetMenu(menuName = "Advanced Scene Manager/Profile dependent collection", order = SceneUtility.basePriority + 100)]
    public class ProfileDependentCollection : ProfileDependent<SceneCollection>, IOpenable
    {

        /// <summary>Converts a <see cref="ProfileDependentCollection"/> to its current <see cref="SceneCollection"/>.</summary>
        /// <param name="instance">The profile-dependent collection instance.</param>
        public static implicit operator SceneCollection(ProfileDependentCollection instance) =>
            instance.GetModel(out var scene) ? scene : null;

        /// <summary>Gets the <see cref="SceneCollection"/> associated with the currently active <see cref="Profile"/>.</summary>
        public SceneCollection collection => GetModel();

        /// <summary>Gets whether the collection is currently open.</summary>
        public bool isOpen => collection.isOpen;

        /// <summary>Gets whether the collection is queued to be opened or closed.</summary>
        public bool isQueued => collection.isQueued;

        /// <summary>Opens the collection.</summary>
        public SceneOperation Open() => DoAction(c => c.Open());

        /// <summary>Opens the collection.</summary>
        /// <param name="openAll">Whether to open all scenes, including those flagged not to auto-open.</param>
        public SceneOperation Open(bool openAll) => DoAction(c => c.Open(openAll));

        /// <inheritdoc cref="Open()"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _Open() => SpamCheck.EventMethods.Execute(() => Open());

        /// <summary>Opens the collection as additive.</summary>
        public SceneOperation OpenAdditive() => DoAction(c => c.OpenAdditive());

        /// <summary>Opens the collection as additive.</summary>
        /// <param name="openAll">Whether to open all scenes, including those flagged not to auto-open.</param>
        public SceneOperation OpenAdditive(bool openAll) => DoAction(c => c.OpenAdditive(openAll));

        /// <inheritdoc cref="OpenAdditive()"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _OpenAdditive() => SpamCheck.EventMethods.Execute(() => OpenAdditive());

        /// <summary>Reopens the collection.</summary>
        public SceneOperation Reopen() => DoAction(c => c.Reopen());

        /// <summary>Reopens the collection.</summary>
        /// <param name="openAll">Whether to open all scenes, including those flagged not to auto-open.</param>
        public SceneOperation Reopen(bool openAll) => DoAction(c => c.Reopen(openAll));

        /// <inheritdoc cref="Reopen()"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _Reopen() => DoAction(c => c.Reopen());

        /// <summary>Preloads the collection.</summary>
        public SceneOperation Preload() => DoAction(c => c.Preload());

        /// <summary>Preloads the collection.</summary>
        /// <param name="openAll">Whether to preload all scenes, including those flagged not to auto-open.</param>
        public SceneOperation Preload(bool openAll) => DoAction(c => c.Preload(openAll));

        /// <inheritdoc cref="Preload()"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _Preload() => SpamCheck.EventMethods.Execute(() => Preload());

        /// <summary>Preloads the collection as additive.</summary>
        public SceneOperation PreloadAdditive() => DoAction(c => c.PreloadAdditive());

        /// <summary>Preloads the collection as additive.</summary>
        /// <param name="openAll">Whether to preload all scenes, including those flagged not to auto-open.</param>
        public SceneOperation PreloadAdditive(bool openAll) => DoAction(c => c.PreloadAdditive(openAll));

        /// <inheritdoc cref="PreloadAdditive()"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _PreloadAdditive() => SpamCheck.EventMethods.Execute(() => PreloadAdditive());

        /// <summary>Toggles the open state of the collection.</summary>
        public SceneOperation ToggleOpen() => DoAction(c => c.ToggleOpen());

        /// <summary>Toggles the open state of the collection.</summary>
        /// <param name="openAll">Whether to include all scenes, including those flagged not to auto-open.</param>
        public SceneOperation ToggleOpen(bool openAll) => DoAction(c => c.ToggleOpen(openAll));

        /// <inheritdoc cref="ToggleOpen()"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _ToggleOpen() => SpamCheck.EventMethods.Execute(() => ToggleOpen());

        /// <summary>Closes the collection.</summary>
        public SceneOperation Close() => DoAction(c => c.Close());

        /// <inheritdoc cref="Close()"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        public void _Close() => SpamCheck.EventMethods.Execute(() => Close());

    }

}

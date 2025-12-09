using AdvancedSceneManager.Core;

namespace AdvancedSceneManager.Models.Interfaces
{

    /// <summary>Defines members for openable assets.</summary>
    public interface IOpenable
    {

        /// <summary>Gets whether this asset is currently open.</summary>
        bool isOpen { get; }

        /// <summary>Gets whether this asset is queued to be opened or closed.</summary>
        bool isQueued { get; }

        /// <summary>Opens this asset.</summary>
        SceneOperation Open();

        /// <inheritdoc cref="Open"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        void _Open();

        /// <summary>Reopens this asset.</summary>
        SceneOperation Reopen();

        /// <inheritdoc cref="Reopen"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        void _Reopen();

        /// <summary>Toggles this asset open or closed.</summary>
        SceneOperation ToggleOpen();

        /// <inheritdoc cref="ToggleOpen"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        void _ToggleOpen();

        /// <summary>Closes this asset.</summary>
        SceneOperation Close();

        /// <inheritdoc cref="Close"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        void _Close();

    }

    /// <inheritdoc cref="IOpenable"/>
    public interface IOpenable<T> where T : IOpenable
    {

        /// <inheritdoc cref="IOpenable.Open"/>
        SceneOperation Open(T model);

        /// <inheritdoc cref="IOpenable.Open"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        void _Open(T model);

        /// <inheritdoc cref="IOpenable.Reopen"/>
        SceneOperation Reopen(T model);

        /// <inheritdoc cref="IOpenable.Reopen"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        void _Reopen(T model);

        /// <inheritdoc cref="IOpenable.ToggleOpen"/>
        SceneOperation ToggleOpen(T model);

        /// <inheritdoc cref="IOpenable.ToggleOpen"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        void _ToggleOpen(T model);

        /// <inheritdoc cref="IOpenable.Close"/>
        SceneOperation Close(T model);

        /// <inheritdoc cref="IOpenable.Close"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        void _Close(T model);

    }

}

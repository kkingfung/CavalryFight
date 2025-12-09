using AdvancedSceneManager.Core;

namespace AdvancedSceneManager.Models.Interfaces
{

    /// <summary>Defines members for openable collections.</summary>
    public interface IOpenableCollection : IOpenable
    {

        /// <summary>Opens the collection as additive.</summary>
        /// <param name="openAll">Whether to open scenes marked not to auto-open in the ASM window or via <see cref="SceneCollection.SetAutoOpen(Scene, bool)"/>.</param>
        SceneOperation OpenAdditive(bool openAll = false);

        /// <inheritdoc cref="OpenAdditive(bool)"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        void _OpenAdditive();
    }

    /// <inheritdoc cref="IOpenableCollection"/>
    public interface IOpenableCollection<T> : IOpenable<SceneCollection>
    {

        /// <summary>Opens the collection as additive.</summary>
        /// <param name="model">The model instance to operate on.</param>
        /// <param name="openAll">Whether to open scenes marked not to auto-open in the ASM window or via <see cref="SceneCollection.SetAutoOpen(Scene, bool)"/>.</param>
        SceneOperation OpenAdditive(T model, bool openAll = false);

        /// <inheritdoc cref="OpenAdditive(T, bool)"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        void _OpenAdditive(T model);

    }

}

using AdvancedSceneManager.Core;

namespace AdvancedSceneManager.Models.Interfaces
{

    /// <summary>Defines members for openable scenes.</summary>
    public interface IOpenableScene : IOpenable
    {

        /// <summary>Opens the scene using the specified loading screen.</summary>
        /// <param name="loadingScene">The loading scene to display while opening.</param>
        SceneOperation OpenWithLoadingScreen(Scene loadingScene);

        /// <summary>Closes the scene using the specified loading screen.</summary>
        /// <param name="loadingScene">The loading scene to display while closing.</param>
        SceneOperation CloseWithLoadingScreen(Scene loadingScene);

        /// <summary>Opens and activates the scene.</summary>
        SceneOperation OpenAndActivate();

        /// <inheritdoc cref="OpenAndActivate"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        void _OpenAndActivate();

        /// <summary>Activates the scene.</summary>
        void Activate();

        /// <inheritdoc cref="Activate"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        void _Activate();

    }

    /// <inheritdoc cref="IOpenableScene"/>
    public interface IOpenableScene<T> : IOpenable<T> where T : Scene
    {

        /// <inheritdoc cref="IOpenableScene.OpenWithLoadingScreen(Scene)"/>
        SceneOperation OpenWithLoadingScreen(T scene, Scene loadingScene);

        /// <inheritdoc cref="IOpenableScene.CloseWithLoadingScreen(Scene)"/>
        SceneOperation CloseWithLoadingScreen(T scene, Scene loadingScene);

        /// <inheritdoc cref="IOpenableScene.OpenAndActivate"/>
        SceneOperation OpenAndActivate(T scene);

        /// <inheritdoc cref="IOpenableScene.OpenAndActivate"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        void _OpenAndActivate(T scene);

        /// <inheritdoc cref="IOpenableScene.Activate"/>
        void Activate(T scene);

        /// <inheritdoc cref="IOpenableScene.Activate"/>
        /// <remarks>Intended for use with UnityEvents.</remarks>
        void _Activate(T scene);

    }

}

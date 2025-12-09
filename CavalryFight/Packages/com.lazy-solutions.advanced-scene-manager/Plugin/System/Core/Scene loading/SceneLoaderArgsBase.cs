using AdvancedSceneManager.Models;

namespace AdvancedSceneManager.Core
{

    /// <summary>Base class for <see cref="SceneLoadArgs"/> and <see cref="SceneUnloadArgs"/>.</summary>
    public abstract class SceneLoaderArgsBase
    {

        /// <summary>The scene associated with this loading or unloading operation.</summary>
        public Scene scene { get; internal set; }

        /// <summary>The collection that the scene belongs to, if any.</summary>
        public SceneCollection collection { get; internal set; }

        /// <summary>The <see cref="SceneOperation"/> representing the current load or unload operation.</summary>
        public SceneOperation operation { get; internal set; }

        internal bool isHandled { get; set; }
        internal bool noSceneWasLoaded { get; set; }

        /// <summary>Indicates whether this operation resulted in an error.</summary>
        public bool isError { get; private set; }

        /// <summary>The error message if <see cref="isError"/> is <see langword="true"/>.</summary>
        public string errorMessage { get; private set; }

        /// <summary>Determines whether progress should be reported during this operation.</summary>
        public bool reportProgress { get; internal set; } = true;

        /// <summary>Marks this operation as failed with the specified error message.</summary>
        /// <param name="message">The error message describing the failure.</param>
        public void SetError(string message)
        {
            isError = true;
            isHandled = true;
            errorMessage = message;
        }

        /// <summary>Gets whether the associated scene is a loading screen.</summary>
        public bool isLoadingScreen => scene && scene.isLoadingScreen;

        /// <summary>Gets whether the associated scene is a splash screen.</summary>
        public bool isSplashScreen => scene && scene.isSplashScreen;

    }

}

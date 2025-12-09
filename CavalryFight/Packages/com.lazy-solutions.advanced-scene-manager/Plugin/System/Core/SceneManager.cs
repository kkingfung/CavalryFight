using AdvancedSceneManager.Callbacks.Events;
using AdvancedSceneManager.Core;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Interfaces;
using AdvancedSceneManager.Utility;
using System;
using System.Collections.Generic;

namespace AdvancedSceneManager
{

    /// <summary>The central Advanced Scene Manager API. Provides access to the most important things in ASM.</summary>
    public static partial class SceneManager
    {

        /// <inheritdoc cref="IAssetsAPI"/>
        public static IAssetsAPI assets => settings.project.assets;

        /// <inheritdoc cref="Runtime.openScenes"/>
        public static IEnumerable<Scene> openScenes => runtime.openScenes;

        /// <inheritdoc cref="Runtime.openCollection"/>
        public static SceneCollection openCollection => runtime.openCollection;

        /// <inheritdoc cref="Runtime.preloadedScenes"/>
        public static IEnumerable<Scene> preloadedScenes => runtime.preloadedScenes;

        /// <inheritdoc cref="Runtime"/>
        public static Runtime runtime { get; } = new();

        /// <inheritdoc cref="App"/>
        public static App app { get; } = new App();

        /// <inheritdoc cref="ISettingsAPI"/>
        public static ISettingsAPI settings => ASMSettings.instance;

        /// <inheritdoc cref="ProfileUtility.active"/>
        public static Profile profile => ProfileUtility.active;

        /// <summary>Provides access to global ASM event callbacks.</summary>
        public static EventCallbackManager<EventCallbackBase> events { get; } = new();

#if UNITY_EDITOR
        /// <inheritdoc cref="Package"/>
        public static Package package => new();
#endif

    }

    public static partial class SceneManager
    {

        /// <summary>Gets whatever ASM is initialized. Calling ASM methods may fail if <see langword="false"/>, this is due to <see cref="ASMSettings"/> singleton not being loaded yet.</summary>
        /// <remarks>See also <see cref="OnInitialized(Action)"/>.</remarks>
        public static bool isInitialized => ASMSettings.isInitialized;

        /// <summary>Call <paramref name="action"/> when ASM has initialized.</summary>
        /// <remarks>Will call immediately if already initialized.</remarks>
        public static void OnInitialized(Action action) =>
            ASMSettings.OnInitialized(action);

    }

}

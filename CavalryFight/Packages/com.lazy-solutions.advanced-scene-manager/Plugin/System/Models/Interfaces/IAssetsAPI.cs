using AdvancedSceneManager.Models.Internal;
using System.Collections.Generic;

namespace AdvancedSceneManager.Models.Interfaces
{

    /// <summary>Provides access to the scenes, collections and profiles managed by ASM.</summary>
    /// <remarks>May not be available in <c>[InitializeOnLoad]</c> and similar, use <see cref="SceneManager.OnInitialized(System.Action)"/> or <see cref="Callbacks.OnLoadAttribute"/> to ensure you're not calling too early.</remarks>
    public interface IAssetsAPI
    {

        /// <summary>Enumerates all profiles tracked by ASM.</summary>
        IEnumerable<Profile> profiles { get; }

        /// <summary>Enumerates all imported scenes tracked by ASM.</summary>
        IEnumerable<Scene> scenes { get; }

        /// <summary>Enumerates all collection templates tracked by ASM.</summary>
        IEnumerable<SceneCollectionTemplate> collectionTemplates { get; }

        /// <summary>Provides access to the scene helper.</summary>
        ASMSceneHelper sceneHelper { get; }

        /// <summary>Provides access to the default ASM scenes.</summary>
        IAssetsAPIDefaultScenes defaults { get; }

        /// <summary>Enumerates all assets of type <typeparamref name="T"/>.</summary>
        IEnumerable<T> Enumerate<T>() where T : IASMModel;

        /// <summary>Enumerates all assets.</summary>
        IEnumerable<IASMModel> Enumerate();

        /// <summary>Gets the path to the fallback scene.</summary>
        string fallbackScenePath { get; }

    }

    /// <summary>Provides access to asset import.</summary>
    /// <remarks>May not be available in <c>[InitializeOnLoad]</c> and similar, use <see cref= "SceneManager.OnInitialized(System.Action)"/> or <see cref="Callbacks.OnLoadAttribute"/> to ensure you're not calling too early.</remarks>
    internal interface IAssetsAPIInternal
    {

#if UNITY_EDITOR

        void Add<T>(T asset, string rootFolder = null, bool save = true) where T : ASMModelBase, new();
        void Remove<T>(T asset, bool save = true) where T : ASMModelBase, new();
        bool IsIDTaken<T>(string id) where T : ASMModelBase, new();
        string GetPath(ASMModelBase asset, string rootFolder);
        string GetFolder(ASMModelBase asset, string rootFolder);
        string GetFolder<T>() where T : ASMModelBase, new();
        string GetFolder<T>(string id) where T : ASMModelBase, new();
        string GetPath<T>(string id, string name) where T : ASMModelBase, new();
        bool Contains<T>(T asset) where T : ASMModelBase, new();

        void SetSceneAssetPath(Scene scene, string path, bool save = true);

        /// <summary>Gets the import path.</summary>
        /// <remarks>Can be changed using <see cref=" ASMSettings.assetPath"/>.</remarks>
        string assetPath { get; }

#endif

    }

    /// <summary>Provides access to the default ASM scenes.</summary>
    /// <remarks>May not be available in <c>[InitializeOnLoad]</c> and similar, use <see cref="SceneManager.OnInitialized(System.Action)"/> or <see cref="Callbacks.OnLoadAttribute"/> to ensure you're not calling too early.</remarks>
    public interface IAssetsAPIDefaultScenes
    {

        /// <summary>Gets the default ASM splash scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        Scene splashASMScene { get; }

        /// <summary>Gets the default fade splash scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        Scene splashFadeScene { get; }

        /// <summary>Gets the default fade loading scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        Scene fadeScene { get; }

        /// <summary>Gets the default progress bar loading scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        Scene progressBarScene { get; }

        /// <summary>Gets the default progress bar loading scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        Scene totalProgressBarScene { get; }

        /// <summary>Gets the default icon bounce loading scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        Scene iconBounceScene { get; }

        /// <summary>Gets the default press any button loading scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        Scene pressAnyKeyScene { get; }

        /// <summary>Gets the default quote loading scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        Scene quoteScene { get; }

        /// <summary>Gets the default pause scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        Scene pauseScene { get; }

        /// <summary>Gets the default in-game-toolbar scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        Scene inGameToolbarScene { get; }

        /// <summary>Enumerates all imported default scenes.</summary>
        IEnumerable<Scene> Enumerate();

    }

}

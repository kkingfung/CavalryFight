#if UNITY_EDITOR

namespace AdvancedSceneManager.DependencyInjection.Editor
{

    /// <inheritdoc cref="SceneManager.package"/>
    public interface IPackage : DependencyInjectionUtility.IInjectable
    {
        /// <inheritdoc cref="AdvancedSceneManager.Core.Package.folder" />
        string folder { get; }

        /// <inheritdoc cref="AdvancedSceneManager.Core.Package.version" />
        string version { get; }
    }

}

#endif

namespace AdvancedSceneManager.Models.Interfaces
{

    /// <summary>Provides access to ASM settings.</summary>
    /// <remarks>May not be available in <c>[InitializeOnLoad]</c> and similar, use <see cref="SceneManager.OnInitialized(System.Action)"/> or <see cref="Callbacks.OnLoadAttribute"/> to ensure you're not calling too early.</remarks>
    public interface ISettingsAPI
    {

#if UNITY_EDITOR
        /// <summary>The user specific ASM settings, not synced to source control.</summary>
        /// <remarks>
        /// <para>May not be available in <c>[InitializeOnLoad]</c> and similar, use <see cref="SceneManager.OnInitialized(System.Action)"/> or <see cref="Callbacks.OnLoadAttribute"/> to ensure you're not calling too early.</para>
        /// <para>Only available in editor.</para>
        /// </remarks>
        ASMUserSettings user { get; }
#endif

        /// <summary>The project-wide ASM settings.</summary>
        ASMSettings project { get; }

        /// <summary>The current ASM profile.</summary>
        /// <remarks>Could be <see langword="null"/>.</remarks>
        Profile profile { get; }

    }
}

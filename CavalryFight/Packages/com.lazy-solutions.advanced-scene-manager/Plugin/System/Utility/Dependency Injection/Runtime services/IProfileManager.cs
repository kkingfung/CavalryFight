using AdvancedSceneManager.Models;
using System;

namespace AdvancedSceneManager.DependencyInjection
{

    /// <summary>Manages the current profile.</summary>
    public interface IProfileManager : DependencyInjectionUtility.IInjectable
    {

        /// <inheritdoc cref="AdvancedSceneManager.Utility.ProfileUtility.active"/>
        Profile current { get; }

#if UNITY_EDITOR

        /// <inheritdoc cref="AdvancedSceneManager.Utility.ProfileUtility.onProfileChanged"/>
        event Action onProfileChanged;

        /// <inheritdoc cref="AdvancedSceneManager.Utility.ProfileUtility.SetProfile(Profile, bool)"/>
        void SetProfile(Profile profile, bool updateBuildSettings);

        /// <inheritdoc cref="ASMSettings.forceProfile"/>
        Profile forceProfile { get; set; }

        /// <inheritdoc cref="ASMSettings.forceProfile"/>
        Profile defaultProfile { get; set; }

        /// <inheritdoc cref="Profile.Create(string)"/>
        Profile Create(string name);

        /// <inheritdoc cref="Profile.CreateEmpty(string, bool)"/>
        Profile CreateEmpty(string name, bool useDefaultSpecialScenes = true);

        /// <inheritdoc cref="Profile.Delete(Profile)"/>
        void Delete(Profile profile);

        /// <inheritdoc cref="Profile.Duplicate(Profile)"/>
        void Duplicate(Profile profile);


#endif

    }

}

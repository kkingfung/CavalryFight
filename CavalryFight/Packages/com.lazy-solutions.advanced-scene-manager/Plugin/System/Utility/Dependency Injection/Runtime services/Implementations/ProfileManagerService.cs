using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using System;

namespace AdvancedSceneManager.DependencyInjection
{

    public static partial class DependencyInjectionUtility
    {

        sealed class ProfileManagerService : IProfileManager
        {

            public static readonly ProfileManagerService instance = new();

            private ProfileManagerService()
            {
#if UNITY_EDITOR
                ProfileUtility.onProfileChanged += onProfileChanged;
#endif
            }

            public Profile current => SceneManager.profile;

#if UNITY_EDITOR

            public void SetProfile(Profile profile, bool updateBuildSettings) => ProfileUtility.SetProfile(profile, updateBuildSettings);

            public Profile Create(string name) => Profile.Create(name);
            public Profile CreateEmpty(string name, bool useDefaultSpecialScenes = true) => Profile.CreateEmpty(name, useDefaultSpecialScenes);

            public void Delete(Profile profile) => Profile.Delete(profile);
            public void Duplicate(Profile profile) => Profile.Duplicate(profile);

            public event Action onProfileChanged;

            public Profile forceProfile
            {
                get => SceneManager.settings.project.forceProfile;
                set
                {
                    SceneManager.settings.project.forceProfile = value;
                    SceneManager.settings.project.Save();
                }
            }

            public Profile defaultProfile
            {
                get => SceneManager.settings.project.defaultProfile;
                set
                {
                    SceneManager.settings.project.defaultProfile = value;
                    SceneManager.settings.project.Save();
                }
            }

#endif

        }

    }

}

#if ASM_CHILD_PROFILES

using AdvancedSceneManager.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using AdvancedSceneManager.Editor.Utility;
#endif

namespace AdvancedSceneManager.Models
{

    partial class Profile
    {


        [SerializeField] private List<Profile> m_childProfiles = new();

        /// <summary>Gets the child profiles for this profile.</summary>
        public IEnumerable<Profile> childProfiles => m_childProfiles;

        /// <summary>Add a child profile.</summary>
        /// <remarks>Child profiles scenes will be included in build scene list, and startup collections will be opened (<see cref="AdvancedSceneManager.Models.Enums.CollectionStartupOption.Auto"/> means to not open for child profiles).</remarks>
        public void AddChildProfile(Profile profile)
        {

            if (profile == this || childProfiles.Contains(profile))
                return;

            m_childProfiles.Add(profile);
            Save();

#if UNITY_EDITOR
            BuildUtility.UpdateSceneList();
#endif

        }

        /// <summary>Remove a child profile.</summary>
        public void RemoveChildProfile(Profile profile)
        {
            if (m_childProfiles.Remove(profile))
            {

                Save();

#if UNITY_EDITOR
                BuildUtility.UpdateSceneList();
#endif

            }
        }

        /// <summary>Gets all scenes, including child profile scenes.</summary>
        public IEnumerable<Scene> allScenes =>
            scenes.Concat(childProfileScenes).Distinct();

        /// <summary>Gets all scenes from child profiles.</summary>
        public IEnumerable<Scene> childProfileScenes =>
            childProfiles.NonNull().SelectMany(p => p.scenes).Distinct();

    }

}
#else
using System.Collections.Generic;
using System.Linq;

namespace AdvancedSceneManager.Models
{
    partial class Profile
    {

        /// <summary>Gets all scenes, including child profile scenes.</summary>
        public IEnumerable<Scene> allScenes =>
            scenes.Distinct();

    }
}
#endif
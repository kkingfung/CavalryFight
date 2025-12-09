using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.Build.Profile;
#endif

namespace AdvancedSceneManager.Models
{

    partial class Profile
    {

#if UNITY_EDITOR

        [SerializeField] private bool m_autoUpdateBuildScenes = true;
        [SerializeField] private bool m_preventAssignmentIfNullAndUnityHasABuildProfileActive = true;
        [SerializeField] private BuildProfile m_unityBuildProfile;

        /// <summary>Specifies whatever build scene list should be automatically updated.</summary>
        public bool autoUpdateBuildScenes
        {
            get => m_autoUpdateBuildScenes;
            set { m_autoUpdateBuildScenes = value; OnPropertyChanged(); }
        }

        /// <summary>Gets or sets whether ASM should prevent writing the build scene list to Unity’s active build profile when <see cref="unityBuildProfile"/> is <see langword="null"/>.</summary>
        /// <remarks>Only available in the editor.</remarks>
        public bool preventAssignmentIfNullAndUnityHasABuildProfileActive
        {
            get => m_preventAssignmentIfNullAndUnityHasABuildProfileActive;
            set { m_preventAssignmentIfNullAndUnityHasABuildProfileActive = value; Save(); }
        }

        /// <summary>Gets or sets the <see cref="BuildProfile"/> that ASM should write its scene list to when the profile is active. Set to <see langword="null"/> to write to the global list, or to the active build profile if one is active.</summary>
        /// <remarks>
        /// <para>If Unity has an active build profile and this property is <see langword="null"/>, Unity will automatically redirect writes to that active profile. This is a behavior of Unity’s API.</para>
        /// <para>To prevent unintended modifications, enable <see cref="preventAssignmentIfNullAndUnityHasABuildProfileActive"/>, which causes ASM to skip assignment in that case.</para>
        /// <para>Only available in the editor.</para>
        /// </remarks>
        public BuildProfile unityBuildProfile
        {
            get => m_unityBuildProfile;
            set { m_unityBuildProfile = value; Save(); }
        }

#endif

    }

}

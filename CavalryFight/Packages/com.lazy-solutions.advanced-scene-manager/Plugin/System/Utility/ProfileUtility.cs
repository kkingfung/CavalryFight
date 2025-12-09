using UnityEngine;
using System;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Callbacks;

#if UNITY_EDITOR
using UnityEditor;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Callbacks.Events.Editor;
#endif

namespace AdvancedSceneManager.Utility
{

    /// <summary>Provides utility methods for working with profiles.</summary>
    public static class ProfileUtility
    {

        /// <summary>Gets the currently active profile.</summary>
        /// <remarks>May not be available in <c>[InitializeOnLoad]</c> and similar, use <see cref="SceneManager.OnInitialized(Action)"/> or <see cref="OnLoadAttribute"/> to ensure you're not calling too early.</remarks>
        internal static Profile active =>
#if UNITY_EDITOR
            SceneManager.settings.user ? SceneManager.settings.user.activeProfile : null;
#else
            SceneManager.settings.project ? SceneManager.settings.project.buildProfile : null;
#endif

#if UNITY_EDITOR

        /// <summary>Gets the cached <see cref="SerializedObject"/> for the current profile.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static SerializedObject serializedObject { get; private set; }

        /// <summary>Gets the build profile.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static Profile buildProfile => SceneManager.settings.project.buildProfile;

        /// <summary>Gets the default profile.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static Profile defaultProfile
        {
            get => SceneManager.settings.project.defaultProfile;
            set => SceneManager.settings.project.defaultProfile = value;
        }

        /// <summary>Gets the force profile.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static Profile forceProfile
        {
            get => SceneManager.settings.project.forceProfile;
            set => SceneManager.settings.project.forceProfile = value;
        }

        /// <summary>Occurs when <see cref="SceneManager.profile"/> changes.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static event Action onProfileChanged;

        /// <summary>Sets the profile to be used by ASM.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static void SetProfile(Profile profile, bool updateBuildSettings = true)
        {

            serializedObject = profile ? new(profile) : null;
            serializedObject?.Update();

            var userSettings = SceneManager.settings.user;
            if (userSettings.activeProfile != profile)
            {
                userSettings.activeProfile = profile;
                userSettings.SaveNow();
            }

            if (EditorUtility.IsPersistent(profile) && !BuildPipeline.isBuildingPlayer && !Application.isBatchMode)
                SceneManager.settings.project.SetBuildProfile(profile);

            if (updateBuildSettings)
                BuildUtility.UpdateSceneList();

            onProfileChanged?.Invoke();
            SceneManager.events.InvokeCallback(new ProfileChangedEvent(profile));

            Log.Info("Set profile: " + profile);

        }

        //[OnLoad]
        //static void OnLoad()
        //{
        //    SceneManager.events.RegisterCallback<ScenesAvailableForImportChangedEvent>(e =>
        //    {
        //        if (!active)
        //            SetProfile(null);
        //    });
        //}

#endif

    }

}

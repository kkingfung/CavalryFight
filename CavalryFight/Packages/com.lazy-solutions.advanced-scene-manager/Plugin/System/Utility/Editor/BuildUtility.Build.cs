#if UNITY_EDITOR

using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;

#if ADDRESSABLES
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

namespace AdvancedSceneManager.Editor.Utility
{

    partial class BuildUtility
    {

        /// <summary>Gets the build profile ASM should use. Prefers the profile set in <see cref="SceneManager.profile"/>, falling back to Unity's currently active build profile.</summary>
        static BuildProfile GetBuildProfile() =>
            SceneManager.profile && SceneManager.profile.unityBuildProfile
            ? SceneManager.profile.unityBuildProfile
            : BuildProfile.GetActiveBuildProfile();

        /// <summary>Performs a build of the active build profile if one exists, otherwise falls back to Unity's legacy build pipeline.</summary>
        public static BuildReport DoBuild(string path, bool attachProfiler = false, bool runGameWhenBuilt = false, bool dev = true, BuildOptions customOptions = BuildOptions.None)
        {
            if (BuildPipeline.isBuildingPlayer)
                throw new InvalidOperationException("Cannot start build when already building.");

            var options =
                customOptions |
                (dev ? BuildOptions.Development : 0) |
                (runGameWhenBuilt ? BuildOptions.AutoRunPlayer : 0) |
                (attachProfiler ? BuildOptions.ConnectWithProfiler : 0);

            var profile = GetBuildProfile();
            if (profile)
            {
                // Build Profiles path
                var profileOpts = new BuildPlayerWithProfileOptions
                {
                    buildProfile = profile,
                    locationPathName = path,
                    options = options
                };
                return DoBuild(profileOpts);
            }
            else
            {
                // Legacy path
                var legacyOpts = new BuildPlayerOptions
                {
                    locationPathName = path,
                    options = options,
                    scenes = EditorBuildSettings.scenes
                        .Where(s => s.enabled)
                        .Select(s => s.path)
                        .ToArray(),
                    target = EditorUserBuildSettings.activeBuildTarget
                };
                return DoBuild(legacyOpts);
            }
        }

        /// <summary>Performs a build using the legacy <see cref="BuildPlayerOptions"/> API.</summary>
        /// <param name="opts">The build options to use. Only <see cref="BuildPlayerOptions.locationPathName"/> 
        /// and <see cref="BuildPlayerOptions.options"/> are respected; the target and scenes are defined by the build profile.</param>
        /// <returns>A <see cref="BuildReport"/> describing the result of the build.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no active build profile could be found, or if a build is already in progress.</exception>
        public static BuildReport DoBuild(BuildPlayerOptions opts)
        {
            if (BuildPipeline.isBuildingPlayer)
                throw new InvalidOperationException("Cannot start build when already building.");

            var profile = GetBuildProfile();
            if (profile)
            {
                var profileOpts = new BuildPlayerWithProfileOptions
                {
                    buildProfile = profile,
                    locationPathName = opts.locationPathName,
                    options = opts.options
                };
                return DoBuild(profileOpts);
            }
            else
            {
                var events = new BuildEvents();
                var report = BuildPipeline.BuildPlayer(opts);
                events.OnAfterASMBuild(report);
                return report;
            }
        }

        /// <inheritdoc cref="BuildPipeline.BuildPlayer(BuildPlayerWithProfileOptions)"/>
        /// <param name="options">The options describing which build profile to build and how to build it.</param>
        /// <returns>A <see cref="BuildReport"/> describing the result of the build.</returns>
        /// <exception cref="InvalidOperationException">Thrown if a build is already in progress.</exception>
        public static BuildReport DoBuild(BuildPlayerWithProfileOptions options)
        {

            if (BuildPipeline.isBuildingPlayer)
                throw new InvalidOperationException("Cannot start build when already building.");

            BuildAddressables();

            var events = new BuildEvents();
            var report = BuildPipeline.BuildPlayer(options);
            events.OnAfterASMBuild(report);
            return report;

        }

        static void BuildAddressables()
        {
#if ADDRESSABLES
            try
            {
                if (AddressableAssetSettingsDefaultObject.SettingsExists)
                    AddressableAssetSettings.BuildPlayerContent();
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
#endif
        }

    }

}
#endif

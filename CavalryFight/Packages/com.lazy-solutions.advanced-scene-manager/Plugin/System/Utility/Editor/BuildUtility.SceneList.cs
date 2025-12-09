#if UNITY_EDITOR
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEngine;

namespace AdvancedSceneManager.Editor.Utility
{

    partial class BuildUtility
    {

        /// <summary>Specifies why a scene is included or excluded from the build.</summary>
        public enum Reason
        {
            /// <summary>Scene is valid and included by default rules.</summary>
            Default,

            /// <summary>Scene reference is missing or path is invalid.</summary>
            InvalidScene,

            /// <summary>Scene is not part of the active ASM profile.</summary>
            NotIncludedInProfile,

            /// <summary>Scene is explicitly included in the active ASM profile.</summary>
            IncludedInProfile,

            /// <summary>Scene inclusion was overridden by a scene loader.</summary>
            SceneLoaderOverride
        }

        #region On scene list changed

        /// <summary>Called when Unity’s build scene list changes. Used to detect manual modifications and keep ASM profiles in sync.</summary>
        static void OnBuildSettingsChanged()
        {

            if (updateDepth > 0)
                return;

            if (!SceneManager.profile)
            {
                Log.Info("Please do not modify build list manually when no profile is active.");
                UpdateSceneList();
                return;
            }

            var oldScenes = GetOrderedList().Select(s => s.buildScene);
            var newScenes = EditorBuildSettings.scenes;

            GetDiff(oldScenes, newScenes, out var added, out var removed, out var modified);

            foreach (var scene in added)
                if (Scene.TryFind(scene.path, out var s))
                    SceneManager.profile.standaloneScenes.Add(s);

            foreach (var scene in modified)
                if (scene.enabled && Scene.TryFind(scene.path, out var s))
                    SceneManager.profile.standaloneScenes.Add(s);

            UpdateSceneList();

        }

        /// <summary>Computes the difference between two scene lists.</summary>
        /// <param name="oldScenes">Scenes from before the change.</param>
        /// <param name="newScenes">Scenes after the change.</param>
        /// <param name="added">Scenes that exist in <paramref name="newScenes"/> but not in <paramref name="oldScenes"/>.</param>
        /// <param name="removed">Scenes that exist in <paramref name="oldScenes"/> but not in <paramref name="newScenes"/>.</param>
        /// <param name="modified">Scenes that exist in both lists but with different enabled state.</param>
        static void GetDiff(IEnumerable<EditorBuildSettingsScene> oldScenes, IEnumerable<EditorBuildSettingsScene> newScenes, out IEnumerable<EditorBuildSettingsScene> added, out IEnumerable<EditorBuildSettingsScene> removed, out IEnumerable<EditorBuildSettingsScene> modified)
        {
            var oldDict = oldScenes.ToDictionary(s => s.path, s => s.enabled);
            var oldSet = oldDict.Keys.ToHashSet();
            var newSet = newScenes.Select(s => s.path).ToHashSet();

            added = newScenes.Where(s => oldSet.DoesNotHaveIt(s));
            removed = oldScenes.Where(s => newSet.DoesNotHaveIt(s));
            modified = newScenes.Where(s => oldDict.HasButDifferentToggle(s));
        }

        static bool DoesNotHaveIt(this HashSet<string> set, EditorBuildSettingsScene scene) =>
            !set.Contains(scene.path);

        static bool HasButDifferentToggle(this Dictionary<string, bool> dict, EditorBuildSettingsScene scene) =>
            dict.TryGetValue(scene.path, out var oldEnabled) && oldEnabled != scene.enabled;

        #endregion

        /// <summary>Updates the scene build settings.</summary>
        public static void UpdateSceneList() =>
            UpdateSceneList(ignorePlaymodeCheck: false, force: false);

        static int updateDepth;

        /// <summary>Updates the scene build settings from the ASM profile.</summary>
        /// <param name="ignorePlaymodeCheck">If true, ignores play mode guard.</param>
        /// <param name="force">If true, updates regardless of <see cref="Profile.autoUpdateBuildScenes"/>.</param>
        public static void UpdateSceneList(bool ignorePlaymodeCheck, bool force = false)
        {

            if (!SceneManager.profile)
                return;

            if (!ignorePlaymodeCheck && Application.isPlaying)
                return;

            if (!SceneManager.profile.autoUpdateBuildScenes && !force)
                return;

            try
            {
                updateDepth += 1;

                var buildScenes = GetOrderedList().Select(s => s.buildScene).ToList();
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(FallbackSceneUtility.GetStartupScene()))
                    buildScenes.Insert(0, new(FallbackSceneUtility.GetStartupScene(), true));

                var list = buildScenes.GroupBy(s => s.path).Select(g => g.First()).ToArray();

                UpdateSceneList(list, SceneManager.profile.unityBuildProfile);
            }
            finally
            {
                updateDepth -= 1;
            }

        }

        /// <summary>Applies the given scene list to either a build profile or legacy <see cref="EditorBuildSettings"/>.</summary>
        static void UpdateSceneList(EditorBuildSettingsScene[] scenes, BuildProfile buildProfile)
        {

            if (GetScenes(buildProfile).SequenceEqual(scenes))
                return;

            LogUtility.LogBuildScenes(scenes);
            SetScenes(scenes, buildProfile);

        }

        /// <summary>Gets the scenes from either a build profile or legacy <see cref="EditorBuildSettings"/>.</summary>
        static IEnumerable<EditorBuildSettingsScene> GetScenes(BuildProfile buildProfile) =>
            buildProfile ? buildProfile!.scenes : EditorBuildSettings.scenes;

        static Profile hasWarnedAboutBuildProfile;
        /// <summary>Sets the scenes on either a build profile or legacy <see cref="EditorBuildSettings"/>.</summary>
        static void SetScenes(IEnumerable<EditorBuildSettingsScene> scenes, BuildProfile buildProfile)
        {
            if (buildProfile)
            {
#if UNITY_6000_2_OR_NEWER
                buildProfile.overrideGlobalScenes = true;
#endif
                buildProfile.scenes = scenes.ToArray();
                Undo.RecordObject(buildProfile, "Set Build Profile Scenes");
                EditorUtility.SetDirty(buildProfile);
            }
            else if (BuildProfile.GetActiveBuildProfile() && SceneManager.profile.preventAssignmentIfNullAndUnityHasABuildProfileActive)
            {
                if (hasWarnedAboutBuildProfile == SceneManager.profile)
                    return;
                hasWarnedAboutBuildProfile = SceneManager.profile;

                Log.Warning(
                    $"ASM skipped updating scenes because Unity redirected to the active Build Profile " +
                    $"'{BuildProfile.GetActiveBuildProfile().name}'.\n" +
                    $"To allow this, disable 'Prevent assignment if null and Unity has a Build Profile active' in ASM settings.",
                    onlyLogInDev: false
                );
            }
            else
                EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>Gets an ordered list of all scenes that ASM would set in the build settings.</summary>
        public static IEnumerable<(EditorBuildSettingsScene buildScene, Reason reason)> GetOrderedList()
        {

            if (!SceneManager.profile)
                return Enumerable.Empty<(EditorBuildSettingsScene, Reason)>();

            var scenes = SceneManager.profile.allScenes
                .Where(s => s.sceneAsset || s.isDefaultASMScene)
                .Concat(SceneManager.profile.EnumerateAutoScenes())
                .Select(s => (scene: s, isIncluded: IsIncluded(s, out _)))
                .Where(s => s.isIncluded)
                .Select(s => s.scene.path)
                .Concat(SceneManager.profile.dynamicCollections.SelectMany(s => s.scenePaths))
                .Concat(SceneManager.profile.EnumerateAutoScenePaths())
                .Distinct();

            return scenes
                .Select(path =>
                {
                    var enabled = IsEnabled(path, out var reason);
                    return (buildScene: new EditorBuildSettingsScene(path, enabled), reason);
                })
                .OrderByDescending(s => s.buildScene.enabled);

        }

        /// <summary>Checks if a scene is valid and included in the ASM profile.</summary>
        public static bool IsIncluded(Scene scene, out Reason reason)
        {
            if (!scene || string.IsNullOrWhiteSpace(scene.path))
            {
                reason = Reason.InvalidScene;
                return false;
            }
            else if (!scene.GetSceneLoader()?.addScenesToBuildSettings ?? false)
            {
                reason = Reason.SceneLoaderOverride;
                return false;
            }
            else if (SceneManager.profile && SceneManager.profile.allScenes.Contains(scene))
            {
                reason = Reason.IncludedInProfile;
                return true;
            }
            else if (SceneManager.profile && SceneManager.profile.EnumerateAutoScenes().Contains(scene))
            {
                reason = Reason.IncludedInProfile;
                return true;
            }
            else
            {
                reason = Reason.NotIncludedInProfile;
                return false;
            }
        }

        /// <summary>Checks if the scene at <paramref name="path"/> is considered enabled for build.</summary>
        /// <param name="path">Path to the scene asset.</param>
        /// <param name="reason">Outputs the reason for enabled/disabled state.</param>
        public static bool IsEnabled(string path, out Reason reason)
        {

            if (string.IsNullOrWhiteSpace(path))
            {
                reason = Reason.InvalidScene;
                return false;
            }

            reason = Reason.Default;
            return true;

        }

    }

}
#endif

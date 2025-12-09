#if UNITY_EDITOR

using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using System;
using System.Linq;
using UnityEditor;

namespace AdvancedSceneManager.Editor.Utility
{

    partial class SceneImportUtility
    {

        /// <summary>Provides extension methods for working with string paths.</summary>
        public static class StringExtensions
        {

            /// <summary>Gets whether the path points to a scene that has been imported.</summary>
            public static bool IsImported(string path) =>
                IsScene(path) && GetImportedScene(path, out _);

            /// <summary>Gets whether this scene is blacklisted.</summary>
            public static bool IsBlacklisted(string path) =>
                BlocklistUtility.IsBlacklisted(path) || IsFallbackScene(path);

            /// <summary>Gets whether this scene is an ASM scene.</summary>
            public static bool IsASMScene(string path) =>
                path?.StartsWith(SceneManager.package.folder, StringComparison.OrdinalIgnoreCase) ?? false;

            /// <summary>Gets whether this scene is a Unity test runner scene.</summary>
            public static bool IsTestScene(string path) =>
                path?.StartsWith("Assets/inittestscene", StringComparison.OrdinalIgnoreCase) ?? false;

            /// <summary>Gets whether this is a package scene.</summary>
            public static bool IsPackageScene(string path) =>
                path?.StartsWith("packages/", StringComparison.OrdinalIgnoreCase) ?? false;

            /// <summary>Gets whether this scene is the default scene.</summary>
            public static bool IsFallbackScene(string path) =>
                path?.EndsWith($"/{FallbackSceneUtility.Name}.unity", StringComparison.OrdinalIgnoreCase) == true
                || path?.EndsWith("/AdvancedSceneManager.unity", StringComparison.OrdinalIgnoreCase) == true;

            /// <summary>Gets whether the path points to a scene asset.</summary>
            public static bool IsScene(string path) =>
                !string.IsNullOrWhiteSpace(path) && path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase);

            /// <summary>Gets whether this <see cref="SceneAsset"/> has an associated <see cref="Scene"/>.</summary>
            public static bool HasScene(string path) =>
                SceneManager.assets.scenes.Any(s => string.Equals(s.path, path, StringComparison.OrdinalIgnoreCase));

            /// <summary>Gets whether this is a scene available for import.</summary>
            public static bool IsValidSceneToImport(string path) =>
                IsScene(path) &&
                !IsImported(path) &&
                !IsBlacklisted(path) &&
                !IsTestScene(path) &&
                !IsPackageScene(path) &&
                !IsASMScene(path);

            /// <summary>Gets whether this is a dynamic scene (in a path managed by a dynamic collection).</summary>
            public static bool IsDynamicScene(string path) =>
                SceneManager.profile &&
                SceneManager.profile.dynamicCollections.Any(
                    c => path?.IndexOf(c.path, StringComparison.OrdinalIgnoreCase) >= 0);
        }

    }

}
#endif

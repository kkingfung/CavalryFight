using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedSceneManager.Utility;
using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AdvancedSceneManager.Models
{

    /// <summary>Collection containing default ASM scenes, if they have been imported.</summary>
    [Serializable]
    public class DefaultASMScenesCollection : DynamicCollectionBase<Scene>
    {

        /// <summary>Called when this collection is enabled.</summary>
        protected override void OnEnable()
        {
            m_scenes = scenes.ToArray();
            m_description =
                "ASM contains some default scenes that you may use or take inspiration from." +
                "The scenes are provided as a UPM sample, you may use the button below, or use the package manager, to import it.";
            Rename("ASM Defaults");
        }

        /// <summary>Called when this collection is disabled.</summary>
        protected override void OnDisable()
        {
            m_scenes = null;
        }

        [SerializeField] internal Scene[] m_scenes;

        /// <summary>Gets whether the default scenes have been imported.</summary>
        public bool isImported;

        /// <summary>Gets the default scenes from the ASM package samples.</summary>
        public override IEnumerable<Scene> scenes => SceneManager.assets.defaults.Enumerate();

        /// <summary>Gets the scene paths of the default scenes.</summary>
        public override IEnumerable<string> scenePaths => scenes.NonNull().Select(s => s.path);

#if UNITY_EDITOR

        /// <summary>Imports the default ASM scenes from the package samples.</summary>
        public static void ImportScenes()
        {

            var folder = SceneManager.package.folder + "/Samples~/Default ASM Scenes";

            var destinationFolder = $"Assets/Samples/Advanced Scene Manager/{SceneManager.package.version}/Default ASM scenes";

            List<string> failed = new();
            if (!AssetDatabase.DeleteAssets(SceneManager.assets.defaults.Enumerate().Select(s => s.path).ToArray(), failed))
                Debug.LogError("Could not delete the following scenes:\n\n" + string.Join("\n", failed));

            AssetDatabase.DeleteAsset(destinationFolder);
            AssetDatabase.Refresh();

            EditorApplication.delayCall += () =>
            {
                Directory.GetParent(destinationFolder).Create();
                FileUtil.CopyFileOrDirectory(folder, destinationFolder);
                AssetDatabase.Refresh();
                SceneManager.profile.defaultASMScenes.m_scenes = SceneManager.profile.defaultASMScenes.scenes.ToArray();
            };

        }

        /// <summary>Removes the imported default ASM scenes.</summary>
        public static void Unimport()
        {

            List<string> failed = new();
            if (!AssetDatabase.DeleteAssets(SceneManager.assets.defaults.Enumerate().Select(s => s.path).ToArray(), failed))
                Debug.LogError("Could not delete the following scenes:\n\n" + string.Join("\n", failed));

            if (SceneManager.profile)
                SceneManager.profile.Delete(SceneManager.profile.defaultASMScenes);

        }

#endif

    }

#if UNITY_EDITOR
    class DefaultASMScenesPostProcessor : AssetPostprocessor
    {

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            Import(importedAssets);
            Unimport(deletedAssets);
        }

        static void Import(string[] importedAssets)
        {

            var paths = importedAssets.Where(path => path.StartsWith("Assets/Samples/Advanced Scene Manager") && path.EndsWith(".unity"));
            if (!paths.Any())
                return;

            var defaultASMScenes = SceneManager.assets.defaults.Enumerate();
            foreach (var path in paths.ToArray())
            {
                //Scenes might already be imported if updating from 2.5 -> 2.6.
                //In order to not break existing references, lets grab those and point them towards the newly imported scene assets instead.

                var name = Path.GetFileNameWithoutExtension(path);
                var scene = defaultASMScenes.Find(name);
                var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);

                if (scene && asset)
                {
                    scene.path = path;
                    scene.sceneAsset = asset;
                    scene.m_sceneAssetGUID = AssetDatabase.GUIDFromAssetPath(path).ToString();
                }
            }

            //Import any scenes that wasn't imported prior (there is a check for duplicate imports)
            var scenes = SceneUtility.Import(paths.ToArray());

            foreach (var scene in scenes)
            {
                scene.m_isDefaultASMScene = true;
                scene.Save();
            }

            if (SceneManager.profile)
                SceneManager.profile.AddCollection(ScriptableObject.CreateInstance<DefaultASMScenesCollection>());

        }

        static void Unimport(string[] deletedAssets)
        {

            var paths = deletedAssets.Where(path => path.StartsWith("Assets/Samples/Advanced Scene Manager") && path.EndsWith(".unity"));
            if (!paths.Any())
                return;

            if (SceneManager.profile)
            {
                EditorApplication.delayCall += () => SceneManager.profile.OnPropertyChanged(nameof(Profile.defaultASMScenes));
            }

        }

    }
#endif

}

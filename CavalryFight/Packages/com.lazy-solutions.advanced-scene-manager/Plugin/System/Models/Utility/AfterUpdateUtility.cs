#if UNITY_EDITOR

using AdvancedSceneManager.Editor.UI;
using AdvancedSceneManager.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Models.Utility
{

    static class AfterUpdateUtility
    {

        public static void OnBeforeInitializeDone()
        {
#if !ASM_DEV
            UnzipSamplesFolder();
#endif
            UpgradeDynamicCollections();
            UpgradeAssets();
        }

        static void UnzipSamplesFolder()
        {

            //Stupidly enough, unity won't include hidden folders in .unitypackage. So I guess we're doing this, its dumb, but it works.

            var folder = SceneManager.package.folder + "/Samples~";

            if (!File.Exists(folder + ".zip"))
                return;

            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);

            ZipFile.ExtractToDirectory(folder + ".zip", folder);
            File.Delete(folder + ".zip");
            if (File.Exists(folder + ".zip.meta")) File.Delete(folder + ".zip.meta");

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        }

        [Serializable]
        public class OldDynamicCollection
        {
            public string m_id;
            public string m_title;
            public string m_path;
        }

        [Serializable]
        public class OldStandaloneCollection
        {
            public string m_id;
            public List<Scene> m_scenes;
        }

        public static void UpgradeDynamicCollections()
        {

            foreach (var profile in SceneManager.assets.profiles)
                Upgrade(profile);

            void Upgrade(Profile profile)
            {

                try
                {

                    var dynamicCollections = profile.m_oldDynamicCollections?.NonNull()?.Where(c => !string.IsNullOrEmpty(c.m_id))?.ToList();
                    if (profile.m_oldDynamicCollections?.Any() ?? false)
                    {

                        Log.Info($"{profile.name}: Upgrading dynamic collections");
                        profile.suppressSave = true;

                        foreach (var collection in dynamicCollections)
                        {
                            var c = profile.CreateDynamicCollection(collection.m_path, collection.m_title);
                            c.m_id = collection.m_id;
                        }

                        profile.m_oldDynamicCollections = null;

                    }

                    var standaloneScenes = profile.m_oldStandaloneCollection?.m_scenes?.NonNull()?.ToList();
                    if (standaloneScenes?.Any() ?? false)
                    {

                        Log.Info($"{profile.name}: Upgrading standalone collection");
                        profile.suppressSave = true;

                        if (!profile.m_standaloneCollection)
                        {
                            var collection = ScriptableObject.CreateInstance<StandaloneCollection>();
                            profile.AddCollection(collection);
                            collection.m_id = profile.m_oldStandaloneCollection.m_id;
                            profile.m_standaloneCollection = collection;
                        }

                        foreach (var scene in standaloneScenes)
                            if (!profile.standaloneScenes.Contains(scene))
                                profile.standaloneScenes.Add(scene);

                        profile.m_oldStandaloneCollection = null;

                    }

                    if (profile.suppressSave)
                    {
                        profile.suppressSave = false;
                        EditorApplication.delayCall += () =>
                        {
                            profile.SaveNow();
                            ASMWindow.ReloadCollections();
                        };
                    }

                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }
                finally
                { }

            }

        }

        public static void UpgradeAssets()
        {

            var settings = SceneManager.settings.project;
            var assets = settings.assets;

            if (settings.m_profiles?.Any() ?? false)
            {
                assets.m_profiles = settings.m_profiles;
                settings.m_profiles = null;
                settings.Save();
            }

            if (settings.m_scenes?.Any() ?? false)
            {
                assets.m_scenes = settings.m_scenes;
                settings.m_scenes = null;
                settings.Save();
            }

            if (settings.m_collectionTemplates?.Any() ?? false)
            {
                assets.m_collectionTemplates = settings.m_collectionTemplates;
                settings.m_collectionTemplates = null;
                settings.Save();
            }

            if (settings.m_sceneHelper)
            {
                assets.m_sceneHelper = settings.m_sceneHelper;
                settings.m_sceneHelper = null;
                settings.Save();
            }

        }

    }
}
#endif

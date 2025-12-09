using AdvancedSceneManager.Editor.UI.Views.Popups;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Interfaces;
using AdvancedSceneManager.Models.Utility;
using AdvancedSceneManager.Services;
using AdvancedSceneManager.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Editor.UI.Utility
{

    static class ContextualActions
    {

        public static void Remove(IEnumerable<ISceneCollection> collections)
        {

            foreach (var collection in collections)
                SceneManager.profile.Remove(collection);

        }

        public static void Remove(ISceneCollection collection)
        {

            if (collection is null)
                return;

            SceneManager.profile.Remove(collection);

        }

        public static void Remove(IEnumerable<CollectionScenePair> items)
        {

            var groupedItems = items.GroupBy(i => i.collection).Select(g => (collection: g.Key, scenes: g.Select(i => i.sceneIndex).ToArray()));

            foreach (var collection in groupedItems)
                foreach (var index in collection.scenes.OrderByDescending(s => s))
                    if (collection.collection is IEditableCollection c)
                        Remove(c, index);

        }

        public static void Remove(IEditableCollection collection, int sceneIndex)
        {
            collection.RemoveAt(sceneIndex);
            ServiceUtility.Get<ISelectionService>()?.Remove(collection, sceneIndex);
        }

        public static void CreateTemplate(SceneCollection collection)
        {
            SceneCollectionTemplate.CreateTemplate(collection);
            ASMWindow.OpenPopup<CollectionTemplatesPopup>();
        }

        public static void ViewInProjectView(Object obj)
        {
            EditorGUIUtility.PingObject(obj);
        }

        public static void Open(IEnumerable<Scene> scenes, bool additive)
        {
            if (!additive)
                SceneManager.runtime.Open(scenes).CloseAll();
            else
                SceneManager.runtime.Open(scenes);
        }

        public static void MergeScenes(IEnumerable<Scene> scenes)
        {

            scenes = scenes.Distinct();
            var targetScene = scenes.First().path;
            var mergeScenes = scenes.Skip(1).Select(s => s.path).ToArray();

            SceneUtility.MergeScenes(targetScene, mergeScenes);

        }

        public static void BakeLightmaps(IEnumerable<Scene> scenes) =>
            Lightmapping.BakeMultipleScenes(scenes.Distinct().Select(s => s.path).ToArray());

    }

}

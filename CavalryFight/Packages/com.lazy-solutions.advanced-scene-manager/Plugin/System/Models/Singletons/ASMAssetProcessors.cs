//#if UNITY_EDITOR
//using AdvancedSceneManager.Utility;
//using System.Linq;
//using UnityEditor;

//namespace AdvancedSceneManager.Models.Internal
//{

//    class ASMAssetImportPostProcessor : AssetPostprocessor
//    {
//        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
//        {

//            var addedAssets = importedAssets.Select(AssetDatabase.LoadAssetAtPath<ASMModel>).NonNull();
//            var profiles = addedAssets.OfType<Profile>().Where(p => !SceneManager.assets.profiles.Contains(p)).ToList();
//            var scenes = addedAssets.OfType<Scene>().Where(s => !SceneManager.assets.scenes.Contains(s)).ToList();
//            var templates = addedAssets.OfType<SceneCollectionTemplate>().Where(c => !SceneManager.assets.collectionTemplates.Contains(c)).ToList();

//            foreach (var profile in profiles)
//                SceneManager.assetImport.Add(profile);

//            foreach (var scene in scenes)
//                SceneManager.assetImport.Add(scene);

//            foreach (var template in templates)
//                SceneManager.assetImport.Add(template);

//        }
//    }

//    class ASMAssetDeletionProcessor : AssetModificationProcessor
//    {
//        private static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions _)
//        {

//            var profile = AssetDatabase.LoadAssetAtPath<Profile>(path);
//            if (profile)
//            {
//                SceneManager.assetImport.Remove(profile);
//                return AssetDeleteResult.DidDelete;
//            }

//            var scene = AssetDatabase.LoadAssetAtPath<Scene>(path);
//            if (scene)
//            {
//                SceneManager.assetImport.Remove(scene);
//                return AssetDeleteResult.DidDelete;
//            }

//            var template = AssetDatabase.LoadAssetAtPath<SceneCollectionTemplate>(path);
//            if (template)
//            {
//                SceneManager.assetImport.Remove(template);
//                return AssetDeleteResult.DidDelete;
//            }

//            return AssetDeleteResult.DidNotDelete;

//        }
//    }

//}
//#endif

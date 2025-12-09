#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MalbersAnimations
{
    public class MalbersACMenu : EditorWindow
    {
        // const string D_Cinemachine3_Path = "Assets/Malbers Animations/Common/Cinemachine/Cinemachine3.unitypackage";
        const string Steve_Path = "Assets/Malbers Animations/Animal Controller/Human/Steve Player.prefab";
        const string WolfLite_Path = "Assets/Malbers Animations/Animal Controller/Wolf Lite/Wolf Lite.prefab";
        const string Camera_Path = "Assets/Malbers Animations/Common/Cinemachine/Cameras CM3.prefab";
        const string Volume_Path = "Assets/Malbers Animations/Common/Prefabs/Global Volume Post Effects.prefab";



        //[MenuItem("Tools/Malbers Animations/Upgrade to Cinemachine 3", false, 300)]
        //public static void InstallCM3()
        //{
        //    AssetDatabase.ImportPackage(D_Cinemachine3_Path, true);
        //}


        [MenuItem("Tools/Malbers Animations/Create Test Scene (Steve)", false, 100)]
        public static void CreateSampleSceneSteve()
        {
            CreateTestPlane(Steve_Path);
        }


        [MenuItem("Tools/Malbers Animations/Create Test Scene (Wolf)", false, 100)]
        public static void CreateSampleSceneWolf()
        {
            CreateTestPlane(WolfLite_Path);
        }


        [MenuItem("Tools/Malbers Animations/Create Test Scene", false, 100)]
        public static void CreateSampleScene()
        {
            RemoveDefaultCamera();
            CreateGroundPlane();

            InstantiateGameObject(Camera_Path);
            InstantiateGameObject(Volume_Path);
            AddUI();
        }


        const string PlayerInputPath = "Assets/Malbers Animations/Common/Prefabs/Game Logic/Player Input.prefab";

        [MenuItem("GameObject/Malbers Animations/Add Player Input", false, -100)]
        static void CreateZoneGameObject()
        {
            var PI = InstantiateGameObject(PlayerInputPath);
            Selection.activeGameObject = PI;
        }



        private static void CreateGroundPlane()
        {
            var TestPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            TestPlane.transform.localScale = new Vector3(20, 1, 20);
            TestPlane.GetComponent<MeshRenderer>().sharedMaterial =
                AssetDatabase.LoadAssetAtPath("Assets/Malbers Animations/Common/Materials & Textures/Environment/Ground 20.mat", typeof(Material)) as Material;
            TestPlane.isStatic = true;
        }

        public static void CreateTestPlane(string character)
        {
            RemoveDefaultCamera();
            CreateGroundPlane();
            InstantiateGameObject(Camera_Path);
            InstantiateGameObject(character);
            InstantiateGameObject(Volume_Path);

            AddUI();
        }


        private static void AddUI()
        {
            InstantiateGameObject("Assets/Malbers Animations/Common/Prefabs/UI/Main UI.prefab");
        }

        private static void RemoveDefaultCamera()
        {
            var all = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().ToList();
            var mainCam = all.Find(x => x.name == "Main Camera");
            if (mainCam)
            { DestroyImmediate(mainCam); }
        }

        private static GameObject InstantiateGameObject(string path)
        {
            var gameObject = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
            if (gameObject)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(gameObject);
                return instance;
            }
            return null;
        }

        [MenuItem("Tools/Malbers Animations/Tools/Remove All MonoBehaviours from Selected", false, 500)]
        public static void RemoveMono()
        {
            var allGo = Selection.gameObjects;

            if (allGo != null)
            {
                foreach (var selected in allGo)
                {
                    var AllComponents = selected.GetComponentsInChildren<MonoBehaviour>(true);

                    Debug.Log($"Removed {AllComponents.Length} from {selected}", selected);

                    foreach (var comp in AllComponents)
                    {
                        var t = comp.gameObject;
                        DestroyImmediate(comp);
                        EditorUtility.SetDirty(t);
                    }
                }
            }
        }

        [MenuItem("GameObject/-Incremental Name Suffix-", false, 0)]
        public static void IncrementalSuffix()
        {
            GameObject[] selectedObjects = Selection.gameObjects;

            if (selectedObjects.Length == 0) return;

            // Sort by hierarchy order
            System.Array.Sort(selectedObjects, (a, b) =>
                a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

            Undo.RecordObjects(selectedObjects, "Add Incremental Naming");

            for (int i = 0; i < selectedObjects.Length; i++)
            {
                GameObject obj = selectedObjects[i];
                obj.name = $"{obj.name} ({i + 1})";
            }

            //Clear Selection
            Selection.objects = new Object[0];
        }
    }
}
#endif
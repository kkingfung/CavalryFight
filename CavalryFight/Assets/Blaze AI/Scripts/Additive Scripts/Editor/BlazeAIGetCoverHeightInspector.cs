using UnityEngine;
using UnityEditor;

namespace BlazeAISpace
{
    [CustomEditor(typeof(BlazeAIGetCoverHeight))]
    public class BlazeAIGetCoverHeightInspector : Editor
    {
        SerializedProperty heightOfObject;


        void OnEnable()
        {
            heightOfObject = serializedObject.FindProperty("heightOfObject");
        }


        public override void OnInspectorGUI ()
        {
            BlazeAIGetCoverHeight script = (BlazeAIGetCoverHeight)target;

            EditorGUILayout.PropertyField(heightOfObject);
            EditorGUILayout.Space(7);

            if (GUILayout.Button("Get Cover Height")) {
                script.GetHeight();

                if (script.heightOfObject == -1) {
                    EditorUtility.DisplayDialog("No Collider",
                    "This gameobject doesn't have a collider so height can't be calculated. Please add a collider and try again.", "Ok");
                }
            }


            serializedObject.ApplyModifiedProperties();
        }
    }
}
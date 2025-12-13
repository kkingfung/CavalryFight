using UnityEngine;
using UnityEditor;
using BlazeAISpace;

namespace BlazeAISpace
{
    [CustomEditor(typeof(BlazeAIDistanceCulling))]
    public class BlazeAIDistanceCullingInspector : Editor
    {
        SerializedProperty autoCatchCamera,
        cameraOrPlayer,
        distanceToCull,
        cycleFrames,
        restFrames,
        disableBlazeOnly,
        batchSize;


        void OnEnable()
        {
            autoCatchCamera = serializedObject.FindProperty("autoCatchCamera");
            cameraOrPlayer = serializedObject.FindProperty("cameraOrPlayer");
            distanceToCull = serializedObject.FindProperty("distanceToCull");
            cycleFrames = serializedObject.FindProperty("cycleFrames");
            restFrames = serializedObject.FindProperty("restFrames");
            disableBlazeOnly = serializedObject.FindProperty("disableBlazeOnly");
            batchSize = serializedObject.FindProperty("batchSize");
        }


        public override void OnInspectorGUI () 
        {
            BlazeAIDistanceCulling script = (BlazeAIDistanceCulling)target;
            
            EditorGUILayout.LabelField("Camera & Distance", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(autoCatchCamera);
            if (!script.autoCatchCamera) 
            {
                EditorGUILayout.PropertyField(cameraOrPlayer);
            }
            
            EditorGUILayout.PropertyField(distanceToCull);
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Frames Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(cycleFrames);
            EditorGUILayout.PropertyField(batchSize);
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Disabling", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(disableBlazeOnly);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
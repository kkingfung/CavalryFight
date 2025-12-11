using UnityEngine;
using UnityEditor;

namespace BlazeAISpace
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HitStateBehaviour))]
    public class HitStateBehaviourInspector : Editor
    {
        SerializedProperty hitAnims,
        hitAnimT,
        hitAnimGap,

        knockOutDuration,
        faceUpStandClipName,
        faceDownStandClipName,
        ragdollToStandSpeed,
        hipBone,
        useNaturalVelocity,
        knockOutForce,

        cancelAttackOnHit,

        playAudio,
        alwaysPlayAudio,

        callOthersRadius,
        agentLayersToCall,
        showCallRadius,

        onStateEnter,
        onStateExit;


        void OnEnable()
        {
            hitAnims = serializedObject.FindProperty("hitAnims");
            hitAnimT = serializedObject.FindProperty("hitAnimT");
            hitAnimGap = serializedObject.FindProperty("hitAnimGap");

            knockOutDuration = serializedObject.FindProperty("knockOutDuration");
            faceUpStandClipName = serializedObject.FindProperty("faceUpStandClipName");
            faceDownStandClipName = serializedObject.FindProperty("faceDownStandClipName");
            ragdollToStandSpeed = serializedObject.FindProperty("ragdollToStandSpeed");
            hipBone = serializedObject.FindProperty("hipBone");
            useNaturalVelocity = serializedObject.FindProperty("useNaturalVelocity");
            knockOutForce = serializedObject.FindProperty("knockOutForce");

            cancelAttackOnHit = serializedObject.FindProperty("cancelAttackOnHit");

            playAudio = serializedObject.FindProperty("playAudio");
            alwaysPlayAudio = serializedObject.FindProperty("alwaysPlayAudio");

            callOthersRadius = serializedObject.FindProperty("callOthersRadius");
            agentLayersToCall = serializedObject.FindProperty("agentLayersToCall");
            showCallRadius = serializedObject.FindProperty("showCallRadius");

            onStateEnter = serializedObject.FindProperty("onStateEnter");
            onStateExit = serializedObject.FindProperty("onStateExit");
        }

        public override void OnInspectorGUI () 
        {
            HitStateBehaviour script = (HitStateBehaviour) target;

            EditorGUILayout.LabelField("Hover on any property below for insights", EditorStyles.helpBox);
            EditorGUILayout.Space(10);

            BlazeAIEditor.DrawArrayWithStyle(hitAnims, "Hit Animations", true);
            
            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.PropertyField(hitAnimT);
                EditorGUILayout.PropertyField(hitAnimGap);
            GUILayout.EndVertical();


            EditorGUILayout.Space();
            
            
            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Knock Out", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(knockOutDuration);
                EditorGUILayout.PropertyField(faceUpStandClipName);
                EditorGUILayout.PropertyField(faceDownStandClipName);
                EditorGUILayout.PropertyField(ragdollToStandSpeed);
                EditorGUILayout.PropertyField(hipBone);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Added Force", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(useNaturalVelocity);
                if (!script.useNaturalVelocity) {
                    EditorGUILayout.PropertyField(knockOutForce);
                }
            GUILayout.EndVertical();
            

            EditorGUILayout.Space();


            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Cancel Attack", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(cancelAttackOnHit);
            GUILayout.EndVertical();


            EditorGUILayout.Space();


            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(playAudio);
                if (script.playAudio) {
                    EditorGUILayout.PropertyField(alwaysPlayAudio);
                }
            GUILayout.EndVertical();


            EditorGUILayout.Space();


            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Call Others", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(callOthersRadius);
                EditorGUILayout.PropertyField(agentLayersToCall);
                EditorGUILayout.PropertyField(showCallRadius);
            GUILayout.EndVertical();


            EditorGUILayout.Space();
            

            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("State Events", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(onStateEnter);
                EditorGUILayout.PropertyField(onStateExit);
            GUILayout.EndVertical();


            serializedObject.ApplyModifiedProperties();
        }
    }
}
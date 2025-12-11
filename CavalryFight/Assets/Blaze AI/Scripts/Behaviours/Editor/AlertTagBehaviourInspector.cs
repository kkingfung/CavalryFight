using UnityEditor;
using UnityEngine;

namespace BlazeAISpace
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AlertTagBehaviour))]
    public class AlertTagBehaviourInspector : Editor
    {
        #region SERIALIZED PROPERTIES

        SerializedProperty checkLocation,
        onSightAnim,
        onSightDuration,
        reachedLocationAnim,
        reachedLocationDuration,
        animT,
        playAudio,
        audioIndex,
        callOtherAgents,
        callRange,
        showCallRange,
        otherAgentsLayers,
        callPassesColliders,
        randomizeCallPosition,
        onStateEnter,
        onStateExit;

        #endregion

        #region EDITOR VARS
        
        AlertTagBehaviour script;
        AlertTagBehaviour[] scripts;
        
        #endregion

        #region METHODS
        
        void SetScripts()
        {
            script = (AlertTagBehaviour) target;

            Object[] objs = targets;
            scripts = new AlertTagBehaviour[objs.Length];
            for (int i = 0; i < objs.Length; i++) {
                scripts[i] = objs[i] as AlertTagBehaviour;
            }
        }

        void OnEnable()
        {
            SetScripts();

            checkLocation = serializedObject.FindProperty("checkLocation");
            onSightAnim = serializedObject.FindProperty("onSightAnim");
            onSightDuration = serializedObject.FindProperty("onSightDuration");
            reachedLocationAnim = serializedObject.FindProperty("reachedLocationAnim");
            reachedLocationDuration = serializedObject.FindProperty("reachedLocationDuration");
            animT = serializedObject.FindProperty("animT");
            playAudio = serializedObject.FindProperty("playAudio");
            audioIndex = serializedObject.FindProperty("audioIndex");
            callOtherAgents = serializedObject.FindProperty("callOtherAgents");
            callRange = serializedObject.FindProperty("callRange");
            showCallRange = serializedObject.FindProperty("showCallRange");
            otherAgentsLayers = serializedObject.FindProperty("otherAgentsLayers");
            callPassesColliders = serializedObject.FindProperty("callPassesColliders");
            randomizeCallPosition = serializedObject.FindProperty("randomizeCallPosition");
            onStateEnter = serializedObject.FindProperty("onStateEnter");
            onStateExit = serializedObject.FindProperty("onStateExit");
        }

        public override void OnInspectorGUI()
        {
            BlazeAIEditor.RefreshAnimationStateNames(script.blaze.anim);
            EditorGUILayout.LabelField("Hover on any property below for insights", EditorStyles.helpBox);
            EditorGUILayout.Space(10);

            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Check Location", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(checkLocation);
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Animations", EditorStyles.boldLabel);
                BlazeAIEditor.DrawPopupProperty(script.blaze.anim, "On Sight Anim", "onSightAnim", ref script.onSightAnim, scripts);
                EditorGUILayout.PropertyField(onSightDuration);
                
                if (script.checkLocation) 
                {
                    EditorGUILayout.Space();
                    BlazeAIEditor.DrawPopupProperty(script.blaze.anim, "Reached Location Anim", "reachedLocationAnim", ref script.reachedLocationAnim, scripts);
                    EditorGUILayout.PropertyField(reachedLocationDuration);
                    EditorGUILayout.Space();
                }

                EditorGUILayout.PropertyField(animT);
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(playAudio);
                if (script.playAudio) {
                    EditorGUILayout.PropertyField(audioIndex);
                }
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Call Others", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(callOtherAgents);
                if (script.callOtherAgents) 
                {
                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(callRange);
                    EditorGUILayout.PropertyField(showCallRange);
                    EditorGUILayout.Space();
                    
                    EditorGUILayout.PropertyField(otherAgentsLayers);
                    EditorGUILayout.PropertyField(callPassesColliders);
                    EditorGUILayout.Space();
                    
                    EditorGUILayout.PropertyField(randomizeCallPosition);
                }
            GUILayout.EndVertical();

            EditorGUILayout.Space();
            
            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("State Events", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(onStateEnter);
                EditorGUILayout.PropertyField(onStateExit);
            GUILayout.EndVertical();
            
            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }
}
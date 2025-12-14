using UnityEditor;
using UnityEngine;

namespace BlazeAISpace
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(GoingToCoverBehaviour))]
    public class GoingToCoverBehaviourInspector : Editor
    {
        #region SERIALIZED PROPERTIES

        SerializedProperty coverLayers,
        hideSensitivity,
        minAndMaxCoverPointHeight,
        searchDistance,
        showSearchDistance,

        minCoverHeight,
        highCoverHeight,
        highCoverAnim,
        lowCoverAnim,
        coverAnimT,

        rotateToCoverNormal,
        rotateToCoverSpeed,

        onlyAttackAfterCover,

        playAudioOnGoingToCover,
        alwaysPlayAudio,

        onStateEnter,
        onStateExit;

        #endregion
        
        #region EDITOR VARIABLES
        
        GoingToCoverBehaviour script;
        GoingToCoverBehaviour[] scripts;

        #endregion

        #region METHODS

        void SetScripts()
        {
            script = (GoingToCoverBehaviour) target;

            Object[] objs = targets;
            scripts = new GoingToCoverBehaviour[objs.Length];
            for (int i = 0; i < objs.Length; i++) {
                scripts[i] = objs[i] as GoingToCoverBehaviour;
            }
        }

        void OnEnable()
        {
            SetScripts();

            coverLayers = serializedObject.FindProperty("coverLayers");
            hideSensitivity = serializedObject.FindProperty("hideSensitivity");
            minAndMaxCoverPointHeight = serializedObject.FindProperty("minAndMaxCoverPointHeight");
            searchDistance = serializedObject.FindProperty("searchDistance");
            showSearchDistance = serializedObject.FindProperty("showSearchDistance");

            minCoverHeight = serializedObject.FindProperty("minCoverHeight");
            highCoverHeight = serializedObject.FindProperty("highCoverHeight");
            highCoverAnim = serializedObject.FindProperty("highCoverAnim");
            lowCoverAnim = serializedObject.FindProperty("lowCoverAnim");
            coverAnimT = serializedObject.FindProperty("coverAnimT");

            rotateToCoverNormal = serializedObject.FindProperty("rotateToCoverNormal");
            rotateToCoverSpeed = serializedObject.FindProperty("rotateToCoverSpeed");

            onlyAttackAfterCover = serializedObject.FindProperty("onlyAttackAfterCover");

            playAudioOnGoingToCover = serializedObject.FindProperty("playAudioOnGoingToCover");
            alwaysPlayAudio = serializedObject.FindProperty("alwaysPlayAudio");

            onStateEnter = serializedObject.FindProperty("onStateEnter");
            onStateExit = serializedObject.FindProperty("onStateExit");
        }

        public override void OnInspectorGUI () 
        {
            BlazeAIEditor.RefreshAnimationStateNames(script.blaze.anim);
            EditorGUILayout.LabelField("Hover on any property below for insights", EditorStyles.helpBox);
            EditorGUILayout.Space(10);

            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(coverLayers);
                EditorGUILayout.PropertyField(hideSensitivity);
                EditorGUILayout.PropertyField(minAndMaxCoverPointHeight);
            GUILayout.EndVertical();
            
            
            EditorGUILayout.Space();


            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Cover Search", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(searchDistance);
                EditorGUILayout.PropertyField(showSearchDistance);
            GUILayout.EndVertical();


            EditorGUILayout.Space();


            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Covers Heights", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(minCoverHeight);
                EditorGUILayout.PropertyField(highCoverHeight);
            GUILayout.EndVertical();


            EditorGUILayout.Space();


            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Taking Cover Animations", EditorStyles.boldLabel);
                BlazeAIEditor.DrawPopupProperty(script.blaze.anim, "High Cover Anim", "highCoverAnim", ref script.highCoverAnim, scripts);
                BlazeAIEditor.DrawPopupProperty(script.blaze.anim, "Low Cover Anim", "lowCoverAnim", ref script.lowCoverAnim, scripts);
                EditorGUILayout.PropertyField(coverAnimT);
            GUILayout.EndVertical();

            
            EditorGUILayout.Space();

            
            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Rotate To Cover", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(rotateToCoverNormal);
                if (script.rotateToCoverNormal) {
                    EditorGUILayout.PropertyField(rotateToCoverSpeed);
                }
            GUILayout.EndVertical();

            
            EditorGUILayout.Space();

            
            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Attack After Cover", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(onlyAttackAfterCover);
            GUILayout.EndVertical();


            EditorGUILayout.Space();


            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(playAudioOnGoingToCover);
                if (script.playAudioOnGoingToCover) {
                    EditorGUILayout.PropertyField(alwaysPlayAudio);
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
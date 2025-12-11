using UnityEditor;
using UnityEngine;

namespace BlazeAISpace 
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SurprisedStateBehaviour))]
    public class SurprisedStateBehaviourInspector : Editor
    {
        #region SERIALIZED PROPERTIES

        SerializedProperty anim,
        animT,
        duration,
        turnSpeed,
        playAudio,
        onStateEnter,
        onStateExit;

        #endregion

        #region VARIABLES

        SurprisedStateBehaviour script;
        SurprisedStateBehaviour[] scripts;

        #endregion

        #region METHODS

        void SetScripts()
        {
            script = (SurprisedStateBehaviour) target;

            Object[] objs = targets;
            scripts = new SurprisedStateBehaviour[objs.Length];
            for (int i = 0; i < objs.Length; i++) {
                scripts[i] = objs[i] as SurprisedStateBehaviour;
            }
        }

        void OnEnable()
        {
            SetScripts();

            anim = serializedObject.FindProperty("anim");
            animT = serializedObject.FindProperty("animT");
            duration = serializedObject.FindProperty("duration");
            turnSpeed = serializedObject.FindProperty("turnSpeed");
            playAudio = serializedObject.FindProperty("playAudio");
            onStateEnter = serializedObject.FindProperty("onStateEnter");
            onStateExit = serializedObject.FindProperty("onStateExit");
        }

        public override void OnInspectorGUI () 
        {
            EditorGUILayout.LabelField("Hover on any property below for insights", EditorStyles.helpBox);
            EditorGUILayout.Space(10);

            BlazeAIEditor.RefreshAnimationStateNames(script.blaze.anim);

            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);
                BlazeAIEditor.DrawPopupProperty(script.blaze.anim, "Anim", "anim", ref script.anim, scripts);
                EditorGUILayout.PropertyField(animT);
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Surprised Duration", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(duration);
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Turning To Distraction", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(turnSpeed);
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(playAudio);
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
using UnityEditor;
using UnityEngine;
using BlazeAISpace;

namespace BlazeAISpace 
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AlertStateBehaviour))]
    public class AlertStateBehaviourInspector : Editor
    {
        #region SERIALIZED PROPERTIES

        SerializedProperty moveSpeed,
        turnSpeed,
        idleAnim,
        moveAnim,
        animT,
        idleTime,
        playPatrolAudio,
        returnToNormal,
        timeToReturnNormal,
        returningDuration,
        returningAnim,
        returningAnimT,
        playAudioOnReturn,
        avoidFacingObstacles,
        obstacleLayers,
        obstacleRayDistance,
        obstacleRayOffset,
        showObstacleRay,
        onStateEnter,
        onStateExit;

        #endregion
        
        #region VARIABLES

        AlertStateBehaviour script;
        AlertStateBehaviour[] scripts;

        #endregion

        #region METHODS

        void SetScripts()
        {
            script = (AlertStateBehaviour) target;

            Object[] objs = targets;
            scripts = new AlertStateBehaviour[objs.Length];
            for (int i = 0; i < objs.Length; i++) {
                scripts[i] = objs[i] as AlertStateBehaviour;
            }
        }

        void OnEnable()
        {
            SetScripts();
            moveSpeed = serializedObject.FindProperty("moveSpeed");
            turnSpeed = serializedObject.FindProperty("turnSpeed");

            idleAnim = serializedObject.FindProperty("idleAnim");
            moveAnim = serializedObject.FindProperty("moveAnim");
            animT = serializedObject.FindProperty("animT");

            idleTime = serializedObject.FindProperty("idleTime");

            playPatrolAudio = serializedObject.FindProperty("playPatrolAudio");

            returnToNormal = serializedObject.FindProperty("returnToNormal");
            timeToReturnNormal = serializedObject.FindProperty("timeToReturnNormal");
            returningDuration = serializedObject.FindProperty("returningDuration");
            returningAnim = serializedObject.FindProperty("returningAnim");
            returningAnimT = serializedObject.FindProperty("returningAnimT");
            playAudioOnReturn = serializedObject.FindProperty("playAudioOnReturn");

            avoidFacingObstacles = serializedObject.FindProperty("avoidFacingObstacles");
            obstacleLayers = serializedObject.FindProperty("obstacleLayers");
            obstacleRayDistance = serializedObject.FindProperty("obstacleRayDistance");
            obstacleRayOffset = serializedObject.FindProperty("obstacleRayOffset");
            showObstacleRay = serializedObject.FindProperty("showObstacleRay");

            onStateEnter = serializedObject.FindProperty("onStateEnter");
            onStateExit = serializedObject.FindProperty("onStateExit");
        }

        public override void OnInspectorGUI () 
        {
            EditorGUILayout.LabelField("Hover on any property below for insights", EditorStyles.helpBox);
            EditorGUILayout.Space(10);
            
            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Speeds", EditorStyles.boldLabel);
                BlazeAIEditor.CheckDisableWithRootMotion(script.blaze, ref moveSpeed);
                EditorGUILayout.PropertyField(turnSpeed);
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            BlazeAIEditor.RefreshAnimationStateNames(script.blaze.anim);
            BlazeAIEditor.DrawArrayWithStyle(idleAnim, "Animations");

            EditorGUILayout.Space();
            
            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                BlazeAIEditor.DrawPopupProperty(script.blaze.anim, "Move Anim", "moveAnim", ref script.moveAnim, scripts);
                EditorGUILayout.PropertyField(animT);
            GUILayout.EndVertical();
            
            EditorGUILayout.Space();

            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Time At Waypoint", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(idleTime);
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(playPatrolAudio);
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Return To Normal", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(returnToNormal);
                if (script.returnToNormal) 
                {
                    EditorGUILayout.PropertyField(timeToReturnNormal);
                    EditorGUILayout.PropertyField(returningDuration);
                    
                    BlazeAIEditor.DrawPopupProperty(script.blaze.anim, "Returning Anim", "returningAnim", ref script.returningAnim, scripts);

                    EditorGUILayout.PropertyField(returningAnimT);
                    EditorGUILayout.PropertyField(playAudioOnReturn);
                }
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Obstacles", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(avoidFacingObstacles);
                if (script.avoidFacingObstacles) {
                    EditorGUILayout.PropertyField(obstacleLayers);
                    EditorGUILayout.PropertyField(obstacleRayDistance);
                    EditorGUILayout.PropertyField(obstacleRayOffset);
                    EditorGUILayout.PropertyField(showObstacleRay);
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
using UnityEditor;
using UnityEngine;
using BlazeAISpace;

namespace BlazeAISpace 
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NormalStateBehaviour))]
    public class NormalStateBehaviourInspector : Editor
    {
        #region SERIALIZED PROPERTIES

        SerializedProperty moveSpeed,
        turnSpeed,
        idleAnim,
        moveAnim,
        animT,
        idleTime,
        playPatrolAudio,
        avoidFacingObstacles,
        obstacleLayers,
        obstacleRayDistance,
        obstacleRayOffset,
        showObstacleRay,
        onStateEnter,
        onStateExit;

        #endregion

        #region VARIABLES

        NormalStateBehaviour script;
        NormalStateBehaviour[] scripts;

        #endregion

        #region METHODS

        void SetScripts()
        {
            script = (NormalStateBehaviour) target;

            Object[] objs = targets;
            scripts = new NormalStateBehaviour[objs.Length];
            for (int i = 0; i < objs.Length; i++) {
                scripts[i] = objs[i] as NormalStateBehaviour;
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
            
            // drawing Move Anim (popup and multi-editing)
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
                EditorGUILayout.LabelField("Obstacles", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(avoidFacingObstacles);
                if (script.avoidFacingObstacles) 
                {
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
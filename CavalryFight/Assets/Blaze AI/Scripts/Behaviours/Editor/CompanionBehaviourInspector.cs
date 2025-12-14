using UnityEngine;
using UnityEditor;

namespace BlazeAISpace
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CompanionBehaviour))]
    public class CompanionBehaviourInspector : Editor
    {
        #region SERIALIZED PROPERTIES

        SerializedProperty follow,
        followIfDistance,
        walkIfDistance,
        movingWindow,

        idleAnim,
        runAnim,
        runSpeed,
        walkAnim,
        walkSpeed,
        turnSpeed,
        stayPutAnim,
        animsT,

        wanderAround,
        wanderPointTime,
        wanderMoveSpeed,
        wanderIdleAnims,
        wanderMoveAnim,

        moveAwayOnContact,
        layersToAvoid,
        walkOnMoveAway,
        moveAwayCoolDownTime,

        playAudioForIdleAndWander,
        audioTimer,
        playAudioOnFollowState,

        onStateEnter,
        onStateExit,
        fireEventDistance,
        exceededDistanceEvent;
        
        #endregion
        
        #region EDITOR VARS

        CompanionBehaviour script;
        CompanionBehaviour[] scripts;

        string[] tabs = {"General", "Wander", "Audios", "Events"};
        int tabSelected = 0;
        int tabIndex = -1;

        #endregion
        
        #region EDITOR METHODS

        void SetScripts()
        {
            script = (CompanionBehaviour) target;

            Object[] objs = targets;
            scripts = new CompanionBehaviour[objs.Length];
            for (int i = 0; i < objs.Length; i++) {
                scripts[i] = objs[i] as CompanionBehaviour;
            }
        }

        void OnEnable()
        {
            SetScripts();
            GetSelectedTab();

            follow = serializedObject.FindProperty("follow");
            followIfDistance = serializedObject.FindProperty("followIfDistance");
            walkIfDistance = serializedObject.FindProperty("walkIfDistance");
            movingWindow = serializedObject.FindProperty("movingWindow");

            idleAnim = serializedObject.FindProperty("idleAnim");

            runAnim = serializedObject.FindProperty("runAnim");
            runSpeed = serializedObject.FindProperty("runSpeed");

            walkAnim = serializedObject.FindProperty("walkAnim");
            walkSpeed = serializedObject.FindProperty("walkSpeed");

            turnSpeed = serializedObject.FindProperty("turnSpeed");

            stayPutAnim = serializedObject.FindProperty("stayPutAnim");
            animsT = serializedObject.FindProperty("animsT");

            wanderAround = serializedObject.FindProperty("wanderAround");
            wanderPointTime = serializedObject.FindProperty("wanderPointTime");
            wanderMoveSpeed = serializedObject.FindProperty("wanderMoveSpeed");
            wanderIdleAnims = serializedObject.FindProperty("wanderIdleAnims");
            wanderMoveAnim = serializedObject.FindProperty("wanderMoveAnim");

            moveAwayOnContact = serializedObject.FindProperty("moveAwayOnContact");
            layersToAvoid = serializedObject.FindProperty("layersToAvoid");
            walkOnMoveAway = serializedObject.FindProperty("walkOnMoveAway");
            moveAwayCoolDownTime = serializedObject.FindProperty("moveAwayCoolDownTime");

            playAudioForIdleAndWander = serializedObject.FindProperty("playAudioForIdleAndWander");
            audioTimer = serializedObject.FindProperty("audioTimer");
            playAudioOnFollowState = serializedObject.FindProperty("playAudioOnFollowState");

            onStateEnter = serializedObject.FindProperty("onStateEnter");
            onStateExit = serializedObject.FindProperty("onStateExit");
            fireEventDistance = serializedObject.FindProperty("fireEventDistance");
            exceededDistanceEvent = serializedObject.FindProperty("exceededDistanceEvent");
        }

        public override void OnInspectorGUI () 
        {
            DrawToolbar();
            EditorGUILayout.LabelField("Hover on any property below for insights", EditorStyles.helpBox);
            EditorGUILayout.Space(10);

            tabIndex = -1;
            BlazeAIEditor.RefreshAnimationStateNames(script.blaze.anim);

            switch (tabSelected)
            {
                case 0:
                    DrawGeneralTab();
                    break;
                case 1:
                    DrawWanderingTab(script);
                    break;
                case 2:
                    DrawAudiosTab(script);
                    break;
                case 3:
                    DrawEventsTab();
                    break;
            }

            EditorPrefs.SetInt("BlazeCompanionTabSelected", tabSelected);
            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region DRAWING

        void GetSelectedTab()
        {
            if (EditorPrefs.HasKey("BlazeCompanionTabSelected")) {
                tabSelected = EditorPrefs.GetInt("BlazeCompanionTabSelected");
            }
            else {
                tabSelected = 0;
            }   
        }

        void DrawToolbar()
        {   
            GUILayout.BeginHorizontal();
            
            foreach (var item in tabs) 
            {
                tabIndex++;

                if (tabIndex == tabSelected) 
                {
                    // selected button
                    GUILayout.Button(item, BlazeAIEditor.ToolbarStyling(true), GUILayout.MinWidth(80), GUILayout.Height(35));
                }
                else 
                {
                    // unselected buttons
                    if (GUILayout.Button(item, BlazeAIEditor.ToolbarStyling(false), GUILayout.MinWidth(80), GUILayout.Height(35))) {
                        // this will trigger when a button is pressed
                        tabSelected = tabIndex;
                    }
                }
            }

            GUILayout.EndHorizontal();
        }

        void DrawGeneralTab()
        {
            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Follow", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(follow);
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(followIfDistance);
                EditorGUILayout.PropertyField(walkIfDistance);
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(movingWindow);
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Speeds & Anims", EditorStyles.boldLabel);
                // draw Idle Anim (popup and multi-editing)
                BlazeAIEditor.DrawPopupProperty(script.blaze.anim, "Idle Anim", "idleAnim", ref script.idleAnim, scripts);
            
                EditorGUILayout.Space();

                // draw Run Anim (popup and multi-editing)
                BlazeAIEditor.DrawPopupProperty(script.blaze.anim, "Run Anim", "runAnim", ref script.runAnim, scripts);
                BlazeAIEditor.CheckDisableWithRootMotion(script.blaze, ref runSpeed);

                EditorGUILayout.Space();

                // draw Walk Anim (popup and multi-editing)
                BlazeAIEditor.DrawPopupProperty(script.blaze.anim, "Walk Anim", "walkAnim", ref script.walkAnim, scripts);
                BlazeAIEditor.CheckDisableWithRootMotion(script.blaze, ref walkSpeed);

                EditorGUILayout.Space();

                BlazeAIEditor.DrawPopupProperty(script.blaze.anim, "Stay Put Anim", "stayPutAnim", ref script.stayPutAnim, scripts);

                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(turnSpeed);
                EditorGUILayout.PropertyField(animsT);
            GUILayout.EndVertical();
        }

        void DrawWanderingTab(CompanionBehaviour script)
        {
            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
            EditorGUILayout.LabelField("Companion Wandering Around", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(wanderAround);
            if (script.wanderAround) 
            {
                EditorGUILayout.PropertyField(wanderPointTime);
                GUILayout.EndVertical();
                EditorGUILayout.Space();

                BlazeAIEditor.DrawArrayWithStyle(wanderIdleAnims, "Idle Animations");

                GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                    BlazeAIEditor.DrawPopupProperty(script.blaze.anim, "Wander Move Anim", "wanderMoveAnim", ref script.wanderMoveAnim, scripts);
                    BlazeAIEditor.CheckDisableWithRootMotion(script.blaze, ref wanderMoveSpeed);
                GUILayout.EndVertical();
            }
            else {
                GUILayout.EndVertical();
            }

            EditorGUILayout.Space();

            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                if (script.moveAwayOnContact)
                {
                    EditorGUILayout.LabelField("This makes the AI move away when contacting a certain layer. To avoid before contact, go Blaze AI > General tab and enable local avoidance. Both options work great together!", BlazeAIEditor.BoxStyle());
                }

                EditorGUILayout.LabelField("On Contact", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(moveAwayOnContact);
                if (script.moveAwayOnContact)
                {
                    EditorGUILayout.PropertyField(layersToAvoid);
                    EditorGUILayout.PropertyField(walkOnMoveAway);
                    EditorGUILayout.PropertyField(moveAwayCoolDownTime);
                }
            GUILayout.EndVertical();
        }

        void DrawAudiosTab(CompanionBehaviour script)
        {
            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Audios", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(playAudioForIdleAndWander);
                if (script.playAudioForIdleAndWander) 
                {
                    EditorGUILayout.PropertyField(audioTimer);
                    EditorGUILayout.Space();
                }
                EditorGUILayout.PropertyField(playAudioOnFollowState);
            GUILayout.EndVertical();
        }

        void DrawEventsTab()
        {
            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("State Events", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(onStateEnter);
                EditorGUILayout.PropertyField(onStateExit);
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Distance Event", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(fireEventDistance);
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(exceededDistanceEvent);
            GUILayout.EndVertical();
        }

        #endregion
    }
}
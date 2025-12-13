using UnityEditor;
using UnityEngine;

namespace BlazeAISpace
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AttackStateBehaviour))]
    public class AttackStateBehaviourInspector : Editor
    {
        #region SERIALIZED PROPERTIES

        SerializedProperty moveSpeed,
        turnSpeed,

        idleAnim,
        moveAnim,
        idleMoveT,
        targetLocationUpdateTime,

        equipWeapon,
        equipAnim,
        equipDuration,
        onEquipEvent,

        unequipAnim,
        unequipDuration,
        onUnequipEvent,
        equipAnimT,

        onStateEnter,
        onStateExit,

        ranged,
        distanceFromEnemy,
        attackDistance,
        layersCheckOnAttacking,
        attacks,
        attackEvent,
        runAttackEventOnAnimTFinish,
        chaseOnEvade,
        attackInIntervals,
        attackInIntervalsTime,

        callOthers,
        callRadius,
        showCallRadius,
        agentLayersToCall,
        callPassesColliders,
        callOthersTime,
        receiveCallFromOthers,

        moveBackwards,
        moveBackwardsDistance,
        moveBackwardsSpeed,
        moveBackwardsAnim,
        moveBackwardsAnimT,

        turnToTarget,
        turnSensitivity,
        useTurnAnims,

        strafe,
        strafeDirection,
        strafeSpeed,
        strafeTime,
        strafeWaitTime,
        leftStrafeAnim,
        rightStrafeAnim,
        strafeAnimT,
        strafeLayersToAvoid,

        rotateWhileAttacking,
        onAttackRotateSpeed,

        searchLocationRadius,
        timeToStartSearch,
        searchPoints,
        searchPointAnim,
        pointWaitTime,
        endSearchAnim,
        endSearchAnimTime,
        searchAnimsT,
        playAudioOnSearchStart,
        playAudioOnSearchEnd,

        returnPatrolAnim,
        returnPatrolAnimT,
        returnPatrolTime,
        playAudioOnReturnPatrol,

        playAttackIdleAudio,
        attackIdleAudioTime,

        playAudioOnChase,
        alwaysPlayOnChase,

        playAudioOnMoveToAttack,
        alwaysPlayOnMoveToAttack;

        #endregion

        #region EDITOR VARIABLES

        bool displayAttackEvents = true;

        string[] tabs = {"General", "Attack", "Idle", "Call Others", "Search", "Misc"};
        int tabSelected = 0;
        int tabIndex = -1;

        AttackStateBehaviour script;
        AttackStateBehaviour[] scripts;

        #endregion

        #region EDITOR METHODS

        void SetScripts()
        {
            script = (AttackStateBehaviour) target;

            Object[] objs = targets;
            scripts = new AttackStateBehaviour[objs.Length];
            for (int i = 0; i < objs.Length; i++) {
                scripts[i] = objs[i] as AttackStateBehaviour;
            }
        }

        void OnEnable()
        {
            SetScripts();
            GetSelectedTab();

            moveSpeed = serializedObject.FindProperty("moveSpeed");
            turnSpeed = serializedObject.FindProperty("turnSpeed");

            idleAnim = serializedObject.FindProperty("idleAnim");
            moveAnim = serializedObject.FindProperty("moveAnim");
            idleMoveT = serializedObject.FindProperty("idleMoveT");

            targetLocationUpdateTime = serializedObject.FindProperty("targetLocationUpdateTime");

            equipWeapon = serializedObject.FindProperty("equipWeapon");
            equipAnim = serializedObject.FindProperty("equipAnim");
            equipDuration = serializedObject.FindProperty("equipDuration");
            equipAnimT = serializedObject.FindProperty("equipAnimT");
            onEquipEvent = serializedObject.FindProperty("onEquipEvent");
    
            unequipAnim = serializedObject.FindProperty("unequipAnim");
            unequipDuration = serializedObject.FindProperty("unequipDuration");
            onUnequipEvent = serializedObject.FindProperty("onUnequipEvent");

            onStateEnter = serializedObject.FindProperty("onStateEnter");
            onStateExit = serializedObject.FindProperty("onStateExit");

            ranged = serializedObject.FindProperty("ranged");
            distanceFromEnemy = serializedObject.FindProperty("distanceFromEnemy");
            attackDistance = serializedObject.FindProperty("attackDistance");
            layersCheckOnAttacking = serializedObject.FindProperty("layersCheckOnAttacking");
            attacks = serializedObject.FindProperty("attacks");
            attackEvent = serializedObject.FindProperty("attackEvent");
            runAttackEventOnAnimTFinish = serializedObject.FindProperty("runAttackEventOnAnimTFinish");
            chaseOnEvade = serializedObject.FindProperty("chaseOnEvade");

            attackInIntervals = serializedObject.FindProperty("attackInIntervals");
            attackInIntervalsTime = serializedObject.FindProperty("attackInIntervalsTime");

            callOthers = serializedObject.FindProperty("callOthers");
            callRadius = serializedObject.FindProperty("callRadius");
            showCallRadius = serializedObject.FindProperty("showCallRadius");
            agentLayersToCall = serializedObject.FindProperty("agentLayersToCall");
            callPassesColliders = serializedObject.FindProperty("callPassesColliders");
            callOthersTime = serializedObject.FindProperty("callOthersTime");
            receiveCallFromOthers = serializedObject.FindProperty("receiveCallFromOthers");

            moveBackwards = serializedObject.FindProperty("moveBackwards");
            moveBackwardsDistance = serializedObject.FindProperty("moveBackwardsDistance");
            moveBackwardsSpeed = serializedObject.FindProperty("moveBackwardsSpeed");
            moveBackwardsAnim = serializedObject.FindProperty("moveBackwardsAnim");
            moveBackwardsAnimT = serializedObject.FindProperty("moveBackwardsAnimT");

            turnToTarget = serializedObject.FindProperty("turnToTarget");
            turnSensitivity = serializedObject.FindProperty("turnSensitivity");
            useTurnAnims = serializedObject.FindProperty("useTurnAnims");

            strafe = serializedObject.FindProperty("strafe");
            strafeDirection = serializedObject.FindProperty("strafeDirection");
            strafeSpeed = serializedObject.FindProperty("strafeSpeed");
            strafeTime = serializedObject.FindProperty("strafeTime");
            strafeWaitTime = serializedObject.FindProperty("strafeWaitTime");
            leftStrafeAnim = serializedObject.FindProperty("leftStrafeAnim");
            rightStrafeAnim = serializedObject.FindProperty("rightStrafeAnim");
            strafeAnimT = serializedObject.FindProperty("strafeAnimT");
            strafeLayersToAvoid = serializedObject.FindProperty("strafeLayersToAvoid");

            rotateWhileAttacking = serializedObject.FindProperty("rotateWhileAttacking");
            onAttackRotateSpeed = serializedObject.FindProperty("onAttackRotateSpeed");

            searchLocationRadius = serializedObject.FindProperty("searchLocationRadius");
            timeToStartSearch = serializedObject.FindProperty("timeToStartSearch");
            searchPoints = serializedObject.FindProperty("searchPoints");
            searchPointAnim = serializedObject.FindProperty("searchPointAnim");
            pointWaitTime = serializedObject.FindProperty("pointWaitTime");
            endSearchAnim = serializedObject.FindProperty("endSearchAnim");
            endSearchAnimTime = serializedObject.FindProperty("endSearchAnimTime");
            searchAnimsT = serializedObject.FindProperty("searchAnimsT");
            playAudioOnSearchStart = serializedObject.FindProperty("playAudioOnSearchStart");
            playAudioOnSearchEnd = serializedObject.FindProperty("playAudioOnSearchEnd");

            returnPatrolAnim = serializedObject.FindProperty("returnPatrolAnim");
            returnPatrolAnimT = serializedObject.FindProperty("returnPatrolAnimT");
            returnPatrolTime = serializedObject.FindProperty("returnPatrolTime");
            playAudioOnReturnPatrol = serializedObject.FindProperty("playAudioOnReturnPatrol");

            playAttackIdleAudio = serializedObject.FindProperty("playAttackIdleAudio");
            attackIdleAudioTime = serializedObject.FindProperty("attackIdleAudioTime");

            playAudioOnChase = serializedObject.FindProperty("playAudioOnChase");
            alwaysPlayOnChase = serializedObject.FindProperty("alwaysPlayOnChase");

            playAudioOnMoveToAttack = serializedObject.FindProperty("playAudioOnMoveToAttack");
            alwaysPlayOnMoveToAttack = serializedObject.FindProperty("alwaysPlayOnMoveToAttack");
        }

        public override void OnInspectorGUI () 
        {
            DrawToolbar();
            BlazeAIEditor.RefreshAnimationStateNames(script.blaze.anim);

            EditorGUILayout.LabelField("Hover on any property below for insights", EditorStyles.helpBox);
            EditorGUILayout.Space(10);
            
            tabIndex = -1;

            switch (tabSelected)
            {
                case 0:
                    DrawGeneralTab(script);
                    break;
                case 1:
                    DrawAttackTab(script);
                    break;
                case 2:
                    DrawAttackIdleTab(script);
                    break;
                case 3:
                    DrawCallOthersTab(script);
                    break;
                case 4:
                    DrawSearchAndReturnTab(script);
                    break;
                case 5:
                    DrawAudiosAndEventsTab(script);
                    break;
            }

            EditorPrefs.SetInt("BlazeAttackTabSelected", tabSelected);
            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region DRAW

        void DrawToolbar()
        {   
            GUILayout.BeginHorizontal();
            
            foreach (var item in tabs) 
            {
                tabIndex++;

                if (tabIndex == 3) 
                {
                    GUILayout.EndHorizontal();
                    EditorGUILayout.Space(0.2f);
                    GUILayout.BeginHorizontal();
                }

                if (tabIndex == tabSelected) {
                    // selected button
                    GUILayout.Button(item, BlazeAIEditor.ToolbarStyling(true), GUILayout.MinWidth(80), GUILayout.Height(35));
                }
                else {
                    // unselected buttons
                    if (GUILayout.Button(item, BlazeAIEditor.ToolbarStyling(false), GUILayout.MinWidth(80), GUILayout.Height(35))) {
                        // this will trigger when a button is pressed
                        tabSelected = tabIndex;
                    }
                }
            }

            GUILayout.EndHorizontal();
        }

        void DrawGeneralTab(AttackStateBehaviour script)
        {
            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Speeds", EditorStyles.boldLabel);
                BlazeAIEditor.CheckDisableWithRootMotion(script.blaze, ref moveSpeed);
                EditorGUILayout.PropertyField(turnSpeed);
            GUILayout.EndVertical();

            
            EditorGUILayout.Space();


            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Animations", EditorStyles.boldLabel);
                BlazeAIEditor.DrawPopupProperty(script.blaze.anim, "Idle Anim", "idleAnim", ref script.idleAnim, scripts);
                BlazeAIEditor.DrawPopupProperty(script.blaze.anim, "Move Anim", "moveAnim", ref script.moveAnim, scripts);
                EditorGUILayout.PropertyField(idleMoveT);
            GUILayout.EndVertical();
            
            
            EditorGUILayout.Space();

            
            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Chase Location Update", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(targetLocationUpdateTime);
            GUILayout.EndVertical();


            EditorGUILayout.Space();


            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Equip Weapon", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(equipWeapon);
                if (script.equipWeapon) 
                {
                    EditorGUILayout.Space();

                    // equip animation
                    BlazeAIEditor.DrawPopupProperty(script.blaze.anim, "Equip Anim", "equipAnim", ref script.equipAnim, scripts);
                    EditorGUILayout.PropertyField(equipDuration);
                    EditorGUILayout.PropertyField(equipAnimT);

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(onEquipEvent);
                    EditorGUILayout.Space();

                    // unequip animation
                    EditorGUILayout.LabelField("Unequip Weapon", EditorStyles.boldLabel);
                    BlazeAIEditor.DrawPopupProperty(script.blaze.anim, "Unequip Anim", "unequipAnim", ref script.unequipAnim, scripts);
                    EditorGUILayout.PropertyField(unequipDuration);
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(onUnequipEvent);
                }
            GUILayout.EndVertical();
        }

        void DrawAttackTab(AttackStateBehaviour script)
        {
            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Ranged AI", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(ranged);
            GUILayout.EndVertical();
            
            
            EditorGUILayout.Space();

            
            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Attacking Distances", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(distanceFromEnemy);
                EditorGUILayout.PropertyField(attackDistance);
            GUILayout.EndVertical();


            EditorGUILayout.Space();

            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Friendly-Fire Layers", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(layersCheckOnAttacking);
            GUILayout.EndVertical();


            EditorGUILayout.Space();

            
            BlazeAIEditor.DrawArrayWithStyle(attacks, "Add Attacks");


            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                displayAttackEvents = EditorGUILayout.Toggle("Display Attack Events", displayAttackEvents);
                if (displayAttackEvents) {
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(attackEvent);
                    EditorGUILayout.Space();
                }
            GUILayout.EndVertical();


            EditorGUILayout.Space();


            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Attack Event Run", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(runAttackEventOnAnimTFinish);
            GUILayout.EndVertical();


            EditorGUILayout.Space();


            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Chase Again On Target Evade Attack", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(chaseOnEvade);
            GUILayout.EndVertical();


            EditorGUILayout.Space();


            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Interval Attacks", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(attackInIntervals);
                if (script.attackInIntervals) {
                    EditorGUILayout.PropertyField(attackInIntervalsTime);
                }
            GUILayout.EndVertical();


            EditorGUILayout.Space();


            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Attack Rotate", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(onAttackRotateSpeed);
                EditorGUILayout.PropertyField(rotateWhileAttacking);
            GUILayout.EndVertical();
        }

        void DrawAttackIdleTab(AttackStateBehaviour script)
        {
            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Move Backwards", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(moveBackwards);
                if (script.moveBackwards) 
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(moveBackwardsDistance);
                    BlazeAIEditor.CheckDisableWithRootMotion(script.blaze, ref moveBackwardsSpeed);
                    EditorGUILayout.Space();

                    BlazeAIEditor.DrawPopupProperty(script.blaze.anim, "Move Backwards Anim", "moveBackwardsAnim", ref script.moveBackwardsAnim, scripts);
                    EditorGUILayout.PropertyField(moveBackwardsAnimT);
                }
            GUILayout.EndVertical();


            EditorGUILayout.Space();


            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Facing Target", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(turnToTarget);
                if (script.turnToTarget) 
                {
                    EditorGUILayout.PropertyField(turnSensitivity);
                    if (script.useTurnAnims)
                    {
                        EditorGUILayout.LabelField("Turn animations are set in Blaze AI > General tab > Waypoints sub-tab > Turning Section", BlazeAIEditor.BoxStyle());
                    }
                    EditorGUILayout.PropertyField(useTurnAnims);
                }
            GUILayout.EndVertical();


            EditorGUILayout.Space();


            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Strafing", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(strafe);
                if (script.strafe) 
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(strafeDirection);
                    BlazeAIEditor.CheckDisableWithRootMotion(script.blaze, ref strafeSpeed);
                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(strafeTime);
                    EditorGUILayout.PropertyField(strafeWaitTime);
                    EditorGUILayout.Space();

                    // left strafe animation
                    BlazeAIEditor.DrawPopupProperty(script.blaze.anim, "Left Strafe Anim", "leftStrafeAnim", ref script.leftStrafeAnim, scripts);

                    // right strafe animation
                    BlazeAIEditor.DrawPopupProperty(script.blaze.anim, "Right Strafe Anim", "rightStrafeAnim", ref script.rightStrafeAnim, scripts);

                    EditorGUILayout.PropertyField(strafeAnimT);
                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(strafeLayersToAvoid);
                }
            GUILayout.EndVertical();
        }

        void DrawCallOthersTab(AttackStateBehaviour script)
        {
            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Call Others", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(callOthers);  
                if (script.callOthers) 
                {
                    EditorGUILayout.Space();
        
                    EditorGUILayout.PropertyField(callRadius);
                    EditorGUILayout.PropertyField(showCallRadius);
                    
                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(agentLayersToCall);
                    EditorGUILayout.PropertyField(callPassesColliders);

                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(callOthersTime);
                }
            GUILayout.EndVertical();  

            EditorGUILayout.Space();

            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Should Receive Call From Others", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(receiveCallFromOthers);
            GUILayout.EndVertical();
        }

        void DrawSearchAndReturnTab(AttackStateBehaviour script)
        {
            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Returns to alert state patrol after searching is complete", BlazeAIEditor.BoxStyle());
                EditorGUILayout.LabelField("Searching Last Location", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(searchLocationRadius);
                if (script.searchLocationRadius)
                {
                    EditorGUILayout.PropertyField(timeToStartSearch);
                    GUILayout.EndVertical();

                    EditorGUILayout.Space();

                    GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                        EditorGUILayout.PropertyField(searchPoints);
                        BlazeAIEditor.DrawPopupProperty(script.blaze.anim, "Search Point Anim", "searchPointAnim", ref script.searchPointAnim, scripts);
                        EditorGUILayout.PropertyField(pointWaitTime);
                    GUILayout.EndVertical();

                    EditorGUILayout.Space();

                    GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                        BlazeAIEditor.DrawPopupProperty(script.blaze.anim, "End Search Anim", "endSearchAnim", ref script.endSearchAnim, scripts);
                        EditorGUILayout.PropertyField(endSearchAnimTime);
                    GUILayout.EndVertical();

                    EditorGUILayout.Space();

                    GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                        EditorGUILayout.PropertyField(searchAnimsT);
                    GUILayout.EndVertical();

                    EditorGUILayout.Space();
                    
                    GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                        EditorGUILayout.PropertyField(playAudioOnSearchStart);
                        EditorGUILayout.PropertyField(playAudioOnSearchEnd);
                    GUILayout.EndVertical();

                    return;
                }
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Returns to alert state patrol instantly after loosing target (without searching)", BlazeAIEditor.BoxStyle());
                EditorGUILayout.LabelField("Returning To Patrol", EditorStyles.boldLabel);
                BlazeAIEditor.DrawPopupProperty(script.blaze.anim, "Return Patrol Anim", "returnPatrolAnim", ref script.returnPatrolAnim, scripts);
                EditorGUILayout.PropertyField(returnPatrolAnimT);
                EditorGUILayout.PropertyField(returnPatrolTime);
                EditorGUILayout.PropertyField(playAudioOnReturnPatrol);
            GUILayout.EndVertical();
        }

        void DrawAudiosAndEventsTab(AttackStateBehaviour script)
        {   
            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Audios", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(playAttackIdleAudio);
                if (script.playAttackIdleAudio) 
                {
                    EditorGUILayout.PropertyField(attackIdleAudioTime);
                    EditorGUILayout.Space();
                }

                EditorGUILayout.PropertyField(playAudioOnChase);
                if (script.playAudioOnChase) 
                {
                    EditorGUILayout.PropertyField(alwaysPlayOnChase);
                    EditorGUILayout.Space();
                }

                EditorGUILayout.PropertyField(playAudioOnMoveToAttack);
                if (script.playAudioOnMoveToAttack) {
                    EditorGUILayout.PropertyField(alwaysPlayOnMoveToAttack);
                }
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("State Events", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(onStateEnter);
                EditorGUILayout.PropertyField(onStateExit);
            GUILayout.EndVertical();
        }

        void GetSelectedTab()
        {
            if (EditorPrefs.HasKey("BlazeAttackTabSelected")) {
                tabSelected = EditorPrefs.GetInt("BlazeAttackTabSelected");
            }
            else{
                tabSelected = 0;
            }   
        }

        #endregion
    }
}
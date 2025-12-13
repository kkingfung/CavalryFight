using UnityEditor;
using UnityEngine;

namespace BlazeAISpace 
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CoverShooterBehaviour))]
    public class CoverShooterBehaviourInspector : Editor
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

        distanceFromEnemy,
        attackDistance,
        layersCheckOnAttacking,
        shootingAnim,
        shootingAnimT,
        shootEvent,
        shootEvery,
        singleShotDuration,
        delayBetweenEachShot,
        totalShootTime,

        firstSightDecision,
        coverBlownDecision,
        attackEnemyCover,

        braveMeter,
        changeCoverFrequency,
        noCoverShootChance,

        callOthers,
        callRadius,
        showCallRadius,
        agentLayersToCall,
        callPassesColliders,
        callOthersTime,
        receiveCallFromOthers,

        moveForwards,
        moveForwardsToDistance,
        moveForwardsSpeed,
        moveForwardsAnim,

        moveBackwards,
        moveBackwardsDistance,
        moveBackwardsSpeed,
        moveBackwardsAnim,
        moveBackwardsAttack,

        forwardAndBackAnimT,

        strafe,
        strafeSpeed,
        strafeTime,
        strafeWaitTime,
        leftStrafeAnim,
        rightStrafeAnim,
        strafeAnimT,
        strafeLayersToAvoid,

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

        playAudioOnChase,
        alwaysPlayOnChase,

        playAudioDuringShooting,
        alwaysPlayDuringShooting,

        playAudioOnMoveToShoot,
        alwaysPlayOnMoveToShoot;

        #endregion

        #region EDITOR VARIABLES

        bool displayshootEvents = true;
        int spaceBetween = 20;

        string[] tabs = {"General", "Attack", "Idle", "Call Others", "Search", "Misc"};
        int tabSelected = 0;
        int tabIndex = -1;

        CoverShooterBehaviour script;
        CoverShooterBehaviour[] scripts;

        #endregion

        #region UNITY METHODS

        void SetScripts()
        {
            script = (CoverShooterBehaviour) target;

            Object[] objs = targets;
            scripts = new CoverShooterBehaviour[objs.Length];
            for (int i = 0; i < objs.Length; i++) {
                scripts[i] = objs[i] as CoverShooterBehaviour;
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


            distanceFromEnemy = serializedObject.FindProperty("distanceFromEnemy");
            attackDistance = serializedObject.FindProperty("attackDistance");
            layersCheckOnAttacking = serializedObject.FindProperty("layersCheckOnAttacking");
            shootingAnim = serializedObject.FindProperty("shootingAnim");
            shootingAnimT = serializedObject.FindProperty("shootingAnimT");
            shootEvent = serializedObject.FindProperty("shootEvent");
            shootEvery = serializedObject.FindProperty("shootEvery");
            singleShotDuration = serializedObject.FindProperty("singleShotDuration");
            delayBetweenEachShot = serializedObject.FindProperty("delayBetweenEachShot");
            totalShootTime = serializedObject.FindProperty("totalShootTime");


            firstSightDecision = serializedObject.FindProperty("firstSightDecision");
            coverBlownDecision = serializedObject.FindProperty("coverBlownDecision");
            attackEnemyCover = serializedObject.FindProperty("attackEnemyCover");


            braveMeter = serializedObject.FindProperty("braveMeter");
            changeCoverFrequency = serializedObject.FindProperty("changeCoverFrequency");
            noCoverShootChance = serializedObject.FindProperty("noCoverShootChance");


            callOthers = serializedObject.FindProperty("callOthers");
            callRadius = serializedObject.FindProperty("callRadius");
            showCallRadius = serializedObject.FindProperty("showCallRadius");
            agentLayersToCall = serializedObject.FindProperty("agentLayersToCall");
            callPassesColliders = serializedObject.FindProperty("callPassesColliders");
            callOthersTime = serializedObject.FindProperty("callOthersTime");
            receiveCallFromOthers = serializedObject.FindProperty("receiveCallFromOthers");


            moveForwards = serializedObject.FindProperty("moveForwards");
            moveForwardsToDistance = serializedObject.FindProperty("moveForwardsToDistance");
            moveForwardsSpeed = serializedObject.FindProperty("moveForwardsSpeed");
            moveForwardsAnim = serializedObject.FindProperty("moveForwardsAnim");


            moveBackwards = serializedObject.FindProperty("moveBackwards");
            moveBackwardsDistance = serializedObject.FindProperty("moveBackwardsDistance");
            moveBackwardsSpeed = serializedObject.FindProperty("moveBackwardsSpeed");
            moveBackwardsAnim = serializedObject.FindProperty("moveBackwardsAnim");
            forwardAndBackAnimT = serializedObject.FindProperty("forwardAndBackAnimT");
            moveBackwardsAttack = serializedObject.FindProperty("moveBackwardsAttack");


            strafe = serializedObject.FindProperty("strafe");
            strafeSpeed = serializedObject.FindProperty("strafeSpeed");
            strafeTime = serializedObject.FindProperty("strafeTime");
            strafeWaitTime = serializedObject.FindProperty("strafeWaitTime");
            leftStrafeAnim = serializedObject.FindProperty("leftStrafeAnim");
            rightStrafeAnim = serializedObject.FindProperty("rightStrafeAnim");
            strafeAnimT = serializedObject.FindProperty("strafeAnimT");
            strafeLayersToAvoid = serializedObject.FindProperty("strafeLayersToAvoid");


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


            playAudioOnChase = serializedObject.FindProperty("playAudioOnChase");
            alwaysPlayOnChase = serializedObject.FindProperty("alwaysPlayOnChase");

            playAudioDuringShooting = serializedObject.FindProperty("playAudioDuringShooting");
            alwaysPlayDuringShooting = serializedObject.FindProperty("alwaysPlayDuringShooting");

            playAudioOnMoveToShoot = serializedObject.FindProperty("playAudioOnMoveToShoot");
            alwaysPlayOnMoveToShoot = serializedObject.FindProperty("alwaysPlayOnMoveToShoot");
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
                    DrawAttackTab();
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

            EditorPrefs.SetInt("BlazeShooterTabSelected", tabSelected);
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

                if (tabIndex == 3) {
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

        void DrawGeneralTab(CoverShooterBehaviour script)
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
                    BlazeAIEditor.DrawPopupProperty(script.blaze.anim, "Equip Anim", "equipAnim", ref script.equipAnim, scripts);
                    EditorGUILayout.PropertyField(equipDuration);
                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(equipAnimT);
                    EditorGUILayout.Space(spaceBetween);
                    
                    EditorGUILayout.PropertyField(onEquipEvent);
                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField("Unequip Weapon", EditorStyles.boldLabel);
                    BlazeAIEditor.DrawPopupProperty(script.blaze.anim, "Unequip Anim", "unequipAnim", ref script.unequipAnim, scripts);
                    EditorGUILayout.PropertyField(unequipDuration);
                    EditorGUILayout.Space(spaceBetween);

                    EditorGUILayout.PropertyField(onUnequipEvent);
                }
            GUILayout.EndVertical();
        }

        void DrawAttackTab()
        {
            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Distances", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(distanceFromEnemy);
                EditorGUILayout.PropertyField(attackDistance);
            GUILayout.EndVertical();


            EditorGUILayout.Space();


            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Friendly-Fire Layers", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(layersCheckOnAttacking);
            GUILayout.EndVertical();


            EditorGUILayout.Space();


            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Shoot Animation", EditorStyles.boldLabel);
                BlazeAIEditor.DrawPopupProperty(script.blaze.anim, "Shooting Anim", "shootingAnim", ref script.shootingAnim, scripts);
                EditorGUILayout.PropertyField(shootingAnimT);
            GUILayout.EndVertical();


            EditorGUILayout.Space();


            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Timing", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(shootEvery);
                EditorGUILayout.PropertyField(singleShotDuration);
                EditorGUILayout.PropertyField(delayBetweenEachShot);
                EditorGUILayout.PropertyField(totalShootTime);
            GUILayout.EndVertical();


            EditorGUILayout.Space();
            

            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                displayshootEvents = EditorGUILayout.Toggle("Display Attack Events", displayshootEvents);
                if (displayshootEvents) 
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(shootEvent);
                }
            GUILayout.EndVertical();
            

            EditorGUILayout.Space();

            
            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Decisions", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(firstSightDecision);
                EditorGUILayout.PropertyField(coverBlownDecision);
                EditorGUILayout.PropertyField(attackEnemyCover);
            GUILayout.EndVertical();

            
            EditorGUILayout.Space();


            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Braveness", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(braveMeter);
            GUILayout.EndVertical();


            EditorGUILayout.Space();

            
            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Change Cover", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(changeCoverFrequency);
            GUILayout.EndVertical();


            EditorGUILayout.Space();


            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("No Cover Found Shoot Chance", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(noCoverShootChance);
            GUILayout.EndVertical();
        }

        void DrawAttackIdleTab(CoverShooterBehaviour script)
        {
            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Move Forwards", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(moveForwards);
                if (script.moveForwards) 
                {
                    EditorGUILayout.PropertyField(moveForwardsToDistance);
                    BlazeAIEditor.CheckDisableWithRootMotion(script.blaze, ref moveForwardsSpeed);
                    BlazeAIEditor.DrawPopupProperty(script.blaze.anim, "Move Forwards Anim", "moveForwardsAnim", ref script.moveForwardsAnim, scripts);
                }
            GUILayout.EndVertical();


            EditorGUILayout.Space();


            if (script.moveForwards && script.moveBackwards) 
            {
                EditorGUILayout.LabelField("To avoid jittering, [Move Backwards Distance] is locked to be always less than [Move Forwards To Distance] - This is because both Move Forwards and Backwards are enabled.", BlazeAIEditor.BoxStyle());
                EditorGUILayout.Space();
            }


            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Move Backwards", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(moveBackwards);
                if (script.moveBackwards) 
                {
                    EditorGUILayout.PropertyField(moveBackwardsDistance);
                    BlazeAIEditor.CheckDisableWithRootMotion(script.blaze, ref moveBackwardsSpeed);
                    BlazeAIEditor.DrawPopupProperty(script.blaze.anim, "Move Backwards Anim", "moveBackwardsAnim", ref script.moveBackwardsAnim, scripts);
                    EditorGUILayout.PropertyField(moveBackwardsAttack);
                }
            GUILayout.EndVertical();


            EditorGUILayout.Space();


            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Animation T of Moving Forwards/Backwards", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(forwardAndBackAnimT);
            GUILayout.EndVertical();


            EditorGUILayout.Space();


            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Strafing", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(strafe);
                if (script.strafe) 
                {
                    BlazeAIEditor.CheckDisableWithRootMotion(script.blaze, ref strafeSpeed);
                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(strafeTime);
                    EditorGUILayout.PropertyField(strafeWaitTime);
                    EditorGUILayout.Space();

                    BlazeAIEditor.DrawPopupProperty(script.blaze.anim, "Left Strafe Anim", "leftStrafeAnim", ref script.leftStrafeAnim, scripts);
                    BlazeAIEditor.DrawPopupProperty(script.blaze.anim, "Right Strafe Anim", "rightStrafeAnim", ref script.rightStrafeAnim, scripts);
                    EditorGUILayout.PropertyField(strafeAnimT);
                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(strafeLayersToAvoid);
                }
            GUILayout.EndVertical();
        }

        void DrawCallOthersTab(CoverShooterBehaviour script)
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

        void DrawSearchAndReturnTab(CoverShooterBehaviour script)
        {
            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Returns to alert state patrol after searching is complete", BlazeAIEditor.BoxStyle());
                EditorGUILayout.LabelField("Searching Location", EditorStyles.boldLabel);
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

        void DrawAudiosAndEventsTab(CoverShooterBehaviour script)
        {
            GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
                EditorGUILayout.LabelField("Audios", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(playAudioOnChase);
                if (script.playAudioOnChase) 
                {
                    EditorGUILayout.PropertyField(alwaysPlayOnChase);
                    EditorGUILayout.Space();
                }

                EditorGUILayout.PropertyField(playAudioDuringShooting);
                if (script.playAudioDuringShooting) 
                {
                    EditorGUILayout.PropertyField(alwaysPlayDuringShooting);
                    EditorGUILayout.Space();
                }

                EditorGUILayout.PropertyField(playAudioOnMoveToShoot);
                if (script.playAudioOnMoveToShoot) 
                {
                    EditorGUILayout.PropertyField(alwaysPlayOnMoveToShoot);
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
            if (EditorPrefs.HasKey("BlazeShooterTabSelected")) {
                tabSelected = EditorPrefs.GetInt("BlazeShooterTabSelected");
            }
            else {
                tabSelected = 0;
            }   
        }

        #endregion
    }
}
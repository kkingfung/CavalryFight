using UnityEditor;
using UnityEngine;
using BlazeAISpace;
using UnityEditor.Animations;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

[CanEditMultipleObjects]
[CustomEditor(typeof(BlazeAI), true)]
[InitializeOnLoad]

public class BlazeAIEditor : Editor
{
    #region EDITOR VARIABLES

    string[] tabs = {"General", "States", "Vision", "Off Mesh", "Distract", "Hit", "Death", "Companion"};
    int tabSelected = 0;
    int tabIndex = -1;

    string[] generalSubTabs = {"Setup", "Waypoints", "Misc", "Warnings"};
    int generalSubTabSelected = 0;
    int generalSubTabIndex = -1;

    string[] visionSubTabs = {"Setup", "Targets", "Events", "Settings"};
    int visionSubTabSelected = 0;
    int visionSubTabIndex = -1;

    BlazeAI script;
    public BlazeAI[] scripts;

    public static string[] animationStateNamesArr = new string[200];
    public static List<string> duplicateAnimNames = new List<string>();
    int getAnimationStatesFrames = 0;

    #endregion

    #region SERIALIZED PROPERTIES

    SerializedProperty useRootMotion,
    groundLayers,

    audioScriptable,
    agentAudio,

    useLocalAvoidance,
    layersToAvoid,
    avoidanceRadius,
    showAvoidanceRadius,
    avoidanceOffsetStrength,
    avoidanceSteerSpeed,
    avoidIfWithinAngle,
    maxAvoidanceTime,
    avoidanceCoolDownTime,

    waypointsProp,
    waypoints,
    loop,
    waypointsRotation,
    timeBeforeTurning,
    turnSpeed,
    showWaypoints,
    randomize,
    randomizeRadius,
    minAndMaxLevelDiff,
    showRandomizeRadius,
    useMovementTurning,
    movementTurningSensitivity,
    useTurnAnims,
    rightTurnAnimNormal,
    leftTurnAnimNormal,
    rightTurnAnimAlert,
    leftTurnAnimAlert,
    turningAnimT,
    
    checkEnemyContact,
    enemyContactRadius,
    showEnemyContactRadius,
    
    useNormalStateOnAwake,
    normalStateBehaviour,

    useAlertStateOnAwake,
    alertStateBehaviour,

    attackStateBehaviour,
    coverShooterMode,
    coverShooterBehaviour,
    goingToCoverBehaviour,

    useSurprisedState,
    surprisedStateBehaviour,

    visionProp,
    visionActive,
    layersToDetect,
    hostileAndAlertLayers,
    hostileTags,
    alertTags,
    visionPosition,
    maxSightLevel,
    checkTargetHeight,
    minSightLevel,
    useMinLevel,
    visionDuringNormalState,
    visionDuringAlertState,
    visionDuringAttackState,
    head,
    showNormalVision,
    normalVisionColor,
    showAlertVision,
    alertVisionColor,
    showAttackVision,
    attackVisionColor,
    showMaxSightLevel,
    showMinSightLevel,
    enemyEnterEvent,
    enemyLeaveEvent,
    visionFrameSkipping,
    multiRayVision,
    useVisionMeter,
    visionMeterSpeeds,
    distractByVMValue,
    distractIfVMReaches,
    overrideVMValueOnDistract,
    vmValueOnDistract,
    dontDecrementVMOnDistract,

    canDistract,
    distractedStateBehaviour,
    priorityLevel,
    turnOnEveryDistraction,
    turnAlertOnDistract,
    playDistractedAudios,

    useHitCooldown,
    maxHitCount,
    hitCooldown,
    hitStateBehaviour,
    
    deathAnim,
    deathAnimT,
    disableCapsuleColliderOnDeath,
    playDeathAudio,
    deathCallRadius,
    agentLayersToDeathCall,
    showDeathCallRadius,
    useRagdoll,
    useNaturalVelocity,
    hipBone,
    deathRagdollForce,
    deathEvent,
    destroyOnDeath,
    timeBeforeDestroy,

    friendly,

    distanceCull,
    animToPlayOnCull,

    ignoreUnreachableEnemy,
    fallBackPoints,
    showPoints,
    
    useOffMeshLinks,
    jumpMethod,
    jumpHeight,
    jumpDuration,
    useMovementSpeedForJump,
    jumpSpeed,
    jumpAnim,
    fallAnim,
    jumpAnimT,
    onTeleportStart,
    onTeleportEnd,

    climbLadders,
    ladderLayers,
    climbUpAnim,
    climbUpSpeed,
    climbToTopAnim,
    climbToTopDuration,
    climbToTopHeadRoom,
    climbAnimT,
    
    warnEmptyBehavioursOnStart,
    warnEmptyAnimations,
    warnEmptyAudio,
    warnAnomaly,

    companionMode,
    companionTo,
    companionBehaviour;

    #endregion

    #region UNITY METHODS
    
    void OnEnable()
    {
        script = (BlazeAI) target;
        script.SetVisionT();

        Object[] objs = targets;
        scripts = new BlazeAI[objs.Length];
        for (int i = 0; i < objs.Length; i++)
        {
            scripts[i] = objs[i] as BlazeAI;
            scripts[i].SetVisionT();
        }

        GetLastTabsSelected();

        useRootMotion = serializedObject.FindProperty("useRootMotion");
        groundLayers = serializedObject.FindProperty("groundLayers");

        audioScriptable = serializedObject.FindProperty("audioScriptable");
        agentAudio = serializedObject.FindProperty("agentAudio");

        useLocalAvoidance = serializedObject.FindProperty("useLocalAvoidance");
        layersToAvoid = serializedObject.FindProperty("layersToAvoid");
        avoidanceRadius = serializedObject.FindProperty("avoidanceRadius");
        showAvoidanceRadius = serializedObject.FindProperty("showAvoidanceRadius");
        avoidanceOffsetStrength = serializedObject.FindProperty("avoidanceOffsetStrength");
        avoidanceSteerSpeed = serializedObject.FindProperty("avoidanceSteerSpeed");
        avoidIfWithinAngle = serializedObject.FindProperty("avoidIfWithinAngle");
        maxAvoidanceTime = serializedObject.FindProperty("maxAvoidanceTime");
        avoidanceCoolDownTime = serializedObject.FindProperty("avoidanceCoolDownTime");


        GetWaypointsProperties();
        GetVisionProperties();


        checkEnemyContact = serializedObject.FindProperty("checkEnemyContact");
        enemyContactRadius = serializedObject.FindProperty("enemyContactRadius");
        showEnemyContactRadius = serializedObject.FindProperty("showEnemyContactRadius");


        distanceCull = serializedObject.FindProperty("distanceCull");
        animToPlayOnCull = serializedObject.FindProperty("animToPlayOnCull");


        friendly = serializedObject.FindProperty("friendly");


        ignoreUnreachableEnemy = serializedObject.FindProperty("ignoreUnreachableEnemy");
        fallBackPoints = serializedObject.FindProperty("fallBackPoints");
        showPoints = serializedObject.FindProperty("showPoints");


        useOffMeshLinks = serializedObject.FindProperty("useOffMeshLinks");
        jumpMethod = serializedObject.FindProperty("jumpMethod");
        jumpHeight = serializedObject.FindProperty("jumpHeight");
        jumpDuration = serializedObject.FindProperty("jumpDuration");
        useMovementSpeedForJump = serializedObject.FindProperty("useMovementSpeedForJump");
        jumpSpeed = serializedObject.FindProperty("jumpSpeed");
        jumpAnim = serializedObject.FindProperty("jumpAnim");
        fallAnim = serializedObject.FindProperty("fallAnim");
        jumpAnimT = serializedObject.FindProperty("jumpAnimT");
        onTeleportStart = serializedObject.FindProperty("onTeleportStart");
        onTeleportEnd = serializedObject.FindProperty("onTeleportEnd");

        climbLadders = serializedObject.FindProperty("climbLadders");
        ladderLayers = serializedObject.FindProperty("ladderLayers");
        climbUpAnim = serializedObject.FindProperty("climbUpAnim");
        climbUpSpeed = serializedObject.FindProperty("climbUpSpeed");
        climbToTopAnim = serializedObject.FindProperty("climbToTopAnim");
        climbToTopDuration = serializedObject.FindProperty("climbToTopDuration");
        climbToTopHeadRoom = serializedObject.FindProperty("climbToTopHeadRoom");
        climbAnimT = serializedObject.FindProperty("climbAnimT");

        warnEmptyBehavioursOnStart = serializedObject.FindProperty("warnEmptyBehavioursOnStart");
        warnEmptyAnimations = serializedObject.FindProperty("warnEmptyAnimations");
        warnEmptyAudio = serializedObject.FindProperty("warnEmptyAudio");
        warnAnomaly = serializedObject.FindProperty("warnAnomaly");


        // STATES TAB
        useNormalStateOnAwake = serializedObject.FindProperty("useNormalStateOnAwake");
        normalStateBehaviour = serializedObject.FindProperty("normalStateBehaviour");


        useAlertStateOnAwake = serializedObject.FindProperty("useAlertStateOnAwake");
        alertStateBehaviour = serializedObject.FindProperty("alertStateBehaviour");


        attackStateBehaviour = serializedObject.FindProperty("attackStateBehaviour");
        coverShooterMode = serializedObject.FindProperty("coverShooterMode");
        coverShooterBehaviour = serializedObject.FindProperty("coverShooterBehaviour");
        goingToCoverBehaviour = serializedObject.FindProperty("goingToCoverBehaviour");


        // SURPRISED TAB
        useSurprisedState = serializedObject.FindProperty("useSurprisedState");
        surprisedStateBehaviour = serializedObject.FindProperty("surprisedStateBehaviour");


        // DISTRACT TAB
        canDistract = serializedObject.FindProperty("canDistract");
        distractedStateBehaviour = serializedObject.FindProperty("distractedStateBehaviour");
        priorityLevel = serializedObject.FindProperty("priorityLevel");
        turnOnEveryDistraction = serializedObject.FindProperty("turnOnEveryDistraction");
        turnAlertOnDistract = serializedObject.FindProperty("turnAlertOnDistract");
        playDistractedAudios = serializedObject.FindProperty("playDistractedAudios");


        // HIT TAB
        useHitCooldown = serializedObject.FindProperty("useHitCooldown");
        maxHitCount = serializedObject.FindProperty("maxHitCount");
        hitCooldown = serializedObject.FindProperty("hitCooldown");
        hitStateBehaviour = serializedObject.FindProperty("hitStateBehaviour");


        // DEATH TAB
        deathAnim = serializedObject.FindProperty("deathAnim");
        deathAnimT = serializedObject.FindProperty("deathAnimT");
        disableCapsuleColliderOnDeath = serializedObject.FindProperty("disableCapsuleColliderOnDeath");
        playDeathAudio = serializedObject.FindProperty("playDeathAudio");
        deathCallRadius = serializedObject.FindProperty("deathCallRadius");
        agentLayersToDeathCall = serializedObject.FindProperty("agentLayersToDeathCall");
        showDeathCallRadius = serializedObject.FindProperty("showDeathCallRadius");
        deathEvent = serializedObject.FindProperty("deathEvent");
        useRagdoll = serializedObject.FindProperty("useRagdoll");
        useNaturalVelocity = serializedObject.FindProperty("useNaturalVelocity");
        hipBone = serializedObject.FindProperty("hipBone");
        deathRagdollForce = serializedObject.FindProperty("deathRagdollForce");
        destroyOnDeath = serializedObject.FindProperty("destroyOnDeath");
        timeBeforeDestroy = serializedObject.FindProperty("timeBeforeDestroy");


        // COMPANION TAB
        companionMode = serializedObject.FindProperty("companionMode");
        companionTo = serializedObject.FindProperty("companionTo");
        companionBehaviour = serializedObject.FindProperty("companionBehaviour");
    }

    public void OnSceneGUI()
    {
        if (script == null) return;

        if (script.vision != null)
        {
            foreach (var item in scripts)
            {
                item.vision.ShowVisionSpheres(item.visionT, item.transform);
            }
        }

        if (script.waypoints.showWaypoints) {
            DrawWaypointHandles();
        }

        if (script.showPoints) {
            DrawFallBackPointsHandles();
        }
    }

    public override void OnInspectorGUI ()
    {   
        StyleToolbar();
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Hover on any property below for insights", EditorStyles.helpBox);
        EditorGUILayout.Space(5);

        // reset the tabs
        tabIndex = -1;
        generalSubTabIndex = -1;
        visionSubTabIndex = -1;
        
        getAnimationStatesFrames++;
        if (getAnimationStatesFrames >= 5) 
        {
            GetAnimatorStates(script.anim);
            getAnimationStatesFrames = 0;
        }
        
        // tab selection
        switch (tabSelected)
        {
            case 0:
                GeneralTab(script);
                break;
            case 1:
                StatesTab(script);
                break;
            case 2:
                VisionTab();
                break;
            case 3:
                OffMeshTab(script);
                break;
            case 4:
                DistractionsTab(script);
                break;
            case 5:
                HitTab(script);
                break;
            case 6:
                DeathTab(script);
                break;
            case 7:
                CompanionTab(script);
                break;
        }

        EditorPrefs.SetInt("BlazeTabSelected", tabSelected);
        EditorPrefs.SetInt("BlazeGeneralSubTabSelected", generalSubTabSelected);
        EditorPrefs.SetInt("BlazeVisionSubTabSelected", visionSubTabSelected);
        
        serializedObject.ApplyModifiedProperties();
    }

    #endregion

    #region DRAWING INSPECTOR

    void StyleToolbar()
    {   
        GUILayout.BeginHorizontal();
        
        foreach (string item in tabs) 
        {
            tabIndex++;

            if (tabIndex == 4) 
            {
                GUILayout.EndHorizontal();
                EditorGUILayout.Space(0.2f);
                GUILayout.BeginHorizontal();
            }

            if (tabIndex == tabSelected) {
                // selected button
                GUILayout.Button(item, ToolbarStyling(true), GUILayout.MinWidth(80), GUILayout.Height(35));
            }
            else {
                // unselected buttons
                if (GUILayout.Button(item, ToolbarStyling(false), GUILayout.MinWidth(80), GUILayout.Height(35))) {
                    // this will get set when button is pressed
                    tabSelected = tabIndex;
                }
            }
        }

        GUILayout.EndHorizontal();

        // general sub tabs
        if (tabSelected == 0)
        {
            EditorGUILayout.Space(5);
            GUILayout.BeginHorizontal(ToolbarSubTabStyling(false));

            foreach (string subTab in generalSubTabs) 
            {
                generalSubTabIndex++;

                if (generalSubTabIndex == generalSubTabSelected) {
                    GUILayout.Button(subTab, ToolbarSubTabStyling(true), GUILayout.MinWidth(70), GUILayout.Height(25));
                }
                else {
                    if (GUILayout.Button(subTab, ToolbarSubTabStyling(false), GUILayout.MinWidth(70), GUILayout.Height(25))) {
                        generalSubTabSelected = generalSubTabIndex;
                    }
                }
            }

            GUILayout.EndHorizontal();
            return;
        }

        // vision sub tabs
        if (tabSelected == 2)
        {
            EditorGUILayout.Space(5);
            GUILayout.BeginHorizontal(ToolbarSubTabStyling(false));

            foreach (string subTab in visionSubTabs) 
            {
                visionSubTabIndex++;

                if (visionSubTabIndex == visionSubTabSelected) {
                    GUILayout.Button(subTab, ToolbarSubTabStyling(true), GUILayout.MinWidth(70), GUILayout.Height(25));
                }
                else {
                    if (GUILayout.Button(subTab, ToolbarSubTabStyling(false), GUILayout.MinWidth(70), GUILayout.Height(25))) {
                        visionSubTabSelected = visionSubTabIndex;
                    }
                }
            }

            GUILayout.EndHorizontal();
        }
    }

    // render the general tab properties
    void GeneralTab(BlazeAI script)
    {   
        // setup sub tab
        if (generalSubTabSelected == 0) {
            DrawGeneralSetup();
            EditorGUILayout.Space();
            return;
        }

        // waypoints sub tab
        if (generalSubTabSelected == 1) {
            DrawGeneralWaypoints();
            EditorGUILayout.Space();
            return;
        }

        // misc sub tab
        if (generalSubTabSelected == 2) {
            DrawGeneralMisc();
            EditorGUILayout.Space();
            return;
        }

        // warnings sub tab
        if (generalSubTabSelected == 3) {
            DrawGeneralWarnings();
            EditorGUILayout.Space();
        }
    }

    // render the states classes
    void StatesTab(BlazeAI script)
    {
        // normal state
        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("Normal State", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useNormalStateOnAwake);
            EditorGUILayout.PropertyField(normalStateBehaviour);
        GUILayout.EndVertical();
        
        EditorGUILayout.Space();        

        // alert state
        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("Alert State", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useAlertStateOnAwake);
            EditorGUILayout.PropertyField(alertStateBehaviour);
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        // attack state
        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("Attack State", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(script.coverShooterMode);
                EditorGUILayout.LabelField("This behaviour is for melee & ranged.", EditorStyles.helpBox);
                EditorGUILayout.PropertyField(attackStateBehaviour);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("To add cover shooter behaviours, enable this mode first before clicking the button. You can dynamically switch the AI attack style by toggling this mode.", EditorStyles.helpBox);
            EditorGUILayout.PropertyField(coverShooterMode);
            EditorGUI.BeginDisabledGroup(!script.coverShooterMode);
                EditorGUILayout.PropertyField(coverShooterBehaviour);
                EditorGUILayout.PropertyField(goingToCoverBehaviour);
            EditorGUI.EndDisabledGroup();
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        // surprised state
        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("An optional & temporary state triggered when the AI spots a target while in normal state. To add this behaviour, please enable [Use Surprised State] first before clicking the button.", EditorStyles.helpBox);
            EditorGUILayout.LabelField("Surprised State", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useSurprisedState);
            EditorGUILayout.PropertyField(surprisedStateBehaviour);
        GUILayout.EndVertical();

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Add Behaviours", GUILayout.Height(40))) {
            script.SetPrimeBehaviours();
        }

        EditorGUILayout.Space();
    }

    void VisionTab()
    {
        if (visionSubTabSelected == 0) {
            DrawVisionSetup();
            EditorGUILayout.Space();
            return;
        }

        if (visionSubTabSelected == 1) {
            DrawVisionTargets();
            EditorGUILayout.Space();
            return;
        }

        if (visionSubTabSelected == 2) {
            DrawVisionEvents();
            EditorGUILayout.Space();
            return;
        }

        if (visionSubTabSelected == 3) {
            DrawVisionSettings();
            EditorGUILayout.Space();
            return;
        }
    }

    void OffMeshTab(BlazeAI script)
    {
        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("Off Mesh Links", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useOffMeshLinks);
        GUILayout.EndVertical();
        
        if (!script.useOffMeshLinks) 
        {
            return;
        }

        EditorGUILayout.Space();

        GUILayout.BeginVertical(BlockStyle());
        EditorGUILayout.PropertyField(jumpMethod);

        if (script.jumpMethod == BlazeAI.OffMeshLinkJumpMethod.Teleport) 
        {
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(onTeleportStart);
            EditorGUILayout.PropertyField(onTeleportEnd);
            GUILayout.EndVertical();
        }

        if (script.jumpMethod == BlazeAI.OffMeshLinkJumpMethod.Parabola) 
        {
            EditorGUILayout.PropertyField(jumpHeight);
            EditorGUILayout.PropertyField(jumpDuration);
            GUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        if (script.jumpMethod == BlazeAI.OffMeshLinkJumpMethod.NormalSpeed) 
        {
            EditorGUILayout.PropertyField(useMovementSpeedForJump);

            if (!script.useMovementSpeedForJump) {
                EditorGUILayout.PropertyField(jumpSpeed);
            }

            GUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        if (script.jumpMethod == BlazeAI.OffMeshLinkJumpMethod.NormalSpeed || script.jumpMethod == BlazeAI.OffMeshLinkJumpMethod.Parabola) 
        {
            GUILayout.BeginVertical(BlockStyle());
                DrawPopupProperty(script.anim, "Jump Anim", "jumpAnim", ref script.jumpAnim, scripts);
                DrawPopupProperty(script.anim, "Fall Anim", "fallAnim", ref script.fallAnim, scripts);
                EditorGUILayout.PropertyField(jumpAnimT);
            GUILayout.EndVertical();
        }
        

        // END VERTICAL IS INSIDE THE IF BLOCKS ABOVE


        EditorGUILayout.Space();
        
        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("Climbing Ladders", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(climbLadders);
        GUILayout.EndVertical();

        if (!script.climbLadders) 
        {
            return;
        }

        EditorGUILayout.Space();

        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.PropertyField(ladderLayers);
            DrawPopupProperty(script.anim, "Climb Up Anim", "climbUpAnim", ref script.climbUpAnim, scripts);
            EditorGUILayout.PropertyField(climbUpSpeed);
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        GUILayout.BeginVertical(BlockStyle());
            DrawPopupProperty(script.anim, "Climb To Top Anim", "climbToTopAnim", ref script.climbToTopAnim, scripts);
            EditorGUILayout.PropertyField(climbToTopDuration);
            EditorGUILayout.PropertyField(climbToTopHeadRoom);
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.PropertyField(climbAnimT);
        GUILayout.EndVertical();
    }

    // render the distractions tab class
    void DistractionsTab(BlazeAI script)
    {
        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("Distracted State", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(canDistract);
            EditorGUILayout.PropertyField(distractedStateBehaviour);
        GUILayout.EndVertical();

        EditorGUILayout.Space();
        
        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.PropertyField(priorityLevel);
            EditorGUILayout.PropertyField(turnOnEveryDistraction);
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        // turn to alert
        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("Turn To Alert", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(turnAlertOnDistract);
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        // audio
        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(playDistractedAudios);
        GUILayout.EndVertical();

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Add Behaviour", GUILayout.Height(40))) {
            script.SetDistractedBehaviour();
        }
        
        EditorGUILayout.Space(5);
    }

    // render the hits tab class
    void HitTab(BlazeAI script)
    {
        GUILayout.BeginVertical(BlockStyle());
        EditorGUILayout.LabelField("Hit State", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(useHitCooldown);
        if (script.useHitCooldown) 
        {
            EditorGUILayout.PropertyField(maxHitCount);
            EditorGUILayout.PropertyField(hitCooldown);
            GUILayout.EndVertical();
        }
        else {
            GUILayout.EndVertical();
        }

        EditorGUILayout.Space();

        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.PropertyField(hitStateBehaviour);
        GUILayout.EndVertical();

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Add Behaviour", GUILayout.Height(40))) {
            script.SetHitBehaviour();
        }
    }

    // render the death tab class
    void DeathTab(BlazeAI script)
    {
        GUILayout.BeginVertical(BlockStyle());

            EditorGUILayout.LabelField("Death", EditorStyles.boldLabel);
            DrawPopupProperty(script.anim, "Death Anim", "deathAnim", ref script.deathAnim, scripts);
            EditorGUILayout.PropertyField(deathAnimT);
        
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("Disable Capsule", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(disableCapsuleColliderOnDeath);
        GUILayout.EndVertical();

        EditorGUILayout.Space();
        
        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(playDeathAudio);
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("Call Others", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(deathCallRadius);
            EditorGUILayout.PropertyField(agentLayersToDeathCall);
            EditorGUILayout.PropertyField(showDeathCallRadius);
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("Ragdoll", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useRagdoll);
            if (script.useRagdoll) 
            {
                EditorGUILayout.PropertyField(useNaturalVelocity);

                if (!script.useNaturalVelocity) {
                    EditorGUILayout.PropertyField(hipBone);
                    EditorGUILayout.PropertyField(deathRagdollForce);
                }
            }
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.PropertyField(deathEvent);
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("Destroy", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(destroyOnDeath);
            if (script.destroyOnDeath) {
                EditorGUILayout.PropertyField(timeBeforeDestroy);
            }
        GUILayout.EndVertical();

        EditorGUILayout.Space();
    }

    // render the companion tab
    void CompanionTab(BlazeAI script)
    {
        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("Companion Mode", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(companionMode);
            EditorGUILayout.PropertyField(companionTo);
            EditorGUILayout.PropertyField(companionBehaviour);
        GUILayout.EndVertical();

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Add Companion Behaviour", GUILayout.Height(40))) {
            script.SetCompanionBehaviour();
        }
    }

    #endregion

    #region DRAW SUBTABS 

    // general sub tabs
    void DrawGeneralSetup()
    {
        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useRootMotion);
            EditorGUILayout.PropertyField(groundLayers);
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(audioScriptable);
            EditorGUILayout.PropertyField(agentAudio);
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        GUILayout.BeginVertical(BlockStyle());
            if (script.useLocalAvoidance)
            {
                EditorGUILayout.LabelField("Local avoidance is for AIs avoiding each other or companions. Do not add obstacle layers!", BoxStyle());
            }
            EditorGUILayout.LabelField("Local Avoidance", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useLocalAvoidance);
            EditorGUILayout.Space();
            if (script.useLocalAvoidance)
            {
                EditorGUILayout.PropertyField(layersToAvoid);
                EditorGUILayout.PropertyField(avoidanceRadius);
                EditorGUILayout.PropertyField(showAvoidanceRadius);
                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(avoidanceOffsetStrength);
                EditorGUILayout.PropertyField(avoidanceSteerSpeed);
                EditorGUILayout.PropertyField(avoidIfWithinAngle);
                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(maxAvoidanceTime);
                EditorGUILayout.PropertyField(avoidanceCoolDownTime);
            }
        GUILayout.EndVertical();
    }

    void DrawGeneralWaypoints()
    {
        int space = 10;

        // waypoints
        GUILayout.BeginVertical();
            if (script.waypoints.randomize) 
            {
                EditorGUILayout.Space(space);
                EditorGUILayout.LabelField("Locked (Randomize is enabled)", BoxStyle());
            }

            EditorGUI.BeginDisabledGroup(script.waypoints.randomize);
                DrawArrayWithStyle(waypoints, "Set Patrol Routes", true);

                GUILayout.BeginVertical(BlockStyle());
                    EditorGUILayout.LabelField("Loop Waypoints", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(loop);
                GUILayout.EndVertical();
            EditorGUI.EndDisabledGroup();

        GUILayout.EndVertical();

        EditorGUILayout.Space(space);

        // waypoint rotations
        GUILayout.BeginVertical();
            DrawArrayWithStyle(waypointsRotation, "Set Waypoints Direction");

            GUILayout.BeginVertical(BlockStyle());
                EditorGUILayout.PropertyField(timeBeforeTurning);
                EditorGUILayout.PropertyField(turnSpeed);
            GUILayout.EndVertical();
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        // show waypoints
        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("Show & Set Waypoints In Scene View", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(showWaypoints);
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        // randomize
        GUILayout.BeginVertical(BlockStyle());
            if (script.waypoints.loop) {
                EditorGUILayout.LabelField("Locked (Loop is enabled)", BoxStyle());
            }

            EditorGUI.BeginDisabledGroup(script.waypoints.loop);
                EditorGUILayout.LabelField("Randomized Waypoints", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(randomize);
                if (script.waypoints.randomize) {
                    EditorGUILayout.PropertyField(randomizeRadius);
                    EditorGUILayout.PropertyField(minAndMaxLevelDiff);
                    EditorGUILayout.PropertyField(showRandomizeRadius);
                }
            EditorGUI.EndDisabledGroup();
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("Turning", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useMovementTurning);
            EditorGUILayout.PropertyField(movementTurningSensitivity);
            EditorGUILayout.PropertyField(useTurnAnims);
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        if (!script.waypoints.useTurnAnims) 
        {
            return;
        }

        GUILayout.BeginVertical(BlockStyle());
            DrawPopupProperty(script.anim, "Right Turn Anim Normal", "waypoints.rightTurnAnimNormal", ref script.waypoints.rightTurnAnimNormal, scripts, scripts.Length);
            DrawPopupProperty(script.anim, "Left Turn Anim Normal", "waypoints.leftTurnAnimNormal", ref script.waypoints.leftTurnAnimNormal, scripts, scripts.Length);
            DrawPopupProperty(script.anim, "Right Turn Anim Alert", "waypoints.rightTurnAnimAlert", ref script.waypoints.rightTurnAnimAlert, scripts, scripts.Length);
            DrawPopupProperty(script.anim, "Left Turn Anim Alert", "waypoints.leftTurnAnimAlert", ref script.waypoints.leftTurnAnimAlert, scripts, scripts.Length);
            EditorGUILayout.PropertyField(turningAnimT);
        GUILayout.EndVertical();
    }

    void DrawGeneralMisc()
    {
        // check enemy contact
        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("Check Enemy Came In Contact", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(checkEnemyContact);
            if (script.checkEnemyContact)
            {
                EditorGUILayout.PropertyField(enemyContactRadius);
                EditorGUILayout.PropertyField(showEnemyContactRadius);
            }
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        // friendly
        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("Friendly AI", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(friendly);
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        // distance culling
        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("Distance Culling", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(distanceCull);
            if (script.distanceCull) 
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Optional: will only play animation if [Disable Blaze Only] is enabled in the distance culling component", BoxStyle());
                DrawPopupProperty(script.anim, "Anim To Play On Cull", "animToPlayOnCull", ref script.animToPlayOnCull, scripts);
            }
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        // unreachable enemies
        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("Unreachable Enemies", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(ignoreUnreachableEnemy);

            if (script.ignoreUnreachableEnemy) 
            {
                GUILayout.EndVertical();
                EditorGUILayout.Space();

                DrawArrayWithStyle(fallBackPoints, "", true);

                GUILayout.BeginVertical(BlockStyle());
                EditorGUILayout.PropertyField(showPoints);
            }
        GUILayout.EndVertical();
    }

    void DrawGeneralWarnings()
    {
        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("Warnings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(warnEmptyBehavioursOnStart);
            EditorGUILayout.PropertyField(warnEmptyAnimations);
            EditorGUILayout.PropertyField(warnEmptyAudio);
            EditorGUILayout.PropertyField(warnAnomaly);
        GUILayout.EndVertical();
    }

    // start of vision sub tabs
    void DrawVisionSetup()
    {
        // vision active
        GUILayout.BeginVertical(BlockStyle());
            if (!script.vision.visionActive) EditorGUILayout.LabelField("Warning: This AI won't be detecting enemies!", BoxStyle());
            EditorGUILayout.LabelField("Detection Ability", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(visionActive);
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        // position & height
        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("Position & Height", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(visionPosition);
            EditorGUILayout.PropertyField(maxSightLevel);
            EditorGUILayout.PropertyField(checkTargetHeight);
            EditorGUILayout.PropertyField(useMinLevel);
            EditorGUI.BeginDisabledGroup(!script.vision.useMinLevel);
                EditorGUILayout.PropertyField(minSightLevel);
            EditorGUI.EndDisabledGroup();
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        // angles
        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("Angle & Range", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(visionDuringNormalState);
            EditorGUILayout.PropertyField(visionDuringAlertState);
            EditorGUILayout.PropertyField(visionDuringAttackState);
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        // head
        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("Vision-Head Rotation Sync (optional)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(head);
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        // show vision
        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("Show Vision (scene view only)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(showNormalVision);
            if (script.vision.showNormalVision)
            {
                EditorGUILayout.PropertyField(normalVisionColor);
                EditorGUILayout.Space();
            }

            EditorGUILayout.PropertyField(showAlertVision);
            if (script.vision.showAlertVision)
            {
                EditorGUILayout.PropertyField(alertVisionColor);
                EditorGUILayout.Space();
            }

            EditorGUILayout.PropertyField(showAttackVision);
            if (script.vision.showAttackVision)
            {
                EditorGUILayout.PropertyField(attackVisionColor);
                EditorGUILayout.Space();
            }
            
            EditorGUILayout.PropertyField(showMaxSightLevel);
            EditorGUILayout.PropertyField(showMinSightLevel);
        GUILayout.EndVertical();
    }

    void DrawVisionTargets()
    {
        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("Obstacle Layers", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(layersToDetect);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Enemy & Alert Layers", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(hostileAndAlertLayers);
        GUILayout.EndVertical();

        EditorGUILayout.Space();
        
        DrawArrayWithStyle(hostileTags, "Hostile Tag Names", true);
        DrawArrayWithStyle(alertTags, "Tag Names of Alerting Objects (optional)", true);
    }

    void DrawVisionEvents()
    {
        GUILayout.BeginVertical(BlazeAIEditor.BlockStyle());
            EditorGUILayout.PropertyField(enemyEnterEvent);
            EditorGUILayout.PropertyField(enemyLeaveEvent);
        GUILayout.EndVertical();
    }

    void DrawVisionSettings()
    {
        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("Take note: increasing this too much may slower AI reactions (optimal value is 1-2)", BoxStyle());
            EditorGUILayout.LabelField("Skipping Frames", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(visionFrameSkipping);
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        GUILayout.BeginVertical(BlockStyle());
            EditorGUILayout.LabelField("Multi-Rays Accuracy", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(multiRayVision);
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        GUILayout.BeginVertical(BlockStyle());
            if (script.vision.useVisionMeter)
            {
                EditorGUILayout.LabelField("You can read the vision meter using: blaze.visionMeter - and get the potential enemy using: blaze.potentialEnemyToAttack", EditorStyles.helpBox);
            }

            EditorGUILayout.LabelField("Detection Meter", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useVisionMeter);

            // don't continue if vision meter is disabled
            if (!script.vision.useVisionMeter) 
            {
                GUILayout.EndVertical();
                return;
            }

            EditorGUILayout.PropertyField(visionMeterSpeeds);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("On Distract", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(distractByVMValue);
            if (script.vision.distractByVMValue)
            {
                EditorGUILayout.PropertyField(distractIfVMReaches);
                EditorGUILayout.Space();
            }

            EditorGUILayout.PropertyField(overrideVMValueOnDistract);
            if (script.vision.overrideVMValueOnDistract)
            {
                EditorGUILayout.PropertyField(vmValueOnDistract);
                EditorGUILayout.Space();
            }

            EditorGUILayout.PropertyField(dontDecrementVMOnDistract);

        GUILayout.EndVertical();
    }

    #endregion

    #region SERIALIZATION
    
    void GetWaypointsProperties()
    {
        waypointsProp = serializedObject.FindProperty("waypoints");

        waypoints = waypointsProp.FindPropertyRelative("waypoints");
        loop = waypointsProp.FindPropertyRelative("loop");

        waypointsRotation = waypointsProp.FindPropertyRelative("waypointsRotation");
        timeBeforeTurning = waypointsProp.FindPropertyRelative("timeBeforeTurning");
        turnSpeed = waypointsProp.FindPropertyRelative("turnSpeed");

        showWaypoints = waypointsProp.FindPropertyRelative("showWaypoints");

        randomize = waypointsProp.FindPropertyRelative("randomize");
        randomizeRadius = waypointsProp.FindPropertyRelative("randomizeRadius");
        minAndMaxLevelDiff = waypointsProp.FindPropertyRelative("minAndMaxLevelDiff");
        showRandomizeRadius = waypointsProp.FindPropertyRelative("showRandomizeRadius");

        useMovementTurning = waypointsProp.FindPropertyRelative("useMovementTurning");
        movementTurningSensitivity = waypointsProp.FindPropertyRelative("movementTurningSensitivity");
        useTurnAnims = waypointsProp.FindPropertyRelative("useTurnAnims");

        rightTurnAnimNormal = waypointsProp.FindPropertyRelative("rightTurnAnimNormal");
        leftTurnAnimNormal = waypointsProp.FindPropertyRelative("leftTurnAnimNormal");
        rightTurnAnimAlert = waypointsProp.FindPropertyRelative("rightTurnAnimAlert");
        leftTurnAnimAlert = waypointsProp.FindPropertyRelative("leftTurnAnimAlert");
        turningAnimT = waypointsProp.FindPropertyRelative("turningAnimT");
    }

    void GetVisionProperties()
    {
        visionProp = serializedObject.FindProperty("vision");
        visionActive = visionProp.FindPropertyRelative("visionActive");

        layersToDetect = visionProp.FindPropertyRelative("layersToDetect");
        hostileAndAlertLayers = visionProp.FindPropertyRelative("hostileAndAlertLayers");
        hostileTags = visionProp.FindPropertyRelative("hostileTags");
        alertTags = visionProp.FindPropertyRelative("alertTags");

        visionPosition = visionProp.FindPropertyRelative("visionPosition");
        maxSightLevel = visionProp.FindPropertyRelative("maxSightLevel");
        checkTargetHeight = visionProp.FindPropertyRelative("checkTargetHeight");
        minSightLevel = visionProp.FindPropertyRelative("minSightLevel");
        useMinLevel = visionProp.FindPropertyRelative("useMinLevel");

        visionDuringNormalState = visionProp.FindPropertyRelative("visionDuringNormalState");
        visionDuringAlertState = visionProp.FindPropertyRelative("visionDuringAlertState");
        visionDuringAttackState = visionProp.FindPropertyRelative("visionDuringAttackState");

        head = visionProp.FindPropertyRelative("head");

        showNormalVision = visionProp.FindPropertyRelative("showNormalVision");
        normalVisionColor = visionProp.FindPropertyRelative("normalVisionColor");
        showAlertVision = visionProp.FindPropertyRelative("showAlertVision");
        alertVisionColor = visionProp.FindPropertyRelative("alertVisionColor");
        showAttackVision = visionProp.FindPropertyRelative("showAttackVision");
        attackVisionColor = visionProp.FindPropertyRelative("attackVisionColor");
        showMaxSightLevel = visionProp.FindPropertyRelative("showMaxSightLevel");
        showMinSightLevel = visionProp.FindPropertyRelative("showMinSightLevel");

        enemyEnterEvent = visionProp.FindPropertyRelative("enemyEnterEvent");
        enemyLeaveEvent = visionProp.FindPropertyRelative("enemyLeaveEvent");

        visionFrameSkipping = visionProp.FindPropertyRelative("visionFrameSkipping");
        multiRayVision = visionProp.FindPropertyRelative("multiRayVision");

        useVisionMeter = visionProp.FindPropertyRelative("useVisionMeter");
        visionMeterSpeeds = visionProp.FindPropertyRelative("visionMeterSpeeds");

        distractByVMValue = visionProp.FindPropertyRelative("distractByVMValue");
        distractIfVMReaches = visionProp.FindPropertyRelative("distractIfVMReaches");
        overrideVMValueOnDistract = visionProp.FindPropertyRelative("overrideVMValueOnDistract");
        vmValueOnDistract = visionProp.FindPropertyRelative("vmValueOnDistract");
        dontDecrementVMOnDistract = visionProp.FindPropertyRelative("dontDecrementVMOnDistract");
    }

    #endregion

    #region STYLING

    public static GUIStyle ToolbarStyling(bool isSelected)
    {
        var btnStyle = new GUIStyle();
        btnStyle.fontSize = 14;
        btnStyle.margin = new RectOffset(4,4,2,2);
        btnStyle.alignment = TextAnchor.MiddleCenter;
        btnStyle.fontStyle = FontStyle.Bold;

        // selected btn style
        if (isSelected) {
            btnStyle.normal.background = MakeTex(1, 1, new Color(1f, 0.55f, 0));
            btnStyle.normal.textColor = Color.black;
            btnStyle.active.textColor = Color.black;
            return btnStyle;
        }

        // unselected btns style
        btnStyle.normal.background = MakeTex(1, 1, new Color(0.15f, 0.15f, 0.15f));
        btnStyle.active.background = MakeTex(1, 1, new Color(0.1f, 0.1f, 0.1f));
        btnStyle.normal.textColor = new Color(1, 0.5f, 0);
        btnStyle.active.textColor = new Color(1, 0.5f, 0);
        return btnStyle;
    }

    public GUIStyle ToolbarSubTabStyling(bool isSelected)
    {
        var btnStyle = new GUIStyle();
        btnStyle.fontSize = 13;
        btnStyle.margin = new RectOffset(0,0,0,0);
        btnStyle.alignment = TextAnchor.MiddleCenter;

        if (isSelected) {
            btnStyle.normal.background = MakeTex(1, 1, new Color(0.85f, 0.85f, 0.85f));
            btnStyle.normal.textColor = Color.black;
            btnStyle.active.textColor = Color.black;
            return btnStyle;
        }

        btnStyle.normal.background = MakeTex(1, 1, new Color(1f, 0.45f, 0));
        btnStyle.active.background = MakeTex(1, 1, new Color(1f, 0.35f, 0));
        btnStyle.normal.textColor = Color.black;
        btnStyle.active.textColor = Color.black;
        return btnStyle;
    }

    // create texture for buttons
    public static Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];

        for (int i = 0; i < pix.Length; ++i) {
            pix[i] = col;
        }

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();

        return result;
    }

    public static GUIStyle BoxStyle()
    {
        var boxStyle = new GUIStyle();
        boxStyle.fontSize = 12;
        boxStyle.margin = new RectOffset(0,0,0,0);
        boxStyle.padding = new RectOffset(5,5,5,5);
        boxStyle.normal.background = BlazeAIEditor.MakeTex(1, 1, new Color(0.15f, 0.15f, 0.15f));
        boxStyle.normal.textColor = new Color(0.85f, 0.5f, 0);
        boxStyle.wordWrap = true;
        boxStyle.fontStyle = FontStyle.Bold;
        return boxStyle;
    }

    public static GUIStyle BlockStyle()
    {
        var boxStyle = new GUIStyle();
        boxStyle.margin = new RectOffset(0,0,0,0);
        boxStyle.padding = new RectOffset(5,5,5,5);
        
        if (EditorGUIUtility.isProSkin)
        {
            boxStyle.normal.background = BlazeAIEditor.MakeTex(1, 1, new Color(0.18f, 0.18f, 0.18f));
        }
        else {
            boxStyle.normal.background = BlazeAIEditor.MakeTex(1, 1, new Color(0.65f, 0.65f, 0.65f));
        }
        
        return boxStyle;
    }

    // creates a horizontal line
    public static void HorizontalLine (Color color)
    {
        GUIStyle horizontalLine;
        horizontalLine = new GUIStyle();
        horizontalLine.normal.background = EditorGUIUtility.whiteTexture;
        horizontalLine.margin = new RectOffset(0, 0, 4, 4);
        horizontalLine.fixedHeight = 2;

        var c = GUI.color;
        GUI.color = color;
        GUILayout.Box(GUIContent.none, horizontalLine);
        GUI.color = c;
    }

    public static void DrawArrayWithStyle(SerializedProperty prop, string label = "", bool isFirstItem = false)
    {
        if (!isFirstItem) HorizontalLine(new Color(0.12f, 0.12f, 0.12f));
        
        if (label != "")
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        }

        EditorGUILayout.PropertyField(prop);
        
        EditorGUILayout.Space();
        HorizontalLine(new Color(0.12f, 0.12f, 0.12f));
        EditorGUILayout.Space();
    }

    #endregion

    #region HANDLES
    
    void DrawWaypointHandles()
    {
        if (!script.waypoints.showWaypoints || script.waypoints.randomize) {
            return;
        }

        int max = script.waypoints.waypoints.Count;

        for (int i=0; i<max; i++) 
        {
            EditorGUI.BeginChangeCheck();

            Vector3 currentWaypoint = script.waypoints.waypoints[i];
            Vector3 newTargetPosition = Handles.PositionHandle(currentWaypoint, Quaternion.identity);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(script, "Change Waypoint");
                script.waypoints.waypoints[i] = newTargetPosition;
            }
        }
    }

    void DrawFallBackPointsHandles()
    {
        if (!script.ignoreUnreachableEnemy) return;
        if (!script.showPoints) return;

        EditorGUI.BeginChangeCheck();

        Vector3 currentPoint;
        int max = script.fallBackPoints.Length;

        for (int i=0; i<max; i++) {
            currentPoint = script.fallBackPoints[i];
            Vector3 newTargetPosition = Handles.PositionHandle(currentPoint, Quaternion.identity);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(script, "Change Fallback Point");
                script.fallBackPoints[i] = newTargetPosition;
            }
        }
    }

    #endregion

    #region MISC

    void GetLastTabsSelected()
    {
        // main blaze tab
        if (EditorPrefs.HasKey("BlazeTabSelected")) {
            tabSelected = EditorPrefs.GetInt("BlazeTabSelected");
        }
        else {
            tabSelected = 0;
        }

        // general sub tabs
        if (EditorPrefs.HasKey("BlazeGeneralSubTabSelected")) {
            generalSubTabSelected = EditorPrefs.GetInt("BlazeGeneralSubTabSelected");
        }
        else {
            generalSubTabSelected = 0;
        }

        // vision sub tabs
        if (EditorPrefs.HasKey("BlazeVisionSubTabSelected")) {
            visionSubTabSelected = EditorPrefs.GetInt("BlazeVisionSubTabSelected");
        }
        else {
            visionSubTabSelected = 0;
        }
    }

    public static void CheckDisableWithRootMotion(BlazeAI script, ref SerializedProperty prop)
    {
        // if no need to disable -> draw and exit
        if (!script.useRootMotion) 
        {
            EditorGUILayout.PropertyField(prop);
            return;
        }

        // print reason
        EditorGUILayout.LabelField("Disabled because [Use Root Motion] is enabled", BoxStyle());

        // disable property
        EditorGUI.BeginDisabledGroup(script.useRootMotion);
            EditorGUILayout.PropertyField(prop);
        EditorGUI.EndDisabledGroup();
    }

    public static void DrawPopupProperty(Animator anim, string nameToPrint, string propName, ref string property, Object[] passedScripts, int maxDups=1)
    {
        if (IsAnimationsEmpty(anim)) 
        {
            NoAnimatorController(nameToPrint);
            return;
        }

        int lastIndex = ArrayUtility.IndexOf(animationStateNamesArr, property);
        int currentIndex = 0;
        duplicateAnimNames.Clear();

        System.Type type;
        FieldInfo myFieldInfo;

        string[] propPath = propName.Split(".");
        bool isNested = false;

        if (propPath.Length > 1) {
            isNested = true;
        }

        if (Selection.objects.Length > 1) 
        {
            FieldInfo tempField;
            foreach (var item in passedScripts)
            {
                string value;
                type = item.GetType();

                if (isNested) {
                    tempField = type.GetField(propPath[0]);
                    myFieldInfo = tempField.GetValue(item).GetType().GetField(propPath[1]);
                    value = myFieldInfo.GetValue(item.GetType().GetField(propPath[0]).GetValue(item)) as string;
                }
                else {
                    myFieldInfo = type.GetField(propName);
                    value = myFieldInfo.GetValue(item).ToString();
                    if (duplicateAnimNames.Contains(value)) {
                        continue;
                    }
                }

                duplicateAnimNames.Add(value);
            }
        }

        if (duplicateAnimNames.Count > maxDups) {
            lastIndex = -1;
        }

        EditorGUI.BeginChangeCheck();
            currentIndex = EditorGUILayout.Popup(nameToPrint, lastIndex, BlazeAIEditor.animationStateNamesArr);
        if (EditorGUI.EndChangeCheck()) 
        {
            FieldInfo tempField;

            foreach (var item in passedScripts)
            {
                string value;

                if (isNested) {
                    type = item.GetType();
                    tempField = type.GetField(propPath[0]);
                    myFieldInfo = tempField.GetValue(item).GetType().GetField(propPath[1]);
                    
                    value = myFieldInfo.GetValue(item.GetType().GetField(propPath[0]).GetValue(item)) as string;
                }
                else {
                    myFieldInfo = item.GetType().GetField(propName);
                    value = myFieldInfo.GetValue(item) as string;
                }

                if (currentIndex < 0) continue;
                if (currentIndex > animationStateNamesArr.Length - 1) continue;
                if (animationStateNamesArr.Length == 0) continue;

                string newValue = animationStateNamesArr[currentIndex];

                if (value == newValue) {
                    continue;
                }

                EditorUtility.SetDirty(item);

                if (isNested) {
                    myFieldInfo.SetValue(item.GetType().GetField(propPath[0]).GetValue(item), newValue);
                    continue;
                }

                myFieldInfo.SetValue(item, newValue);
            }
        }
    }

    public static void GetAnimatorStates(Animator animator)
    {
        if (animator == null) {
            return;
        }

        AnimatorController ac = animator.runtimeAnimatorController as AnimatorController;
        List<string> animationNames = new List<string>();

        if (ac == null) {
            return;
        }

        AnimatorControllerLayer[] acLayers = ac.layers;

        ChildAnimatorState[] singleStatesArr;
        ChildAnimatorStateMachine[] subStatesArr;
        ChildAnimatorState[] statesInSub;
        
        foreach (AnimatorControllerLayer i in acLayers)
        {
            // get the single states
            singleStatesArr = i.stateMachine.states;
            foreach (ChildAnimatorState j in singleStatesArr) 
            {
                // turn state name to string and remove the object name
                string name = j.state.ToString();
                string[] tempArr = name.Split("(UnityEngine.AnimatorState)");
                name = tempArr[0].Substring(0, tempArr[0].Length - 1);
                animationNames.Add(name);
            }

            // get the sub-states
            subStatesArr = i.stateMachine.stateMachines;
            foreach (ChildAnimatorStateMachine s in subStatesArr)
            {
                statesInSub = s.stateMachine.states;
                foreach (var x in statesInSub) {
                    string name = x.state.ToString();
                    string[] tempArr = name.Split("(UnityEngine.AnimatorState)");
                    name = tempArr[0].Substring(0, tempArr[0].Length - 1);
                    animationNames.Add(name);
                }
            }
        }

        animationNames.Sort();
        animationStateNamesArr = animationNames.ToArray();
    }

    public static void RefreshAnimationStateNames(Animator animator)
    {
        if (animator == null) {
            return;
        }
        
        System.Array.Clear(animationStateNamesArr, 0, animationStateNamesArr.Length);
        GetAnimatorStates(animator);
    }

    public static void NoAnimatorController(string propertyName)
    {
        string warning = "No animations or animator controller";
        EditorGUILayout.LabelField(propertyName, warning);
    }

    public static bool IsAnimationsEmpty(Animator anim)
    {
        if (animationStateNamesArr.Length == 0 || animationStateNamesArr[0] == null || anim == null || anim.runtimeAnimatorController == null) {
            return true;
        }

        return false;
    }

    #endregion
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using BlazeAISpace;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(AudioSource))]
[AddComponentMenu("Blaze AI/Blaze AI")]

public class BlazeAI : MonoBehaviour
{
    #region PROPERTIES

    [Tooltip("Enabling this will make the agent use root motion, this gives more accurate and realistic movement but any move speed property will not be considered as the speed will be that of the animation.")]
    public bool useRootMotion = false;
    public LayerMask groundLayers = Physics.AllLayers;
    
    [Tooltip("All audios are added in a scriptable then both Blaze and it's behaviours will read from this scriptable. To create an audio scriptable: Right-click in the Project window > Create > Blaze AI > Audio Scriptable.")]
    public AudioScriptable audioScriptable;
    public AudioSource agentAudio;

    public bool useLocalAvoidance;
    [Tooltip("Set the layers of other AIs you don't want to collide with.")]
    public LayerMask layersToAvoid;
    [Min(0)] public float avoidanceRadius = 2;
    public bool showAvoidanceRadius;
    [Tooltip("Set the strength of the side-stepping offset.")]
    [Min(0)] public float avoidanceOffsetStrength = 0.15f;
    [Tooltip("Set the speed of the turning to the offset.")]
    [Min(0)] public float avoidanceSteerSpeed = 15f;
    [Tooltip("AI will try to avoid others if they're within this angle.")]
    [Min(0)] public float avoidIfWithinAngle = 90f;
    [Tooltip("The maximum allowed time for continued avoidance. If the AI has been avoiding for this amount of time, it could mean it's stuck and so cool down will trigger allowing for normal movement.")]
    [Min(0)] public float maxAvoidanceTime = 1;
    [Tooltip("The amount of time to cool down from avoidance and allow normal movement to take over.")]
    [Min(0)] public float avoidanceCoolDownTime = 0.35f;

    public Waypoints waypoints;

    [Header("Vision & Adding Enemies")]
    public Vision vision;
    
    [Tooltip("Check if a hostile got too close and came in contact with the AI. If so, will turn to attack state.")]
    public bool checkEnemyContact;
    [Min(0), Tooltip("The radius for checking if a hostile came in contact.")]
    public float enemyContactRadius = 1.2f;
    [Tooltip("Shows the radius as a grey wire sphere in the scene view.")]
    public bool showEnemyContactRadius;

    [Tooltip("If this is enabled the AI will never turn to attack state when seeing a hostile tag or on enemy contact until this property is set to false. TAKE NOTE: specifically calling the API SetEnemy(player) or Hit(player) will force disable friendly mode. If you want to hit the friendly AI without it turning off friendly mode then call Hit() without passing a target.")]
    public bool friendly;

    [Tooltip("Disable this gameobject when it exceeds the distance set in the BlazeAIDistanceCulling component. This will drastically improve performance.")]
    public bool distanceCull;
    [Tooltip("An animation to play when the AI is culled. This will only be considered if BlazeAIDistanceCulling is set to Disable Blaze Only. You can leave this empty if you want.")]
    public string animToPlayOnCull;

    [Tooltip("If enabled and an enemy is unreachable or becomes unreachable the AI will ignore it and continue patrolling. If turned off, the AI will not ignore the enemy and turn to attack state and wait for it to be reachable.")]
    public bool ignoreUnreachableEnemy;
    [Tooltip("If an unreachable enemy has been detected, the AI will choose ONE random point from this array to move to in alert state then continue it's patrol. If array length is 0 OR the chosen index == Vector3.zero then the AI will turn to alert state and patrol it's normal waypoints set in the Waypoints class.")]
    public Vector3[] fallBackPoints;
    [Tooltip("Will show the fallback points.")]
    public bool showPoints = false;

    public bool useOffMeshLinks = false;
    public OffMeshLinkJumpMethod jumpMethod = OffMeshLinkJumpMethod.Parabola;
    [Min(0)]
    public float jumpHeight = 2;
    [Min(0)]
    public float jumpDuration = 0.8f;
    public bool useMovementSpeedForJump;
    [Min(0)]
    public float jumpSpeed = 3;
    public string jumpAnim;
    public string fallAnim;
    [Min(0)]
    public float jumpAnimT = 0.25f;
    public UnityEvent onTeleportStart;
    public UnityEvent onTeleportEnd;
    
    public bool climbLadders;
    public LayerMask ladderLayers;
    [Min(0)]
    public float climbUpSpeed = 3;
    public string climbUpAnim;
    [Min(0)]
    public float climbToTopDuration = 1;
    public string climbToTopAnim;
    [Min(0), Tooltip("The bigger the value, the less head room will be give to initiate the climbing to top animation and vice versa. This value is different for every AI model and you should be trying out different values.")]
    public float climbToTopHeadRoom = 0.4f;
    [Min(0)]
    public float climbAnimT = 0.25f;

    public bool useNormalStateOnAwake;
    public MonoBehaviour normalStateBehaviour;

    public bool useAlertStateOnAwake;
    public MonoBehaviour alertStateBehaviour;
    
    public MonoBehaviour attackStateBehaviour;
    public bool coverShooterMode;
    public MonoBehaviour coverShooterBehaviour;
    public MonoBehaviour goingToCoverBehaviour;

    public bool useSurprisedState;
    public MonoBehaviour surprisedStateBehaviour;
    
    public bool canDistract = true;
    public MonoBehaviour distractedStateBehaviour;
    [Range(0, 100), Tooltip("If a distraction triggers a group of agents, the highest priority AI only is sent to the distraction point. Here you can set which AI is more prone to check the distraction.")]
    public float priorityLevel = 50;
    [Tooltip("If enabled, the AI will turn to face every distraction it gets. If disabled, the AI turns only once per distracted state duration.")]
    public bool turnOnEveryDistraction = true;

    [Tooltip("If enabled and the AI gets distracted in normal state. It'll play the alert movement animation as well as have the alert vision and when the distracted state is finished, the AI will return to alert state instead of normal. Enabling this option makes the AI act exactly as if it's been distracted during alert state.")]
    public bool turnAlertOnDistract;
    
    [Tooltip("Play audio when distracted. Set the audios in the audio scriptable in the General tab.")]
    public bool playDistractedAudios;

    [Tooltip("If enabled, the AI will react to a max number of hits, after which it'll need to cooldown before reacting to hits again. If you don't want your players to exploit the AI being in a continous hit state this option is great to use. If disabled, the AI will always react to hits.")]
    public bool useHitCooldown;
    [Tooltip("The maximum number of hits before the AI will require a cool down..")]
    public int maxHitCount = 3;
    [Tooltip("The time for cooldown after which the AI will resume reacting to hits.")]
    public float hitCooldown = 5;
    public MonoBehaviour hitStateBehaviour;

    public string deathAnim;
    [Min(0)]
    public float deathAnimT = 0.25f;

    public bool disableCapsuleColliderOnDeath;
    
    [Tooltip("Set your audios in the audio scriptable in the General Tab in Blaze AI.")]
    public bool playDeathAudio;
    
    [Min(0), Tooltip("The radius of calling other AIs on death. The will appear in the scene view as a cyan colored wire sphere.")]
    public float deathCallRadius = 10f;
    [Tooltip("The layers of the AIs to call.")]
    public LayerMask agentLayersToDeathCall; 
    [Tooltip("If enabled, this will show the death call radius in the scene view as a cyan colored wire sphere.")]
    public bool showDeathCallRadius;

    [Tooltip("If enabled, on death the system will trigger all colliders and disable the animator to have a ragdoll. To have a ragdoll, right click on the AI in the heirarchy > 3D Object > Ragdoll. And follow the wizard.")]
    public bool useRagdoll;
    [Tooltip("Use the natural velocity of the Rigidbody at the time of ragdoll trigger.")]
    public bool useNaturalVelocity;
    [Tooltip("Set the hip/pelvis of the AI.")]
    public Transform hipBone;
    [Tooltip("Set manually the force of the rigidbody in ragdoll. You can change this according to the type of death, for example: increase the Y axis when AI is killed by an explosion to have it fly into the air.")]
    public Vector3 deathRagdollForce;

    [Space(15), Tooltip("Set an event to trigger on death.")]
    public UnityEvent deathEvent;

    [Tooltip("Will destroy this gameobject on death.")]
    public bool destroyOnDeath;
    [Min(0), Tooltip("The time to pass in death before destroying the gameobject.")]
    public float timeBeforeDestroy = 5;

    [Tooltip("If enabled, the AI will trigger the companion behaviour to follow the target.")]
    public bool companionMode;
    [Tooltip("Set the player or any other target you want this AI to follow and be a companion to.")]
    public Transform companionTo;
    [Tooltip("The companion behaviour script.")]
    public MonoBehaviour companionBehaviour;

    [Tooltip("Will print in the console to warn you if any behaviour is empty.")]
    public bool warnEmptyBehavioursOnStart = true;
    [Tooltip("Will print in the console to warn you if any animation name is empty or doesn't exist.")]
    public bool warnEmptyAnimations = true;
    [Tooltip("Will print in the console to warn you if the audio scriptable is empty.")]
    public bool warnEmptyAudio;
    [Tooltip("Warns about any anomaly in the AI that could break the behaviour or cause unexpected behaviour.")]
    public bool warnAnomaly = true;
    
    #endregion
    
    #region SYSTEM VARIABLES

    public Animator anim { get; private set; }
    public NavMeshAgent navmeshAgent { get; private set; }
    public CapsuleCollider capsuleCollider;
    NavMeshPath path;
    [HideInInspector] public AnimationManager animManager;
    public BlazeAISpareState spareState;
    public BlazeAIEnemyManager enemyManager;
    public BlazeAIAudioManager audioManager;
    public Transform previousEnemy;

    public State state { get; private set; }
    public bool isAttacking { get; set; }
    public bool isAttackingAnim { get; set; }
    public GameObject enemyToAttack { get; private set; }
    public Vector3 enemyColPoint { get; private set; }
    public float distanceToEnemySqrMag { get; private set; }
    public float distanceToEnemy { get; private set; }
    public float visionMeter { get; private set; }
    public GameObject potentialEnemyToAttack { get; private set; }
    public float visionMeterSpeedInUse { private set; get; }
    public bool forcedRootMotion { get; set; }
    public Vector3 StartPosition 
    {
        get { return startPosition; }
        set { startPosition = value; }
    }
    
    public int waypointIndex { get; private set; }
    public bool isPathReachable { get; private set; }
    public Vector3 endDestination { get; private set; }
    public State previousState { get; private set; }
    public Vector3 checkEnemyPosition { get; set; }
    public float captureEnemyTimeStamp { get; private set; }
    public Vector3 enemyPosOnSurprised { get; private set; }
    public string sawAlertTagName { get; private set; }
    public Vector3 sawAlertTagPos { get; private set; }
    public Transform sawAlertTagObject { get; private set; }
    public Transform visionT { private set; get; }
    
    // read by behaviours
    public bool movedToLocation { get; set; }
    public bool ignoreMoveToLocation { get; set; }
    public bool stayIdle { get; set; }
    public bool isIdle { get; set; }
    public bool tookCover { get; set; }
    public bool stayAlertUntilPos { get; set; }
    public bool isFleeing { get; set; }
    public Vector3 defaultVisionPos { get; private set; }
    public bool isTargetTagChanged { get; set; }
    
    public Vector3 pathCorner { get; set; }
    Vector3 lastCalculatedPath;
    Vector3 startPosition;
    Queue<Vector3> cornersQueue;
    
    bool isEnemyEnterEventRan;
    bool useNormalStateOnAwakeInspectorState;
    bool useAlertStateOnAwakeInspectorState;
    bool isturningToCorner;
    bool isOffMeshJumpFinished = true;
    bool isChangeStateLooping = false;
    bool canChangeVisionTarget = false;

    public BlazeBehaviour lastBlazeBehaviour {private set; get; }

    int visionCheckElapsed = 0;
    int checkSurroundingElapsed = 0;
    int hitCount;

    float timeOfLastHit;
    float offMeshDoneTimePassed = 0.3f;
    float timeToAllowTurning = 0.3f;

    string lastAnim;
    string lastFinishedAnimT;

    Collider ignoredEnemy = null;
    Collider collOfTarget;
    
    Transform chosenLadder;

    List<Collider> ragdollColls = new List<Collider>();
    Avatar defaultAvatar;

    DeathDollCallProps deathDollCallProps;
    LastMoveProps lastMoveProperties;
    OffMeshDeath offMeshDeathProps;
    public HitProps hitProps;


    public enum State 
    {
        normal,
        alert,
        attack,
        goingToCover,
        sawAlertTag,
        returningToAlert,
        surprised,
        distracted,
        hit,
        death,
        spareState
    }

    public enum OffMeshLinkJumpMethod
    {
        Parabola,
        NormalSpeed,
        Teleport
    }

    struct DeathDollCallProps
    {
        public bool isCalled;
        public bool callOthers;
        public GameObject enemy;

        public DeathDollCallProps(bool isCalled, bool callOthers, GameObject enemy) {
            this.isCalled = isCalled;
            this.callOthers = callOthers;
            this.enemy = enemy;
        } 
    }

    struct LastMoveProps 
    {
        public Vector3 location;
        public float moveSpeed;
        public float turnSpeed;

        public LastMoveProps(Vector3 location, float moveSpeed, float turnSpeed) {
            this.location = location;
            this.moveSpeed = moveSpeed;
            this.turnSpeed = turnSpeed;
        }
    }

    struct OffMeshDeath
    {
        public bool isCalled;
        public bool callOthers;
        public GameObject enemy;
        public bool comingFromDeathDoll;

        public OffMeshDeath(bool isCalled, bool callOthers, GameObject enemy, bool comingFromDeathDoll) {
            this.isCalled = isCalled;
            this.callOthers = callOthers;
            this.enemy = enemy;
            this.comingFromDeathDoll = comingFromDeathDoll;    
        }
    }

    public struct HitProps
    {
        public bool hitRegister;
        public int knockOutRegister;
        public GameObject hitEnemy;
        public bool callOthers;
        public bool hitWhileInCover;

        public HitProps(bool hitRegister, int knockOutRegister, GameObject hitEnemy, bool callOthers, bool hitWhileInCover) {
            this.hitRegister = hitRegister;
            this.knockOutRegister = knockOutRegister;
            this.hitEnemy = hitEnemy;
            this.callOthers = callOthers;
            this.hitWhileInCover = hitWhileInCover;
        }
    }

    // local avoidance variables
    public bool isAvoiding = false;
    bool isStickyAvoiding = false;
    bool isAvoidingCoolDown = false;
    float lastAvoidanceTimeStamp = 0;
    float avoidingTimer = 0f;
    float avoidingGraceTimer = 0f;
    float avoidingGraceTime = 0.2f;
    float avoidanceCoolDownTimer = 0;

    #endregion
    
    #region GARBAGE REDUCTION
    
    Collider[] visionHitArr = new Collider[20];
    Collider[] skinHitArr = new Collider[15];
    Collider[] avoidanceHitArr = new Collider[5];
    RaycastHit[] checkObjVisibleRayHitArr = new RaycastHit[15];
    List<RaycastHit> orderedRayHits = new List<RaycastHit>();
    Collider[] searchLadderHitArr = new Collider[7];
    Dictionary<Collider, int> enemiesTargetedByAmount = new Dictionary<Collider, int>();
    RaycastHit[] checkTargetSeenHitArr = new RaycastHit[12];

    #endregion
    
    #region UNITY METHODS
    
    void Awake()
    {
        anim = GetComponent<Animator>();
        animManager = new AnimationManager(anim, this);
        capsuleCollider = GetComponent<CapsuleCollider>();
        navmeshAgent = GetComponent<NavMeshAgent>();
    }

    public virtual void Start()
    {
        path = new NavMeshPath();
        cornersQueue = new Queue<Vector3>();

        SetAgentAudio();
        
        startPosition = transform.position;
        waypointIndex = -1;
        offMeshDoneTimePassed = timeToAllowTurning;

        deathDollCallProps = new DeathDollCallProps(false, false, null);
        offMeshDeathProps = new OffMeshDeath(false, false, null, false);
        hitProps = new HitProps(false, 0, null, false, false);

        ComponentsOnAwake();
        vision.CheckHostileAndAlertItemEqual();
        visionCheckElapsed = Random.Range(0, vision.visionFrameSkipping);

        // set state on awake
        if (useNormalStateOnAwake) 
        {
            SetState(State.normal);
            return;
        }
        
        SetState(State.alert);
    }

    public virtual void Update()
    {
        SetAudioManager();

        // set the vision to head if available
        if (vision.head == null) visionT = transform;
        else visionT = vision.head;

        AvoidanceTimers();

        if (vision.distractByVMValue)
        {
            if (visionMeter >= vision.distractIfVMReaches && state != State.distracted && state != State.attack)
            {
                if (potentialEnemyToAttack != null) {
                    Distract(potentialEnemyToAttack.transform.position);
                }
            }
        }
        
        // always apply the anim root speed if using root motion
        if (useRootMotion || forcedRootMotion)
        {
            Vector3 worldDeltaPosition = navmeshAgent.nextPosition - transform.position;
            if (worldDeltaPosition.magnitude > navmeshAgent.radius) {
                navmeshAgent.nextPosition = transform.position + 0.9f * worldDeltaPosition;
            }
        }

        if (state == State.attack && vision.useVisionMeter)
        {
            visionMeter = 1;
        }

        // if AI died during off mesh jump
        if (CheckDeathOnOffMesh()) 
        {
            return;
        }

        // check for off mesh link
        if (navmeshAgent.isOnOffMeshLink || !isOffMeshJumpFinished) 
        {
            MoveTo(lastMoveProperties.location, lastMoveProperties.moveSpeed, lastMoveProperties.turnSpeed);
            return;
        }

        // track time after jump finished to re-enable turning
        TurningTimerAfterOffMesh();
        CheckState();
        VisionCheck();
        SurroundingsCheck();        
        RemoveMoveToLocation();
        CleanKnockOutRegister();
        ManageHitsCooldown();
        
        if (state != State.goingToCover)
        {
            ResetVisionPosition();
        }

        if (enemyToAttack != null)
        {
            SetEnemyColPoint();
        }
    }
    
    void OnAnimatorMove()
    {
        if (navmeshAgent == null) return;
        if ((!useRootMotion && !forcedRootMotion) || navmeshAgent.isOnOffMeshLink) return;
        if (anim == null) return;
        
        Vector3 position = anim.rootPosition;
        position.y = navmeshAgent.nextPosition.y;
        transform.position = position;
    }

    void OnValidate()
    {
        // choose either UseNormalStateOnAwake or UseAlertStateOnAwake (can't be both)
        if (!useAlertStateOnAwake && !useNormalStateOnAwake) {
            useNormalStateOnAwake = true;
        }

        if (useAlertStateOnAwake && useNormalStateOnAwake) 
        {
            useAlertStateOnAwake = !useAlertStateOnAwakeInspectorState;
            useNormalStateOnAwake = !useNormalStateOnAwakeInspectorState;
        }

        useNormalStateOnAwakeInspectorState = useNormalStateOnAwake;
        useAlertStateOnAwakeInspectorState = useAlertStateOnAwake;

        // validate waypoints system
        if (waypoints != null) {
            waypoints.WaypointsValidation(transform.position);
        }

        CloseLastBehaviour();

        if (vision != null)
        {
            vision.CheckHostileAndAlertItemEqual(true);
            vision.Validate();

            if (vision.minSightLevel > vision.visionPosition.y) {
                vision.minSightLevel = -vision.visionPosition.y;
            }

            if (vision.head == null) {
                visionT = transform;
            }
            else {
                visionT = vision.head;
            }

            GoingToCoverBehaviour coverBehaviour = GetComponent<GoingToCoverBehaviour>();
            if (coverBehaviour) 
            {
                if (Mathf.Abs(vision.visionDuringAttackState.sightRange - coverBehaviour.searchDistance) > 5) 
                {
                    #if UNITY_EDITOR
                    if (UnityEditor.EditorUtility.DisplayDialog("Attack Sight Range Too Low",
                        "Sight range of attack state can't be more than 5 units lower from [Search Distance] of Going To Cover Behaviour. Sight range has been reset.", "Ok")) {
                    }
                    #endif

                    vision.visionDuringAttackState.sightRange = coverBehaviour.searchDistance;
                }
            }
        }

        SetAgentAudio();
        ValidateFallBackPoints();

        if (anim == null) {
            anim = GetComponent<Animator>();
        }
    }

    void OnDestroy()
    {
        RemoveFromAudioManager();
        ResetEnemyManager();
    }

    // enable & set important components on awake
    void ComponentsOnAwake()
    {
        // disable any obstacle component
        NavMeshObstacle obstacleComp = GetComponent<NavMeshObstacle>();
        if (obstacleComp != null)
        {
            obstacleComp.enabled = false;

            #if UNITY_EDITOR
            PrintWarning(warnAnomaly, $"Nav Mesh Obstacle detected on {gameObject.name} and has been disabled. You should remove any Nav Mesh Obstacle as it breaks the AI.");
            #endif
        }

        NavMesh.avoidancePredictionTime = 0.5f;

        // set navmesh agent properties
        navmeshAgent.enabled = true;
        navmeshAgent.stoppingDistance = 0;
        navmeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        navmeshAgent.speed = 0;
        navmeshAgent.angularSpeed = 0;

        if (coverShooterMode) 
        {
            capsuleCollider.isTrigger = true;
        }

        CollectRagdollColliders();
        hitProps.knockOutRegister = 0;

        if (groundLayers.value == 0) 
        {
            groundLayers = Physics.AllLayers;
        }

        // if distance culling enabled then add this transform to the list
        if (distanceCull) 
        {
            AddDistanceCulling();
        }

        #if UNITY_EDITOR
        if (warnEmptyBehavioursOnStart) 
        {
            CheckEmptyBehaviours();
        }
        #endif

        defaultVisionPos = vision.visionPosition;

        spareState = GetComponent<BlazeAISpareState>();
        if (spareState != null) 
        {
            spareState.blaze = this;
        }
    }
    
    #if UNITY_EDITOR
    void OnDrawGizmosSelected() 
    {
        waypoints.Draw(transform.position, this);
        ShowAvoidanceRadiusSphere();
        ShowEnemyContactRadius();
        ShowDeathCallRadius();
        DrawFallBackPoints();
    }
    #endif

    public virtual void OnEnable()
    {
        // if blaze is enabled -> enable navmesh agent component
        if (navmeshAgent != null) {
            navmeshAgent.enabled = true;
        }

        SetAgentAudio();
        lastBlazeBehaviour = null;
        anim = GetComponent<Animator>();
    }
    
    public virtual void OnDisable() 
    {
        CloseLastBehaviour();
        
        if (animManager != null) {
            animManager.ResetLastState();
        }
        
        ResetEnemyManager();
        
        if (audioManager != null) {
            audioManager.RemoveFromManager(this);
        }

        isChangeStateLooping = false;
        ResetAllAvoidance();
    }

    #endregion
    
    #region MOVEMENT
    
    // move to location
    public virtual bool MoveTo(Vector3 location, float moveSpeed, float turnSpeed, string animName=null, float animT=0.25f, string dir="front", float closestPointDistance=0)
    {
        if (!navmeshAgent.enabled) 
        {
            #if UNITY_EDITOR
            PrintWarning(warnAnomaly, "AI is trying to move but the navmesh agent is disabled.");
            #endif

            return false;
        }

        if (navmeshAgent.enabled && navmeshAgent.speed > 0)
        {
            navmeshAgent.speed = 0;
            navmeshAgent.angularSpeed = 0;
        }

        if (dir == "front") 
        {
            lastMoveProperties = new LastMoveProps(location, moveSpeed, turnSpeed);
            
            if ((!isAttacking || enemyToAttack == null) && (lastCalculatedPath == location) && cornersQueue.Count == 0)
            {
                // check if AI is already at the min possible distance from location
                float dist = (new Vector3(pathCorner.x, transform.position.y, pathCorner.z) - transform.position).sqrMagnitude;
                float minDis = 0;

                if (navmeshAgent.radius < 0.3f) minDis = 0.3f;
                else minDis = navmeshAgent.radius;
                
                if (dist <= (minDis * minDis) || pathCorner == Vector3.zero) 
                {
                    movedToLocation = false;
                    return true;
                }
            }
        }

        // clear the corners
        cornersQueue.Clear();
        
        // calculates path corners and returns if reachable or not
        if (!IsPathReachable(location, true))
        {
            if (dir != "front")  
            {
                return false;
            }
        }
        
        // add the corners to queue so we can follow
        int max = path.corners.Length;
        for (int i=1; i<max; i++) 
        {
            cornersQueue.Enqueue(path.corners[i]);
        }
        
        // get the next corner
        GetNextCorner();
        return GoToCorner(animName, animT, moveSpeed, turnSpeed, dir);
    }

    // follow the path corners
    bool GoToCorner(string anim, float animT, float moveSpeed, float turnSpeed, string dir)
    {
        float currentDistance = 0f;
        float minDistance = 0f;
       
        bool isLastCorner = false;
        bool isReachedEnd = false;

        // set the min distance
        if (navmeshAgent.radius < 0.3f) minDistance = 0.3f;
        else minDistance = navmeshAgent.radius;

        // check if there are other corners
        if (cornersQueue.Count > 0)
        {   
            currentDistance = (pathCorner - transform.position).sqrMagnitude;
        }
        else 
        {
            isLastCorner = true;

            // for the final point -> the distance check differs
            currentDistance = (new Vector3(pathCorner.x, transform.position.y, pathCorner.z) - transform.position).sqrMagnitude;
        }
        
        // if reached min distance of corner
        if (currentDistance <= (minDistance * minDistance))
        {
            if (isLastCorner) isReachedEnd = true;
            else {
                GetNextCorner();
            }
        }

        // if on an off mesh link
        if (navmeshAgent.isOnOffMeshLink)
        {
            OffMeshLinkJump(moveSpeed);
            return false;
        }

        // turning to path corner
        if (waypoints.useMovementTurning) 
        {
            if (offMeshDoneTimePassed >= timeToAllowTurning) 
            {
                // turning to path corner shouldn't be done in attack states or jumping        
                if (state != State.attack && state != State.goingToCover && !navmeshAgent.isOnOffMeshLink && !isAvoiding && (Time.time - lastAvoidanceTimeStamp) > 0.1f)
                {
                    if (!MovementTurning()) return false;
                }
            }
        }

        if (dir == "front" && isReachedEnd) 
        {
            return true;
        }

        // only applied if not using root motion -> if using root motion, speed apply is in OnAnimatorMove()
        if (useRootMotion) {   
            RootMotionMovement(anim, animT, turnSpeed);
        }
        else {
            NonRootMotionMovement(anim, animT, moveSpeed, turnSpeed, dir);
        }
        
        return isReachedEnd;
    }
    
    public virtual void NonRootMotionMovement(string anim, float animT, float moveSpeed, float turnSpeed, string dir)
    {
        if (Vector3.Distance(lastCalculatedPath, transform.position) <= 0.05f) 
        {
            return;
        }

        Vector3 transformDir;
        switch (dir)
        {
            case "backwards":
                transformDir = -transform.forward;
                break;
            case "left":
                transformDir = -transform.right;
                break;
            case "right":
                transformDir = transform.right;
                break;
            default:
                transformDir = transform.forward;
                break;
        }

        if (!isAvoiding) 
        {
            MovementRotate(turnSpeed);
        }

        animManager.Play(anim, animT);

        if (anim != null && anim.Length > 0)
        {
            if (lastAnim != anim) 
            {
                lastAnim = anim;
                return;
            }

            if (lastAnim == anim) 
            {
                bool isDone = CheckAnimTFinished(anim, animT);
                if (!isDone) return;
            }
        }
        
        navmeshAgent.isStopped = true;

        Vector3 avoidanceOffset = Vector3.zero;
        if (dir == "front") {
            avoidanceOffset = ComputeAvoidanceOffset();
        }

        navmeshAgent.Move((transformDir + avoidanceOffset) * moveSpeed * Time.deltaTime);
        
        if (isAvoiding)
        {
            Quaternion targetRotation = Quaternion.LookRotation((transformDir + avoidanceOffset), Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * avoidanceSteerSpeed);
        }
    }

    public virtual void RootMotionMovement(string anim, float animT, float turnSpeed)
    {
        if (Vector3.Distance(lastCalculatedPath, transform.position) <= 0.05f) 
        {
            return;
        }

        navmeshAgent.isStopped = true;
        animManager.Play(anim, animT);
    
        Vector3 avoidanceOffset = ComputeAvoidanceOffset();
        if (!isAvoiding) {
            MovementRotate(turnSpeed);
        }
        else {
            Quaternion targetRotation = Quaternion.LookRotation((transform.forward + avoidanceOffset), Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * avoidanceSteerSpeed);
        }
    }
    
    Vector3 ComputeAvoidanceOffset()
    {
        if (!useLocalAvoidance || !isPathReachable || isAvoidingCoolDown)
        {
            isAvoiding = false;
            return Vector3.zero;
        }

        Vector3 offset = Vector3.zero;
        bool isTouched = false;

        System.Array.Clear(avoidanceHitArr, 0, avoidanceHitArr.Length);
        int numColliders = Physics.OverlapSphereNonAlloc(transform.position, avoidanceRadius, avoidanceHitArr, layersToAvoid);

        // Don't run avoidance if too close to end point
        if (numColliders > 0)
        {
            if (CalculateCornersDistanceFrom(transform.position, pathCorner) <= navmeshAgent.radius * 2)
            {
                isAvoiding = false;
                return offset;
            }
        }

        for (int i=0; i<numColliders; i++)
        {
            Collider hit = avoidanceHitArr[i];
            
            if (hit.transform.IsChildOf(transform)) 
            {
                continue;
            }

            if (enemyToAttack != null)
            {
                if (hit.transform.IsChildOf(enemyToAttack.transform))
                {
                    continue;
                }
            }

            Vector3 toOther = new Vector3(hit.transform.position.x, transform.position.y, hit.transform.position.z) - transform.position;
            float angle = Vector3.Angle(transform.forward, toOther);

            // Only consider agents roughly in front
            if (angle < avoidIfWithinAngle * 0.5f)
            {
                // Calculate a side vector (right or left)
                Vector3 side = Vector3.Cross(Vector3.up, toOther).normalized;

                // Pick side based on position to avoid consistent same-side swaying
                float sideDot = Vector3.Dot(transform.right, side);
                side *= Mathf.Sign(sideDot);

                offset += side * avoidanceOffsetStrength;
                isTouched = true;
            }
        }

        isAvoiding = isTouched;
        
        // take a time stamp of the last avoidance -> to reenable movement turning after 0.1f
        if (isAvoiding) {
            lastAvoidanceTimeStamp = Time.time;
        }

        return offset;
    }

    void AvoidanceTimers()
    {
        // Avoidance timer
        if (isAvoiding)
        {
            avoidingTimer += Time.deltaTime;
            avoidingGraceTimer = 0;
            isStickyAvoiding = true;
        }
        else if (isStickyAvoiding)
        {
            avoidingGraceTimer += Time.deltaTime;
            if (avoidingGraceTimer >= avoidingGraceTime)
            {
                isStickyAvoiding = false;
                avoidingTimer = 0f;
            }
            else {
                avoidingTimer += Time.deltaTime;
            }
        }

        // Force exit avoidance if it's taking too long
        if (isStickyAvoiding && avoidingTimer >= maxAvoidanceTime)
        {
            avoidingTimer = 0f;
            avoidingGraceTimer = 0;
            lastAvoidanceTimeStamp = 0;
            isAvoiding = false;
            isStickyAvoiding = false;
            isAvoidingCoolDown = true;
        }

        // Avoidance cool down
        if (isAvoidingCoolDown)
        {
            avoidanceCoolDownTimer += Time.deltaTime;
            if (avoidanceCoolDownTimer >= avoidanceCoolDownTime)
            {
                isAvoidingCoolDown = false;
                avoidanceCoolDownTimer = 0;
            }
        }
    }

    void ResetAllAvoidance()
    {
        avoidingTimer = 0;
        avoidingGraceTimer = 0;
        lastAvoidanceTimeStamp = 0;
        avoidanceCoolDownTimer = 0;
        isAvoiding = false;
        isStickyAvoiding = false;
        isAvoidingCoolDown = false;
    }

    bool MovementTurning()
    {
        if (!waypoints.useMovementTurning) 
        {
            isturningToCorner = false;
            return true;
        }
        
        // check is turning
        if (isturningToCorner) 
        {
            if (!TurnTo(pathCorner, GetTurnAnim("left"), GetTurnAnim("right"), waypoints.turningAnimT, waypoints.turnSpeed, waypoints.useTurnAnims)) {
                return false;
            }

            isturningToCorner = false;
        }

        // calculate the dot prod of the path corner
        if (IsPathCornerDotProdExtreme()) 
        {
            isturningToCorner = true;
            return false;
        }

        return true;
    }

    void MovementRotate(float turnSpeed)
    {
        if ((state == State.attack || state == State.goingToCover) && enemyToAttack != null) 
        {
            RotateTo(pathCorner, turnSpeed);
            return;
        }

        if (cornersQueue.Count > 0) 
        {
            RotateTo(pathCorner, turnSpeed);
            return;
        }

        if (IsPathCornerDotProdExtreme(true)) 
        {
            RotateTo(pathCorner, 20);
            return;
        }
        
        RotateTo(pathCorner, turnSpeed);
    }

    bool IsPathCornerDotProdExtreme(bool onCloseDistanceOnly=false)
    {
        if (onCloseDistanceOnly) {
            float distance = CalculateCornersDistanceFrom(transform.position, pathCorner);
            if (distance > Mathf.Clamp(navmeshAgent.radius * 2.2f, 2f, Mathf.Infinity)) {
                return false;
            }
        }

        float dotProd = Vector3.Dot((pathCorner - transform.position).normalized, transform.forward);
        if (dotProd < Mathf.Clamp(waypoints.movementTurningSensitivity, -1, 0.97f)) {
            return true;
        }

        return false;
    }

    // get the next corner
    void GetNextCorner()
    {   
        if (cornersQueue.Count > 0) {
            pathCorner = cornersQueue.Dequeue();
        }

        if (navmeshAgent.isOnOffMeshLink) return;

        // SetDestination() enables us to check for off mesh links
        // but speed and turn speed = 0 
        navmeshAgent.SetDestination(pathCorner);
    }

    // smooth rotate agent to location
    public void RotateTo(Vector3 location, float speed)
    {   
        Vector3 rotationVector = (new Vector3(location.x, transform.position.y, location.z) - transform.position).normalized;
        if (rotationVector == Vector3.zero) return;

        Quaternion lookRotation = Quaternion.LookRotation(rotationVector);
        lookRotation = new Quaternion(0f, lookRotation.y, 0f, lookRotation.w);
        transform.rotation = Quaternion.Slerp(new Quaternion(0f, transform.rotation.y, 0f, transform.rotation.w), lookRotation, speed * Time.deltaTime);
    }

    // set waypoint index to the next waypoint
    public Vector3 NextWayPoint()
    {   
        if (waypoints.waypoints.Count == 0) return Vector3.zero;

        if (waypointIndex >= waypoints.waypoints.Count - 1) {
            if (waypoints.loop) waypointIndex = 0;
        }
        else {
            waypointIndex++;
        }

        endDestination = waypoints.waypoints[waypointIndex];
        return endDestination;
    }

    // returns true if there is a waypoint rotation
    public bool CheckWayPointRotation()
    {
        if ((waypoints.waypointsRotation[waypointIndex].x != 0 || waypoints.waypointsRotation[waypointIndex].y != 0)) {
            float dotProd = Vector3.Dot((new Vector3(transform.position.x + waypoints.waypointsRotation[waypointIndex].x, transform.position.y, transform.position.z + waypoints.waypointsRotation[waypointIndex].y) - transform.position).normalized, transform.forward);
            
            if (dotProd < 0.97f) {
                return true;
            }else{
                return false;
            }
        }
        else {
            return false;
        }
    }
    
    // turns AI to waypoint rotations and returns true when done
    public bool WayPointTurning()
    {   
        // set the turning anims of the state
        string leftTurnAnim = GetTurnAnim("left");
        string rightTurnAnim = GetTurnAnim("right");
        float animT = waypoints.turningAnimT;

        if ((waypoints.waypointsRotation[waypointIndex].x != 0 || waypoints.waypointsRotation[waypointIndex].y != 0)) {
            Vector3 wayPointDir = new Vector3(transform.position.x + waypoints.waypointsRotation[waypointIndex].x, transform.position.y, transform.position.z + waypoints.waypointsRotation[waypointIndex].y);
            return TurnTo(wayPointDir, leftTurnAnim, rightTurnAnim, animT);
        }
        else {
            return true;
        }
    }

    // turn to location and returns true when done
    public bool TurnTo(Vector3 location, string leftTurnAnim = null, string rightTurnAnim = null, float animT = 0.25f, float turnSpeed=0, bool playAnims = true)
    {
        location = new Vector3(location.x, transform.position.y, location.z);

        // get dir (left or right)
        int waypointTurnDir = AngleDir
        (transform.forward, 
        location - transform.position, 
        transform.up);
        
        float dotProd = Vector3.Dot((location - transform.position).normalized, transform.forward);
        if (dotProd >= 0.97f) {
            return true;
        }

        // turn right if dir is 1
        if (playAnims)
        {
            if (waypointTurnDir == 1) 
            {
                if (rightTurnAnim == null || rightTurnAnim == "")
                {
                    PrintWarning(warnEmptyAnimations, "AI tried to right turn but the animation name is empty so it got ignored.");
                }
                else {
                    animManager.Play(rightTurnAnim, animT);
                }
            }
            else {
                if (leftTurnAnim == null || leftTurnAnim == "")
                {
                    PrintWarning(warnEmptyAnimations, "AI tried to left turn but the animation name is empty so it got ignored.");
                }
                else {
                    animManager.Play(leftTurnAnim, animT);
                }
            }
        }

        if (turnSpeed == 0) {
            turnSpeed = waypoints.turnSpeed;
        }
        
        RotateTo(location, turnSpeed);
        return false;
    }

    // return 1 if location is to the right and -1 if left
    int AngleDir(Vector3 fwd, Vector3 targetDir, Vector3 up) 
    {
        Vector3 perp = Vector3.Cross(fwd, targetDir);
        float dir = Vector3.Dot(perp, up);
        
        if (dir > 0f) {
            return 1;
        } else if (dir < 0f) {
            return -1;
        } else {
            return 0;
        }
    }

    // return the turn anim name depending on current state
    string GetTurnAnim(string dir) 
    {
        if (state == State.normal) {
            if (dir == "right") return waypoints.rightTurnAnimNormal;
            else return waypoints.leftTurnAnimNormal;
        }
        else{
            if (dir == "right") return waypoints.rightTurnAnimAlert;
            else return waypoints.leftTurnAnimAlert;
        }
    }

    bool CheckAnimTFinished(string animName, float tTime)
    {
        if (animName == lastFinishedAnimT) {
            return true;
        }

        if (anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= tTime/2) {
            lastFinishedAnimT = lastAnim;
            return true;
        }

        return false;
    }

    #endregion

    #region VISION
    
    public virtual void VisionCheck()
    {
        if (!vision.visionActive)
        {
            if (enemyToAttack) 
            {
                VisionReset();
            }

            return;
        }

        // don't run vision if dead or hit
        if (state == State.death || state == State.hit) {
            return;
        }

        // run method once every pulse rate
        if (visionCheckElapsed < vision.visionFrameSkipping) 
        {
            visionCheckElapsed++;
            return;
        }

        visionCheckElapsed = 0;

        // check if previous target changed tag to non-hostile or is disabled
        if (enemyToAttack != null)
        {
            if (System.Array.IndexOf(vision.hostileTags, enemyToAttack.tag) < 0 || !enemyToAttack.activeSelf) 
            {
                isTargetTagChanged = true;
                VisionReset();
                return;
            }

            isTargetTagChanged = false;
        }
        
        Vector3 npcDir = transform.position + vision.visionPosition;
        // set the vision range and angle according to state
        float range, angle;
        
        switch (state) 
        {
            case State.normal:
                angle = vision.visionDuringNormalState.coneAngle;
                range = vision.visionDuringNormalState.sightRange;
                break;
            case State.alert:
                angle = vision.visionDuringAlertState.coneAngle;
                range = vision.visionDuringAlertState.sightRange;
                break;
            case State.attack:
                if (vision.visionDuringAttackState.alwaysApply)
                {
                    angle = vision.visionDuringAttackState.coneAngle;
                    range = vision.visionDuringAttackState.sightRange;
                    break;
                }

                if (!enemyToAttack) 
                {
                    angle = vision.visionDuringAlertState.coneAngle;
                    range = vision.visionDuringAlertState.sightRange;
                    break;
                }

                angle = vision.visionDuringAttackState.coneAngle;
                range = vision.visionDuringAttackState.sightRange;
                break;
            case State.goingToCover:
                angle = vision.visionDuringAttackState.coneAngle;
                range = vision.visionDuringAttackState.sightRange;
                break;
            case State.returningToAlert:
                angle = vision.visionDuringAlertState.coneAngle;
                range = vision.visionDuringAlertState.sightRange;
                break;
            case State.sawAlertTag:
                angle = vision.visionDuringAlertState.coneAngle;
                range = vision.visionDuringAlertState.sightRange;
                break;
            case State.surprised:
                angle = vision.visionDuringAttackState.coneAngle;
                range = vision.visionDuringAttackState.sightRange;
                break;
            case State.hit:
                if (enemyToAttack) {
                    angle = vision.visionDuringAttackState.coneAngle;
                    range = vision.visionDuringAttackState.sightRange;
                    break;
                }
                angle = vision.visionDuringAlertState.coneAngle;
                range = vision.visionDuringAlertState.sightRange;
                break;
            default:
                angle = PreviousStateVAngle();
                range = PreviousStateVRange();
                break;
        }

        System.Array.Clear(visionHitArr, 0, 20);
        enemiesTargetedByAmount.Clear();

        int visionHitArrNum = Physics.OverlapSphereNonAlloc(transform.position, range, visionHitArr, vision.hostileAndAlertLayers);

        float smallestDist = Mathf.Infinity;
        float testDist = 0;
        int smallestDistIndex = -1;
        
        for (int i=0; i<visionHitArrNum; i++) 
        {
            if (transform.IsChildOf(visionHitArr[i].transform) || visionHitArr[i].transform.IsChildOf(transform)) {
                continue;
            }

            // if companion mode is on -> stop AI from targeting companion 
            if (companionMode && companionTo != null && companionTo.IsChildOf(visionHitArr[i].transform)) {
                continue;
            }
            
            // check angle and heights
            Collider hostile = visionHitArr[i];
            Vector3 targetCaughtPoint = hostile.ClosestPoint(transform.position + vision.visionPosition);

            if (Vector3.Angle(visionT.forward, (targetCaughtPoint - npcDir)) > (angle * 0.5f))
            {
                enemiesTargetedByAmount.Remove(hostile);
                continue;
            }

            // check if height too high
            if (enemyToAttack == null || vision.checkTargetHeight && enemyToAttack != null) 
            {
                float suspectHeight = (targetCaughtPoint.y + vision.visionPosition.y) - (transform.position.y + vision.visionPosition.y + vision.maxSightLevel);
                if (suspectHeight > 0) 
                {
                    enemiesTargetedByAmount.Remove(hostile);
                    continue;
                }
            }
            
            // check if height too low (only if not in attack or surprised)
            if (vision.useMinLevel)
            {
                if (state != State.attack && state != State.goingToCover && state != State.surprised)
                {
                    if (targetCaughtPoint.y < (transform.position.y + vision.minSightLevel)) {
                        enemiesTargetedByAmount.Remove(hostile);
                        continue;
                    }
                }
            }

            // check for alert tag
            if (state != State.spareState && state != State.attack && state != State.goingToCover && state != State.hit)
            {
                int alertTagIndex = vision.GetAlertTagIndex(visionHitArr[i].tag);
                if (alertTagIndex >= 0) 
                {
                    GameObject alertObj = visionHitArr[i].transform.gameObject;
                    SawAlertTag(alertObj, alertTagIndex);
                }
            }

            // THE STARTING CODE FOR HOSTILE
            if (friendly) 
            {
                return;
            }
            
            // check for hostile tags
            if (System.Array.IndexOf(vision.hostileTags, visionHitArr[i].tag) < 0) 
            {
                enemiesTargetedByAmount.Remove(hostile);
                continue;
            }

            // set the raycast layers for vision
            int layersToHit;
            int ignoreRaycastLayer = 1 << 2;

            if (state != State.attack && state != State.goingToCover) {
                layersToHit = vision.layersToDetect | vision.hostileAndAlertLayers;
            }
            else {
                if (coverShooterMode) layersToHit = vision.hostileAndAlertLayers;
                else layersToHit = vision.layersToDetect | vision.hostileAndAlertLayers;
            }

            layersToHit &= ~ignoreRaycastLayer;
            
            // check target hasn't been disabled
            if (!hostile.transform.gameObject.activeSelf || System.Array.IndexOf(vision.hostileTags, hostile.transform.gameObject.tag) < 0)
            {
                enemiesTargetedByAmount.Remove(hostile);
                continue;
            }
            
            Collider[] enemyToAttackColliders = hostile.transform.GetComponentsInChildren<Collider>();
            int colSize = enemyToAttackColliders.Length;
            int testHits = 1;

            if (colSize > 2)
            {
                testHits = colSize/2;
            }

            if (RayCastObjectColliders(hostile.transform.gameObject, layersToHit, testHits, enemyToAttackColliders))
            {
                BlazeAIEnemyManager enemyManagerTemp = hostile.transform.GetComponent<BlazeAIEnemyManager>();

                // get the closest enemy by distance
                testDist = (transform.position - visionHitArr[i].transform.position).sqrMagnitude;
                if (testDist <= smallestDist)
                {
                    smallestDist = testDist;
                    smallestDistIndex = i;
                }

                // if hostile doesn't have an enemy manager -> means it's not targeted yet by any AI
                // so any free AI should target it, plus, make sure the key (hostile) doesn't exist already in the dictionary
                if (enemyManagerTemp == null && !enemiesTargetedByAmount.ContainsKey(hostile)) 
                {
                    enemiesTargetedByAmount.Add(hostile, -1);
                    continue;
                }

                // make sure the key (hostile) doesn't exist in the dictionary
                if (!enemiesTargetedByAmount.ContainsKey(hostile)) {
                    enemiesTargetedByAmount.Add(hostile, enemyManagerTemp.targetedBy.Count);
                }
            }
            else {
                enemiesTargetedByAmount.Remove(hostile);
            }
        }

        // if no valid enemies -> return
        if (enemiesTargetedByAmount.Count == 0)
        {
            VisionReset();
            return;
        }

        // sort the enemies dictionary in ascending order
        var sortedDict = from entry in enemiesTargetedByAmount orderby entry.Value ascending select entry;

        if (enemyToAttack) 
        {
            if (canChangeVisionTarget) 
            {
                collOfTarget = visionHitArr[smallestDistIndex];
                canChangeVisionTarget = false;
            }
        }
        else {
            collOfTarget = enemiesTargetedByAmount.ElementAt(0).Key;

            if (enemiesTargetedByAmount.Count > 1)
            {
                // if the first two items have the same amount of targeted AIs then choose the closest one
                if (enemiesTargetedByAmount.ElementAt(0).Value == enemiesTargetedByAmount.ElementAt(1).Value)
                {
                    float tempDist1 = (enemiesTargetedByAmount.ElementAt(0).Key.transform.position - transform.position).sqrMagnitude;
                    float tempDist2 = (enemiesTargetedByAmount.ElementAt(1).Key.transform.position - transform.position).sqrMagnitude;
                    int leastIndex = 0;
                    
                    if (tempDist1 > tempDist2) {
                        leastIndex = 1;
                    }

                    collOfTarget = enemiesTargetedByAmount.ElementAt(leastIndex).Key;
                }
            }
        }

        if (collOfTarget == null)
        {
            VisionReset();
            return;
        }
        
        // if set to ignore unreachable enemy -> check if enemy is unreachable 
        if (ignoreUnreachableEnemy) 
        {
            if (!IsPathReachable(collOfTarget.transform.position)) 
            {
                // check if there's a previously ignored enemy
                if (ignoredEnemy != null) 
                {
                    // if the previously ignored enemy didn't leave vision -> don't trigger function again until it gets out of vision and caught again
                    if (System.Array.IndexOf(visionHitArr, ignoredEnemy) >= 0) {
                        return;
                    }

                    ignoredEnemy = null;
                }
                
                // if no previously ignored enemy -> trigger the function
                IgnoreEnemy(collOfTarget);
                return;
            }
        }

        SetEnemyColPoint();

        // track distances
        distanceToEnemySqrMag = (ValidateYPoint(enemyColPoint) - transform.position).sqrMagnitude;
        distanceToEnemy = Vector3.Distance(ValidateYPoint(enemyColPoint), transform.position);
        
        if (previousEnemy != null && previousEnemy != collOfTarget.transform) 
        {
            ResetEnemyManager();
        }
        
        if (vision.useVisionMeter) 
        {
            // increment the vision meter
            if (collOfTarget != null)
            {
                if (!CheckVisionMeter(true, collOfTarget.transform.gameObject, range)) {
                    return;
                }
            }
        }

        // set the target
        enemyToAttack = collOfTarget.transform.gameObject;

        // reset check enemy position since AI has a target and no AI can call it
        checkEnemyPosition = Vector3.zero;
        captureEnemyTimeStamp = Time.time;
        visionMeter = 1;
        RunEnemyOnEnterEvent();


        if (state == State.spareState) 
        {
            return;
        }

        // activate surprised state
        if (state == State.normal) 
        {
            Surprised();
            return;
        }
        
        if (state != State.distracted) 
        {
            TurnToAttackState();
            return;
        }

        if (previousState != State.normal) 
        {
            TurnToAttackState();
            return;
        }

        Surprised();
    }

    void VisionReset()
    {
        RunEnemyOnExitEvent();
        VisionNoEnemies();
        
        if (vision.useVisionMeter) {
            CheckVisionMeter(false);
        }
        else {
            visionMeter = 0;
        }
    }

    void VisionNoEnemies()
    {   
        enemiesTargetedByAmount.Clear();
        enemyToAttack = null;
        ignoredEnemy = null;

        distanceToEnemySqrMag = (ValidateYPoint(enemyColPoint) - transform.position).sqrMagnitude;
        distanceToEnemy = Vector3.Distance(ValidateYPoint(enemyColPoint), transform.position);

        ResetEnemyManager();
    }

    void SetEnemyColPoint()
    {
        if (collOfTarget == null)
        {
            return;
        }

        enemyColPoint = collOfTarget.ClosestPoint(transform.position + vision.visionPosition);
    }

    bool HasParentWithTag(Transform child, string tag)
    {
        Transform current = child.parent;

        while (current != null)
        {
            if (current.CompareTag(tag))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    // this method will trigger when the AI sees an alert tag
    public virtual void SawAlertTag(GameObject alertObj, int index)
    {
        // save the saw tag name and the object's position
        sawAlertTagName = alertObj.tag;
        sawAlertTagObject = alertObj.transform;

        Collider objectColl = sawAlertTagObject.GetComponent<Collider>();
        sawAlertTagPos = GetSamplePosition(ValidateYPoint(alertObj.transform.position), objectColl.bounds.size.x + objectColl.bounds.size.z);

        int layers = vision.layersToDetect | vision.hostileAndAlertLayers;
        
        // check if any collider is caught
        if (!RayCastObjectColliders(alertObj, layers, 1, alertObj.GetComponentsInChildren<Collider>()))
        {
            return;
        }

        string fallBackTag;

        // check whether a fallback tag is set
        if (vision.alertTags[index].fallBackTag.Length <= 0) {
            fallBackTag = "Untagged";
        }
        else {
            fallBackTag = vision.alertTags[index].fallBackTag;
        }

        // if behaviour is empty -> tell the user
        if (vision.alertTags[index].behaviourScript == null) {
            Debug.Log($"Alert Tag: {sawAlertTagName} behaviour is empty so nothing will be enabled.");
        }

        // change the tag name of the alert object to the fallback tag
        alertObj.tag = fallBackTag;

        // set the state to saw alert tag and Update() enables the corresponding behaviour
        SetState(State.sawAlertTag);
    }
    
    // check if colliders of a gameobject are seen
    bool RayCastObjectColliders(GameObject go, int layersToHit, int minDetectionScore, Collider[] objColls)
    {
        Vector3 npcDir = transform.position + vision.visionPosition;
        Vector3 colDir;

        if (!vision.multiRayVision)
        {
            Collider item = go.GetComponent<Collider>();
            colDir = item.ClosestPoint(transform.position + vision.visionPosition) - npcDir;
            float rayDistance = Vector3.Distance(go.transform.position, npcDir) + 2;

            if (CheckTargetVisibleWithRay(go.transform, npcDir, colDir, rayDistance, layersToHit)) 
            {
                return true;
            }

            return false;
        }

        int colSize = objColls.Length;
        int detectionScore = 0;

        // check if raycast can hit target colliders
        for (int i=0; i<colSize; i++) 
        {
            Collider item = objColls[i];
            if (item == null)
            {
                continue;
            }

            npcDir = transform.position + vision.visionPosition;
            colDir = item.ClosestPoint(transform.position + vision.visionPosition) - npcDir;
            float rayDistance = Vector3.Distance(item.transform.position, transform.position) + 5;

            // check center
            if (CheckTargetVisibleWithRay(go.transform, npcDir, colDir, rayDistance, layersToHit)) {
                detectionScore++;
            }
        }

        // if detection score is bigger or equal to the minimum required -> return true
        if (detectionScore >= minDetectionScore) 
        {
            return true;
        }
        
        return false;
    }

    public bool CheckTargetVisibleWithRay(Transform target, Vector3 startDir, Vector3 targetDir, float rayDistance, int layers)
    {
        if (Physics.Raycast(startDir, targetDir, out RaycastHit hit, rayDistance, layers)) 
        {
            if (hit.transform.IsChildOf(target) || target.IsChildOf(hit.transform)) {
                return true;
            }
            
            return false;
        }

        return false;
    }

    public bool CheckIfTargetSeenFromPoint(Vector3 point, bool useNewCenterPos=false, float visionPosH = -1)
    {
        int layers = vision.layersToDetect | vision.hostileAndAlertLayers;
        float visionPosHeight;

        if (useNewCenterPos) {
            visionPosHeight = visionPosH;
        }
        else {
            visionPosHeight = defaultVisionPos.y;
        }

        Vector3 finalCenterPos = new Vector3(vision.visionPosition.x, visionPosHeight, vision.visionPosition.z);
        Vector3 dir = (enemyColPoint - (point + finalCenterPos)).normalized;
        
        System.Array.Clear(checkTargetSeenHitArr, 0, 12);
        int sphereCastHits = Physics.SphereCastNonAlloc(point + finalCenterPos, 0.05f, dir, checkTargetSeenHitArr, Vector3.Distance(point, enemyColPoint) + 3, layers);
        float smallestDist = Mathf.Infinity;
        int bestIndex = -1;

        // filter the results
        for (int i=0; i<sphereCastHits; i++) 
        {
            if (transform.IsChildOf(checkTargetSeenHitArr[i].transform) || checkTargetSeenHitArr[i].transform.IsChildOf(transform)) {
                continue;
            }

            // get the smallest distance
            if (checkTargetSeenHitArr[i].distance <= smallestDist)
            {
                smallestDist = checkTargetSeenHitArr[i].distance;
                bestIndex = i;
            }
        }

        if (bestIndex == -1) 
        {
            return false;
        }

        if (enemyToAttack.transform.IsChildOf(checkTargetSeenHitArr[bestIndex].transform) || checkTargetSeenHitArr[bestIndex].transform.IsChildOf(enemyToAttack.transform)) {
            return true;
        }
        
        return false;
    }

    // get the vision angle of the previous state
    float PreviousStateVAngle()
    {
        if (previousState == State.normal) {
            return vision.visionDuringNormalState.coneAngle;
        }

        if (previousState == State.alert) {
            return vision.visionDuringAlertState.coneAngle;
        }

        if (previousState == State.attack) {
            return vision.visionDuringAttackState.coneAngle;
        }

        if (previousState == State.hit) {
            if (enemyToAttack) {
                return vision.visionDuringAttackState.coneAngle;
            }

            return vision.visionDuringAlertState.coneAngle;
        }

        return vision.visionDuringNormalState.coneAngle;
    }

    // get the vision range of the previous state
    public float PreviousStateVRange()
    {
        if (previousState == State.normal) {
            return vision.visionDuringNormalState.sightRange;
        }

        if (previousState == State.alert) {
            return vision.visionDuringAlertState.sightRange;
        }

        if (previousState == State.attack) {
            return vision.visionDuringAttackState.sightRange;
        }

        if (previousState == State.hit) {
            if (enemyToAttack) {
                return vision.visionDuringAttackState.sightRange;
            }

            return vision.visionDuringAlertState.sightRange;
        }

        return vision.visionDuringNormalState.sightRange;
    }

    // check if an enemy got too close and turn to attack if so
    void SurroundingsCheck()
    {
        // return if any of these conditions are true
        if (!checkEnemyContact || friendly || enemyToAttack || state == State.hit || state == State.death) {
            return;
        }

        GameObject closeTarget = CheckSurroundingForTarget();

        // check if an enemy got too close
        if (closeTarget == null) {
            return;
        }


        // if companion mode is on -> eliminate the companion being targeted
        if (companionMode && companionTo != null && companionTo.IsChildOf(closeTarget.transform)) {
            return;
        }


        // if caught collider is a child of the same AI -> skip
        if (transform.IsChildOf(closeTarget.transform) || closeTarget.transform.IsChildOf(transform)) {
            return;
        }


        // if set to ignore unreachable enemy -> check if enemy is unreachable 
        if (ignoreUnreachableEnemy) 
        {
            if (!IsPathReachable(closeTarget.transform.position)) {
                // check if there's a previously ignored enemy
                if (ignoredEnemy != null) {
                    // if previously ignored enemy didn't leave vision -> don't trigger function again until it gets out of vision and caught again
                    if (ignoredEnemy.transform.IsChildOf(closeTarget.transform) || closeTarget.transform.IsChildOf(ignoredEnemy.transform)) {
                        return;
                    }

                    ignoredEnemy = null;
                }
                
                // if no previously ignored enemy -> trigger the function
                IgnoreEnemy(closeTarget.GetComponent<Collider>());
                return;
            }
        }
            
        
        if (state == State.distracted) 
        {
            if (previousState == State.normal) {
                Surprised();
                return;
            }

            SetEnemy(closeTarget);
            return;
        }


        if (state != State.normal) {
            SetEnemy(closeTarget);
            return;
        }


        SetEnemy(closeTarget, false);
        Surprised();
        if (vision.useVisionMeter) visionMeter = 1;
    }

    // check for an enemy character specific radius
    GameObject CheckSurroundingForTarget()
    {
        if (state == State.attack || state == State.goingToCover || state == State.surprised) {
            return null;
        }
        
        checkSurroundingElapsed++;
        if (checkSurroundingElapsed < 5) {
            return null;
        }
        checkSurroundingElapsed = 0;
        
        System.Array.Clear(skinHitArr, 0, 15);
        int numColliders = Physics.OverlapSphereNonAlloc(transform.position + vision.visionPosition, enemyContactRadius, skinHitArr, vision.hostileAndAlertLayers);

        for (int i=0; i<numColliders; i++) 
        {
            if (skinHitArr[i].transform.IsChildOf(transform) || transform.IsChildOf(skinHitArr[i].transform)) {
                continue;
            }

            if (System.Array.IndexOf(vision.hostileTags, skinHitArr[i].transform.tag) >= 0)
            {
                enemyPosOnSurprised = skinHitArr[i].transform.position;
                if (RayCastObjectColliders(skinHitArr[i].transform.gameObject, vision.layersToDetect | vision.hostileAndAlertLayers, 1, skinHitArr)) {
                    return skinHitArr[i].transform.gameObject;
                }
            }
        }

        return null;
    }

    // ignore passed enemy
    void IgnoreEnemy(Collider enemyColl)
    {
        enemyToAttack = null;
        isAttacking = false;

        // smooth transition to alert if in normal state
        if (state == State.normal) {
            ChangeState("alert");
        }
        else {
            // don't force out of these states
            if (state == State.hit || state == State.surprised || state == State.death || state == State.distracted || state == State.spareState) {
                return;
            }

            if (state != State.alert) {
                SetState(State.alert);
            }
        }

        ignoredEnemy = enemyColl;

        // if no fallback points set -> exit
        if (fallBackPoints.Length == 0) return;

        // choose a random point from the fallback points
        Vector3 chosenPoint = fallBackPoints[Random.Range(0, fallBackPoints.Length)];
        
        // if the randomly chosen point is zero -> exit
        if (chosenPoint == Vector3.zero) return;

        // if point != Vector3.zero then go to that point
        MoveToLocation(chosenPoint);
    }

    // check if gameobject is completely visible by firing rays at it's center
    public bool CheckObjectCompletelyVisible(GameObject go, int layersToHit, int minDetectionScore)
    {
        if (go == null) return false;
        minDetectionScore = Mathf.Clamp(minDetectionScore, 1, 3);

        Collider objColl = go.GetComponentInChildren<Collider>();
        if (objColl == null) return false;

        Vector3 startCenter = transform.position + vision.visionPosition;
        Vector3 targetPoint = objColl.ClosestPoint(startCenter);
        float maxDistance = distanceToEnemy + 5f;

        float sideOffset = Mathf.Max(navmeshAgent.radius / 2f, 0.3f); // tweak if needed
        Vector3[] rayStarts = new Vector3[]
        {
            startCenter,                         // center
            startCenter + transform.right * sideOffset,  // right
            startCenter - transform.right * sideOffset   // left
        };

        int detectionScore = 0;

        foreach (Vector3 rayStart in rayStarts)
        {
            Vector3 dir = targetPoint - rayStart;
            float dist = dir.magnitude;
            if (dist <= 0.001f)
            {
                detectionScore++;
                continue;
            }
            dir /= dist; // normalize

            System.Array.Clear(checkObjVisibleRayHitArr, 0, checkObjVisibleRayHitArr.Length);

            int hits = Physics.RaycastNonAlloc(rayStart, dir, checkObjVisibleRayHitArr, Mathf.Min(dist, maxDistance), layersToHit);
            float closestDist = Mathf.Infinity;
            RaycastHit closestHit = default;
            bool hitFound = false;
            Debug.DrawRay(rayStart, dir * Mathf.Min(dist, maxDistance), Color.red, 1);

            for (int i = 0; i < hits; i++)
            {
                RaycastHit hit = checkObjVisibleRayHitArr[i];
                if (hit.collider == null) continue;
                if (hit.collider.transform.root == transform.root) continue; // ignore self
                if (hit.distance <= 0f) continue;

                if (hit.distance < closestDist)
                {
                    closestDist = hit.distance;
                    closestHit = hit;
                    hitFound = true;
                }
            }

            if (!hitFound || closestHit.collider.transform.root == go.transform.root)
            {
                detectionScore++;
            }
        }

        return detectionScore >= minDetectionScore;
    }

    void RunEnemyOnEnterEvent()
    {
        if (isEnemyEnterEventRan) {
            return;
        }

        vision.enemyEnterEvent.Invoke();
        isEnemyEnterEventRan = true;
    }

    void RunEnemyOnExitEvent()
    {
        if (!isEnemyEnterEventRan) {
            return;
        }

        vision.enemyLeaveEvent.Invoke();
        isEnemyEnterEventRan = false;
    }

    bool CheckVisionMeter(bool isDetected, GameObject potentialEnemy=null, float currentVisionRange=0)
    {
        float speed;

        if (isDetected)
        {
            float halfVisionRange = currentVisionRange/2;
            potentialEnemyToAttack = potentialEnemy;
            AddEnemyManager(potentialEnemyToAttack.transform, false, true);

            if (distanceToEnemy > halfVisionRange) {
                speed = vision.visionMeterSpeeds.speedOnFullDistance;
            }
            else {
                speed = vision.visionMeterSpeeds.speedOnHalfDistance;
            }

            visionMeterSpeedInUse = speed;
            visionMeter += Time.deltaTime * speed;
            
            if (visionMeter >= 1) 
            {
                MovePotentialToTargetEnemyManager();
                visionMeter = 1;
                return true;
            }

            return false;
        }

        potentialEnemyToAttack = null;
        ResetEnemyManager();

        if (state == State.distracted)
        {
            if (vision.dontDecrementVMOnDistract)
            {
                return false;
            }
        }
        
        speed = vision.visionMeterSpeeds.speedOnEmpty;
        visionMeterSpeedInUse = speed;
        
        visionMeter -= Time.deltaTime * speed;
        if (visionMeter <= 0) 
        {
            visionMeter = 0;
        }

        return false;
    }

    public void SetVisionT()
    {
        if (vision == null) return;

        if (vision.head == null) {
            visionT = transform;
        }
        else {
            visionT = vision.head;
        }
    }

    #endregion

    #region ENEMY MANAGER

    // add enemy manager to target
    public void AddEnemyManager(Transform currentEnemy, bool addAsScheduled = true, bool potentialEnemy = false)
    {
        if (currentEnemy == null || currentEnemy == previousEnemy) 
        {
            return;
        }

        ResetEnemyManager();

        // add the enemy manager component
        enemyManager = currentEnemy.GetComponent<BlazeAIEnemyManager>();
        if (enemyManager == null) {
            enemyManager = currentEnemy.gameObject.AddComponent(typeof(BlazeAIEnemyManager)) as BlazeAIEnemyManager;
        }
        
        if (potentialEnemy) {
            enemyManager.AddPotentialEnemy(this);
        }
        else {
            if (addAsScheduled) {
                enemyManager.AddScheduledEnemy(this);
            }
            else {
                enemyManager.AddTarget(this);
            }
        }
        
        previousEnemy = currentEnemy;
    }

    public void ResetEnemyManager(bool shouldRemoveEnemy = true)
    {
        if (enemyManager == null) 
        {
            return;
        }

        previousEnemy = null;
        
        if (shouldRemoveEnemy) 
        {
            enemyManager.RemoveEnemy(this);
            enemyManager = null;
        }
    }

    void MovePotentialToTargetEnemyManager()
    {
        enemyManager.AddTarget(this);
        enemyManager.potentialEnemies.Remove(this);
        ResetEnemyManager(false);
    }

    #endregion

    #region BEHAVIOURS & STATE MANAGEMENT
    
    // set the state of the AI to passed value
    public virtual void SetState(State stateToTurnTo, bool forceSetInOffMesh = false)
    {
        if (!System.Enum.IsDefined(typeof(State), stateToTurnTo)) {
            Debug.Log("Trying to set state to a value that is not defined.");
            return;
        }
        
        state = stateToTurnTo;
        CheckState(forceSetInOffMesh);
        CheckDistanceCullingWithState();
    }

    // check state and enable the behaviour
    void CheckState(bool forceChange = false)
    {
        if (navmeshAgent == null) return;
        if (navmeshAgent.isOnOffMeshLink && !forceChange || isChangeStateLooping)
        {
            return;
        }
        
        isChangeStateLooping = true;

        // enable the state's behaviour
        switch (state) 
        {
            case State.normal:
                if (companionMode) {
                    RunBehaviour(companionBehaviour);
                    break;
                }

                RunBehaviour(normalStateBehaviour);
                break;
            case State.alert:
                if (companionMode) {
                    RunBehaviour(companionBehaviour);
                    break;
                }

                RunBehaviour(alertStateBehaviour);
                break;
            case State.attack:
                if (coverShooterMode) {
                    RunBehaviour(coverShooterBehaviour);
                    break;
                }
                
                RunBehaviour(attackStateBehaviour);
                break;
            case State.sawAlertTag:
                RunBehaviour(vision.alertTags[vision.GetAlertTagIndex(sawAlertTagName)].behaviourScript);
                break;
            case State.distracted:
                RunBehaviour(distractedStateBehaviour);
                break;
            case State.surprised:
                RunBehaviour(surprisedStateBehaviour);
                break;
            case State.goingToCover:
                RunBehaviour(goingToCoverBehaviour);
                break;
            case State.hit:
                RunBehaviour(hitStateBehaviour);
                break;
            case State.returningToAlert:
                if (coverShooterMode) {
                    RunBehaviour(coverShooterBehaviour);
                    break;
                }
                
                RunBehaviour(attackStateBehaviour);
                break;
            case State.spareState:
                spareState.StateTimer();
                break;
        }

        isChangeStateLooping = false;
    }
    
    // run behaviour current state
    void RunBehaviour(MonoBehaviour passedBehaviour)
    {
        if (passedBehaviour == null) 
        {
            return;
        }
        
        if (passedBehaviour == lastBlazeBehaviour) 
        {
            lastBlazeBehaviour.Main();
            return;
        }
        
        // useful if behaviour script changed programmatically then disable that previous one
        if (lastBlazeBehaviour != null) {
            lastBlazeBehaviour.Close();
        }

        lastBlazeBehaviour = null;
        BlazeBehaviour[] blazeBehaviours = passedBehaviour.GetComponents<BlazeBehaviour>();

        foreach (var item in blazeBehaviours) 
        {
            if (passedBehaviour != item) {
                continue;
            }
            
            lastBlazeBehaviour = item;
            break;
        }
        
        if (lastBlazeBehaviour != null) {
            lastBlazeBehaviour.Open();
        }
    }

    // close last behaviour
    public void CloseLastBehaviour()
    {
        if (lastBlazeBehaviour == null) 
        {
            return;
        }

        lastBlazeBehaviour.Close();
        lastBlazeBehaviour = null;
    }

    void RemoveMoveToLocation()
    {
        if (state != State.alert && state != State.normal) {
            IgnoreMoveToLocation();
        }
    }
    
    #endregion

    #region CHARACTER

    // shows the enemy contact radius in scene view
    void ShowEnemyContactRadius()
    {
        if (!showEnemyContactRadius || !checkEnemyContact) {
            return;
        }

        Gizmos.color = Color.grey;
        Gizmos.DrawWireSphere(transform.position + vision.visionPosition, enemyContactRadius);
    }

    void ValidateFallBackPoints()
    {
        if (fallBackPoints == null) return;
        
        for (int i=0; i<fallBackPoints.Length; i++) {
            if (fallBackPoints[i] != Vector3.zero) {
                return;
            }

            fallBackPoints[i] = transform.position;
        }
    }

    #if UNITY_EDITOR
    void DrawFallBackPoints()
    {
        if (!ignoreUnreachableEnemy) return;
        if (!showPoints) return;
        
        if (groundLayers.value == 0) {
            Debug.LogWarning("Ground layers property not set. Make sure to set the ground layers in the main Blaze inspector (general tab) in order to see the fallback points visually.");
        }

        for (int i=0; i<fallBackPoints.Length; i++)
        {
            RaycastHit hit;
            Vector3 point = fallBackPoints[i];

            if (Physics.Raycast(point, -Vector3.up, out hit, Mathf.Infinity, groundLayers)) {
                Debug.DrawRay(transform.position, point - transform.position, new Color(1f, 0.3f, 0f), 0.1f);
                Debug.DrawRay(point, hit.point - point, new Color(1f, 0.3f, 0f), 0.1f);

                UnityEditor.Handles.color = new Color(0.3f, 1f, 0f);
                UnityEditor.Handles.DrawWireDisc(hit.point, transform.up, 0.5f);
                UnityEditor.Handles.Label(hit.point + new Vector3(0, 1, 0), "Fallback Point");
            }
        }
    }
    #endif

    public void ResetVisionPosition()
    {
        vision.visionPosition = defaultVisionPos;
    }
    
    #endregion

    #region DISTRACTED
    
    // distract the AI
    public virtual void Distract(Vector3 location, bool playAudio = true)
    {
        if (!canDistract || state == State.attack || !enabled || state == State.hit || state == State.death || companionMode) 
        {
            return;
        }
        
        // get nearest navmesh position
        Vector3 pos = GetSamplePosition(ValidateYPoint(location), navmeshAgent.height * 2);
        if (pos == Vector3.zero || pos == endDestination) {
            return;
        }

        if (turnAlertOnDistract) {
            SetState(State.alert);
        }

        if (state != State.distracted) 
        {
            if (state == State.returningToAlert) previousState = State.alert;
            else previousState = state;
        }
        else {
            endDestination = pos;
            if (turnOnEveryDistraction) 
            {
                // if already in distracted state -> re-enable behaviour to reset
                distractedStateBehaviour.enabled = false;
                distractedStateBehaviour.enabled = true;
            }
        }

        // sometimes this parameter is passed as false to avoid all distracted agents playing audio
        // which will sound distorted -> so only one agent in a group plays the audio
        if (playAudio) 
        {
            if (playDistractedAudios) 
            {
                // play audio only if not already in distracted state
                if (state != State.distracted) {
                    if (!IsAudioScriptableEmpty()) {
                        PlayAudio(audioScriptable.GetAudio(AudioScriptable.AudioType.Distracted));
                    }
                }
            }
        }

        endDestination = pos;

        if (vision.overrideVMValueOnDistract)
        {
            if (visionMeter < vision.vmValueOnDistract)
            {
                visionMeter = vision.vmValueOnDistract;
            }
        }

        // change the state to distracted
        SetState(State.distracted);
    }
    
    #endregion
    
    #region ATTACK STATE
    
    // trigger the surprised state
    public virtual void Surprised()
    {
        if (state == State.hit) return;

        if (!useSurprisedState) {
            TurnToAttackState();
            return;
        }

        if (enemyToAttack) {
            enemyPosOnSurprised = enemyToAttack.transform.position;
        }

        SetState(State.surprised);
    }

    // turn to attack state
    public virtual void TurnToAttackState()
    {
        if (state == State.attack || state == State.goingToCover || state == State.surprised || state == State.hit || state == State.death) {
            return;
        }

        SetState(State.attack);
    }

    // set an enemy and turn to attack state
    public void SetEnemy(GameObject enemy, bool turnStateToAttack = true, bool randomizePoint = false) 
    {
        // if dead or disabled -> ignore
        if (state == State.death || !enabled) {
            return;
        }

        // if already attacking or going to cover -> ignore
        if (state == State.attack || state == State.goingToCover)
        {
            return;
        }

        // force the friendly mode off if enemy is passed
        if (enemy != null) 
        {
            friendly = false;
        }

        if (enemyToAttack && enemy) 
        {
            if (!enemyToAttack.transform.IsChildOf(enemy.transform)) {
                return;
            }
        }
        
        // the randomized point is the point told to other AIs when calling them
        // so they don't climb on each another on arrival
        if (randomizePoint) {
            checkEnemyPosition = RandomSpherePoint(enemy.transform.position);
        }
        else {
            checkEnemyPosition = enemy.transform.position;
        }

        // check and set path of enemy
        if (!IsPathReachable(checkEnemyPosition)) 
        {
            Vector3 point;

            if (ClosestNavMeshPoint(enemy.transform.position, navmeshAgent.height * 2, out point)) {
                checkEnemyPosition = point;
            }
            else {
                ChangeState("alert");
                return;
            }   
        }
        
        enemyColPoint = enemy.transform.position;

        if (turnStateToAttack) {
            SetState(State.attack);
        }
    }

    // returns whether the AI is a companion to the passed gameobject
    bool IsCompanion(GameObject enemy)
    {
        if (enemy == null) {
            return false;
        }

        // if companion mode is on -> eliminate the companion from targeting 
        if (companionMode && companionTo != null && enemy.transform.IsChildOf(companionTo)) {
            return true;
        }

        return false;
    }

    public Vector3 RandomizePosition(Vector3 targetLocation, float range)
    {
        Vector3 result = targetLocation + transform.forward;
        float chosenRange = Random.Range(0.1f, range);
        
        // randomize add or subtract to X axis
        int choose = 0;
        choose = Random.Range(0, 2);
        
        if (choose == 0) result.x += chosenRange;
        else result.x -= chosenRange;

        // randomize add or subtract to Z axis
        choose = Random.Range(0, 2);
        chosenRange = Random.Range(0.1f, range);

        if (choose == 0) result.z += chosenRange;
        else result.z -= chosenRange;
        
        return ValidateYPoint(result);
    }
    
    #endregion

    #region HIT

    // KnockOut() and Hit() are similar so this gets called by the APIs to specify which one it is and trigger the similar flags
    void TurnToHitState(string typeOfHit = "hit", GameObject enemy = null, bool callOthers = false)
    {
        if (IsCompanion(enemy)) {
            enemy = null;
            Debug.Log("Hit() called on companion. It has been negated. This is just a warning.");
        }

        // read by the hit state behaviour
        hitProps.hitEnemy = enemy;
        hitProps.callOthers = callOthers;

        if (typeOfHit == "hit") {
            hitProps.hitRegister = true;
        }

        if (typeOfHit == "knockout") {
            hitProps.hitRegister = false;
            hitProps.knockOutRegister += 1;
        }

        // if AI has took cover and got hit -> flag this occurance to have the AI change cover
        if (state == State.goingToCover && tookCover) {
            hitProps.hitWhileInCover = true;
        }

        // if knock out called during jumping
        if (navmeshAgent.isOnOffMeshLink && typeOfHit == "knockout") {
            SetState(State.hit, true);
            return;
        }

        SetState(State.hit);
    }

    void CleanKnockOutRegister()
    {
        if (state != State.hit) {
            hitProps.knockOutRegister = 0;
        }
    }

    void ManageHitsCooldown()
    {
        if (!useHitCooldown) return;

        timeOfLastHit += Time.deltaTime;
        if (timeOfLastHit >= hitCooldown)
        {
            timeOfLastHit = 0;
            hitCount = 0;
        }
    }

    #endregion

    #region DEATH

    // destroy this AI gameobject
    void DestroyMe()
    {
        Destroy(gameObject);
    }

    // cancel the destroy call
    public void CancelDestroy()
    {
        CancelInvoke("DestroyMe");
    }

    void DeathReset()
    {
        CloseLastBehaviour();
        enemyToAttack = null;
        navmeshAgent.enabled = false;
        ResetEnemyManager();
        isAttacking = false;
        isAttackingAnim = false;
        ResetAllAvoidance();
    }
    
    // show the death call radius in scene view
    void ShowDeathCallRadius()
    {
        if (!showDeathCallRadius) {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + vision.visionPosition, deathCallRadius);
    }

    // call other AIs to a location on death
    void CallOthersOnDeath(bool callOthers, GameObject enemy)
    {
        // only run if set to do so
        if (callOthers) 
        {
            Collider[] agentsColl = new Collider[5];
            int agentsCollNum = Physics.OverlapSphereNonAlloc(transform.position, deathCallRadius, agentsColl, agentLayersToDeathCall);
        
            for (int i=0; i<agentsCollNum; i++) 
            {
                BlazeAI script = agentsColl[i].GetComponent<BlazeAI>();

                // if caught collider is that of the same AI -> skip
                if (agentsColl[i].transform.IsChildOf(transform)) {
                    continue;
                }
                

                // if script doesn't exist -> skip
                if (script == null) {
                    continue;
                }


                // ignore this operation if AI is busy in action (attack state, going to cover or already has an enemy target)
                if (script.state == BlazeAI.State.attack || script.state == BlazeAI.State.goingToCover || script.enemyToAttack != null) {
                    continue;
                }


                // reaching this point means item is valid
                if (enemy) {
                    script.SetEnemy(enemy, true, true);
                    continue;
                }
                
                script.SetEnemy(gameObject, true, true);
            }
        }
    }

    // play death animation or enable ragdoll (depending on settings)
    void RagdollOrDeathAnim()
    {
        // use death animation
        if (!useRagdoll) 
        {
            if (!gameObject.activeSelf) return;
            if (hitProps.knockOutRegister > 0) return;
            
            animManager.Play(deathAnim, deathAnimT);
            return;
        }

        // get the colliders if list is 0
        if (useRagdoll && ragdollColls.Count <= 0) {
            CollectRagdollColliders();
        }

        EnableRagdoll();

        if (useNaturalVelocity) return;
        if (hipBone == null) {
            PrintWarning(warnAnomaly, "Hip Bone property in death tab hasn't been set. No force can be applied to the ragdoll unless you set this.");
            return;
        }
        
        Rigidbody rb = hipBone.GetComponent<Rigidbody>();
        if (rb == null) 
        {
            PrintWarning(warnAnomaly, "The set hip bone doesn't have a Rigidbody component. No force will be applied.");
            return;
        }

        Vector3 dir = transform.TransformDirection(deathRagdollForce);
        rb.AddForce(dir, ForceMode.Impulse);
    }

    void DeathDollCall()
    {
        Death(deathDollCallProps.callOthers, deathDollCallProps.enemy, true);
    }

    void CancelDeathDoll()
    {
        CancelInvoke("DeathDollCall");
    }

    bool CheckDeathOnOffMesh()
    {
        if (navmeshAgent.isOnOffMeshLink || !isOffMeshJumpFinished) return false;
        if (!offMeshDeathProps.isCalled) return false;

        if (state == State.death) {
            offMeshDeathProps.isCalled = false;
            return false;
        }
            
        Death(offMeshDeathProps.callOthers, offMeshDeathProps.enemy, offMeshDeathProps.comingFromDeathDoll);
        return true;
    }

    #endregion
    
    #region RAGDOLL

    // cache the ragdoll collider parts
    public virtual void CollectRagdollColliders()
    {
        defaultAvatar = anim.avatar;
        Collider[] coll = GetComponentsInChildren<Collider>();

        foreach (Collider c in coll) 
        {
            if (c.gameObject.transform == this.gameObject.transform) 
            {
                continue;
            }

            if (!c.attachedRigidbody) 
            {
                continue;
            }

            c.attachedRigidbody.isKinematic = true;

            if (ragdollColls.Contains(c)) 
            {
                continue;
            }
            
            ragdollColls.Add(c);
            
            BlazeAIRagdollData script = c.gameObject.AddComponent(typeof(BlazeAIRagdollData)) as BlazeAIRagdollData;
            script.originalPos = c.transform.localPosition;
            script.originalRot = c.transform.localRotation;
        }
    }

    // disable ragdoll colliders
    public virtual void DisableRagdoll(bool reset = false)
    {
        foreach (Collider c in ragdollColls) 
        {
            if (c.gameObject.transform == this.gameObject.transform) {
                continue;
            }
            
            if (c.attachedRigidbody != null) 
            {
                c.attachedRigidbody.isKinematic = false;
                c.attachedRigidbody.linearVelocity = Vector3.zero;
                c.attachedRigidbody.isKinematic = true;
            }

            if (reset)
            {
                BlazeAIRagdollData script = c.transform.GetComponent<BlazeAIRagdollData>();
                c.transform.localPosition = script.originalPos;
                c.transform.localRotation = script.originalRot;
            }
        }

        capsuleCollider.enabled = true;
        anim.enabled = true;
    }

    // enable the ragdoll
    public virtual void EnableRagdoll()
    {
        capsuleCollider.enabled = false;
        anim.enabled = false;
        
        foreach (Collider c in ragdollColls) 
        {
            c.isTrigger = false;
            if (c.attachedRigidbody == null) continue;
            c.attachedRigidbody.isKinematic = false;
        }
    }

    public List<Collider> GetRagdollColliders()
    {
        return ragdollColls;
    }

    public Transform[] GetRagdollTransforms()
    {
        CollectRagdollColliders();
        List<Transform> list = new List<Transform>();

        foreach (Collider c in ragdollColls) {
            list.Add(c.transform);
        }

        return list.ToArray();
    }

    #endregion

    #region DISTANCE CULLING
    
    // distance culling works only on normal and alert states -> if the AI is in any other state the culling is removed temporarily
    // so the AI can perform the state action -> until the AI goes back to either normal or alert
    void CheckDistanceCullingWithState()
    {
        if (!distanceCull) return;

        if (state != State.normal && state != State.alert) 
        {
            RemoveDistanceCulling();
            return;
        }

        AddDistanceCulling();
    }
    
    #endregion

    #region INSPECTOR

    #if UNITY_EDITOR
    // log in the console if a behaviour is missing a script
    void CheckEmptyBehaviours()
    {
        if (useNormalStateOnAwake) {
            if (normalStateBehaviour == null) {
                PrintWarning(warnEmptyBehavioursOnStart, $"Normal State Behaviour is empty in game object: {gameObject.name}.");
            }
        }


        if (useAlertStateOnAwake) {
            if (alertStateBehaviour == null) {
                PrintWarning(warnEmptyBehavioursOnStart, $"Alert State Behaviour is empty in game object: {gameObject.name}.");
            }
        }


        if (canDistract) {
            if (distractedStateBehaviour == null) {
                PrintWarning(warnEmptyBehavioursOnStart, $"Distracted State Behaviour is empty in game object: {gameObject.name}.");
            }
        }


        if (useSurprisedState) {
            if (surprisedStateBehaviour == null) {
                PrintWarning(warnEmptyBehavioursOnStart, $"Surprised State Behaviour is empty in game object: {gameObject.name}.");
            }
        }


        if (!coverShooterMode) {
            if (attackStateBehaviour == null) {
                PrintWarning(warnEmptyBehavioursOnStart, $"Attack State Behaviour is empty in game object: {gameObject.name}.");
            }
        }
        else {
            if (coverShooterBehaviour == null) {
                PrintWarning(warnEmptyBehavioursOnStart, $"Cover Shooter Behaviour is empty in game object: {gameObject.name}.");
            }

            if (goingToCoverBehaviour == null) {
                PrintWarning(warnEmptyBehavioursOnStart, $"Going To Cover Behaviour is empty in game object: {gameObject.name}.");
            }
        }


        if (hitStateBehaviour == null) {
            PrintWarning(warnEmptyBehavioursOnStart, $"Hit State Behaviour is empty in game object: {gameObject.name}.");
        }
    }

    // set the default normal, alert, attack and cover shooter behaviours
    public void SetPrimeBehaviours()
    {
        // setting normal behaviour
        NormalStateBehaviour normalBehaviour = GetComponent<NormalStateBehaviour>();

        if (normalBehaviour == null) {
            normalBehaviour = gameObject.AddComponent(typeof(NormalStateBehaviour)) as NormalStateBehaviour;
        }

        if (normalBehaviour != null) normalStateBehaviour = normalBehaviour;


        // setting alert behaviour
        AlertStateBehaviour alertBehaviour = GetComponent<AlertStateBehaviour>();

        if (alertBehaviour == null) {
            alertBehaviour = gameObject.AddComponent(typeof(AlertStateBehaviour)) as AlertStateBehaviour;
        }

        if (alertBehaviour != null) alertStateBehaviour = alertBehaviour;


        // setting attack behaviour
        AttackStateBehaviour attackBehaviour = GetComponent<AttackStateBehaviour>();

        if (attackBehaviour == null) {
            attackBehaviour = gameObject.AddComponent(typeof(AttackStateBehaviour)) as AttackStateBehaviour;
        }

        if (attackBehaviour != null) attackStateBehaviour = attackBehaviour;

        
        if (coverShooterMode)
        {
            // setting cover shooter behaviour
            CoverShooterBehaviour shooterBehaviour = GetComponent<CoverShooterBehaviour>();

            if (shooterBehaviour == null) {
                shooterBehaviour = gameObject.AddComponent(typeof(CoverShooterBehaviour)) as CoverShooterBehaviour;
            }

            if (shooterBehaviour != null) coverShooterBehaviour = shooterBehaviour;


            // setting going to cover behaviour
            GoingToCoverBehaviour goingBehaviour = GetComponent<GoingToCoverBehaviour>();

            if (goingBehaviour == null) {
                goingBehaviour = gameObject.AddComponent(typeof(GoingToCoverBehaviour)) as GoingToCoverBehaviour;
            }

            if (goingBehaviour != null) goingToCoverBehaviour = goingBehaviour;
        }

        SetSurprisedBehaviour();
        CloseLastBehaviour();
    }

    // set the default surprised behaviour
    void SetSurprisedBehaviour()
    {
        if (!useSurprisedState)
        {
            return;
        }

        SurprisedStateBehaviour surprisedBehaviour = GetComponent<SurprisedStateBehaviour>();

        if (surprisedBehaviour == null) {
            surprisedBehaviour = gameObject.AddComponent(typeof(SurprisedStateBehaviour)) as SurprisedStateBehaviour;
        }

        if (surprisedBehaviour != null) surprisedStateBehaviour = surprisedBehaviour;
    } 

    // set the default distracted behaviour
    public void SetDistractedBehaviour()
    {
        DistractedStateBehaviour distractedBehaviour = GetComponent<DistractedStateBehaviour>();

        if (distractedBehaviour == null) {
            distractedBehaviour = gameObject.AddComponent(typeof(DistractedStateBehaviour)) as DistractedStateBehaviour;
        }

        if (distractedBehaviour != null) distractedStateBehaviour = distractedBehaviour;

        CloseLastBehaviour();
    }

    // set the default hit behaviour
    public void SetHitBehaviour()
    {
        HitStateBehaviour hitBehaviour = GetComponent<HitStateBehaviour>();

        if (hitBehaviour == null) {
            hitBehaviour = gameObject.AddComponent(typeof(HitStateBehaviour)) as HitStateBehaviour;
        }

        if (hitBehaviour != null) hitStateBehaviour = hitBehaviour;

        CloseLastBehaviour();
    }

    public void SetCompanionBehaviour()
    {
        CompanionBehaviour cb = GetComponent<CompanionBehaviour>();

        if (cb == null) {
            cb = gameObject.AddComponent(typeof(CompanionBehaviour)) as CompanionBehaviour;
        }

        if (cb != null) companionBehaviour = cb;


        CloseLastBehaviour();
        companionMode = true;
        waypoints.useMovementTurning = false;
    }

    void ShowAvoidanceRadiusSphere()
    {
        if (!showAvoidanceRadius) {
            return;
        }

        Gizmos.color = new Color(1, 1, 0.3f);
        Gizmos.DrawWireSphere(transform.position, avoidanceRadius);
    }
    #endif

    public void PrintWarning(bool logState, string warningMsg)
    {
        #if UNITY_EDITOR
        
        if (!logState) return;
        Debug.LogWarning(warningMsg);

        #endif
    }
    
    #endregion

    #region AUDIOS

    // play a passed audio
    public virtual bool PlayAudio(AudioClip audio) 
    {
        // if passed audio is null -> return
        if (audio == null) return false;

        // same audio is playing -> return
        if (audio == agentAudio.clip && agentAudio.isPlaying) return true;

        agentAudio.Stop();
        agentAudio.clip = audio;
        agentAudio.Play();

        return true;
    }

    public void PlayPatrolAudio()
    {
        if (IsAudioScriptableEmpty()) return;

        if (state == State.normal) {
            NormalStateBehaviour stateBehaviour = (NormalStateBehaviour) normalStateBehaviour;
            if (stateBehaviour.playPatrolAudio) {
                PlayAudio(audioScriptable.GetAudio(AudioScriptable.AudioType.NormalState));
            }
        }

        if (state == State.alert) {
            AlertStateBehaviour stateBehaviour = (AlertStateBehaviour) alertStateBehaviour;
            if (stateBehaviour.playPatrolAudio) {
                PlayAudio(audioScriptable.GetAudio(AudioScriptable.AudioType.AlertState));
            }
        }
    }

    void SetAgentAudio()
    {
        SetAudioManager();

        if (agentAudio) 
        {
            return;
        }

        agentAudio = GetComponent<AudioSource>();

        if (agentAudio == null) {
            agentAudio = gameObject.AddComponent(typeof(AudioSource)) as AudioSource;
        }

        agentAudio.playOnAwake = false;
    }

    void SetAudioManager()
    {
        if (BlazeAIAudioManager.instance == null || audioManager != null)
        {
            return;
        }

        audioManager = BlazeAIAudioManager.instance;
        audioManager.AddToManager(this);
    }

    void RemoveFromAudioManager()
    {
        if (BlazeAIAudioManager.instance == null)
        {
            return;
        }

        BlazeAIAudioManager.instance.RemoveFromManager(this);
    }
    
    public bool IsAudioScriptableEmpty()
    {
        if (audioScriptable == null) 
        {
            if (warnEmptyAudio) 
            {
                Debug.LogWarning("A behaviour checked for an audio scriptable to play an audio but the property was empty.");
            }

            return true;
        }

        return false;
    }

    public void StopAudio()
    {
        agentAudio.Stop();
    }

    #endregion
    
    #region NAV MESH

    // get random point from navmesh
    public virtual Vector3 RandomNavMeshLocation(float minLevel, float maxLevel) 
    {
        if (navmeshAgent == null) {
            return transform.position;
        }

        Vector3 randomDirection = Random.insideUnitSphere * waypoints.randomizeRadius;
        randomDirection += startPosition;
        
        NavMeshHit hit;
        Vector3 point = Vector3.zero;

        float radius = navmeshAgent.radius * 2;
        float distance;
        bool isChosen = false;

        for (int i=0; i<5; i++) 
        {
            NavMesh.SamplePosition(randomDirection, out hit, waypoints.randomizeRadius, 1);
            point = hit.position;

            distance = (new Vector3(point.x, transform.position.y, point.y) - transform.position).sqrMagnitude;
            if (distance < (radius * radius)) {
                continue;
            }

            if (!IsPathReachable(point)) {
                continue;
            }
            
            float heightProjection = Mathf.Abs(point.y) - Mathf.Abs(ValidateYPoint(startPosition).y);
            if (heightProjection != 0 && (heightProjection < minLevel || heightProjection > maxLevel)) {
                continue;
            }

            isChosen = true;
            break;
        }
        
        if (!isChosen) 
        {
            endDestination = transform.position;
            return endDestination;
        }

        endDestination = point;
        return point;
    }

    // check whether point is on navmesh or not
    public virtual bool IsPointOnNavMesh(Vector3 point, float radius = 2f)
    {
        NavMeshHit hit;

        if (NavMesh.SamplePosition(point, out hit, radius, NavMesh.AllAreas)) return true;
        else return false;
    }

    // get nearest position within point
    public virtual Vector3 GetSamplePosition(Vector3 point, float range)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(point, out hit, range, NavMesh.AllAreas)) {
            return hit.position;
        }
        
        return Vector3.zero;
    }

    // get the correct y position of an enemy
    public virtual Vector3 ValidateYPoint(Vector3 pos)
    {
        if (!IsPointOnNavMesh(pos, 0.3f)) 
        {
            RaycastHit downHit;
            
            if (Physics.Raycast(pos, -Vector3.up, out downHit, Mathf.Infinity, groundLayers)) 
            {
                return downHit.point;
            }
        }

        return pos;
    }

    // is path status complete
    public virtual bool IsPathReachable(Vector3 position, bool addAsLastCalcPath=false) 
    {
        if (path == null)
        {
            path = new NavMeshPath();
        }

        // prevent calculating infinity
        if (position.x == Mathf.Infinity || position.z == Mathf.Infinity || position.y == Mathf.Infinity) 
        {
            return false;
        }

        // set layer of path calculation
        int pathBit = NavMesh.AllAreas;
        if (!useOffMeshLinks) 
        {
            pathBit = 1;
        }
        
        NavMesh.CalculatePath(ValidateYPoint(transform.position), ValidateYPoint(position), pathBit, path);
        
        // used in movement
        if (addAsLastCalcPath) 
        {
            lastCalculatedPath = position;
        }

        // check calculation status
        if (path.status == NavMeshPathStatus.PathComplete) {
            isPathReachable = true;
        }
        else {
            isPathReachable = false;
        }
        
        // return path status
        return isPathReachable;
    }

    // get closest navmesh point to center by doing a loop
    public virtual bool ClosestNavMeshPoint(Vector3 center, float range, out Vector3 result)
    {
        for (int i = 0; i < range; i++) 
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(center, out hit, range, NavMesh.AllAreas)) 
            {
                if (IsPathReachable(hit.position)) {
                    result = hit.position;
                    return true;
                }
            }
        }

        result = Vector3.zero;
        return false;
    }

    // get a randomized point within a sphere location
    public virtual Vector3 RandomSpherePoint(Vector3 point, float range = -1, bool divideRange=true)
    {
        if (range <= -1) {
            range = navmeshAgent.height * 2;
        }
        
        Vector3 random = point + Random.insideUnitSphere.normalized * range;

        if (divideRange) {
            return GetSamplePosition(random, range/2);
        }
        
        return GetSamplePosition(random, range);
    }

    // calculate the distance to destination using the path corners
    public virtual float CalculateCornersDistanceFrom(Vector3 beginLocation, Vector3 destination)
    {
        NavMeshPath calculatedPath = new NavMeshPath();
        NavMesh.CalculatePath(ValidateYPoint(beginLocation), ValidateYPoint(destination), NavMesh.AllAreas, calculatedPath);
    
        float distance = 0f;
        int max = calculatedPath.corners.Length;

        if (max == 1) {
            distance = Vector3.Distance(beginLocation, calculatedPath.corners[0]);
            return distance;
        }

        for (int i=0; i<max; i++) {
            if (i < max-1) {
                distance += Vector3.Distance(calculatedPath.corners[i], calculatedPath.corners[i+1]);
            }
        }

        return distance;
    }

    public virtual bool FindClosestEdge(Vector3 point, out Vector3 hitPoint)
    {
        NavMeshHit edgePoint;

        if (NavMesh.FindClosestEdge(point, out edgePoint, NavMesh.AllAreas)) 
        {
            hitPoint = edgePoint.position;
            return true;
        }
        
        hitPoint = Vector3.zero;
        return false;
    }

    #endregion
    
    #region OFF MESH LINK

    public virtual void OffMeshLinkJump(float speed)
    {
        if (!useOffMeshLinks) {
            return;
        }

        // true by default
        if (isOffMeshJumpFinished) 
        {
            CloseLastBehaviour();
            offMeshDoneTimePassed = 0;

            if (SearchForLadder()) 
            {
                ClimbLadderBehaviour();
                return;
            }

            PlayOffMeshAnim();
        }

        // jumping method
        if (jumpMethod == OffMeshLinkJumpMethod.NormalSpeed) {
            if (isOffMeshJumpFinished) 
            {
                float speedToUse;

                if (useMovementSpeedForJump) speedToUse = speed;
                else speedToUse = jumpSpeed;

                StartCoroutine(NormalSpeed(navmeshAgent, speedToUse));
                return;
            }
        }
        else if (jumpMethod == OffMeshLinkJumpMethod.Parabola) {
            if (isOffMeshJumpFinished) {
                StartCoroutine(Parabola(navmeshAgent, jumpHeight, jumpDuration));
                return;
            }
        }
        else 
        {
            onTeleportStart.Invoke();
            navmeshAgent.CompleteOffMeshLink();
            onTeleportEnd.Invoke();
        }
    }

    public bool IfShouldIgnoreOffMesh()
    {
        if (useOffMeshLinks) return false;
        if (!IsOnOffMeshLink()) return false;
        return true;
    }

    IEnumerator NormalSpeed(NavMeshAgent agent, float speed, bool climbing = false)
    {
        isOffMeshJumpFinished = false;

        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
        float headRoom = (agent.height / 2) / 2 - climbToTopHeadRoom;

        if (climbing) {
            endPos = new Vector3(transform.position.x, endPos.y + headRoom, transform.position.z);
        }

        while (agent.transform.position != endPos)
        {
            if (ConditionsToBreakOffMeshLink()) {
                break;
            }

            if (climbing) RotateTo(chosenLadder.position, 20);
            else RotateTo(endPos, 20);
            
            agent.transform.position = Vector3.MoveTowards(agent.transform.position, endPos, speed * Time.deltaTime);
            yield return null;
        }

        // for climbing to top
        if (climbing) 
        {
            float normalizedTime = 0.0f;
            Vector3 startPos = agent.transform.position;
            endPos = data.endPos + Vector3.up * agent.baseOffset;
            

            while (normalizedTime < 1.0f)
            {
                if (ConditionsToBreakOffMeshLink()) {
                    break;
                }

                animManager.Play(climbToTopAnim, climbAnimT);

                agent.transform.position = Vector3.Lerp(startPos, endPos, normalizedTime);
                normalizedTime += Time.deltaTime / climbToTopDuration;

                yield return null;
            }
        }

        JumpEnd(endPos, agent);
    }
    
    IEnumerator Parabola(NavMeshAgent agent, float height, float duration)
    {
        isOffMeshJumpFinished = false;

        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 startPos = agent.transform.position;
        Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
        float normalizedTime = 0.0f;
        
        while (normalizedTime < 1.0f)
        {
            if (ConditionsToBreakOffMeshLink()) {
                break;
            }

            RotateTo(endPos, 20);   

            float yOffset = height * 4.0f * (normalizedTime - normalizedTime * normalizedTime);
            agent.transform.position = Vector3.Lerp(startPos, endPos, normalizedTime) + yOffset * Vector3.up;
            normalizedTime += Time.deltaTime / duration;
            
            yield return null;
        }
        
        JumpEnd(endPos, agent);
    }

    void JumpEnd(Vector3 endPos, NavMeshAgent agent)
    {
        if (ConditionsToBreakOffMeshLink()) 
        {
            isOffMeshJumpFinished = true;
            return;
        }

        RotateTo(endPos, 20);
        if (agent.enabled) agent.CompleteOffMeshLink();
        isOffMeshJumpFinished = true;
    }

    void TurningTimerAfterOffMesh()
    {
        if (navmeshAgent.isOnOffMeshLink) return;
        if (!isOffMeshJumpFinished) return;

        if (offMeshDoneTimePassed < timeToAllowTurning) {
            offMeshDoneTimePassed += Time.deltaTime;
        }        
    }

    bool ConditionsToBreakOffMeshLink()
    {
        if (state == State.death && !deathDollCallProps.isCalled && useRagdoll) {
            return true;
        }

        if (state == State.hit && !hitProps.hitRegister) {
            return true;
        }

        return false;
    }

    void PlayOffMeshAnim()
    {
        OffMeshLinkData data = navmeshAgent.currentOffMeshLinkData;
        Vector3 endPos = data.endPos + Vector3.up * navmeshAgent.baseOffset;

        if (endPos.y >= transform.position.y) {
            animManager.Play(jumpAnim, jumpAnimT);
            return;
        }

        animManager.Play(fallAnim, jumpAnimT);
    }

    bool SearchForLadder()
    {
        if (!climbLadders) return false;
        if (navmeshAgent.currentOffMeshLinkData.endPos.y <= transform.position.y) return false;

        System.Array.Clear(searchLadderHitArr, 0, 7);
        int numColliders = Physics.OverlapSphereNonAlloc(transform.position + vision.visionPosition, navmeshAgent.radius + 0.3f, searchLadderHitArr, ladderLayers);

        for (int i=0; i<numColliders; i++) 
        {
            if (searchLadderHitArr[i].transform.IsChildOf(transform) || transform.IsChildOf(searchLadderHitArr[i].transform)) {
                continue;
            }

            chosenLadder = searchLadderHitArr[i].transform;
            return true;
        }

        return false;
    }

    void ClimbLadderBehaviour()
    {
        if (!isOffMeshJumpFinished) return;
        
        animManager.Play(climbUpAnim, climbAnimT);

        StartCoroutine(NormalSpeed(navmeshAgent, climbUpSpeed, true));
        return;
    }

    #endregion

    #region PUBLIC METHODS (APIs)
    
    // force the AI to move to a specified location
    public virtual void MoveToLocation(Vector3 location, bool randomize=false)
    {
        if (state != State.normal && state != State.alert) 
        {
            PrintWarning(warnAnomaly, "MoveToLocation() only works when the AI is in normal and alert states.");
            return;
        }

        stayAlertUntilPos = true;

        // if randomize is set, get a random point within location sphere
        // to avoid sending all the AIs to the exact same point (this is good with groups)
        if (randomize) {
            endDestination = RandomSpherePoint(location);
        }
        else 
        {
            endDestination = ValidateYPoint(location);
            
            if (waypointIndex >= 0 && !movedToLocation) {
                waypointIndex--;
            }
        }
        
        // end destination and this are both read by the normal and alert behaviours
        movedToLocation = true;
        stayIdle = false;
    }

    // ignore the forcing of movement to a certain location
    public virtual void IgnoreMoveToLocation()
    {
        if (!movedToLocation) return;
        
        ignoreMoveToLocation = true;
        movedToLocation = false;
        stayAlertUntilPos = false;
    }

    // force the AI to go idle
    public virtual void StayIdle()
    {
        if (state != State.normal && state != State.alert && state != State.distracted) {
            Debug.Log("StayIdle() only works when the AI is in normal, alert and distracted states.");
            return;
        }

        // this public property will be read by the behaviours
        stayIdle = true;
    }

    // ignore stay to idle and return to patroling
    public virtual void IgnoreStayIdle()
    {
        if (state == State.normal) {
            GetComponent<NormalStateBehaviour>().ForceMove();
        }
        else {
            GetComponent<AlertStateBehaviour>().ForceMove();
        }
        
        stayIdle = false;
    }

    // check whether the AI is idle or not
    public virtual bool IsIdle()
    {
        if (state != State.normal && state != State.alert && state != State.distracted) {
            PrintWarning(warnAnomaly, "IsIdle() only works when the AI is in normal, alert and distracted states. Will return false.");
            return false;
        }

        // set by the behaviours
        return isIdle;
    }

    // force to attack target
    public virtual void Attack()
    {
        if (state == State.death || !enabled) {
            PrintWarning(warnAnomaly, "Attack() can't be called when the AI is in death state or Blaze AI disabled.");
            return;
        }   

        if (enemyToAttack == null) {
            PrintWarning(warnAnomaly, "Attack() can't be called when the AI doesn't have a target.");
            return;
        }

        if (state == State.spareState) {
            return;
        }

        isAttacking = true;
        
        if (state != State.hit) {
            SetState(State.attack);
        }
    }

    // cancel current attack
    public virtual void StopAttack()
    {
        isAttacking = false;
        canChangeVisionTarget = true;
    }
    
    // Set AI to normal or alert states only -> used for AI revive from death too
    public virtual void ChangeState(string stateStr)
    {
        enabled = true;
        navmeshAgent.enabled = true;
        capsuleCollider.enabled = true;
        isOffMeshJumpFinished = true;
        offMeshDoneTimePassed = timeToAllowTurning;
        offMeshDeathProps.isCalled = false;
        deathDollCallProps.isCalled = false;

        if (state == State.death && hipBone != null && useRagdoll) {
            navmeshAgent.Warp(hipBone.position);
        }

        if (stateStr == "normal" && state == State.normal) return;
        if (stateStr == "alert" && state == State.alert) return;

        CancelDestroy();
        CancelDeathDoll();

        // if coming from death -> reset entire ragdoll
        if (state == State.death) {
            if (hipBone != null) {
                hipBone.position = transform.position;
            }

            DisableRagdoll(true);
        }
        else {
            DisableRagdoll();
        }
        

        if (stateStr == "normal") {
            SetState(State.normal);
        }

        if (stateStr == "alert") {
            SetState(State.alert);
        }
    }

    // set target 
    public virtual void SetTarget(GameObject enemy, bool randomizePoint = false, bool applyAttackVisionForFrame = false) 
    {
        if (!enabled || state == State.death) 
        {
            PrintWarning(warnAnomaly, "Can't call SetTarget() when AI is in death state or Blaze AI is disabled. You have to call ChangeState(string state) first to revive the AI.");
            return;
        }
        
        if (enemy == null) 
        {
            Debug.Log("There is no passed enemy.");
            return;
        }

        if (IsCompanion(enemy)) 
        {
            Debug.Log("You can't SetTarget() on the companion. Companion Mode needs to be turned off first.");
            return;
        }

        if (enemyToAttack && enemy) 
        {
            if (!enemyToAttack.transform.IsChildOf(enemy.transform)) {
                Debug.Log("Can't call SetTarget() when there's already a target chosen by the AI.");
                return;
            }
        }

        if (applyAttackVisionForFrame) 
        {
            enemyToAttack = enemy;
        }
        
        checkEnemyPosition = enemy.transform.position;
        SetEnemy(enemy, true, randomizePoint);
    }

    // hit the AI
    public virtual void Hit(GameObject enemy = null, bool callOthers = false) 
    {
        if (state == State.death || !enabled) {
            PrintWarning(warnAnomaly, "Hit() can't be called when the AI is in death state or Blaze AI is disabled.");
            return;
        }

        // hits cool down
        if (useHitCooldown) 
        {
            if (hitCount >= maxHitCount) return;
            
            timeOfLastHit = 0;
            hitCount++;
        }

        TurnToHitState("hit", enemy, callOthers);
    }

    // knock out the AI for a certain time
    public virtual void KnockOut(GameObject enemy = null, bool callOthers = false)
    {
        if (state == State.death || !enabled) {
            PrintWarning(warnAnomaly, "KnockOut() can't be called when the AI is in death state or Blaze AI is disabled.");
            return;
        }

        // collect colliders if list is empty
        if (ragdollColls.Count <= 0) {
            CollectRagdollColliders();
        }

        navmeshAgent.enabled = false;
        TurnToHitState("knockout", enemy, callOthers);
    }

    // kill the AI - either plays animation or ragdoll - last parameter is for system use (ignore it), use the first 2
    public virtual void Death(bool callOthers = false, GameObject enemy = null, bool comingFromDeathDoll = false)
    {
        // return if already dead or Blaze disabled
        if (!comingFromDeathDoll) 
        {
            if (state == State.death || !enabled) {
                PrintWarning(warnAnomaly, "Death() can't be called when the AI is in death state or Blaze AI is disabled.");
                return;
            }
        }
        
        // if killed during off mesh
        if (navmeshAgent.isOnOffMeshLink && !useRagdoll) 
        {
            offMeshDeathProps = new OffMeshDeath(true, callOthers, enemy, comingFromDeathDoll);
            return;
        }

        offMeshDeathProps.isCalled = false;
        deathDollCallProps.isCalled = false;

        // set state to death
        SetState(State.death);
        DeathReset();
        StopAttack();

        // call others -> if triggered to do so
        CallOthersOnDeath(callOthers, enemy);

        // invoke event
        deathEvent.Invoke();
        
        // turn on ragdoll or play death animation depending on the set options
        RagdollOrDeathAnim();
        CleanKnockOutRegister();

        // play audio
        if (!IsAudioScriptableEmpty() && playDeathAudio) {
            PlayAudio(audioScriptable.GetAudio(AudioScriptable.AudioType.Death));
        }

        if (capsuleCollider != null && disableCapsuleColliderOnDeath) {
            capsuleCollider.enabled = false;
        }

        // if set to destroy game object
        if (destroyOnDeath) 
        {
            if (distanceCull) {
                RemoveDistanceCulling();
            }

            Invoke("DestroyMe", timeBeforeDestroy);
            return;
        }

        enabled = false;
    }

    // kill the AI - plays death animation and then ragdolls mid way
    public virtual void DeathDoll(float timeToRagdoll, bool callOthers = false, GameObject enemy = null)
    {
        // return if already dead or Blaze disabled
        if (state == State.death || !enabled) {
            PrintWarning(warnAnomaly, "DeathDoll() can't be called when the AI is in death state or Blaze AI is disabled.");
            return;
        }

        SetState(State.death);
        DeathReset();

        animManager.Play(deathAnim, deathAnimT);

        deathDollCallProps.isCalled = true;
        deathDollCallProps.callOthers = callOthers;
        deathDollCallProps.enemy = enemy;
        
        // invoke the middle method which will call the main Death()
        Invoke("DeathDollCall", timeToRagdoll);
    }

    // add agent to the distance culling list
    public virtual void AddDistanceCulling()
    {
        if (BlazeAIDistanceCulling.instance) 
        {
            BlazeAIDistanceCulling.instance.AddAgent(this);
            return;
        }

        PrintWarning(warnAnomaly, "Can't add agent to distance culling list since no distance culling instance can be found.");
    }

    // remove agent from the distance culling list
    public virtual void RemoveDistanceCulling(bool enableObject=false)
    {
        if (BlazeAIDistanceCulling.instance) 
        {
            BlazeAIDistanceCulling.instance.RemoveAgent(this);
            
            if (enableObject) {
                gameObject.SetActive(true);
            }

            return;
        }
    }

    // check if agent is in the distance culling list
    public virtual bool CheckDistanceCulling()
    {
        if (BlazeAIDistanceCulling.instance) {
            return BlazeAIDistanceCulling.instance.CheckAgent(this);
        }

        return false;
    }

    public virtual bool IsOnOffMeshLink()
    {
        return navmeshAgent.isOnOffMeshLink;
    }

    // call a spare state
    public virtual void SetSpareState(string stateName, int animIndex = -1, int audioIndex = -1)
    {
        if (spareState == null) {
            PrintWarning(warnAnomaly, "Can't set spare state as the Blaze AI Spare State component hasn't been added to the AI. Please add the component first.");
            return;
        }

        if (state == State.death || !enabled) {
            PrintWarning(warnAnomaly, "SetSpareState() can't be called when the AI is in death state or Blaze AI is disabled.");
            return;
        }

        if (state == State.spareState) {
            PrintWarning(warnAnomaly, "Blaze is already in spare state. Current spare state must finish first before calling another one.");
            return;
        }
        
        if (state != State.spareState) {
            previousState = state;
        }

        spareState.SetState(stateName, animIndex, audioIndex);
    }
    
    // exit from the current spare state
    public virtual void ExitSpareState()
    {
        if (!enabled) {
            PrintWarning(warnAnomaly, "ExitSpareState() can't be called when Blaze AI is disabled.");
            return;
        }

        if (state != State.spareState) {
            PrintWarning(warnAnomaly, "Operation skipped: AI isn't in a spare state to exit from.");
            return;
        }

        if (spareState == null) {
            PrintWarning(warnAnomaly, "Can't set spare state as the Blaze AI Spare State component hasn't been added to the AI. Please add the component first.");
            return;
        }

        spareState.ExitState();
    }

    // returns the current active spare state
    public virtual SpareState CurrentSpareState()
    {
        if (state != State.spareState) {
            return null;
        }

        if (spareState == null) {
            PrintWarning(warnAnomaly, "Can't set spare state as the Blaze AI Spare State component hasn't been added to the AI. Please add the component first.");
            return null;
        }

        return spareState.chosenState;
    }

    public virtual void TakeCover(GameObject enemy)
    {
        if (state == State.goingToCover)
        {
            PrintWarning(warnAnomaly, "Operation skipped: AI already in going to cover state.");
            return;
        }

        if (goingToCoverBehaviour == null)
        {
            PrintWarning(warnAnomaly, "Operation skipped: Going to cover behaviour not found. Add the behaviour and link it to the states tab and try again.");
            return;
        }

        SetTarget(enemy, false, true);
        StopAttack();
        SetState(State.goingToCover);
    }
    
    #endregion
}
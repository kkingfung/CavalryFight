using UnityEngine;
using System.Collections;
using UnityEngine.Events;

namespace BlazeAISpace
{
    [AddComponentMenu("Blaze AI/Normal State Behaviour")]
    public class NormalStateBehaviour : BlazeBehaviour
    {
        #region PROPERTIES

        [Min(0), Tooltip("Won't be used if root motion is used.")]
        public float moveSpeed = 3f;
        [Min(0)]
        public float turnSpeed = 5f;

        [Tooltip("Add animations for idle. One will be chosen at random. If only one is added then only that will play.")]
        public string[] idleAnim;
        [Tooltip("Movement animation.")]
        public string moveAnim;
        [Min(0), Tooltip("Animation transition time from idle to move and vice versa.")]
        public float animT = 0.25f;
        
        [Min(0), Tooltip("Time in seconds to stay in idle before going to the next waypoint. Will generate a random number between the two values. For a fixed value make both inputs the same.")]
        public Vector2 idleTime = new Vector2(5f, 5f);

        [Tooltip("Will tell the Audio Manager component to play patrol audios in this state.")]
        public bool playPatrolAudio;

        public bool avoidFacingObstacles;
        public LayerMask obstacleLayers = Physics.AllLayers;
        public float obstacleRayDistance = 3f;
        public Vector3 obstacleRayOffset;
        public bool showObstacleRay;

        public UnityEvent onStateEnter;
        public UnityEvent onStateExit;
        
        #endregion

        #region BEHAVIOUR VARS

        public BlazeAI blaze {private set; get; }
        bool isFirstRun = true;

        Vector3 waypoint;
        Vector3 badWaypoint;

        bool isIdle;
        bool shouldStop;
        bool movedToLocation;
        bool isOffMeshByPassed = false;

        float _turnToWP = 0f;

        #endregion

        #region MAIN METHODS
        
        void Awake()
        {
            blaze = GetComponent<BlazeAI>();
        }

        public virtual void OnStart() 
        {
            isFirstRun = false;
            SetEndDestination();
        }

        public override void Open() 
        {
            if (isFirstRun) {
                OnStart();
            }

            onStateEnter.Invoke();

            if (blaze == null) {
                Debug.LogWarning($"No blaze AI component found in the gameobject: {gameObject.name}. AI behaviour will have issues.");
            }
        }

        public override void Close()
        {
            isIdle = false;
            isOffMeshByPassed = false;
            onStateExit.Invoke();
        }
        
        public override void Main()
        {
            // end destination is set by blaze.NextWayPoint or blaze.RandomNavmeshLocation in the SetEndDestination() method
            // OR if forced to move to a specific location using MoveToLocation() inside blaze.cs
            waypoint = blaze.endDestination;
            SetIdleState();
            
            // if forced to stay idle
            if (blaze.stayIdle) {
                StayIdle();
                return;
            }

            if (OffMeshToBypass()) {
                return;
            }

            // check if blaze has been called to move to a certain location
            movedToLocation = blaze.movedToLocation;
            if (movedToLocation && isIdle) {
                ForceMove();
            }

            // correct waypoint if move to location cancelled
            CorrectWaypoint();
            MoveToPoint();
    
            if (avoidFacingObstacles) {
                ObstacleRay();
            }
        }

        void OnDrawGizmosSelected()
        {
            if (showObstacleRay) {
                Debug.DrawRay(transform.position + obstacleRayOffset, transform.TransformDirection(Vector3.forward) * obstacleRayDistance, Color.yellow);
            }
        }

        void OnValidate()
        {
            if (blaze == null) {
                blaze = GetComponent<BlazeAI>();
            }
        }
        
        #endregion

        #region BEHAVIOUR
        
        // move AI to waypoint
        void MoveToPoint()
        {
            if (isIdle) return;

            // check if using randomized waypoints
            if (blaze.waypoints.randomize) {
                RandomizedWaypointsMove();
                return;
            }

            // if using normal pre-set waypoints
            PreSetWaypointsMove();
        }

        // move to the pre set waypoints
        public virtual void PreSetWaypointsMove()
        {   
            // MoveTo() moves to point and returns true when reaches destination -> false if not
            if (blaze.MoveTo(waypoint, moveSpeed, turnSpeed, moveAnim, animT)) 
            {
                // if was moving to a certain location then there's no waypoint rotation -> go idle instantly
                if (movedToLocation) 
                {
                    StartCoroutine("Idle");
                    return;
                }
                
                // CheckWayPointRotation() returns true if there is a waypoint rotation
                if (blaze.CheckWayPointRotation()) 
                {
                    _turnToWP += Time.deltaTime;

                    // play idle anim while waiting for time before turn
                    if (_turnToWP < blaze.waypoints.timeBeforeTurning) {
                        string waitAnim = "";

                        if (idleAnim.Length > 0) waitAnim = idleAnim[0];

                        blaze.animManager.Play(waitAnim, animT);
                        return;
                    }
                }

                // turns AI to waypoint rotation and returns true when done
                if (blaze.WayPointTurning()) 
                {
                    StartCoroutine("Idle");
                }
                
                return;
            }
            
            // code below runs if not reached position yet 

            if (isIdle) {
                ForceMove();
            }

            // checks if the passed location in MoveTo() is not reachable
            if (!blaze.isPathReachable) {
                blaze.NextWayPoint();
            }
        }

        // move to random point
        public virtual void RandomizedWaypointsMove()
        {
            // MoveTo() moves to point and returns true when reaches destination -> false if not
            if (blaze.MoveTo(waypoint, moveSpeed, turnSpeed, moveAnim, animT)) 
            {
                StartCoroutine("Idle");
                return;
            }
            
            // code below runs when not reached position yet

            if (isIdle) {
                ForceMove();
            }

            if (!blaze.isPathReachable) {
                SetEndDestination();
            }
        }

        // reached waypoint location so turn idle for a time
        public virtual IEnumerator Idle()
        {
            isIdle = true;
            movedToLocation = false;
            _turnToWP = 0f;

            // play the idle anim
            string animToPlay = "";

            if (idleAnim.Length > 0) {
                int animIndex = Random.Range(0, idleAnim.Length);
                animToPlay = idleAnim[animIndex];
            }
            
            blaze.animManager.Play(animToPlay, animT);
            if (blaze.stayIdle) yield break;

            // set the wait time
            float _idleTime = Random.Range(idleTime.x, idleTime.y);
            SetEndDestination();

            yield return new WaitForSeconds(_idleTime);
            isIdle = false;
        }

        public virtual void SetEndDestination()
        {
            // if forced to move to a location
            if (blaze.movedToLocation) {
                ForceMove();
                return;
            }

            // if randomize then get a random navmesh location
            if (blaze.waypoints.randomize) {
                blaze.RandomNavMeshLocation(blaze.waypoints.minAndMaxLevelDiff.x, blaze.waypoints.minAndMaxLevelDiff.y);
                return;
            }
            
            // if reached this point -> means randomize is off so sets the waypointIndex var to the next waypoint
            blaze.NextWayPoint();
        }

        // fire ray to avoid AI standing too close facing obstacles
        void ObstacleRay()
        {
            float distance = (waypoint - transform.position).sqrMagnitude;
            float minDistance = obstacleRayDistance * obstacleRayDistance;
            
            if (distance <= minDistance) {
                // AI should be facing the waypoint
                if (Vector3.Dot((waypoint - transform.position).normalized, transform.forward) < 0.8f) {
                    return;
                }

                RaycastHit hit;
                if (Physics.Raycast(transform.position + obstacleRayOffset, transform.TransformDirection(Vector3.forward), out hit, obstacleRayDistance, obstacleLayers))
                {
                    StartCoroutine("Idle");
                }
            }
        }

        // correct waypoint if move to location cancelled
        void CorrectWaypoint()
        {
            if (!blaze.ignoreMoveToLocation) return;
            
            SetEndDestination();
            blaze.ignoreMoveToLocation = false;
        }

        bool OffMeshToBypass()
        {   
            if (!blaze.IfShouldIgnoreOffMesh()) {
                return false;
            }

            if (isOffMeshByPassed) {
                return false;
            }

            isOffMeshByPassed = true;
            
            if (blaze.waypoints.waypoints.Count <= 1 && !isIdle) {
                StartCoroutine("Idle");
                return true;
            }

            if (isIdle) {
                return false;
            }
            
            blaze.NextWayPoint();
            blaze.navmeshAgent.Warp(transform.position);
            isOffMeshByPassed = false;
            
            return true;
        }
        
        #endregion

        #region USED WITH PUBLIC METHODS
        
        // force the AI behaviour to quit idle and move
        public virtual void ForceMove()
        {
            StopAllCoroutines();
            isIdle = false;
        }

        // tell the AI behaviour to stay idle
        public virtual void StayIdle()
        {
            if (isIdle) return;
            StartCoroutine("Idle");
        }

        // returns true or false whether the AI is idle
        void SetIdleState()
        {
            if (isIdle || blaze.stayIdle) {
                blaze.isIdle = true;
                return;
            }

            blaze.isIdle = false;
        }
        
        #endregion
    }
}
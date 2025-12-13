using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace BlazeAISpace
{
    [AddComponentMenu("Blaze AI/Flee Behaviour")]
    public class FleeBehaviour : BlazeBehaviour
    {   
        #region PROPERTIES
        [Tooltip("If enabled, the AI will run back and forth within it's own radius as if it's on fire. This mode doesn't require that the AI has a target. If disabled, normal flee behaviour where the AI checks where the target is and runs in the opposite direction.")]
        public bool runAroundMode = false;
        
        [Tooltip("The distance to run to away from the target OR the radius in which the AI runs around if Run Around Mode is enabled.")]
        public float distanceRun = 10;

        public float moveSpeed = 5;
        public float turnSpeed = 5;

        public string moveAnim;
        public float moveAnimT = 0.25f;

        [Tooltip("Go to a specific position.")]
        public bool goToPosition;
        [Tooltip("Set the specific position to go to.")]
        public Vector3 setPosition;
        [Tooltip("Shows the specific position point in the scene view (green circle marked as flee position)")]
        public bool showPosition;
        [Tooltip("Fire an event when the specific position is reached.")]
        public UnityEvent reachEvent;

        [Tooltip("Play an audio when fleeing. Set the audio in the audio scriptable. Fleeing array.")]
        public bool playAudio;
        [Tooltip("If enabled, an audio will always play when fleeing. If set to false, there is a 50/50 chance whether an audio will be played or not.")]
        public bool alwaysPlayAudio;

        public UnityEvent onStateEnter;
        public UnityEvent onStateExit;

        #endregion

        #region BEHAVIOUR VARS

        public BlazeAI blaze { private set; get; }
        bool isFirstRun = true;
        GameObject lastEnemy;
        Vector3 fleePosition;

        public Vector3 fleeingTo {
            get { return fleePosition; }
        }

        bool isMoving;
        float cornersDist;
        int _framesElapsed = 0;

        int savedVisionLayers;
        bool savedMovementTurningState;
        bool savedAudioManagerState;


        #endregion

        #region MAIN METHODS

        void Awake()
        {
            blaze = GetComponent<BlazeAI>();
        }

        public virtual void OnStart()
        {
            isFirstRun = false;
        }

        public override void Open()
        {
            if (isFirstRun) {
                OnStart();
            }

            onStateEnter.Invoke();
            _framesElapsed = 5;

            if (runAroundMode) {
                DisableBlazeProperties();
            }
            
            if (blaze == null) {
                Debug.LogWarning($"No Blaze AI component found in the gameobject: {gameObject.name}. AI behaviour will have issues.");
                return;
            }

            blaze.isFleeing = true;
            PlayAudio();
        }

        public override void Close()
        {
            if (blaze == null) {
                return;
            }

            // reset flags, except if hit state
            if (blaze.state != BlazeAI.State.hit) {
                lastEnemy = null;
                blaze.isFleeing = false;
            }

            onStateExit.Invoke();
            
            if (runAroundMode) {
                ReturnBlazeProperties();
            }
        }

        public override void Main()
        {
            if (runAroundMode) {
                RunAround();
                return;
            }

            if (blaze.enemyToAttack != null) {
                lastEnemy = blaze.enemyToAttack;
                Flee();
                return;
            }
            
            if (lastEnemy != null) {
                Flee();
                return;
            }

            blaze.SetState(BlazeAI.State.alert);
        }

        void OnValidate()
        {
            if (blaze == null) {
                blaze = GetComponent<BlazeAI>();
            }

            if (goToPosition && setPosition == Vector3.zero) {
                setPosition = transform.position;
            }
        }

        #if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (blaze == null) return;

            if (goToPosition && showPosition) 
            {
                if (blaze.groundLayers.value == 0) {
                    Debug.LogWarning("Ground layers property not set. Make sure to set the ground layers in the main Blaze inspector (general tab) in order to see the flee points visually.");
                }

                RaycastHit hit;
                if (Physics.Raycast(setPosition, -Vector3.up, out hit, Mathf.Infinity, blaze.groundLayers)) {
                    Debug.DrawRay(transform.position, setPosition - transform.position, new Color(1f, 0.3f, 0f), 0.1f);
                    Debug.DrawRay(setPosition, hit.point - setPosition, new Color(1f, 0.3f, 0f), 0.1f);

                    UnityEditor.Handles.color = new Color(0.3f, 1f, 0f);
                    UnityEditor.Handles.DrawWireDisc(hit.point, blaze.transform.up, 0.5f);
                    UnityEditor.Handles.Label(hit.point + new Vector3(0, 1, 0), "Flee Position");
                }
            }
        }
        #endif

        #endregion

        #region BEHAVIOUR

        public virtual void RunAround()
        {
            if (isMoving) 
            {
                Move(fleePosition);
                return;
            }
            
            for (int i=0; i<5; i++) {
                fleePosition = blaze.RandomSpherePoint(transform.position, distanceRun);
                if (!blaze.IsPathReachable(fleePosition)) {
                    continue;
                }
            }

            Move(fleePosition);
        }

        // temporarily disable vision to avoid interrupting the behaviour if taking place during normal state
        public virtual void DisableBlazeProperties()
        {
            savedVisionLayers = blaze.vision.hostileAndAlertLayers;
            blaze.vision.hostileAndAlertLayers = 0;

            savedMovementTurningState = blaze.waypoints.useMovementTurning;
            blaze.waypoints.useMovementTurning = false;

            if (BlazeAIAudioManager.instance != null) 
            {
                List<BlazeAI> list = BlazeAIAudioManager.instance.ReturnList();
                
                if (list.Contains(blaze)) {
                    savedAudioManagerState = true;
                    BlazeAIAudioManager.instance.RemoveFromManager(blaze);
                }
                else {
                    savedAudioManagerState = false;
                }
            }
        }

        public virtual void ReturnBlazeProperties()
        {
            blaze.vision.hostileAndAlertLayers = savedVisionLayers;
            blaze.waypoints.useMovementTurning = savedMovementTurningState;
            
            if (BlazeAIAudioManager.instance != null && savedAudioManagerState) {
                BlazeAIAudioManager.instance.AddToManager(blaze);
            }
        }

        public virtual void Flee()
        {
            if (goToPosition) 
            {
                if (_framesElapsed >= 5) 
                {
                    cornersDist = blaze.CalculateCornersDistanceFrom(transform.position, setPosition);
                    _framesElapsed = 0;
                }
                else {
                    _framesElapsed++;
                }
                
                float radius = blaze.navmeshAgent.radius * 2;
                if (cornersDist <= radius) 
                {
                    reachEvent.Invoke();
                    blaze.SetState(BlazeAI.State.alert);
                    return;
                }

                Move(setPosition);
                return;
            }

            
            if (isMoving) 
            {
                Move(fleePosition);
                return;
            }


            float distance = (transform.position - lastEnemy.transform.position).sqrMagnitude;
            if (distance >= distanceRun * distanceRun) 
            {
                if (blaze.enemyToAttack == null) 
                {
                    blaze.SetState(BlazeAI.State.alert);
                    return;
                }
            }

            
            Vector3 fleeDir = lastEnemy.transform.position - transform.position;
            fleePosition = transform.position - fleeDir;

            if (!blaze.IsPathReachable(fleePosition)) {
                fleePosition = blaze.RandomSpherePoint(transform.position, distanceRun);
            }

            Move(fleePosition);
        }

        public virtual void Move(Vector3 pos)
        {
            if (blaze.MoveTo(pos, moveSpeed, turnSpeed, moveAnim, moveAnimT)) {
                isMoving = false;
                return;
            }

            isMoving = true;
        }

        void PlayAudio()
        {
            if (blaze == null) return;
            if (blaze.IsAudioScriptableEmpty() || !playAudio) return;

            if (!alwaysPlayAudio) {
                int rand = Random.Range(0, 2);
                if (rand == 0) {
                    return;
                }
            }

            blaze.PlayAudio(blaze.audioScriptable.GetAudio(AudioScriptable.AudioType.Fleeing));
        }

        #endregion
    }
}
using UnityEngine;
using UnityEngine.Events;

namespace BlazeAISpace
{
    [AddComponentMenu("Blaze AI/Surprised State Behaviour")]
    public class SurprisedStateBehaviour : BlazeBehaviour
    {
        #region PROPERTIES

        [Tooltip("The surprised animation to play.")]
        public string anim;
        [Tooltip("The animation transition.")]
        public float animT = 0.25f;
        [Min(0), Tooltip("The duration to stay in this state and playing the animation.")]
        public float duration;

        [Tooltip("The speed of turning to face the target's caught position.")]
        public float turnSpeed = 10;

        [Tooltip("Set your audios in the audio scriptable in the General Tab in Blaze AI.")]
        public bool playAudio;

        public UnityEvent onStateEnter;
        public UnityEvent onStateExit;

        #endregion

        #region BEHAVIOUR VARS

        public BlazeAI blaze { get; private set; }
        bool isFirstRun = true;
        float _duration = 0f;
        bool playedAudio;
        bool turningDone;

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
            
            if (blaze == null) {
                Debug.LogWarning($"No Blaze AI component found in the gameobject: {gameObject.name}. AI behaviour will have issues.");
            }
        }

        public override void Close()
        {
            Reset();
            onStateExit.Invoke();
        }

        void OnValidate()
        {
            if (blaze == null) {
                blaze = GetComponent<BlazeAI>();
            }
        }
        
        public override void Main()
        {
            // only turn if turning hasn't finished
            if (!turningDone)
            {
                // turn to face enemy -> this function returns true when done
                if (blaze.TurnTo(blaze.enemyPosOnSurprised, "", "", blaze.waypoints.turningAnimT, turnSpeed, false)) {
                    turningDone = true;
                }

                return;
            }
            
            blaze.animManager.Play(anim, animT);
            
            if (playAudio && !playedAudio) 
            {
                if (!blaze.IsAudioScriptableEmpty()) {
                    if (blaze.PlayAudio(blaze.audioScriptable.GetAudio(AudioScriptable.AudioType.SurprisedState))) {
                        playedAudio = true;
                    }
                }
            }

            // timer to quit surprised state
            _duration += Time.deltaTime;
            
            if (_duration >= duration) 
            {
                Reset();
                blaze.SetState(BlazeAI.State.attack);
            }
        }

        void Reset()
        {
            turningDone = false;
            _duration = 0f;
            playedAudio = false;
        }

        #endregion
    }
}
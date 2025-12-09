using MalbersAnimations.Events;
using MalbersAnimations.Scriptables;
using UnityEngine;

namespace MalbersAnimations.Utilities
{
    [HelpURL("https://malbersanimations.gitbook.io/animal-controller/utilities/lock-on-target")]
    public class LockOnTarget : MonoBehaviour
    {
        [Tooltip("The Lock On Target will activate automatically if any target is stored on the list")]
        public BoolReference Auto = new(false);

        [Tooltip("The Lock On Target requires an Aim Component")]
        [RequiredField] public Aim aim;

        [Tooltip("Set of the focused 'potential' Targets")]
        [RequiredField] public RuntimeGameObjects Targets;

        [Tooltip("Time needed to change to the next or previous target")]
        public FloatReference NextTargetTime = new(0f);

        [Tooltip("If an obstacle is between the target and the character. Locking will be disabled")]
        public BoolReference CheckForObstacles = new(false);

        [Tooltip("If another target can be found then swap to that target")]
        public BoolReference AutoSwapOnObstacle = new(false);
        // public LayerReference ObstacleLayer = new(1);
        private float CurrentTime;

        private int CurrentTargetIndex = -1;
        public bool debug;

        [Header("Events")]
        public TransformEvent OnTargetChanged = new();
        //  public TransformEvent OnTargetAimAssist = new();
        public BoolEvent OnLockingTarget = new();


        public Transform LockedTarget
        {
            get => locketTarget;

            private set
            {
                if (CheckForObstacles.Value)
                {
                    aim.SetTargetValueInternal(value);  //Temporal set the Value to the new target to check for obstacles
                    aim.AimLogic(true);                 //Calculate the HIT RAYCAST
                    aim.SetTargetValueInternal(null);   //Reset the value to the original one

                    var aimHit = aim.AimHit.transform;

                    if (aimHit != null && !aimHit.SameHierarchy(value) && value != null && aimHit.gameObject.layer != value.gameObject.layer)
                    {
                        value = null;
                        aim.SetTarget(value);
                    }
                }

                if (value != locketTarget)
                {
                    locketTarget = value;
                    aim.SetTarget(value);

                    IsAimTarget = value != null ? value.FindComponent<AimTarget>() : null;
                    aimTargetAssist = IsAimTarget != null ? IsAimTarget.AimPoint : null;

                    OnTargetChanged.Invoke(aimTargetAssist != null ? aimTargetAssist : value);
                    // OnTargetAimAssist.Invoke(aimTargetAssist);
                }
            }
        }
        private Transform locketTarget;
        private Transform aimTargetAssist;
        //  private Transform DefaultAimTarget;

        public bool LockingOn { get; private set; }

        public AimTarget IsAimTarget { get; private set; }
        public GameObject Owner => transform.root.gameObject;

        private void Awake()
        {
            Targets.Clear();

            if (aim != null) aim.FindComponent<Aim>();
        }

        private void OnEnable()
        {
            if (Targets != null)
            {
                Targets.OnItemAdded.AddListener(OnItemAdded);
                Targets.OnItemRemoved.AddListener(OnItemRemoved);
            }

            if (aim != null) aim.OnRayHitChanged += OnHitChanged;


            ResetLockOn();
        }



        private void OnDisable()
        {
            if (Targets != null)
            {
                Targets.OnItemAdded.RemoveListener(OnItemAdded);

                Targets.OnItemRemoved.RemoveListener(OnItemRemoved);
            }
            ResetLockOn();

            if (aim != null) aim.OnRayHitChanged -= OnHitChanged;

        }
        private void OnHitChanged(RaycastHit hit)
        {
            if (CheckForObstacles.Value && LockedTarget != null)
            {
                var aimHit = aim.AimHit.transform;

                if (aimHit != null && !aimHit.SameHierarchy(LockedTarget) && aimHit.gameObject.layer != LockedTarget.gameObject.layer)
                {
                    if (AutoSwapOnObstacle.Value)
                    {
                        FindNextInLineWhenObstacle();
                    }
                    else
                    {
                        ResetLockOn();
                    }
                }
            }
        }

        private void OnItemAdded(GameObject arg0)
        {
            if (Auto.Value && !LockingOn)
            {
                LockTarget(true);
            }
        }

        public void LockTargetToggle()
        {
            if (Time.timeScale == 0) return; //Do nothing if the game is paused

            LockingOn ^= true;
            LookingTarget();
        }


        public void LockTarget(bool value)
        {
            if (Time.timeScale == 0) return; //Do nothing if the game is paused

            LockingOn = value;
            LookingTarget();
        }

        private void LookingTarget()
        {
            if (LockingOn)
            {
                if (Targets != null && Targets.Count > 0) //if we have a focused Item
                {
                    FindNearestTarget();
                    OnLockingTarget.Invoke(true);
                }
            }
            else
            {
                ResetLockOn();
            }
        }

        //Reset the values when the Lock Target is off
        private void ResetLockOn()
        {
            //if (LockedTarget != null)
            {
                CurrentTargetIndex = -1;
                LockedTarget = null;
                LockingOn = false;
                OnLockingTarget.Invoke(false);
                Debugging($"Reset Locked Target: [Empty]");
            }
        }

        private void FindNearestTarget()
        {
            var closest = Targets.Item_GetClosest(gameObject);  //When Lock Target is On.. Get the nearest Target on the Set 

            if (closest)
            {
                LockedTarget = closest.transform;

                if (LockedTarget != null)
                {
                    CurrentTargetIndex = Targets.items.IndexOf(closest);    //Save the Index so we can cycle to all the targets.
                    Debugging($"Locked Target: {LockedTarget.name}");
                }
                else
                {
                    if (AutoSwapOnObstacle.Value) FindNextInLineWhenObstacle();
                }
            }
            else
            {
                ResetLockOn();
            }
        }

        private void FindNextInLineWhenObstacle()
        {
            for (int i = 0; i < Targets.Count; i++)
            {
                LockedTarget = Targets.items[i].transform;
                if (LockedTarget != null) break;
            }

            if (LockedTarget == null) ResetLockOn();
        }

        public void Target_Scroll(Vector2 value)
        {
            if (value.y > 0 || value.x > 0)
                Target_Next();
            else if (value.y < 0 || value.x < 0)
                Target_Previous();
        }


        public void Target_Scroll(float value)
        {
            if (value > 0) Target_Next();
            else if (value < 0) Target_Previous();
        }

        public void Target_Next()
        {
            if (NextTargetTime > 0 && (Time.time - CurrentTime) <= NextTargetTime) return;
            // if (CurrentTime == Time.time) return;

            if (Targets != null && LockedTarget != null && CurrentTargetIndex != -1) //Check everything is working
            {
                CurrentTime = Time.time;

                CurrentTargetIndex++;
                CurrentTargetIndex %= Targets.Count; //Cycle to the first in case we are on the last item on the list

                // Debug.Log($"CurrentTargetIndex {CurrentTargetIndex}");


                LockedTarget = Targets.Item_Get(CurrentTargetIndex).transform;     //Store the Next Target

                if (LockedTarget)
                {
                    Debugging($"Locked Next Target: {LockedTarget.name}");
                }
                else
                {
                    if (AutoSwapOnObstacle.Value) FindNextInLineWhenObstacle();
                }
            }
        }

        public void Target_Previous()
        {
            if (NextTargetTime > 0 && (Time.time - CurrentTime) <= NextTargetTime) return;
            // if (CurrentTime == Time.time) return;

            if (Targets != null && LockedTarget != null && CurrentTargetIndex != -1) //Check everything is working
            {
                CurrentTime = Time.time;

                CurrentTargetIndex--;
                if (CurrentTargetIndex == -1) CurrentTargetIndex = Targets.Count - 1;

                Debug.Log($"CurrentTargetIndex {CurrentTargetIndex}");

                LockedTarget = Targets.Item_Get(CurrentTargetIndex).transform;     //Store the Next Target

                if (LockedTarget)
                {
                    Debugging($"Locked Previous Target: {LockedTarget.name}");

                }
                else
                {
                    if (AutoSwapOnObstacle.Value) FindNextInLineWhenObstacle();
                }
            }
        }

        private void OnItemRemoved(GameObject _)
        {
            if (LockingOn) //If we are still on Lock Mode then find the next Target
                this.Delay_Action(() => FindNearestTarget()); //Find the nearest target the next frame
        }


        public void Debugging(string value)
        {
#if UNITY_EDITOR
            if (debug)
                Debug.Log($"<B>[{aim.name}]</B> → <color=white>{value}</color>", this);
#endif
        }


#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!aim) aim = this.FindComponent<Aim>();
        }


        private void Reset()
        {
            aim = this.FindComponent<Aim>();

            Targets = MTools.GetInstance<RuntimeGameObjects>("Lock on Targets");
            var lockedTarget = MTools.GetInstance<TransformVar>("Locked Target");

            var CamEvent = MTools.GetInstance<MEvent>("Set Camera LockOnTarget");


            UnityEditor.Events.UnityEventTools.AddPersistentListener(OnTargetChanged, CamEvent.Invoke);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(OnTargetChanged, lockedTarget.SetValue);
        }
#endif
    }
}
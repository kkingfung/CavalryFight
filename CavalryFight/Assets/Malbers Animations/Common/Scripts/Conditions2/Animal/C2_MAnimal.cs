using MalbersAnimations.Controller;
using MalbersAnimations.Scriptables;
using UnityEngine;


namespace MalbersAnimations.Conditions
{
    [System.Serializable]
    public abstract class C2_MAnimal : ConditionCore
    {
        [Hide(nameof(LocalTarget))] public MAnimal Target;
        protected override void _SetTarget(Object target)
        {
            Target = MTools.VerifyComponent(target, Target);
        }
    }

    //------------------------------------------------------------------------------------------------------------------------------------

    #region Animal General Values 
    [System.Serializable, AddTypeMenu("Animal/General")]
    public class C2_AnimalGeneral : C2_MAnimal
    {
        public override string DynamicName => $"Animal [{Condition}]";

        public enum AnimalCondition
        {
            Grounded, RootMotion, FreeMovement, AlwaysForward, Sleep, AdditivePosition,
            AdditiveRotation, InZone, InGroundChanger, Strafing, CanStrafe, MovementDetected, InTimeline
        }

        public AnimalCondition Condition;

        protected override bool _Evaluate()
        {
            if (Target)
            {
                switch (Condition)
                {
                    case AnimalCondition.Grounded: return Target.Grounded;
                    case AnimalCondition.RootMotion: return Target.RootMotion;
                    case AnimalCondition.FreeMovement: return Target.FreeMovement;
                    case AnimalCondition.AlwaysForward: return Target.AlwaysForward;
                    case AnimalCondition.Sleep: return Target.Sleep;
                    case AnimalCondition.AdditivePosition: return Target.UseAdditivePos;
                    case AnimalCondition.AdditiveRotation: return Target.UseAdditiveRot;
                    case AnimalCondition.InZone: return Target.InZone;
                    case AnimalCondition.InGroundChanger: return Target.GroundChanger != null && Target.GroundChanger.Lerp > 0;
                    case AnimalCondition.Strafing: return Target.Strafe;
                    case AnimalCondition.CanStrafe: return Target.CanStrafe && Target.ActiveStance.CanStrafe && Target.ActiveState.CanStrafe;
                    case AnimalCondition.MovementDetected: return Target.MovementDetected;
                    case AnimalCondition.InTimeline: return Target.InTimeline;
                }
            }
            return false;
        }
    }
    #endregion

    //------------------------------------------------------------------------------------------------------------------------------------

    #region Animal Modes
    [System.Serializable, AddTypeMenu("Animal/Modes")]
    public class C2_AnimalMode : C2_MAnimal
    {
        public override string DynamicName
        {
            get
            {
                var display = $"Animal Mode {(Value != null && Condition != ModeCondition.PlayingAnyMode ? "[" + Value.name + "]" : "")} [{Condition}";

                string extraData = Condition switch
                {
                    ModeCondition.PlayingAbility => $" '{AbilityName.Value}']",
                    ModeCondition.HasAbility => $" '{AbilityName.Value}']",

                    ModeCondition.PlayingAbilityByIndex => $" {AbilityIndex.Value}]",
                    ModeCondition.HasAbilityIndex => $" {AbilityIndex.Value}]",
                    ModeCondition.ActiveAbilityIndex => $" {AbilityIndex.Value}]",
                    ModeCondition.DefaultAbilityIndex => $" {AbilityIndex.Value}]",
                    _ => "]",
                };

                return display + extraData;
            }
        }

        public enum ModeCondition
        { PlayingAnyMode, PlayingMode, PlayingAbility, PlayingAbilities, PlayingAbilityByIndex, HasMode, HasAbility, HasAbilityIndex, Enabled, ActiveAbilityIndex, DefaultAbilityIndex }

        public ModeCondition Condition;
        [Hide(nameof(Condition), true, 0)]
        public ModeID Value;
        [Hide(nameof(Condition), 2, 5)]
        public StringReference AbilityName;
        [Hide(nameof(Condition), 3, 6, 8, 9)]
        public IntReference AbilityIndex;

        public void SetValue(ModeID v) => Value = v;

        Mode mode;

        protected override bool _Evaluate()
        {
            if (Target == null) return false;

            mode ??= Target.Mode_Get(Value);        //cache the mode

            if (mode == null) return false;

            return Condition switch
            {
                ModeCondition.PlayingMode => Target.IsPlayingMode && (Value == null || Target.ActiveMode.ID == Value),
                ModeCondition.PlayingAbility =>
                Target.IsPlayingMode && (string.IsNullOrEmpty(AbilityName.Value) || Target.ActiveMode.ActiveAbility.Name == AbilityName),
                ModeCondition.HasMode => mode != null,
                ModeCondition.HasAbility => mode != null && mode.Abilities.Exists(x => x.Name == AbilityName),
                ModeCondition.HasAbilityIndex => mode != null && mode.Abilities.Exists(x => x.Index == AbilityIndex.Value),
                ModeCondition.Enabled => mode != null && mode.Active,
                ModeCondition.PlayingAnyMode => Target.IsPlayingMode,
                ModeCondition.PlayingAbilityByIndex => Target.IsPlayingMode && Target.ActiveMode.ActiveAbility.Index.Value == AbilityIndex.Value,
                ModeCondition.ActiveAbilityIndex => mode != null && mode.AbilityIndex == AbilityIndex,
                ModeCondition.DefaultAbilityIndex => mode != null && mode.DefaultIndex.Value == AbilityIndex,
                _ => false,
            };
        }

        public override void TargetHasChanged()
        {
            if (Target) mode = Target.Mode_Get(Value);        //Update the mode
        }
    }
    #endregion

    //------------------------------------------------------------------------------------------------------------------------------------

    #region Animal States
    [System.Serializable, AddTypeMenu("Animal/States")]
    public class C2_AnimalState : C2_MAnimal
    {
        public override string DynamicName => $"Animal [{Condition}] {(Value != null ? $"[{Value.name}]" : string.Empty)}";
        public enum StateCondition { ActiveState, Enabled, HasState, LastState, SleepFromMode, SleepFromState, SleepFromStance, Pending, IsPersistent }
        public StateCondition Condition = StateCondition.ActiveState;
        public StateID Value;
        private State state;

        public void SetValue(StateID v) => Value = v;

        protected override bool _Evaluate()
        {
            if (!Target) return false;

            if (state == null) state = Target.State_Get(Value); //cache the state

            return Condition switch
            {
                StateCondition.ActiveState => Target.ActiveStateID.ID == Value.ID,    //Check if the Active state is the one with this ID
                StateCondition.HasState => state != null,                       //Check if the State exist on the Current Animal
                StateCondition.Enabled => state.Active,
                StateCondition.SleepFromMode => state.IsSleepFromMode,
                StateCondition.SleepFromState => state.IsSleepFromState,
                StateCondition.SleepFromStance => state.IsSleepFromStance,
                StateCondition.LastState => Target.LastState.ID == Value,       //Check if the LastState is this ID
                StateCondition.Pending => state.IsPending,
                StateCondition.IsPersistent => state.IsPersistent,
                _ => false,
            };
        }

        public override void TargetHasChanged()
        {
            if (Target) state = Target.State_Get(Value); //update the state
        }
    }
    #endregion

    //------------------------------------------------------------------------------------------------------------------------------------

    #region Animal Stances
    [System.Serializable, AddTypeMenu("Animal/Stances")]
    public class C2_AnimalStance : C2_MAnimal
    {
        public override string DynamicName => $"Animal [{Condition}] {(Value != null ? $"[{Value.name}]" : string.Empty)}";

        public enum StanceCondition { CurrentStance, DefaultStance, LastStance, HasStance }
        public StanceCondition Condition;
        public StanceID Value;
        private Stance stance;

        public void SetValue(StanceID v) => Value = v;

        protected override bool _Evaluate()
        {
            if (stance == null && Target != null) stance = Target.Stance_Get(Value); //cache the stance

            if (Target != null && stance != null)
            {
                return Condition switch
                {
                    StanceCondition.CurrentStance => Target.Stance == Value,
                    StanceCondition.DefaultStance => Target.DefaultStanceID == Value,
                    StanceCondition.LastStance => Target.LastStanceID == Value,
                    StanceCondition.HasStance => stance != null,
                    _ => false,
                };
            }
            return false;
        }
    }
    #endregion

    //------------------------------------------------------------------------------------------------------------------------------------

    #region Animal Speeds
    [System.Serializable, AddTypeMenu("Animal/Speeds")]
    public class C2_AnimalSpeed : C2_MAnimal
    {
        public override string DynamicName
        {
            get
            {
                var display = $"Animal [{Condition}";

                string extraData = Condition switch
                {
                    SpeedCondition.VerticalSpeed => $": {MTools.CompareToString(compare)} {Value.Value}]",
                    SpeedCondition.CurrentSpeedSet => $": <{SpeedName.Value}>]",
                    SpeedCondition.CurrentSpeedModifier => $": <{SpeedName.Value}>]",
                    SpeedCondition.ActiveIndex => $": {Value.Value}]",
                    _ => "]",
                };

                return display + extraData;
            }
        }

        public enum SpeedCondition { VerticalSpeed, CurrentSpeedSet, CurrentSpeedModifier, ActiveIndex, IsSprinting, CanSprint }

        public SpeedCondition Condition;

        [Hide(nameof(Condition), (int)SpeedCondition.VerticalSpeed, (int)SpeedCondition.ActiveIndex)]
        public ComparerInt compare = ComparerInt.Equal;

        [Hide(nameof(Condition), (int)SpeedCondition.VerticalSpeed, (int)SpeedCondition.ActiveIndex)]
        public FloatReference Value = new();

        [Hide(nameof(Condition), (int)SpeedCondition.CurrentSpeedSet, (int)SpeedCondition.CurrentSpeedModifier)]
        public StringReference SpeedName = new();

        protected override bool _Evaluate()
        {
            if (!Target) return false;

            return Condition switch
            {
                SpeedCondition.VerticalSpeed => Target.VerticalSmooth.CompareFloat(Value, compare),
                SpeedCondition.CurrentSpeedSet => Target.CurrentSpeedSet.name == SpeedName,
                SpeedCondition.CurrentSpeedModifier => Target.CurrentSpeedModifier.name == SpeedName,
                SpeedCondition.ActiveIndex => Target.CurrentSpeedIndex == (int)Value,
                SpeedCondition.IsSprinting => Target.Sprint,
                SpeedCondition.CanSprint => Target.CanSprint,
                _ => false,
            };
        }
    }
    #endregion

    //------------------------------------------------------------------------------------------------------------------------------------

    #region Animal Strafe
    [System.Serializable, AddTypeMenu("Animal/Strafe")]
    public class C2_AnimalStrafe : C2_MAnimal
    {
        public override string DynamicName => $"Animal [{Condition}]";
        public enum StrafeCondition { Strafing, CanStrafe }
        public StrafeCondition Condition;

        protected override bool _Evaluate()
        {
            if (Target)
            {
                return Condition switch
                {
                    StrafeCondition.Strafing => Target.Strafe,
                    StrafeCondition.CanStrafe => Target.CanStrafe && Target.ActiveStance.CanStrafe && Target.ActiveState.CanStrafe,
                    _ => false,
                };
            }
            return false;
        }
    }


    [System.Serializable, AddTypeMenu("Animal/Direction Angle")]
    public class C2_AnimalDirectionAngle : C2_MAnimal
    {
        public FloatReference MinAngle = new(-45);
        public FloatReference MaxAngle = new(45);
        [Tooltip("If true the angle will be compared as absolute value")]
        public BoolReference Abs = new(false); //If true the angle will be compared as absolute value

        public override string DynamicName => $"Animal Move Direction Angle [{MinAngle.Value}]<={angle:f2}<=[{MaxAngle.Value}]";

        private float angle;

        protected override bool _Evaluate()
        {
            if (Target)
            {
                var AxisRaw = Target.Move_Direction;
                angle = Vector3.SignedAngle(Target.Forward, AxisRaw, Target.UpVector); //Get The angle
                if (Abs.Value) angle = Mathf.Abs(angle); //If we are using absolute value, then we take the absolute value of the angle
                var result = angle >= MinAngle && angle <= MaxAngle;

                MDebug.Draw_Arrow(Target.transform.position, Target.Move_Direction, Color.green, 0.5f);
                MDebug.Draw_Arrow(Target.transform.position, Target.Forward, Color.red, 0.5f);

                return result;
            }
            return false;
        }
    }
    #endregion

    //------------------------------------------------------------------------------------------------------------------------------------
}

using MalbersAnimations.Controller;
using UnityEngine;


namespace MalbersAnimations.Reactions
{
    [System.Serializable]
    [AddTypeMenu("Malbers/Animal/State")]
    public class StateReaction : MReaction
    {

        override public string DynamicName
        {
            get
            {
                var display = $"Animal State [{type}: {(ID != null ? ID.name : "<Null>")}]";

                switch (type)
                {
                    case State_Reaction.AllowExit:
                        display = $"Animal State [Allow Exit: {(ID != null ? $"If Active is {ID.name}" : "Current")}]. [Exit: {ExitStatus}]";
                        break;
                    case State_Reaction.SetExitStatus:
                        display = $"Animal State [Set Exit Status to {ExitStatus}: {(ID != null ? $"If Active is {ID.name}" : "To Current")}]";
                        break;
                    case State_Reaction.ExitToState:
                        display = $"Animal State [Exit Active State to: {(ID != null ? $"{ID.name}" : "<Null>")}]. [Exit: {ExitStatus}]";
                        break;
                    case State_Reaction.Replace:
                        display = $"Animal State [Replace new state: {(replace != null ? replace.name : "<Null>")}]";
                        break;
                    case State_Reaction.Activate:
                        display += $" [Enter: {EnterStatus}]";
                        break;
                    case State_Reaction.ForceActivate:
                        display += $" [Enter: {EnterStatus}]";
                        break;

                    default:
                        break;
                }
                return display;
            }
        }

        public State_Reaction type = State_Reaction.Activate;

        [Tooltip("State you want to activate or Exit")]
        [Hide(nameof(type), true, (int)State_Reaction.Replace)]
        public StateID ID;

        [Hide(nameof(type), (int)State_Reaction.Activate, (int)State_Reaction.ForceActivate)]
        [Tooltip("This will change the value of the Enter Status parameter on the Animator. Useful to use different animations when Activating a State")]
        public int EnterStatus;

        [Tooltip("This will change the value of the Exit Status parameter on the Animator. Useful to use different animations when Exiting a State")]
        [Hide(nameof(type), (int)State_Reaction.SetExitStatus, (int)State_Reaction.AllowExit, (int)State_Reaction.ExitToState)]
        public int ExitStatus;

        [Hide(nameof(type), (int)State_Reaction.Replace)]
        public State replace;

        protected override bool _TryReact(Component component)
        {
            var animal = component as MAnimal;

            switch (type)
            {
                case State_Reaction.Activate:
                    State NewState = animal.State_Get(ID);
                    if (NewState && NewState.CanBeActivated)
                    {
                        NewState.Activate();
                        return true;
                    }
                    return false;
                case State_Reaction.AllowExit:
                    if (ID == null || animal.ActiveStateID == ID)
                    {
                        return animal.ActiveState.AllowExit();
                    }
                    return false;
                case State_Reaction.ForceActivate:
                    animal.State_Force(ID);
                    return true;
                case State_Reaction.Enable:
                    animal.State_Enable(ID);
                    return true;
                case State_Reaction.Disable:
                    animal.State_Disable(ID);
                    return true;
                case State_Reaction.SetExitStatus:
                    animal.State_SetEnterStatus(ExitStatus);
                    return true;
                case State_Reaction.ExitToState:
                    animal.ActiveState.AllowExit(ID.ID, ExitStatus);
                    return true;
                case State_Reaction.Replace:
                    animal.State_Replace(replace);
                    return true;

                default:
                    return false;
            }
        }

        public enum State_Reaction
        {
            /// <summary>Tries to Activate the State of the Zone</summary>
            Activate,
            /// <summary>If the Animal is already on the state of the zone it will allow to exit and activate states below the Active one</summary>
            AllowExit,
            /// <summary>Force the State of the Zone to be enable even if it cannot be activate at the moment</summary>
            ForceActivate,
            /// <summary>Enable a  Disabled State </summary>
            Enable,
            /// <summary>Disable State </summary>
            Disable,
            /// <summary>Change the Status ID of the State in case the State uses its</summary>
            SetExitStatus,
            /// <summary>AllowExitTo</summary>
            ExitToState,
            Replace
        }


    }
}

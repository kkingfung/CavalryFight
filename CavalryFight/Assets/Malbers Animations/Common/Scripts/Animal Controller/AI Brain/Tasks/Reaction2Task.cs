using MalbersAnimations.Reactions;
using UnityEngine;

namespace MalbersAnimations.Controller.AI
{
    [CreateAssetMenu(menuName = "Malbers Animations/Pluggable AI/Tasks/Reactions2 Task")]
    public class Reaction2Task : MTask
    {
        public override string DisplayName => "General/Reactions2";

        [Space, Tooltip("Apply the Task to the Animal(Self) or the Target(Target)")]
        public Affected affect = Affected.Self;

        [Tooltip("Use the Interval value to repeat the reaction")]
        public bool repeat = false;

        [Tooltip("Reaction when the AI Task begin")]
        public Reaction2 startTaskReaction;

        [Tooltip("Reaction to repeat ")]
        [Hide("repeat")]
        public Reaction2 repeatTaskReaction;

        [Tooltip("Reaction when the AI State ends")]
        public Reaction2 endTaskReaction;

        public override void StartTask(MAnimalBrain brain, int index)
        {
            //Cache the component to avoid calling GetComponent every time
            brain.TasksVars[index].Components = new Component[1];
            //Store the Component for the reaction here so its easy to find later
            brain.TasksVars[index].Components[0] = (affect == Affected.Self ? brain.Animal : brain.Target);

            React(brain, index, startTaskReaction);

            if (!repeat)
                brain.TaskDone(index);
        }

        private void React(MAnimalBrain brain, int index, Reaction2 reaction)
        {
            reaction.React(brain.TasksVars[index].Components[0]); //Get the component again from the vars
        }


        public override void UpdateTask(MAnimalBrain brain, int index)
        {
            //repeat the reaction using the Update Interval (If Repeat is false) this will be skipped
            React(brain, index, repeatTaskReaction);
        }

        public override void ExitAIState(MAnimalBrain brain, int index)
        {
            React(brain, index, endTaskReaction);
        }

        private void Reset()
        => Description = "Add a Reaction to the Target or the Animal";

    }
}

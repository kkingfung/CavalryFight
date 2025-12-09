using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MalbersAnimations.Conditions
{
    /// <summary>Conditions to Run on a Object </summary>
    [System.Serializable]
    public abstract class ConditionCore : ICloneable
    {
        /// <summary> Display name in the Condition</summary>
        public virtual string DynamicName => this.GetType().ToString() + " Condition";

        [Tooltip("Description of the Condition")]
        [HideInInspector] public string desc = string.Empty;
        [HideInInspector] public bool invert;
        [HideInInspector] public bool OrAnd;
        [HideInInspector] public bool LocalTarget = false;
        protected Object CacheTarget;

        [SerializeField, HideInInspector] bool debug;



        public bool DebugCondition => debug;

        //Call this on On Enable on any component
        public virtual void OnEnable() { }

        //Call this on On Disable on any component
        public virtual void OnDisable() { }

        /// <summary>Evaluate a condition using the Target</summary>
        protected abstract bool _Evaluate();

        /// <summary>Evaluate a condition using the Target</summary>
        public bool Evaluate(Object target)
        {
            SetTarget(target);
            return Evaluate();
        }

        /// <summary>Set target correct type on the on the Conditions</summary>
        protected abstract void _SetTarget(Object target);

        public virtual void SetTarget(Object target)
        {
            if (LocalTarget) return; //Do nothing if is Local Target

            //If the target is different from the last one
            if (CacheTarget != target)
            {
                CacheTarget = target;
                _SetTarget(target);
                TargetHasChanged();
            }
        }

        /// <summary> Change internal variables here when a target has changed </summary>
        public virtual void TargetHasChanged() { }

        public bool Evaluate()
        {
            var result = invert ? !_Evaluate() : _Evaluate();

            Debugging(result);

            return result;
        }

        // protected override void _SetTarget(Object target) => Target = MTools.VerifyComponent(target, Target);

        /// <summary> Optional Method to Draw Gizmos on the Scene View </summary>
        public virtual void DrawGizmos(Component target) { }

        /// <summary> Optional Method to Draw GizmosOnSelected on the Scene View </summary>
        public virtual void DrawGizmosSelected(Component target) { }


        public virtual void Debugging(bool result)
        {
            if (debug)
            {
                var color = result ? "cyan" : "orange";

                var inverted = invert ? "<B><color=red>[NOT]</color></B>" : string.Empty;

                result = invert ? !result : result;

                MDebug.Log($"<color=yellow><B>Condition: {inverted}[{DynamicName}] </B></color> Result: <color={color}> <B>{result}</B> </color> ", CacheTarget);
            }
        }

        public virtual void Debugging(string data, bool result, Object target)
        {
            if (debug)
            {
                var color = result ? "cyan" : "orange";

                var inverted = invert ? "<B><color=red>[NOT]</color></B>" : string.Empty;

                result = invert ? !result : result;

                MDebug.Log($"<color=yellow><B>Condition: {inverted} [{DynamicName}] </B></color> Result: <color={color}> <B>{result}</B> </color> ", target);
            }
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
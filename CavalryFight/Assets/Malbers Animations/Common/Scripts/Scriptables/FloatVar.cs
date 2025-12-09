using System;
using UnityEngine;

namespace MalbersAnimations.Scriptables
{
    ///<summary>  Float Scriptable Variable. Based on the Talk - Game Architecture with Scriptable Objects by Ryan Hipple  </summary>
    [CreateAssetMenu(menuName = "Malbers Animations/Variables/Float", order = 1000)]
    public class FloatVar : ScriptableVar
    {
        /// <summary>The current value</summary>
        [SerializeField] protected float value = 0;

        /// <summary> Invoked when the value changes</summary>
        public Action<float> OnValueChanged;

        /// <summary>Value of the Float Scriptable variable </summary>
        public virtual float Value
        {
            get => value;
            set
            {
                //  if (this.value != value)                                //If the value is different change it
                {
                    this.value = value;
                    OnValueChanged?.Invoke(value);         //If we are using OnChange event Invoked
#if UNITY_EDITOR
                    if (debug) Debug.Log($"<B>{name} -> [<color=red> {value:F3} </color>] </B>", this);
#endif
                }
            }
        }

        /// <summary>Set the Value using another FloatVar</summary>
        public virtual void SetValue(FloatVar var) => Value = var.Value;
        public virtual void SetValue(float var) => Value = var;

        /// <summary>Add or Remove the passed var value</summary>
        public virtual void Add(FloatVar var) => Value += var.Value;

        /// <summary>Add or Remove the passed var value</summary>
        public virtual void Add(float var) => Value += var;



        public static implicit operator float(FloatVar reference) => reference.Value;
    }

    [System.Serializable]
    public class FloatReference : ReferenceVar
    {
        public float ConstantValue;
        [RequiredField] public FloatVar Variable;

        public FloatReference() => Value = 0;

        public FloatReference(float value) => Value = value;

        public FloatReference(FloatVar value)
        {
            Variable = value;
            UseConstant = false;
        }

        public float Value
        {
            get => UseConstant || Variable == null ? ConstantValue : Variable.Value;
            set
            {
                //CustomPatch: TODO: consider using this assert in these data types (let's talk if you have something different intended in mind); this assert is meant to highlight possible user setup errors (see below comment)
                Debug.Assert(UseConstant || !UseConstant && Variable != null, "Expected FloatVar object is not set when UseConstant is false!");

                //CustomPatch: TODO: this logic hides an unintentional errors that can be hard to track when UseConstant is intentionally set to false and the Variable is accidentally null => is user sets UseConstant = false this FloatReference should expect a FloatVar object to be set
                if (UseConstant || Variable == null)
                    ConstantValue = value;
                else
                    Variable.Value = value;
            }
        }

        public static implicit operator float(FloatReference reference) => reference.Value;

        public static implicit operator FloatReference(float reference) => new(reference);
    }

#if UNITY_EDITOR
    [UnityEditor.CanEditMultipleObjects, UnityEditor.CustomEditor(typeof(FloatVar))]
    public class FloatVarEditor : VariableEditor
    {
        public override void OnInspectorGUI() => PaintInspectorGUI("Float Variable");
    }
#endif
}
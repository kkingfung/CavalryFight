using System.Collections.Generic;
using UnityEngine;

namespace MalbersAnimations.Utilities
{
    /// <summary>  Based on 3DKit Controller from Unity  </summary>
    [SelectionBase, AddComponentMenu("Malbers/Utilities/Transform/Blend Local Rotations")]
    public class MBlendLocalRotation : MSimpleTransformer
    {
        [System.Serializable]
        public class TransformRotationData
        {
            [RequiredField] public Transform Transform;
            public Vector3 StartRotation;
            public Vector3 EndRotation;
        }

        public bool ManualPreview = false; //If true, the Evaluate method must be called manually to update the rotation

        [ContextMenuItem("Set [Start] from current Rotation", nameof(ExtractLocalStartRotations))]
        [ContextMenuItem("Set [ End ] from current Rotation", nameof(ExtractLocalEndRotations))]
        public List<TransformRotationData> objects = new();


        private void Awake()
        {
            Inverted = false;
        }


        public override void Evaluate(float value)
        {
            if (!Application.isPlaying)
            {
                if (!ManualPreview) return; // Do not evaluate if not playing and ManualPreview is false
            }

            var curvePosition = m_Curve.Evaluate(value);

            foreach (var obj in objects)
            {
                if (obj.StartRotation == Vector3.zero) continue;
                if (obj.EndRotation == Vector3.zero) continue;
                if (obj.Transform == null) continue; // Skip if the Transform is null

                var q = Quaternion.Euler(Vector3.LerpUnclamped(obj.StartRotation, obj.EndRotation, curvePosition));
                obj.Transform.localRotation = q;
            }
        }


        /// <summary> When using Additive the rotation will continue from the last position  </summary>
        protected override void Pre_End()
        {
            //if (loopType == LoopType.Once && endType == EndType.Additive)
            //{
            //    startAngle.Value = endAngle.Value; //use the end value as start value
            //    endAngle.Value += difference;
            //}
        }


        protected override void Pos_End()
        {
            if (loopType == LoopType.Once && endType == EndType.Invert)
                Invert_Start_End();
        }


        [ContextMenu("Invert Value")]
        public void Invert_Value()
        {
            //if (!enabled) return; //Do not invert while disabled
            if (Playing) { Debug.Log("Cannot invert value while playing"); return; } //Do not invert while playing

            Inverted ^= true;

            foreach (var rotData in objects)
            {
                (rotData.StartRotation, rotData.EndRotation) = (rotData.EndRotation, rotData.StartRotation);
            }

            //difference *= -1;
            //endAngle.Value = startAngle.Value + difference;

            Debug.Log("Rotation Value Inverted");
        }


        [ContextMenu("Invert Value +")]
        public void Invert_Value_Positive() { if (Inverted) Invert_Value(); }


        [ContextMenu("Invert Value -")]
        public void Invert_Value_Negative() { if (!Inverted) Invert_Value(); }


        [ContextMenu("Invert Start - End")]
        public void Invert_Start_End()
        {
            foreach (var rotData in objects)
            {
                (rotData.StartRotation, rotData.EndRotation) = (rotData.EndRotation, rotData.StartRotation);
            }

            value = 0;
            Evaluate(0);
            MTools.SetDirty(this);
        }

        [ContextMenu("Extract Local Start Rotations")]
        private void ExtractLocalStartRotations()
        {
            foreach (var rotData in objects)
            {
                if (rotData.Transform != null)
                    rotData.StartRotation = rotData.Transform.localEulerAngles;
            }

            MTools.SetDirty(this);
        }

        [ContextMenu("Extract Local End Rotations")]
        private void ExtractLocalEndRotations()
        {

            foreach (var rotData in objects)
            {
                if (rotData.Transform != null)
                    rotData.EndRotation = rotData.Transform.localEulerAngles;
            }
            MTools.SetDirty(this);
        }


        //private void OnValidate()
        //{
        //    if (objects.Count != StartRotations.Count)
        //    {
        //        StartRotations = new List<Vector3>(objects.Count);

        //        for (int i = 0; i < StartRotations.Count; i++)
        //        {
        //            if (StartRotations[i] == Vector3.zero && objects[i] != null) StartRotations[i] = objects[i].localEulerAngles;
        //        }
        //    }
        //}
    }
}
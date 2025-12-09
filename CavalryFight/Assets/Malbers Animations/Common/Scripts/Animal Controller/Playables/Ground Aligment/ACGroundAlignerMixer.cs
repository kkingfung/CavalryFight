using UnityEngine;
using UnityEngine.Playables;

namespace MalbersAnimations.Controller
{
    public class ACGroundAlignerMixer : PlayableBehaviour
    {
        bool m_ShouldInitializeTransform = true;
        Vector3 m_InitialPosition;
        Quaternion m_InitialRotation;

        void InitializeIfNecessary(Transform transform)
        {
            if (m_ShouldInitializeTransform)
            {
                m_InitialPosition = transform.position;
                m_InitialRotation = transform.rotation;
                m_ShouldInitializeTransform = false;
            }
        }


        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var trackBinding = playerData as MAnimal;
            if (trackBinding == null) return;

            // Get the initial position and rotation of the track binding, only when ProcessFrame is first called
            InitializeIfNecessary(trackBinding.transform);

            int inputCount = playable.GetInputCount();

            for (int i = 0; i < inputCount; i++)
            {
                float inputWeight = playable.GetInputWeight(i);
                ScriptPlayable<ACGroundAlignerBehaviour> inputPlayable = (ScriptPlayable<ACGroundAlignerBehaviour>)playable.GetInput(i);
                ACGroundAlignerBehaviour input = inputPlayable.GetBehaviour();

                input.GroundRayCast(trackBinding);
            }
        }
    }
}

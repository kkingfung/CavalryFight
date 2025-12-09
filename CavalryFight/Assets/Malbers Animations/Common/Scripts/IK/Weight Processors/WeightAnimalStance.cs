using System.Collections.Generic;
using UnityEngine;

namespace MalbersAnimations.IK
{
    /// <summary>  Process the weight by checking the Look At Angle of the Animator / </summary>
    [System.Serializable, AddTypeMenu("Animal/Weight Stance")]
    public class WeightAnimalStance : WeightProcessor
    {
        public override string DynamicName
        {
            get
            {
                var result = $"Animal is in Stances. [";

                for (int i = 0; i < Stances.Count; i++)
                {
                    if (Stances[i] != null) result += Stances[i].name;

                    if (i < Stances.Count - 1) result += ",";
                }

                return result + "]";
            }
        }

        [Tooltip("Stance to check if the animal is on. Weight will be set to 1")]
        public List<StanceID> Stances = new();
        private List<int> stances = new();

        private ICharacterAction character;

        private float StateWeight = 0;
        public override void OnEnable(IKSet set, Animator anim)
        {
            stances = Stances.ConvertAll(x => x.ID); //Convert to Ints

            if (anim.TryGetComponent(out character))
                character.OnStance += OnStance;
            else
            {
                Active = false;
                Debug.LogWarning("The Stance weight processor requires an Animal Controller. Disabling it");
            }
        }

        private void OnStance(int newState)
        {
            StateWeight = stances.Contains(newState) ? 1 : 0;
        }

        public override void OnDisable(IKSet set, Animator anim)
        {
            if (character != null) character.OnStance -= OnStance;

        }

        public override float Process(IKSet set, float weight) => weight * StateWeight;
    }
}

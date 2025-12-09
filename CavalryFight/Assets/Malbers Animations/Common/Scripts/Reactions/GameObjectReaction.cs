using MalbersAnimations.Scriptables;
using System;
using UnityEngine;

namespace MalbersAnimations.Reactions
{
    [System.Serializable]
    [AddTypeMenu("Unity/GameObject")]
    public class GameObjectReaction : Reaction
    {
        public override string DynamicName => $"GameObject [{action} : {(Value.Value != null ? Value.Value.name : "")}]"; //Name of the Reaction

        public enum GameObject_Reaction { Enable, Disable, Instantiate }

        public override Type ReactionType => typeof(GameObject);

        public GameObject_Reaction action = GameObject_Reaction.Instantiate;

        public GameObjectReference Value = new();

        protected override bool _TryReact(Component component)
        {
            if (Value.Value != null)
            {
                switch (action)
                {
                    case GameObject_Reaction.Enable:
                        Value.Value.SetActive(true);
                        break;
                    case GameObject_Reaction.Disable:
                        Value.Value.SetActive(false);
                        break;
                    case GameObject_Reaction.Instantiate:
                        Value.Value = GameObject.Instantiate(Value.Value);
                        break;
                }
            }
            return false;
        }
    }
}

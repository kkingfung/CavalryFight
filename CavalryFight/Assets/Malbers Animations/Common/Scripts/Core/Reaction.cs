using System;
using UnityEngine;

namespace MalbersAnimations.Reactions
{
    [Serializable]
    public abstract class Reaction
    {
        [HideInInspector] public string desc = string.Empty;

        public static MonoBehaviour Delay;

        /// <summary>Instant Reaction ... without considering Active or Delay parameters</summary>
        protected abstract bool _TryReact(Component reactor);

        /// <summary>Get the Type of the reaction</summary>
        public abstract Type ReactionType { get; }

        public virtual string DynamicName => this.GetType().Name + " Reaction";

        public void React(Component component) => TryReact(useLocal ? localTarget : component);

        public void React(GameObject go) => TryReact(go.transform);

        [Tooltip("Enable or Disable the Reaction")]
        [HideInInspector] public bool Active = true;

        [Tooltip("Use a local Target instead of a dynamic one")]
        [HideInInspector] public bool useLocal;
        [HideInInspector] public Component localTarget;
        [HideInInspector, Min(0)] public float delay = 0;

        /// <summary>  Checks and find the correct component to apply a reaction  </summary>  
        public Component VerifyComponent(Component component)
        {
            if (component == null) return null; //If the component is null return null (No Component to React_)

            Component TrueComponent;

            //Find if the component is the same 
            if (ReactionType.IsAssignableFrom(component.GetType()) || ReactionType == typeof(GameObject))
            {
                TrueComponent = component;
            }
            else
            {
                //Debug.Log($"Component {component.name} REACTION TYPE: {ReactionType.Name}");

                TrueComponent = component.GetComponent(ReactionType);

                if (TrueComponent == null)
                    TrueComponent = component.GetComponentInParent(ReactionType);
                if (TrueComponent == null)
                    TrueComponent = component.GetComponentInChildren(ReactionType);
            }
            return TrueComponent;
        }

        public bool TryReact(Component component)
        {
            if (Application.isPlaying) //Reactions cannot be called in Editor!!
            {
                if (Active)
                {
                    component = VerifyComponent(useLocal ? localTarget : component);

                    if (component == null) //verification if the component is null
                    {
                        Debug.Log($"Component is null. Ignoring the Reaction. <b>[{ReactionType.Name}] </b>");
                        return false; //NO Component to React
                    }


                    if (!component.gameObject.scene.isLoaded || component.gameObject == null) return false; //If the component is in the scene return false

                    if (delay > 0)
                    {
                        //Create the Delay Reactions for the first time
                        if (Delay == null)
                        {
                            var DelayGameObject = new GameObject("Reaction Delay");
                            Delay = DelayGameObject.AddComponent<UnityUtils>();
                            // Delay.hideFlags = HideFlags.HideInInspector;
                            // Debug.Log($"Creating Delay Reaction GameObject for Delay Reactions. Created by [{ReactionType.Name}]", component);
                        }

                        Delay.Delay_Action(delay, () => _TryReact(component));
                        return true;
                    }
                    else
                    {
                        return _TryReact(component);
                    }
                }
            }
            return false;
        }

        //React to multiple components
        public bool TryReact(params Component[] components)
        {
            if (Active && components != null && components.Length > 0)
            {
                foreach (var component in components)
                {
                    var comp = VerifyComponent(component);
                    _TryReact(comp);
                }
            }
            return true;
        }


        public static implicit operator Reaction(Reaction2 reference)
        {
            if (!reference.IsValid) return null;

            if (reference.reactions.Length == 1)
            {
                return reference.reactions[0]; //Get the first reaction
            }
            else
            {
                return new ListReaction(reference.reactions);
            }
        }
    }
}
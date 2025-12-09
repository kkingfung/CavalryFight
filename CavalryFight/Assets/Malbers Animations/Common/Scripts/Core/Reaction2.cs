using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif 

namespace MalbersAnimations.Reactions
{
    [System.Serializable]
    public struct Reaction2
    {
        [SerializeReference] public Reaction[] reactions;

        public Reaction2(Reaction reaction)
        {
            this.reactions = new Reaction[1];
            this.reactions[0] = reaction;
        }

        public Reaction2(ListReaction reaction)
        {
            this.reactions = reaction.reactions.ToArray();
        }

        /// <summary>  Conditions can be used the list has some conditions </summary>
        public readonly bool IsValid => reactions != null && reactions.Length > 0;

        //Find the first reaction type
        public readonly Type ReactionType => reactions != null && reactions.Length > 0 && reactions[0] != null ? reactions[0].ReactionType : null;

        /// <summary>  Cache the component value on each reaction to avoid calling GetComponent every time  </summary>
        /// <param name="target">Global Target to find each component</param>
        public readonly void VerifyComponent(Component target)
        {
            for (int i = 0; i < reactions.Length; i++)
            {
                reactions[i].VerifyComponent(target);
            }
        }

        public readonly bool TryReact(GameObject target)
        {
            if (!IsValid) return false; //If there are no reactions, return false

            bool result = true;
            for (int i = 0; i < reactions.Length; i++)
            {
                reactions[i].VerifyComponent(target.transform);
                result = result && reactions[i].TryReact(target.transform);
            }

            return result;
        }
        public readonly bool TryReact(Component target)
        {
            if (!IsValid || target == null) return false; //If there are no reactions, return false

            bool result = true;

            for (int i = 0; i < reactions.Length; i++)
            {
                if (reactions[i] == null) { Debug.Log("There's a null Reaction. Please check all your reactions."); continue; } //Skip null reactions
                reactions[i].VerifyComponent(target);
                result = result && reactions[i].TryReact(target);
            }

            return result;
        }
        public readonly bool React(Component target) => TryReact(target);
        public readonly bool React(GameObject target) => TryReact(target);



        public static implicit operator Reaction2(Reaction reference)
        {
            if (reference == null) return new Reaction2();

            if (reference is ListReaction listReact)
            {
                return new(listReact);
            }
            else
            {
                return new(reference);
            }
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(Reaction2))]
    public class Reactions_Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.BeginProperty(position, label, property);

            var reactions = property.FindPropertyRelative("reactions");

            label.text += $" ({reactions.arraySize})";

            EditorGUI.PropertyField(position, reactions, label, true);
            EditorGUI.EndProperty();
            EditorGUI.indentLevel = indent;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("reactions"), label);
        }
    }
#endif 
}
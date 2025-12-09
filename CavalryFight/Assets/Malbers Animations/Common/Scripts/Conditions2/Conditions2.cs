using MalbersAnimations.Scriptables;
using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MalbersAnimations.Conditions
{
    [System.Serializable]
    public struct Conditions2
    {
        [SerializeReference] public ConditionCore[] conditions;

        public bool active;

        public Conditions2(int length)
        {
            conditions = new ConditionCore[length];
            active = true;
        }

        /// <summary>  Conditions can be used the list has some conditions </summary>
        public readonly bool Valid => active && conditions != null && conditions.Length > 0;

        public readonly bool Evaluate(UnityEngine.Object target)
        {
            if (!Valid) return true; //by default return true

            if (conditions[0] == null)
            {
                Debug.LogError($"[Null] Condition not Allowed. Please Check your conditions.", target);
                return false;
            }

            bool result = conditions[0].Evaluate(target); //Get the first one

            for (int i = 1; i < conditions.Length; i++) //start from the 2nd one
            {
                try
                {
                    bool nextResult = conditions[i].Evaluate(target);
                    result = conditions[i].OrAnd ? (result || nextResult) : (result && nextResult);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Null] Condition Result [{i}] [{conditions[i].DynamicName}]. Please Check your conditions.", target);
                    Debug.LogException(e, target);
                }
            }
            return result;
        }


        public static bool FindConditions2ByName(MonoBehaviour mono, string fieldName, out Conditions2 result)
        {
            result = default;

            if (mono == null || string.IsNullOrEmpty(fieldName)) return false;
            return FindInObjectByName(mono, fieldName, ref result);
        }

        private static bool FindInObjectByName(object obj, string fieldName, ref Conditions2 result)
        {
            if (obj == null) return false;

            Type type = obj.GetType();
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (FieldInfo field in fields)
            {
                object value = field.GetValue(obj);

                if (value == null) continue;

                // Match field name directly
                if (string.Equals(field.Name, fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    if (field.FieldType == typeof(Conditions2))
                    {
                        result = (Conditions2)value;
                        return true;
                    }
                    // List of Conditions2
                    else if (typeof(IEnumerable).IsAssignableFrom(field.FieldType))
                    {
                        IEnumerable enumerable = value as IEnumerable;
                        foreach (var element in enumerable)
                        {
                            if (element is Conditions2 found)
                            {
                                result = found;
                                return true;
                            }
                        }
                    }
                    // Nested object
                    else
                    {
                        if (FindInObjectByName(value, fieldName, ref result))
                            return true;
                    }
                }
            }
            return false;
        }


        public void Add(ConditionCore condition)
        {
            if (condition == null) return;
            if (conditions == null || conditions.Length == 0)
            {
                conditions = new ConditionCore[1];
                conditions[0] = condition;
            }
            else
            {
                var newConditions = new ConditionCore[conditions.Length + 1];
                for (int i = 0; i < conditions.Length; i++)
                {
                    newConditions[i] = conditions[i];
                }
                newConditions[^1] = condition;
                conditions = newConditions;
            }
        }

        public void Add(Conditions2 newConditions)
        {
            if (newConditions.conditions == null || newConditions.conditions.Length == 0) return;

            if (conditions == null || conditions.Length == 0)
            {
                conditions = newConditions.conditions; //If there are no conditions, set the new ones
            }
            else
            {
                var combinedConditions = new ConditionCore[conditions.Length + newConditions.conditions.Length];
                for (int i = 0; i < conditions.Length; i++)
                {
                    combinedConditions[i] = conditions[i];
                }
                for (int i = 0; i < newConditions.conditions.Length; i++)
                {
                    combinedConditions[conditions.Length + i] = newConditions.conditions[i];
                }
                conditions = combinedConditions;
            }
        }

        public void Remove(int index)
        {
            if (conditions == null || conditions.Length == 0 || index < 0 || index >= conditions.Length) return;

            if (conditions.Length == 1)
            {
                conditions = null; //If there is only one condition, set it to null
            }
            else
            {
                var newConditions = new ConditionCore[conditions.Length - 1];
                for (int i = 0, j = 0; i < conditions.Length; i++)
                {
                    if (i != index)
                    {
                        newConditions[j++] = conditions[i];
                    }
                }
                conditions = newConditions;
            }
        }

        public void RemoveLast() => Remove(conditions.Length - 1);

        public void RemoveFirst() => Remove(0);

        public void Remove(string description)
        {
            if (conditions == null || conditions.Length == 0) return;
            for (int i = 0; i < conditions.Length; i++)
            {
                if (conditions[i].desc.Equals(description))
                {
                    Remove(i);
                    return; // Exit after removing the first match
                }
            }
        }

        public readonly void Gizmos(Component comp)
        {
            if (conditions == null || conditions.Length == 0) return;

            for (int i = 0; i < conditions.Length; i++)
            {
                if (conditions[i].DebugCondition) conditions[i].DrawGizmos(comp);
            }
        }
    }


    /*EXTRAS CONDITIONS Combos */
    [System.Serializable]
    public struct Conditions2Int
    {
        public Conditions2 conditions;
        public IntReference value;
    }

    [System.Serializable]
    public struct Conditions2Float
    {
        public Conditions2 conditions;
        public float value;
    }

    [System.Serializable]
    public struct Conditions2Bool
    {
        public Conditions2 conditions;
        public bool value;
    }

    [System.Serializable]
    public struct Conditions2String
    {
        public Conditions2 conditions;
        public string value;
    }


#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(Conditions2))]
    public class Conditions2Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var active = property.FindPropertyRelative("active");
            EditorGUI.BeginProperty(position, label, property);

            var rect1 = new Rect(position);// rect1.width -= 12;
            var activeRect = new Rect(rect1.x + rect1.width - 65, position.y, 20, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(activeRect, active, GUIContent.none);

            label.text += active.boolValue ? "" : " (Disabled)";

            using (new EditorGUI.DisabledGroupScope(!active.boolValue))
                EditorGUI.PropertyField(rect1, property.FindPropertyRelative("conditions"), label, true);

            //Had to painted twice, otherwise it wont show in the place I wanted
            EditorGUI.PropertyField(activeRect, active, GUIContent.none);

            EditorGUI.EndProperty();
            EditorGUI.indentLevel = indent;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("conditions"), label);
        }
    }
#endif
}
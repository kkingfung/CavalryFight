using MalbersAnimations.Scriptables;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;

#if UNITY_EDITOR 
using UnityEditor;
#endif

namespace MalbersAnimations
{
    [AddComponentMenu("Malbers/Stats/Stats Modifier")]

    public class ModifyStat : MonoBehaviour
    {
        public static readonly string[] Tooltips = {
          "[None] Skips the stat modification",
          "Adds to the stat Value",
          "Sets the stat value",
          "Subtracts from the stat value",
          "Modifies the Stat maximum Value (Adds or Remove)",
          "Set the Stat maximum Value",
          "Enables the Degeneration and sets the Degen Rate Value. If the value is 0, the rate Value wont be changed",
          "Stops the Degeneration",
          "Enables the Regeneration and sets the Regen Rate Value.  If the value is 0, the rate Value wont be changed",
          "Stops the Regeneration",
          "Reset the Stat to the Default Min or Max Value",
          "Reduce the Value of the Stat by a percent",
          "Increase the Value of the Stat by a percent",
          "Sets the multiplier value of the stat",
          "Reset the Stat to the maximum Value",
          "Reset the Stat to the minimun Value",
          "Enable/Disable the Stat",
          "Set Immune",
          "Starts the Regeneration",
          "Restore the Regeneration to its default",
          "Restore the Degeneration to its default",
          "Restore the Value to its default",
          "Restore the Max Value to its default",
          "Restore the Min to its default",
          "Restore the value to its default",
          "Restore the Multiplier to its default",
          "Adds or Remove a value to the Multiplier",
    };


        public Stats stats;

        public List<StatModifier> modifiers = new();

        public virtual void SetStats(GameObject go) => stats = go.FindComponent<Stats>();
        public virtual void SetStats(Component go) => SetStats(go.gameObject);


        /// <summary> Apply All Modifiers to the Stats </summary>
        public virtual void Modify()
        {
            foreach (var statmod in modifiers)
                statmod.ModifyStat(stats);
        }

        public virtual void Modify(GameObject target)
        {
            SetStats(target);
            Modify();
        }
        public virtual void Modify(Component target)
        {
            Modify(target.gameObject);
        }


        /// <summary> Apply a Modifiers to the Stats using its Index</summary>
        public virtual void Modify(int index)
        {
            if (modifiers != null && index < modifiers.Count && modifiers[index].Valid)
                modifiers[index].ModifyStat(stats);
        }
    }



    [System.Serializable]
    public struct StatModifier
    {
        public StatID ID;
        public StatOption modify;
        public FloatReference MinValue;
        public FloatReference MaxValue;
        public BoolReference enable;

        [Tooltip("Base Stat to extract the base Value from")]
        public StatID Base;

        [Tooltip("If true, the Base Stat will be used to extract the value from the owner of the Stat.")]
        public bool useBase;

        public float Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] //CustomPatch: force hint-optimize this method
            get => UnityEngine.Random.Range(MinValue, MaxValue); //Get the value from a random range from min to max

            [MethodImpl(MethodImplOptions.AggressiveInlining)] //CustomPatch: force hint-optimize this method
            set
            {
                MinValue = new(value);
                MaxValue = new(value);
            }
        }
        public StatModifier(StatModifier mod)
        {
            ID = mod.ID;
            modify = mod.modify;
            MinValue = new(mod.MinValue.Value);
            MaxValue = new(mod.MaxValue.Value);
            enable = new(true);
            Base = mod.Base;
            useBase = mod.useBase;
        }

        public StatModifier(StatID id, float value)
        {
            ID = id;
            modify = StatOption.SubstractValue;
            MinValue = new(value);
            MaxValue = new(value);
            enable = new(true);
            Base = null;
            useBase = false;
        }

        /// <summary>There's No ID stat</summary>
        public readonly bool IsNull => ID == null;
        /// <summary>There is an ID stat</summary>
        public readonly bool Valid => ID != null;

        /// <summary>Modify the Stats on an animal </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] //CustomPatch: force hint-optimize this method
        public readonly bool ModifyStat(Stats stats, Stats baseStats = null)
        {
            if (stats && stats.enabled && !IsNull)
            {
                float baseStatValue = (useBase && baseStats != null && Base != null) ? baseStats.Stat_Get(Base).Value : 0f;

                return ModifyStat(stats.Stat_Get(ID), baseStatValue);
            }
            return false;
        }

        /// <summary>Modify the Stats on an animal applying a random value from Min to Max </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool ModifyStat(Stat s, float baseStatValue)
        {
            if (s == null || !s.Active || s.IsImmune) return false; //Do nothing if the stat is null, inmune or disabled)

            if (modify == StatOption.Inmune || modify == StatOption.Enable)
            {
                s.Modify(enable.Value ? 1 : 0, modify);
            }
            else
            {
                s.Modify(Random.Range(MinValue, MaxValue) + baseStatValue, modify);
            }
            return true;

        }

        /// <summary>Modify the Stats on an animal applying a value from Min to Max get by the Normalized parameter</summary>
        public readonly bool ModifyStat(Stat s, float baseStatValue, float Normalized)
        {
            if (s != null)
            {
                if (modify == StatOption.Inmune || modify == StatOption.Enable)
                {
                    s.Modify(enable.Value ? 1 : 0, modify);
                    return true;
                }
                else
                {
                    s.Modify(Mathf.Lerp(MinValue, MaxValue, Normalized) + baseStatValue, modify);
                    return true;
                }
            }
            return false;
        }
        /// <summary>Gets a value from the Modifier (Normalized value from Min to Max)</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] //CustomPatch: force hint-optimize this method
        public readonly float GetValue(float Normalized) => Mathf.Lerp(MinValue, MaxValue, Normalized);

        /// <summary>Gets a value from the Modifier (Random from Min to Max)</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] //CustomPatch: force hint-optimize this method
        public readonly float GetValue() => UnityEngine.Random.Range(MinValue, MaxValue);
    }


    //--------------------EDITOR----------------
#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(StatModifier))]
    public class StatModifierDrawer : PropertyDrawer
    {

        private static GUIContent Icon_Base;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var indent = EditorGUI.indentLevel;
            var height = EditorGUIUtility.singleLineHeight;

            EditorGUI.indentLevel = 0;

            if (Icon_Base == null)
            {
                Icon_Base = EditorGUIUtility.IconContent("d_CreateAddNew");
                Icon_Base.tooltip = "Use the Base Stat from the Owner";
            }

            var ID = property.FindPropertyRelative("ID");
            var MaxValue = property.FindPropertyRelative("MaxValue");
            var MinValue = property.FindPropertyRelative("MinValue");
            var modify = property.FindPropertyRelative("modify");
            var enable = property.FindPropertyRelative("enable");
            var Base = property.FindPropertyRelative("Base");
            var useBase = property.FindPropertyRelative("useBase");


            var Width = useBase.boolValue ? position.width * 0.4f : position.width * 0.5f;

            var line = new Rect(position)
            {
                width = Width,
                height = height,
            };

            var LabelWith = 45;

            EditorGUIUtility.labelWidth = LabelWith;

            if (useBase.boolValue)
            {
                EditorGUI.PropertyField(line, Base,
                    new GUIContent("Base",
                    "Base Stat from the owner to increase the value of the target Stat. Leave empty to start from 0." +
                    "\n E.g. Base Damage of a sword will be extracted from the Owner [Attack] stat, and it will be added to the current modifier value "));

                line.x += Width + 5;
                line.width = Width - 5;
            }


            EditorGUI.PropertyField(line, ID, new GUIContent("Stat", "Stat ID to modify on the Target Stats"));

            EditorGUIUtility.labelWidth = 0;

            if (useBase.boolValue)
            {
                line.x += Width + 2;
                line.width = position.width * 0.2f - 5 - 20;
            }
            else
            {
                line.x += Width + 7;
                line.width = position.width * 0.5f - 5 - 20;
            }

            EditorGUI.PropertyField(line, modify, new GUIContent(string.Empty, ModifyStat.Tooltips[modify.intValue]));


            var UseLocalTargetRect = new Rect(line)
            {
                x = line.x + line.width + 5,
                width = 22,
                height = height,
            };

            var guiColor = GUI.contentColor;
            GUI.contentColor = useBase.boolValue ? Color.green * 2 : GUI.contentColor; //If the useBase is false, then the icon will be faded
            useBase.boolValue = GUI.Toggle(UseLocalTargetRect, useBase.boolValue, Icon_Base, EditorStyles.iconButton);
            GUI.contentColor = guiColor; //Reset the color


            EditorGUI.LabelField(line, new GUIContent("             ", ModifyStat.Tooltips[modify.intValue]));

            var line2 = new Rect(position);
            line2.y += height + 2;



            EditorGUIUtility.labelWidth = LabelWith;

            if (CheckEnum(modify.intValue))
            {
                //Don't Draw anything
            }
            else if (modify.intValue == (int)StatOption.Enable || modify.intValue == (int)StatOption.Inmune)
            {
                EditorGUI.PropertyField(line2, enable, new GUIContent("Value"));
            }
            else
            {
                line2.width = position.width / 2;


                EditorGUI.PropertyField(line2, MinValue, new GUIContent("Min", "Minimun Value"));

                line2.x += position.width / 2 + 7;
                line2.width -= 5;
                EditorGUI.PropertyField(line2, MaxValue, new GUIContent("Max", "Maximum Value"));

            }
            EditorGUIUtility.labelWidth = 0;
            property.serializedObject.ApplyModifiedProperties();

            EditorGUI.EndProperty();
            EditorGUI.indentLevel = indent;
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var modify = property.FindPropertyRelative("modify");

            if (CheckEnum(modify.intValue))
            {
                return 18;
            }
            else
            {
                return (18 * 2) + 2;
            }
        }

        private bool CheckEnum(int modify)
        {
            return
                modify == (int)StatOption.None ||
                modify == (int)StatOption.Reset ||
                modify == (int)StatOption.DegenerateOff ||
                modify == (int)StatOption.RegenerateOff ||
                modify == (int)StatOption.ResetToMax ||
                modify == (int)StatOption.ResetToMin ||
                modify == (int)StatOption.RegenerateOn ||
                modify == (int)StatOption.DegenerateOn ||
                modify == (int)StatOption.RestoreValue ||
                modify == (int)StatOption.RestoreMax ||
                modify == (int)StatOption.RestoreMin ||
                modify == (int)StatOption.RestoreDegeneration ||
                modify == (int)StatOption.RestoreMultiplier ||
                modify == (int)StatOption.RestoreRegeneration;
        }
    }
#endif

}


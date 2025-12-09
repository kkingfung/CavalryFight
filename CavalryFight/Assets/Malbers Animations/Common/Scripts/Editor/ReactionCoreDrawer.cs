using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Reflection;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace MalbersAnimations.Reactions
{

    [CustomPropertyDrawer(typeof(Reaction))]
    public class ReactionCoreDrawer : PropertyDrawer
    {
        const int k_MaxTypePopupLineCount = 8;
        static readonly Type k_UnityObjectType = typeof(UnityEngine.Object);
        // static readonly GUIContent k_NullDisplayName = new(TypeMenuUtility.k_NullDisplayName);

        readonly Dictionary<string, TypePopupCache> m_TypePopups = new();
        readonly Dictionary<string, GUIContent> m_TypeNameCaches = new();

        SerializedProperty m_TargetProperty;

        private static GUIContent Icon_Delete;
        private static GUIContent Icon_Edit;
        private static GUIContent Icon_Global;
        private static GUIContent Icon_Local;
        private static GUIContent Icon_Delay;
        //private bool editName;

        private static GUIContent debugCont;

        public static GUIContent DebugCont
        {
            get
            {
                debugCont ??= new GUIContent(EditorGUIUtility.IconContent("d_debug"));
                return debugCont;
            }
        }

        public readonly GUIContent DelayContent = new("☼", "Delay before the reaction is executed");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            FindIcons();

            var isNull = property.managedReferenceValue == null;
            var Active = property.FindPropertyRelative("Active");
            var delay = property.FindPropertyRelative("delay");
            var desc = property.FindPropertyRelative("desc");
            var useLocal = property.FindPropertyRelative("useLocal");
            var localTarget = property.FindPropertyRelative("localTarget");

            string targetName = label.text;

            if (!isNull)
            {
                var targetType = property.managedReferenceValue.GetType();
                targetName = $"{(property.managedReferenceValue as Reaction).DynamicName}";

                var att = targetType.GetCustomAttribute<MDescriptionAttribute>(false); //Find the correct name

                if (!string.IsNullOrEmpty(desc.stringValue))
                {
                    targetName = $" {desc.stringValue}";
                }
                else if (att != null)
                {
                    desc.stringValue = att.Description;
                    targetName = $" {att.Description}";
                }

                label.text = "       " + targetName; //Add the space for the active option

                // if (desc.isExpanded) //Clear the label when the description is being edited
                label.text = string.Empty;  //Make the label to be empty, I'll draw it later on
            }
            else
            {
                label.text = targetName.Replace("Element", "Reaction");
            }

            // targetName = Regex.Replace(targetName, "(?<!^)([A-Z])", " $1");
            // targetName = $"[{targetName}]";


            EditorGUIUtility.labelWidth = 0;

            label = EditorGUI.BeginProperty(position, label, property);
            {
                var width = 25;
                var popupPosition = new Rect(position);
                popupPosition.width -= EditorGUIUtility.labelWidth;
                popupPosition.x += EditorGUIUtility.labelWidth;
                popupPosition.height = EditorGUIUtility.singleLineHeight;

                if (!isNull)
                {
                    var first = position.x + popupPosition.width + EditorGUIUtility.labelWidth - width;

                    #region Rects

                    var Height = EditorGUIUtility.singleLineHeight - 2;

                    var RemoveRect = new Rect(position)
                    {
                        height = Height,
                        y = position.y,
                        width = width,
                        x = first
                    };

                    var DescripRect = new Rect(position)
                    {
                        height = Height,
                        y = position.y,
                        width = width,
                        x = RemoveRect.x - width
                    };


                    var DelayRect = new Rect(position)
                    {
                        height = Height + 1,
                        y = position.y + 1,
                        width = width + 5,
                        x = DescripRect.x - width - 2 - 5
                    };

                    //----------
                    var UseLocalTargetRect = new Rect(position)
                    {
                        height = Height,
                        y = position.y,
                        width = width + 2,
                        x = DelayRect.x - width - 25
                    };

                    var DelayIconRect = new Rect(position)
                    {
                        height = Height,
                        y = position.y + 2,
                        width = width + 2,
                        x = DelayRect.x - width + 5
                    };

                    var TargetRect2 = new Rect(position)
                    {
                        height = Height + 2,
                        y = position.y + 1,
                        width = position.width - EditorGUIUtility.labelWidth - 138,
                        x = position.x + EditorGUIUtility.labelWidth + 2,
                    };

                    var actRect = new Rect(position)
                    {
                        height = Height,
                        y = position.y + 2,
                        width = width - 4,
                        x = position.x + 4,
                    };

                    var TextRect = new Rect(position)
                    {
                        height = Height + 2,
                        y = position.y + 1,
                        width = !useLocal.boolValue ? position.width - 165 : EditorGUIUtility.labelWidth - 22,
                        x = actRect.x + 15,
                    };

                    var style = new GUIStyle(EditorStyles.toolbarButton)
                    {
                        fontStyle = FontStyle.Bold
                    };
                    #endregion

                    if (desc.isExpanded)
                        desc.stringValue = EditorGUI.TextField(TextRect, desc.stringValue);
                    else
                        GUI.Label(TextRect, targetName);

                    // if (Target != null) //Draw the Target Global/Local
                    useLocal.boolValue = GUI.Toggle(UseLocalTargetRect, useLocal.boolValue, useLocal.boolValue ? Icon_Local : Icon_Global, style);

                    Active.boolValue = GUI.Toggle(actRect, Active.boolValue, GUIContent.none);

                    GUI.Label(DelayIconRect, Icon_Delay, EditorStyles.iconButton);

                    delay.floatValue = EditorGUI.FloatField(DelayRect, GUIContent.none, delay.floatValue);


                    if (useLocal.boolValue)
                    {
                        var typ = (property.managedReferenceValue as Reaction).ReactionType;
                        EditorGUI.ObjectField(TargetRect2, localTarget, typ, GUIContent.none); //Draw the Local Target and cast it as the Reaction Type
                    }

                    var dC = GUI.color;
                    GUI.color = desc.isExpanded ? Color.blue + Color.white : dC;
                    desc.isExpanded = GUI.Toggle(DescripRect, desc.isExpanded, Icon_Edit, style);
                    GUI.color = dC;


                    if (GUI.Button(RemoveRect, Icon_Delete, style))
                    {
                        property.managedReferenceValue = null;
                        property.serializedObject.ApplyModifiedProperties();
                    }
                }

                if (isNull)
                {
                    var propertyName = TypePopupCache.GetTypeName(property, m_TypeNameCaches);

                    if (EditorGUI.DropdownButton(popupPosition, propertyName, FocusType.Keyboard))
                    {
                        TypePopupCache popup = GetTypePopup(property);
                        m_TargetProperty = property;
                        popup.TypePopup.Show(popupPosition);
                    }
                }
                // Draw the managed reference property.
                EditorGUI.PropertyField(position, property, label, true);
            }
            EditorGUI.EndProperty();
            EditorGUI.indentLevel = indent;
        }

        private static void FindIcons()
        {
            if (Icon_Delete == null)
            {
                Icon_Delete = EditorGUIUtility.IconContent("d_TreeEditor.Trash");
                Icon_Delete.tooltip = "Clear the Condition";
            }
            if (Icon_Global == null)
            {
                Icon_Global = EditorGUIUtility.IconContent("d_ToolHandleGlobal@2x");
                Icon_Global.tooltip = "Dynamic Target. The target for the reaction will be found dynamically";
            }
            if (Icon_Local == null)
            {
                Icon_Local = EditorGUIUtility.IconContent("d_ToolHandleLocal@2x");
                Icon_Local.tooltip = "Local Target. The target for the reaction will be set in the Editor and it will not be changed";
            }

            if (Icon_Delay == null)
            {
                Icon_Delay = EditorGUIUtility.IconContent("d_UnityEditor.AnimationWindow@2x");
                Icon_Delay.tooltip = "Delay the Reaction for x Seconds";
            }
            if (Icon_Edit == null)
            {
                Icon_Edit = MalbersEditor.Icon_Edit;
                Icon_Edit.tooltip = "Edit the reaction Description";
            }
        }

        TypePopupCache GetTypePopup(SerializedProperty property)
        {
            // Cache this string. This property internally call Assembly.GetName, which result in a large allocation.
            string managedReferenceFieldTypeName = property.managedReferenceFieldTypename;

            if (!m_TypePopups.TryGetValue(managedReferenceFieldTypeName, out TypePopupCache result))
            {
                var state = new AdvancedDropdownState();

                Type baseType = MSerializedTools.GetType(managedReferenceFieldTypeName);
                var popup = new AdvancedTypePopup(
                    TypeCache.GetTypesDerivedFrom(baseType).Append(baseType).Where(p =>
                        (p.IsPublic || p.IsNestedPublic) &&
                        !p.IsAbstract &&
                        !p.IsGenericType &&
                        !k_UnityObjectType.IsAssignableFrom(p) &&
                        Attribute.IsDefined(p, typeof(SerializableAttribute))
                    ),
                    k_MaxTypePopupLineCount, state);

                popup.OnItemSelected += item =>
                {
                    Type type = item.Type;
                    object obj = m_TargetProperty.SetManagedReference(type);
                    m_TargetProperty.isExpanded = (obj != null);
                    m_TargetProperty.serializedObject.ApplyModifiedProperties();
                    m_TargetProperty.serializedObject.Update();
                };

                result = new TypePopupCache(popup, state);
                m_TypePopups.Add(managedReferenceFieldTypeName, result);
            }
            return result;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, true) + 2;
        }
    }
}
#endif

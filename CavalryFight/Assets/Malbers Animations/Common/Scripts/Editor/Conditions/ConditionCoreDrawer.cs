using MalbersAnimations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Reflection;
using MalbersAnimations.Conditions;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;

[CustomPropertyDrawer(typeof(ConditionCore))]
public class ConditionCoreDrawer : PropertyDrawer
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
    private static GUIContent Icon_InvertOff;
    private static GUIContent Icon_InvertOn;
    //private bool editName;

    private static GUIContent debugCont;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        FindIcons();

        var blueColor = MTools.MBlue * 2;
        var greenColor = MTools.MGreen * 2;

        var isNull = property.managedReferenceValue == null;
        var invert = property.FindPropertyRelative("invert");
        var desc = property.FindPropertyRelative("desc");
        //var Condition = property.FindPropertyRelative("Condition");
        var Target = property.FindPropertyRelative("Target");
        var LocalTarget = property.FindPropertyRelative("LocalTarget");
        var debug = property.FindPropertyRelative("debug");

        var rectBox = new Rect(position)
        {
            x = position.x - 13,
        };


        GUI.Box(rectBox, GUIContent.none, EditorStyles.toolbar); // Draw a box around the property

        if (!isNull)
        {
            var type = property.managedReferenceValue.GetType();
            var att = type.GetCustomAttribute<MDescriptionAttribute>(false); //Find the correct name

            //  string targetName = label.text;

            if (desc.stringValue != string.Empty)
            {
                label.text = $" {desc.stringValue}";
            }
            else if (att != null)
            {
                desc.stringValue = att.Description;
            }
            else
            {
                label.text = $"{(property.managedReferenceValue as ConditionCore).DynamicName}";

                //if (Target != null && Target.objectReferenceValue != null && LocalTarget.boolValue)
                //{
                //    targetName = Target.objectReferenceValue.name;
                //    label.text += $" on [{targetName}]";
                //}
            }

            if (desc.isExpanded) label.text = string.Empty;
            if (invert.boolValue) label.text = "[NOT] " + label.text;
        }

        label = EditorGUI.BeginProperty(position, label, property);
        {
            var width = 25;
            var popupPosition = new Rect(position);
            popupPosition.width -= EditorGUIUtility.labelWidth;
            popupPosition.x += EditorGUIUtility.labelWidth;
            popupPosition.height = EditorGUIUtility.singleLineHeight;

            if (!isNull)
            {
                var OrAnd = property.FindPropertyRelative("OrAnd");

                var dC = GUI.color;

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

                var DebugRect = new Rect(position)
                {
                    height = Height,
                    y = position.y,
                    width = width,
                    x = RemoveRect.x - width
                };


                var EditRect = new Rect(position)
                {
                    height = Height,
                    y = position.y,
                    width = width,
                    x = DebugRect.x - width
                };

                //----------
                var LocalTargetRect = new Rect(position)
                {
                    height = Height,
                    y = position.y,
                    width = width,
                    x = EditRect.x - width
                };

                var invRect = new Rect(position)
                {
                    height = Height,
                    y = position.y,
                    width = width,
                    x = LocalTargetRect.x - width
                };

                var andOrRect = new Rect(position)
                {
                    height = Height,
                    y = position.y,
                    width = width + 10,
                    x = invRect.x - width - 10
                };

                var TextRect = new Rect(position)
                {
                    height = Height + 3,
                    y = position.y,
                    width = position.width - 240,
                    x = 1 + (invert.boolValue ? position.x + 50 : position.x + 15)
                };

                #endregion

                var style = new GUIStyle(EditorStyles.toolbarButton)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                };

                if (desc.isExpanded)
                    desc.stringValue = EditorGUI.TextField(TextRect, desc.stringValue);

                if (Target != null) //Draw the Target Global/Local
                {
                    var bgc = GUI.backgroundColor;
                    GUI.backgroundColor = LocalTarget.boolValue ? blueColor : GUI.backgroundColor;
                    LocalTarget.boolValue = GUI.Toggle(LocalTargetRect, LocalTarget.boolValue, LocalTarget.boolValue ? Icon_Local : Icon_Global, style);
                    GUI.backgroundColor = bgc;
                }

                var orAndContent = new GUIContent(OrAnd.boolValue ? "OR" : "AND",
                    OrAnd.boolValue ? "OR. First Condition will be ignored" : "AND. First Condition will be ignored");


                if (!property.propertyPath.EndsWith("data[0]")) //Do not  AND- OR if if this is the first condition on the list
                {
                    GUI.color = OrAnd.boolValue ? blueColor : greenColor;
                    OrAnd.boolValue = GUI.Toggle(andOrRect, OrAnd.boolValue, orAndContent, style);
                    GUI.color = dC;
                }

                GUI.color = invert.boolValue ? Color.red : dC;
                invert.boolValue = GUI.Toggle(invRect, invert.boolValue, invert.boolValue ? Icon_InvertOn : Icon_InvertOff, style);
                GUI.color = dC;

                GUI.color = debug.boolValue ? Color.red + Color.white : dC;
                debug.boolValue = GUI.Toggle(DebugRect, debug.boolValue, DebugCont, style);
                GUI.color = dC;

                GUI.color = desc.isExpanded ? blueColor : dC;
                desc.isExpanded = GUI.Toggle(EditRect, desc.isExpanded, Icon_Edit, style);
                GUI.color = dC;

                position.y += 2;

                if (GUI.Button(RemoveRect, Icon_Delete, style))
                {
                    property.managedReferenceValue = null;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }

            var propertyName = TypePopupCache.GetTypeName(property, m_TypeNameCaches);

            if (isNull)
            {
                var guiColor = GUI.color;
                GUI.color = MTools.MRed;

                if (EditorGUI.DropdownButton(popupPosition, propertyName, FocusType.Keyboard))
                {
                    TypePopupCache popup = GetTypePopup(property);
                    m_TargetProperty = property;
                    popup.TypePopup.Show(popupPosition);
                }

                GUI.color = guiColor;
            }

            // Draw the managed reference property.
            EditorGUI.PropertyField(position, property, label, true);
        }
        EditorGUI.EndProperty();
        EditorGUI.indentLevel = indent;
    }


    public static GUIContent DebugCont
    {
        get
        {
            debugCont ??= new GUIContent(EditorGUIUtility.IconContent("d_debug"));
            return debugCont;
        }
    }

    private static void FindIcons()
    {
        if (Icon_Delete == null)
        {
            Icon_Delete = EditorGUIUtility.IconContent("d_TreeEditor.Trash");
            Icon_Delete.tooltip = "Clear the Condition";
        }
        if (Icon_InvertOff == null)
        {
            Icon_InvertOff = EditorGUIUtility.IconContent("console.erroricon.inactive.sml@2x");
            Icon_InvertOff.tooltip = "Invert Condition. The result will be the opposite";
        }
        if (Icon_InvertOn == null)
        {
            Icon_InvertOn = EditorGUIUtility.IconContent("console.erroricon.sml@2x");
            Icon_InvertOn.tooltip = "Invert Condition. The result will be the opposite";
        }
        if (Icon_Global == null)
        {
            Icon_Global = EditorGUIUtility.IconContent("d_ToolHandleGlobal@2x");
            Icon_Global.tooltip = "Dynamic Target. The target for the condition will be found dynamically";
        }
        if (Icon_Local == null)
        {
            Icon_Local = EditorGUIUtility.IconContent("d_ToolHandleLocal@2x");
            Icon_Local.tooltip = "Local Target. The target for the condition will be set in the Editor and it will not be changed";
        }
        if (Icon_Edit == null)
        {
            Icon_Edit = MalbersEditor.Icon_Edit;
            Icon_Edit.tooltip = "Edit the Condition Description";
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
        return EditorGUI.GetPropertyHeight(property, true);
    }
}
#endif

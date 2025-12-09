using UnityEngine;
using MalbersAnimations.Scriptables;



#if UNITY_EDITOR
using UnityEditor;
#endif


namespace MalbersAnimations
{
    [System.Serializable]
    public class IDEnable<T> where T : IDs
    {
        public T ID;
        public bool enable = true;
    }

    [System.Serializable]
    public class IDString<T> where T : IDs
    {
        public bool active;
        public T ID;
        public StringReference value;

        public bool Value { get; set; }
    }


#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(IDEnable<>), true)]
    public class IDEnableDrawer : IDDrawer
    {
        protected override void DrawProperty(Rect newPos, SerializedProperty property)
        {
            var IDRect = new Rect(newPos);
            IDRect.width -= 25;

            var toggleRect = new Rect(newPos);
            toggleRect.x = IDRect.x + IDRect.width + 5;
            toggleRect.width = 20;

            var ID = property.FindPropertyRelative("ID");
            var enable = property.FindPropertyRelative("enable");

            EditorGUI.PropertyField(IDRect, ID, GUIContent.none, false);
            EditorGUI.PropertyField(toggleRect, enable, GUIContent.none, false);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            popupStyle ??= new(GUI.skin.GetStyle("PaneOptions"))
            {
                imagePosition = ImagePosition.ImageOnly
            };

            label = EditorGUI.BeginProperty(position, label, property);

            if (label.text.Contains("Element"))
            {
                position.x += 12;
                position.width -= 12;
            }
            else
                position = EditorGUI.PrefixLabel(position, label);

            EditorGUI.BeginChangeCheck();

            float height = EditorGUIUtility.singleLineHeight;


            // Calculate rect for configuration button
            Rect buttonRect = new(position);
            buttonRect.yMin += popupStyle.margin.top;
            buttonRect.width = popupStyle.fixedWidth + popupStyle.margin.right;
            buttonRect.x -= 20;
            buttonRect.height = height;

            // Store old indent level and set it to 0, the PrefixLabel takes care of it
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            if (EditorGUI.DropdownButton(buttonRect, GUIContent.none, FocusType.Passive, popupStyle))
            {
                var ID = property.FindPropertyRelative("ID");
                FindAllInstances(ID);  //Find the instances only when the dropdown is pressed
                menu.DropDown(buttonRect);
            }

            position.height = EditorGUIUtility.singleLineHeight;

            DrawProperty(position, property);

            if (EditorGUI.EndChangeCheck())
                property.serializedObject.ApplyModifiedProperties();

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
#endif


    //--------------------------------------


#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(IDString<>), true)]
    public class IDValueDrawer : IDDrawer
    {
        protected override void DrawProperty(Rect pos, SerializedProperty property)
        {
            var IDRect = new Rect(pos)
            {
                x = pos.x + 20,
                width = pos.width * 0.5f - 10
            };

            var valueRect = new Rect(pos)
            {
                x = IDRect.x + IDRect.width + 20,
                width = pos.width - 40
            };
            valueRect.width = valueRect.width * 0.5f - 10;

            var activeRect = new Rect(pos)
            {
                x = IDRect.x - 40,
                width = 20
            };

            var ID = property.FindPropertyRelative("ID");
            var active = property.FindPropertyRelative("active");
            var value = property.FindPropertyRelative("value");

            EditorGUI.PropertyField(IDRect, ID, GUIContent.none, false);
            EditorGUI.PropertyField(valueRect, value, GUIContent.none, false);
            EditorGUI.PropertyField(activeRect, active, GUIContent.none, false);

        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            popupStyle ??= new(GUI.skin.GetStyle("PaneOptions"))
            {
                imagePosition = ImagePosition.ImageOnly
            };

            label = EditorGUI.BeginProperty(position, label, property);

            if (label.text.Contains("Element"))
            {
                position.x += 12;
                position.width -= 12;
            }
            else
                position = EditorGUI.PrefixLabel(position, label);

            EditorGUI.BeginChangeCheck();

            float height = EditorGUIUtility.singleLineHeight;


            // Calculate rect for configuration button
            Rect buttonRect = new(position);
            buttonRect.yMin += popupStyle.margin.top;
            buttonRect.width = popupStyle.fixedWidth + popupStyle.margin.right;
            buttonRect.x -= 20;
            buttonRect.height = height;

            // Store old indent level and set it to 0, the PrefixLabel takes care of it
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            //if (EditorGUI.DropdownButton(buttonRect, GUIContent.none, FocusType.Passive, popupStyle))
            //{
            //    var ID = property.FindPropertyRelative("ID");
            //    FindAllInstances(ID);  //Find the instances only when the dropdown is pressed
            //    menu.DropDown(buttonRect);
            //}

            position.height = EditorGUIUtility.singleLineHeight;

            DrawProperty(position, property);

            if (EditorGUI.EndChangeCheck())
                property.serializedObject.ApplyModifiedProperties();

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
#endif

}

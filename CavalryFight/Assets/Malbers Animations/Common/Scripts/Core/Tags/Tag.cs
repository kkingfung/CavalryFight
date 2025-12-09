using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MalbersAnimations
{
    [CreateAssetMenu(menuName = "Malbers Animations/Tag", fileName = "New Tag", order = 3000)]
    public class Tag : IDs
    {
        public string TagName;

        private void OnEnable() => ID = name.GetHashCode();
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(Tag))]
    public class TagEditor : Editor
    {
        SerializedProperty ID, TagName;

        void OnEnable()
        {
            ID = serializedObject.FindProperty("ID");
            TagName = serializedObject.FindProperty("TagName");

            if (!Application.isPlaying)
            {
                var tag = (Tag)target;
                var newName = tag.name;
                if (TagName.stringValue != newName)
                {
                    TagName.stringValue = newName;
                    ID.intValue = newName.GetHashCode();
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox("Tag ID is generated using name.GetHashCode().", MessageType.None);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(ID);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.PropertyField(TagName);
            serializedObject.ApplyModifiedProperties();
        }
    }

    public class TagNameSetter : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] _, string[] __, string[] ___)
        {
            foreach (var path in importedAssets)
            {
                var tag = AssetDatabase.LoadAssetAtPath<Tag>(path);
                if (tag && tag.TagName != tag.name)
                {
                    tag.TagName = tag.name;
                    EditorUtility.SetDirty(tag);
                }
            }
        }
    }
#endif
}
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(BlazeAISpareState))]
public class BlazeAISpareStateInspector : Editor
{
    SerializedProperty spareStates;

    void OnEnable()
    {
        spareStates = serializedObject.FindProperty("spareStates");
    }

    public override void OnInspectorGUI () 
    {
        EditorGUILayout.PropertyField(spareStates);
        serializedObject.ApplyModifiedProperties();
    }
}
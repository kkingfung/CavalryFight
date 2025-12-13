using UnityEditor;

[CustomEditor(typeof(BlazeAIEnemyManager))]
public class BlazeAIEnemyManagerInspector : Editor
{
    SerializedProperty attackTimer,
    callEnemies;

    void OnEnable()
    {
        attackTimer = serializedObject.FindProperty("attackTimer");
        callEnemies = serializedObject.FindProperty("_callEnemies");
    }

    public override void OnInspectorGUI () 
    {
        EditorGUILayout.LabelField("Set AIs Attack Time Cycle", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(attackTimer);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Set If AIs Can Attack", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(callEnemies);

        serializedObject.ApplyModifiedProperties();
    }
}
using UnityEditor;

[CustomEditor(typeof(BlazeAIDistraction))]
public class BlazeAIDistractionInspector : Editor
{
    SerializedProperty distractOnAwake,
    agentLayers,
    distractionRadius,
    blockingLayers,
    distractOnlyPrioritizedAgent;

    void OnEnable()
    {
        distractOnAwake = serializedObject.FindProperty("distractOnAwake");
        agentLayers = serializedObject.FindProperty("agentLayers");
        distractionRadius = serializedObject.FindProperty("distractionRadius");
        blockingLayers = serializedObject.FindProperty("blockingLayers");
        distractOnlyPrioritizedAgent = serializedObject.FindProperty("distractOnlyPrioritizedAgent");
    }

    public override void OnInspectorGUI () 
    {
        EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(distractOnAwake);
        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Layers & Radius", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(agentLayers);
        EditorGUILayout.PropertyField(distractionRadius);
        EditorGUILayout.PropertyField(blockingLayers);
        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Distraction Options", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(distractOnlyPrioritizedAgent);

        serializedObject.ApplyModifiedProperties();
    }
}
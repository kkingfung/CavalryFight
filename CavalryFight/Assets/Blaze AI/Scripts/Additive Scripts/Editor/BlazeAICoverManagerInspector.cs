using UnityEngine;
using UnityEditor;
using BlazeAISpace;

[CustomEditor(typeof(BlazeAICoverManager))]
public class BlazeAICoverManagerInspector : Editor
{
    #region EDITOR VARIABLES

    BlazeAICoverManager script;

    #endregion

    #region SERIALIZED PROPERTIES

    SerializedProperty coverPositions,
    showCoverPositions;

    #endregion

    void OnEnable()
    {
        script = (BlazeAICoverManager) target;
        coverPositions = serializedObject.FindProperty("coverPositions");
        showCoverPositions = serializedObject.FindProperty("showCoverPositions");
    }

    void OnSceneGUI()
    {
        if (script == null) return;
        DrawCoverPoints();
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("If the automated cover points are not accurate then you can set them manually", BlazeAIEditor.BoxStyle());
        EditorGUILayout.LabelField("Manual Cover Positions (optional)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(coverPositions);
        EditorGUILayout.PropertyField(showCoverPositions);
        serializedObject.ApplyModifiedProperties();
    }

    public void DrawCoverPoints()
    {
        if (!script.showCoverPositions)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        RaycastHit hit;

        int max = script.coverPositions.Length;
        for(int i=0; i<max; i++)
        {
            Vector3 currentPoint = script.coverPositions[i];

            if (Physics.Raycast(currentPoint, -Vector3.up, out hit, Mathf.Infinity, Physics.AllLayers)) 
            {
                Debug.DrawRay(currentPoint, hit.point - currentPoint, Color.yellow, 0.1f);
                UnityEditor.Handles.color = Color.yellow;
                UnityEditor.Handles.DrawWireDisc(hit.point, Vector3.up, 0.5f);
                UnityEditor.Handles.Label(hit.point + new Vector3(0, 1, 0), "CoverPos " + (i));
            }

            EditorGUI.BeginChangeCheck();

            currentPoint = script.coverPositions[i];
            Vector3 newTargetPosition = Handles.PositionHandle(currentPoint, Quaternion.identity);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(script, "Set Cover Position");
                script.coverPositions[i] = newTargetPosition;
            }
        }
    }
}
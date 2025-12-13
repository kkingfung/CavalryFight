using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BlazeAIStateTracker))]
public class BlazeAIStateTrackerInspector : Editor
{
    BlazeAIStateTracker script;

    public override void OnInspectorGUI()
    {
        script = (BlazeAIStateTracker) target;
        string text;
        bool blazeExist = script.GetBlaze();

        if (!Application.isPlaying) {
            text = "Place on a gameobject where Blaze AI exists. Then on game start this will automatically track the state as it changes.";
        }
        else {
            if (blazeExist) {
                text = script.GetState();
            }
            else {
                text = "Place on a gameobject where Blaze AI exists. Then on game start this will automatically track the state as it changes.";
            }
        }

        GUILayout.Button(text, BoxStyling(blazeExist && Application.isPlaying), GUILayout.MinWidth(70), GUILayout.Height(200));
        EditorGUILayout.Space();

        if (blazeExist) {
            EditorGUILayout.LabelField("Blaze AI detected", LabelStyling(true), GUILayout.MinWidth(70), GUILayout.Height(30));
        }
        else {
            EditorGUILayout.LabelField("No Blaze AI detected", LabelStyling(false), GUILayout.MinWidth(70), GUILayout.Height(30));
        }
    }

    GUIStyle BoxStyling(bool isPlaying)
    {
        var boxStyle = new GUIStyle();
        
        if (isPlaying) boxStyle.fontSize = 25;
        else boxStyle.fontSize = 18;

        boxStyle.margin = new RectOffset(4,4,2,2);
        boxStyle.alignment = TextAnchor.MiddleCenter;
        boxStyle.normal.background = BlazeAIEditor.MakeTex(1, 1, new Color(0.15f, 0.15f, 0.15f));
        boxStyle.normal.textColor = new Color(1, 0.5f, 0);
        boxStyle.active.textColor = new Color(1, 0.5f, 0);
        boxStyle.wordWrap = true;

        return boxStyle;
    }

    GUIStyle LabelStyling(bool isDetected)
    {
        var labelStyle = new GUIStyle();
        labelStyle.fontSize = 15;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.normal.background = BlazeAIEditor.MakeTex(1, 1, new Color(0.1f, 0.1f, 0.1f));
        
        if (isDetected) labelStyle.normal.textColor = new Color(0, 1, 0);
        else labelStyle.normal.textColor = new Color(1, 0, 0);
        
        labelStyle.wordWrap = true;
        return labelStyle;
    }
}
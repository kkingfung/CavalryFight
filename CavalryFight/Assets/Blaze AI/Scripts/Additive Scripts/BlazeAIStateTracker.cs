#if UNITY_EDITOR
using UnityEngine;
using BlazeAISpace;

[AddComponentMenu("Blaze AI/Additive Scripts/Blaze AI State Tracker")]
public class BlazeAIStateTracker : MonoBehaviour
{
    BlazeAI blaze;

    void Awake()
    {
        blaze = GetComponent<BlazeAI>();    
    }

    public string GetState()
    {
        string text;
        SpareState spareState = blaze.CurrentSpareState();

        if (spareState != null) {
            text = $"Spare State ({spareState.stateName})";
            return text;
        }

        text = blaze.state.ToString();
        return text;
    }

    public bool GetBlaze()
    {
        blaze = GetComponent<BlazeAI>();
        if (blaze == null) return false;
        
        return true;
    }
}
#endif
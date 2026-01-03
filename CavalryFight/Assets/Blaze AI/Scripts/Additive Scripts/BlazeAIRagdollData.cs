using UnityEngine;

namespace BlazeAISpace
{
    public class BlazeAIRagdollData : MonoBehaviour
    {
        // This script is used to save important transform data for ragdoll
        [HideInInspector] public Vector3 originalPos;
        [HideInInspector] public Quaternion originalRot;
    }
}

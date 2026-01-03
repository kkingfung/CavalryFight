using UnityEngine;

namespace BlazeAISpace
{
    [AddComponentMenu("Blaze AI/Additive Scripts/Blaze AI Cover Manager")]
    public class BlazeAICoverManager : MonoBehaviour
    {
        [Tooltip("Set cover positions for custom and uneven meshes where the automated system may have a hard time placing the AI in a good spot. Using this you can manually set best position for each direction.")]
        public Vector3[] coverPositions;
        public bool showCoverPositions;

        [HideInInspector] public Transform occupiedBy;


        void OnValidate()
        {
            if (coverPositions != null)
            {
                if (coverPositions.Length == 1 && coverPositions[0] == Vector3.zero)
                {
                    coverPositions[0] = transform.position;
                }
            }
        }
    }
}
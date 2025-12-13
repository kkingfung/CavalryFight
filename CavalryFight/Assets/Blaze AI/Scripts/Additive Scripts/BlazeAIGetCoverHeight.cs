using UnityEngine;

namespace BlazeAISpace
{
    public class BlazeAIGetCoverHeight : MonoBehaviour
    {
        public float heightOfObject;

        public void GetHeight()
        {
            Collider coll = GetComponent<Collider>();

            if (coll == null) {
                heightOfObject = -1;
                return;
            }

            heightOfObject = coll.bounds.size.y;
        }
    }
}

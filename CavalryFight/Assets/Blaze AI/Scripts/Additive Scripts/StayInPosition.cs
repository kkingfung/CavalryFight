using UnityEngine;

namespace BlazeAISpace
{
    public class StayInPosition : MonoBehaviour
    {
        BlazeAI blaze;

        void Start()
        {
            blaze = GetComponent<BlazeAI>();
        }
        
        void Update()
        {
            if (blaze.state == BlazeAI.State.normal || blaze.state == BlazeAI.State.alert) {
                blaze.StayIdle();
            }
        }
    }
}
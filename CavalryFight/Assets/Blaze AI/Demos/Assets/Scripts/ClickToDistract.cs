using UnityEngine;

namespace BlazeAIDemo
{
    public class ClickToDistract : MonoBehaviour
    {
        public AudioSource distractionAudio;
        BlazeAIDistraction distractionScript;
        
        void Start() {
            distractionScript = GetComponent<BlazeAIDistraction>();
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetMouseButtonDown(0)){
                if (distractionAudio) distractionAudio.Play();
                distractionScript.TriggerDistraction();
            }
        }
    }
}

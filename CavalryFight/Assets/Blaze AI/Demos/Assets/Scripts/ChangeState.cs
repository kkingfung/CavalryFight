using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeState : MonoBehaviour
{
    BlazeAI blaze;

    // Start is called before the first frame update
    void Start()
    {
        blaze = GetComponent<BlazeAI>();    
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E)) {
            blaze.ChangeState("normal");
        }

        if (Input.GetKeyDown(KeyCode.R)) {
            blaze.ChangeState("alert");
        }
    }
}

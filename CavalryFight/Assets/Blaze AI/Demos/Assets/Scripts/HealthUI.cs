using UnityEngine;
using TMPro;
using BlazeAIDemo;

public class HealthUI : MonoBehaviour
{
    public TextMeshProUGUI healthText;
    public BlazeAIDemo.Health blazeHealth;
    

    // Update is called once per frame
    void Update()
    {
        healthText.text = "Health: " + blazeHealth.currentHealth;
    }
}


using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIPurgeSystem : MonoBehaviour
{
    [Header("Cooldown Settings")]
    public float cooldownTime = 4.5f;
    private bool isCoolingDown = false;
    private float currentCooldown = 0f;

    [Header("UI Feedback Links")]
    public Button purgeButton; // The clickable Button component
    public Image cooldownOverlay; // The dark sweep effect
    public TextMeshProUGUI buttonText; // The text inside the button
    
    private Color originalTextColor;

    void Start()
    {
        if (buttonText != null) originalTextColor = buttonText.color;
        if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0f; // Hide overlay on start
    }

    void Update()
    {
        if (isCoolingDown)
        {
            // Count down the timer
            currentCooldown -= Time.deltaTime;
            
            // Update the visual sweep (math: current time / total time = percentage from 0.0 to 1.0)
            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = currentCooldown / cooldownTime;
            }

            // Finish the cooldown
            if (currentCooldown <= 0f)
            {
                EndCooldown();
            }
        }
    }

    public void PurgeCorruptedData()
    {
        if (isCoolingDown) return; // Prevent clicking if it's already cooling down

        Debug.Log("--- PURGE SEQUENCE INITIATED ---");
        
        InventoryManager manager = FindAnyObjectByType<InventoryManager>();
        
        if (manager != null)
        {
            manager.ExecuteCleanProtocol();
            Debug.Log("<color=green>M.E.T. Rig: ExecuteCleanProtocol() fired.</color>");
        }
        else
        {
            Debug.LogError("PROXY AI: Cannot find InventoryManager to purge data!");
        }

        StartCooldown();
    }

    private void StartCooldown()
    {
        isCoolingDown = true;
        currentCooldown = cooldownTime;
        
        // Turn off the button and dim the text
        if (purgeButton != null) purgeButton.interactable = false;
        if (buttonText != null) buttonText.color = new Color(0.4f, 0.4f, 0.4f); // Dark gray
        if (cooldownOverlay != null) cooldownOverlay.fillAmount = 1f; // Fill the overlay completely
    }

    private void EndCooldown()
    {
        isCoolingDown = false;
        currentCooldown = 0f;
        
        // Turn the button back on and restore the original red text
        if (purgeButton != null) purgeButton.interactable = true;
        if (buttonText != null) buttonText.color = originalTextColor;
        if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0f; // Hide the overlay
    }
}
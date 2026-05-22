using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the Emergency Clean protocol UI, handling its cooldown and execution.
/// </summary>
public class UIPurgeSystem : MonoBehaviour
{
    [Header("Cooldown Settings")]
    [Tooltip("The duration in seconds before the purge system can be used again.")]
    public float cooldownTime = 4.5f;
    
    private bool isCoolingDown = false;
    private float currentCooldown = 0f;

    [Header("UI Feedback Links")]
    [Tooltip("The clickable button component that triggers the purge.")]
    public Button purgeButton; 
    [Tooltip("The UI image used to visually display the remaining cooldown sweep.")]
    public Image cooldownOverlay; 
    [Tooltip("The text label inside the purge button.")]
    public TextMeshProUGUI buttonText; 
    
    private Color originalTextColor;

    void Awake()
    {
        if (purgeButton == null) purgeButton = GetComponent<Button>();
        if (buttonText == null) buttonText = GetComponentInChildren<TextMeshProUGUI>();
        if (cooldownOverlay == null && transform.Find("CooldownOverlay") != null) 
            cooldownOverlay = transform.Find("CooldownOverlay").GetComponent<Image>();

        if (buttonText != null) originalTextColor = buttonText.color;
    }

    void Start()
    {
        if (cooldownOverlay != null && !isCoolingDown) cooldownOverlay.fillAmount = 0f; 
    }

    void Update()
    {
        if (isCoolingDown)
        {
            currentCooldown -= Time.deltaTime;
            
            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = currentCooldown / cooldownTime;
            }

            if (currentCooldown <= 0f)
            {
                EndCooldown();
            }
        }
    }

    /// <summary>
    /// Initiates the emergency clean protocol in the InventoryManager and starts the local cooldown.
    /// </summary>
    public void PurgeCorruptedData()
    {
        if (isCoolingDown) return; 

        
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ExecuteCleanProtocol();
        }

        StartCooldown();
    }

    private void StartCooldown()
    {
        isCoolingDown = true;
        currentCooldown = cooldownTime;
        
        if (purgeButton != null) purgeButton.interactable = false;
        if (buttonText != null) buttonText.color = new Color(0.4f, 0.4f, 0.4f); 
        if (cooldownOverlay != null) cooldownOverlay.fillAmount = 1f; 
    }

    private void EndCooldown()
    {
        isCoolingDown = false;
        currentCooldown = 0f;
        
        if (purgeButton != null) purgeButton.interactable = true;
        if (buttonText != null) buttonText.color = originalTextColor;
        if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0f; 
    }

    /// <summary>Returns the current cooldown value for saving.</summary>
    public float GetCurrentCooldown()
    {
        return currentCooldown; 
    }

    /// <summary>Restores the UI cooldown state from a loaded save file.</summary>
    public void LoadCooldownState(float savedCooldown)
    {
        currentCooldown = savedCooldown;

        if (currentCooldown > 0f)
        {
            isCoolingDown = true;
            
            if (purgeButton != null) purgeButton.interactable = false;
            if (buttonText != null) buttonText.color = new Color(0.4f, 0.4f, 0.4f);
            
            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = currentCooldown / cooldownTime;
            }
        }
        else
        {
            EndCooldown();
        }
    }
}
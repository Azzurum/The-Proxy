using UnityEngine;

/// <summary>
/// Manages the player's health pool and communicates damage events to the M.E.T. Rig UI.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("The current health of the player.")]
    [SerializeField] private float currentHealth = 100f;
    [Tooltip("The maximum possible health of the player.")]
    [SerializeField] private float maxHealth = 100f;

    [Header("M.E.T. Rig UI Links")]
    [Tooltip("Reference to the UI component that visually represents the player's health state.")]
    [SerializeField] private UIBioFace bioFace;

    /// <summary>
    /// Reduces the player's health by the specified amount and updates the UI.
    /// </summary>
    public void TakeDamage(float damageAmount)
    {
        currentHealth = Mathf.Clamp(currentHealth - damageAmount, 0f, maxHealth);
        UpdateRigMonitors();
    }

    private void UpdateRigMonitors()
    {
        float healthPct = Mathf.Clamp01(currentHealth / maxHealth);
        
        if (bioFace != null)
        {
            bioFace.UpdateFace(healthPct);
        }
    }
}
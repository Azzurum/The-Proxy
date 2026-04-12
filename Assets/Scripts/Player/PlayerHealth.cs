// Example addition to your existing Player Health script
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float currentHealth = 100f;
    public float maxHealth = 100f;

    [Header("M.E.T. Rig UI Links")]
    public UIBioFace bioFace;

    // Call this whenever Kaelen takes damage from an enemy or hazard
    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        UpdateRigMonitors();
    }

    private void UpdateRigMonitors()
    {
        // Calculate health percentage (0.0 to 1.0)
        float healthPct = Mathf.Clamp01(currentHealth / maxHealth);
        
        // Update the Doom-style face
        if (bioFace != null) bioFace.UpdateFace(healthPct);
    }
}
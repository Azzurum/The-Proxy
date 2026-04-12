using UnityEngine;
using TMPro;

public class UIMotherLog : MonoBehaviour
{
    [Header("Log UI References")]
    public TextMeshProUGUI labelText;
    public TextMeshProUGUI contentText;

    private InventoryManager invManager;
    private bool isDead = false;

    void Start()
    {
        invManager = FindAnyObjectByType<InventoryManager>();
        if (invManager != null)
        {
            invManager.OnHealthStateChanged += UpdateMotherLog;
            invManager.BroadcastHealthState(); // Force update on boot
        }
    }

    void OnDestroy()
    {
        if (invManager != null) invManager.OnHealthStateChanged -= UpdateMotherLog;
    }

    public void UpdateMotherLog(float healthPercentage)
    {
        if (labelText == null || contentText == null) return;

        float corruptionLevel = 1f - healthPercentage;

        // 4. GAME OVER FLATLINE
        if (healthPercentage <= 0f)
        {
            if (!isDead)
            {
                labelText.text = "<color=red>SYSTEM FAILURE</color>";
                contentText.text = "> <color=red><b>CRITICAL:</b></color> Host vitals terminated.\n> Neural handshake severed.\n> <b>M.E.T. RIG OFFLINE.</b>";
                isDead = true;
            }
        }
        // 1. STABLE (0% - 20% Corruption)
        else if (corruptionLevel <= 0.2f)
        {
            labelText.text = "SYMBIOTIC LINK // MOTHER-V4";
            contentText.text = "> Connection stable. All systems nominal.\n> <color=#00ffcc><b>Awaiting directives, Custodian.</b></color>";
            isDead = false;
        }
        // 2. WARNING (20% - 60% Corruption)
        else if (corruptionLevel <= 0.6f)
        {
            labelText.text = "<color=#ffaa00>SYMBIOTIC LINK // WARNING</color>";
            contentText.text = "> Proximity alert. Massive anomaly detected on Nav-Link.\n> <color=#ffaa00><b>Minimize digitization noise immediately.</b></color>";
            isDead = false;
        }
        // 3. CRITICAL (60%+ Corruption)
        else
        {
            labelText.text = "<color=red>SYMBIOTIC LINK // CRITICAL</color>";
            contentText.text = "> <color=red><b>CRITICAL: ENTITY IS IN VISUAL RANGE.</b></color>\n> Host survival probability: 4.2%";
            isDead = false;
        }
    }
}
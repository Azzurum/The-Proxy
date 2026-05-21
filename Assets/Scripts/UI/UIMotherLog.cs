using UnityEngine;
using TMPro;

/// <summary>
/// Translates raw health state percentages into corresponding flavor and warning text messages from MOTHER.
/// </summary>
public class UIMotherLog : MonoBehaviour
{
    [Header("Log UI References")]
    public TextMeshProUGUI labelText;
    public TextMeshProUGUI contentText;

    private InventoryManager _invManager;
    private bool _isDead = false;

    private void Start()
    {
        _invManager = FindAnyObjectByType<InventoryManager>();
        if (_invManager != null)
        {
            _invManager.OnHealthStateChanged += UpdateMotherLog;
            _invManager.BroadcastHealthState(); 
        }
    }

    private void OnDestroy()
    {
        if (_invManager != null) _invManager.OnHealthStateChanged -= UpdateMotherLog;
    }

    /// <summary>
    /// Modifies the local text components relying on standard corruption percentages (0.0 - 1.0).
    /// </summary>
    public void UpdateMotherLog(float healthPercentage)
    {
        if (labelText == null || contentText == null) return;

        float corruptionLevel = 1f - healthPercentage;

        if (healthPercentage <= 0f)
        {
            if (!_isDead)
            {
                labelText.text = "<color=red>SYSTEM FAILURE</color>";
                contentText.text = "> <color=red><b>CRITICAL:</b></color> Host vitals terminated.\n> Neural handshake severed.\n> <b>M.E.T. RIG OFFLINE.</b>";
                _isDead = true;
            }
        }
        else if (corruptionLevel <= 0.2f)
        {
            labelText.text = "SYMBIOTIC LINK // CLINICAL";
            contentText.text = "> Connection stable. Bandwidth nominal.\n> <color=#00ffcc><b>Awaiting directives, Custodian.</b></color>";
            _isDead = false;
        }
        else if (corruptionLevel <= 0.6f)
        {
            labelText.text = "<color=#ffaa00>SYMBIOTIC LINK // MANIPULATIVE</color>";
            contentText.text = "> You need me to survive this, Kaelen.\n> <color=#ffaa00><b>Let me in. The vessel approaches.</b></color>";
            _isDead = false;
        }
        else
        {
            labelText.text = "<color=red>SYMBIOTIC LINK // POSSESSIVE</color>";
            contentText.text = "> <color=red><b>Stop fighting it. We are finally one.</b></color>\n> Purging organic resistance...";
            _isDead = false;
        }
    }
}
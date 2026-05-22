using UnityEngine;
using TMPro;

/// <summary>
/// Coordinates the top-level readout panel which highlights details about the currently selected item on the grid.
/// </summary>
public class UIInspectorManager : MonoBehaviour
{
    public static UIInspectorManager Instance;

    [Header("Inspector Bar (Top)")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI substatsText;
    public TextMeshProUGUI descriptionText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        ClearInspector();
    }

    /// <summary>
    /// Clears the inspector panels returning them to an idle state readout.
    /// </summary>
    public void ClearInspector()
    {
        if (titleText != null) titleText.text = "AWAITING I/O";
        if (substatsText != null) substatsText.text = "MASS: N/A | STATUS: UNKNOWN";
        if (descriptionText != null) descriptionText.text = "Select a digitized matter node to view quantum properties and structural analysis.";
    }

    /// <summary>
    /// Populates the inspector interface components with a clicked item's underlying ItemData context.
    /// </summary>
    public void InspectItem(ItemData data)
    {
        if (data == null) return;

        if (titleText != null) titleText.text = data.itemName;
        if (substatsText != null) substatsText.text = data.substats;
        if (descriptionText != null) descriptionText.text = data.description;
    }
}
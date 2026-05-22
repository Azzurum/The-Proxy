using UnityEngine;
using TMPro;

/// <summary>
/// Coordinates the modal UI panel responsible for displaying detailed lore and mechanical stats of a selected inventory item.
/// </summary>
public class ItemInspector : MonoBehaviour
{
    public static ItemInspector Instance;

    [Header("UI References")]
    [Tooltip("The main panel to be toggled on/off.")]
    public GameObject inspectorPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI sizeText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (inspectorPanel != null) inspectorPanel.SetActive(false);
    }

    /// <summary>
    /// Extracts the data from the target draggable item and maps it into the UI elements, then reveals the panel.
    /// </summary>
    public void InspectItem(DraggableItem item)
    {
        if (item == null) return;

        titleText.text = item.itemName;
        descriptionText.text = item.itemDescription;

        if (item.footprint != null)
        {
            sizeText.text = $"{item.footprint.width}×{item.footprint.height}";
        }
        else
        {
            sizeText.text = "N/A";
        }

        inspectorPanel.SetActive(true);
    }

    /// <summary>
    /// Disables the inspector panel visual. Intended to be bound to a UI exit button.
    /// </summary>
    public void CloseInspector()
    {
        if (inspectorPanel != null) inspectorPanel.SetActive(false);
    }
}
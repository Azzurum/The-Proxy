using UnityEngine;
using TMPro;

public class ItemInspector : MonoBehaviour
{
    public static ItemInspector Instance;

    [Header("UI References")]
    public GameObject inspectorPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI sizeText;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Hide it by default when the game starts
        if (inspectorPanel != null) inspectorPanel.SetActive(false);
    }

    public void InspectItem(DraggableItem item)
    {
        if (item == null) return;

        titleText.text = item.itemName;
        descriptionText.text = item.itemDescription;

        // Show size as WxH
        sizeText.text = $"{item.sizeX}×{item.sizeY}";

        inspectorPanel.SetActive(true);
    }

    // Link this to an 'X' button on the UI panel
    public void CloseInspector()
    {
        inspectorPanel.SetActive(false);
    }
}
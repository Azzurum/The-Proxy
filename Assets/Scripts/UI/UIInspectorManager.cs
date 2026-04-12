using UnityEngine;
using TMPro;

public class UIInspectorManager : MonoBehaviour
{
    // A Singleton allows any item to easily find this manager without complicated wiring
    public static UIInspectorManager Instance;

    [Header("Inspector Bar (Top)")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI substatsText;
    public TextMeshProUGUI descriptionText;

    void Awake()
    {
        // Set up the Singleton
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Clear the screen when the game boots
        ClearInspector();
    }

    // Call this to wipe the screens clean to their default states
    public void ClearInspector()
    {
        if (titleText != null) titleText.text = "AWAITING I/O";
        if (substatsText != null) substatsText.text = "MASS: N/A | STATUS: UNKNOWN";
        if (descriptionText != null) descriptionText.text = "Select a digitized matter node to view quantum properties and structural analysis.";
    }

    // Items will call this and pass their data when clicked!
    public void InspectItem(ItemData data)
    {
        if (data == null) return;

        if (titleText != null) titleText.text = data.itemName;
        if (substatsText != null) substatsText.text = data.substats;
        if (descriptionText != null) descriptionText.text = data.description;
    }
}
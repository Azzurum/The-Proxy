using UnityEngine;

public class HotbarManager : MonoBehaviour
{
    public static HotbarManager Instance;

    [Header("Hotbar Array")]
    public HotbarSlot[] quickSlots = new HotbarSlot[3];

    [Header("Stamina Bar")]
    public UnityEngine.UI.Slider staminaBar;
    public UnityEngine.UI.Image staminaFill;
    public PlayerController playerController; // Manually assign if auto-find fails

    [Header("Stamina Color Thresholds")]
    [Range(0f, 1f)] public float yellowThreshold = 0.5f;

    private int currentEquippedIndex = -1; // -1 means hands are empty

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        foreach (var slot in quickSlots)
        {
            slot.ClearSlot();
        }

        // Try to find PlayerController if not manually assigned
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
            Debug.Log($"Searching for PlayerController: {(playerController != null ? "FOUND" : "NOT FOUND")}");
        }

        if (staminaBar != null && staminaFill == null && staminaBar.fillRect != null)
        {
            staminaFill = staminaBar.fillRect.GetComponent<UnityEngine.UI.Image>();
        }

        // Debug logging
        Debug.Log($"HotbarManager Start - staminaBar: {staminaBar}, playerController: {playerController}, staminaFill: {staminaFill}");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) EquipSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) EquipSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) EquipSlot(3);

        // Update stamina bar
        if (staminaBar != null && playerController != null)
        {
            float normalized = playerController.SprintMeter / playerController.SprintMeterThreshold;
            staminaBar.value = Mathf.Clamp01(normalized);

            Debug.Log($"Stamina Update - Meter: {playerController.SprintMeter:F2}, Threshold: {playerController.SprintMeterThreshold:F2}, Normalized: {normalized:F2}, Slider Value: {staminaBar.value:F2}");

            if (staminaFill != null)
            {
                if (normalized < yellowThreshold)
                    staminaFill.color = Color.Lerp(Color.green, Color.yellow, normalized / yellowThreshold);
                else
                    staminaFill.color = Color.Lerp(Color.yellow, Color.red, (normalized - yellowThreshold) / (1 - yellowThreshold));
            }
        }
        else
        {
            Debug.LogWarning($"Stamina bar not updating - staminaBar: {staminaBar}, playerController: {playerController}");
        }
    }

    public void EquipSlot(int slotNumber)
    {
        int arrayIndex = slotNumber - 1;

        // Failsafe: Make sure the slot isn't empty, and the item hasn't been destroyed by corruption
        if (quickSlots[arrayIndex].assignedItem == null)
        {
            Debug.Log($"Slot {slotNumber} is empty or item was destroyed.");
            return;
        }

        currentEquippedIndex = arrayIndex;

        // Update Visual Highlights
        for (int i = 0; i < quickSlots.Length; i++)
        {
            quickSlots[i].SetHighlight(i == currentEquippedIndex);
        }

        // Get the real item data to send to your player controller!
        DraggableItem equippedItem = quickSlots[arrayIndex].assignedItem;
        Debug.Log($"<color=green>EQUIPPED:</color> {equippedItem.itemName}");

        // Example: PlayerController.EquipWeapon(equippedItem.itemName);
    }
}
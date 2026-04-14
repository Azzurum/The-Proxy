using UnityEngine;

public class HotbarManager : MonoBehaviour
{
    public static HotbarManager Instance;

    [Header("Hotbar Array")]
    public HotbarSlot[] quickSlots = new HotbarSlot[3];

    private int currentEquippedIndex = -1; // -1 means hands are empty

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Safely clear all slots when the game boots
        foreach (var slot in quickSlots)
        {
            if (slot != null) slot.ClearSlot();
        }
    }

    void Update()
    {
        // Listen for hotbar shortcut keys
        if (Input.GetKeyDown(KeyCode.Alpha1)) EquipSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) EquipSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) EquipSlot(3);
    }

    public void EquipSlot(int slotNumber)
    {
        int arrayIndex = slotNumber - 1;

        if (arrayIndex < 0 || arrayIndex >= quickSlots.Length) return;
        if (quickSlots[arrayIndex] == null) return;

        // Check if the physical hotbar slot is actually holding an item
        if (quickSlots[arrayIndex].containedItem == null)
        {
            // If the slot is empty, but we were holding it, unequip Kaelen's hands
            if (currentEquippedIndex == arrayIndex)
            {
                currentEquippedIndex = -1; 
                UpdateHighlights();
                Debug.Log("<color=gray>UNEQUIPPED:</color> Hands are empty.");
            }
            return;
        }

        // Equip the new item
        currentEquippedIndex = arrayIndex;
        UpdateHighlights();

        ItemData equippedItem = quickSlots[arrayIndex].containedItem.itemData;
        Debug.Log($"<color=green>EQUIPPED:</color> {equippedItem.itemName}");
    }

    // Helper method to keep your UI frames synced up perfectly
    private void UpdateHighlights()
    {
        for (int i = 0; i < quickSlots.Length; i++)
        {
            if (quickSlots[i] != null)
            {
                // Only turn on the glowing highlight if it matches the currently equipped index
                quickSlots[i].SetHighlight(i == currentEquippedIndex);
            }
        }
    }
}
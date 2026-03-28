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
        foreach (var slot in quickSlots)
        {
            slot.ClearSlot();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) EquipSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) EquipSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) EquipSlot(3);
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
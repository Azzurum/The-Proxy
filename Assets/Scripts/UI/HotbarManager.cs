using UnityEngine;
using UnityEngine.UI;

public class HotbarManager : MonoBehaviour
{
    public static HotbarManager Instance;

    [Header("Physical Inventory Slots")]
    public HotbarSlot[] quickSlots = new HotbarSlot[3];

    [Header("HUD Sync References")]
    public Image[] hudIcons = new Image[3];        // The images inside your HUD slots
    public Outline[] hudOutlines = new Outline[3]; // The Outlines on the HUD slots

    public int currentEquippedIndex = -1; // -1 means hands are empty

    void Awake()
    {
        if (Instance == null) Instance = this;
        else 
        {
            Debug.LogWarning($"<color=red>[SINGLETON WARNING]</color> Multiple HotbarManager scripts found! Destroying the duplicate on '{gameObject.name}'.");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        CheckForMissingReferences();
        // Ensure outlines start turned off
        UpdateHighlights();
    }

    private void CheckForMissingReferences()
    {
        for (int i = 0; i < 3; i++)
        {
            if (i >= quickSlots.Length || quickSlots[i] == null)
                Debug.LogError($"<color=red>[HOTBAR ERROR]</color> Quick Slot {i + 1} is missing! Assign the physical Inventory Slot in the HotbarManager inspector.");
                
            if (i >= hudIcons.Length || hudIcons[i] == null)
                Debug.LogError($"<color=red>[HOTBAR ERROR]</color> HUD Icon {i + 1} is missing! Assign the HUD Image in the HotbarManager inspector.");
                
            if (i >= hudOutlines.Length || hudOutlines[i] == null)
                Debug.LogWarning($"<color=yellow>[HOTBAR WARNING]</color> HUD Outline {i + 1} is missing! Assign the HUD Outline in the HotbarManager inspector.");
        }
    }

    void Update()
    {
        // Listen for hotbar shortcut keys
        if (Input.GetKeyDown(KeyCode.Alpha1)) EquipSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) EquipSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) EquipSlot(3);

        // Continuously project the physical inventory state onto the HUD!
        SyncHUD();
    }

    public void EquipSlot(int slotNumber)
    {
        int arrayIndex = slotNumber - 1;

        if (arrayIndex < 0 || arrayIndex >= quickSlots.Length) return;
        if (quickSlots[arrayIndex] == null) return;

        // If the physical slot is empty
        if (quickSlots[arrayIndex].containedItem == null)
        {
            if (currentEquippedIndex == arrayIndex)
            {
                currentEquippedIndex = -1; 
                UpdateHighlights();
                Debug.Log("<color=gray>UNEQUIPPED:</color> Hands are empty.");
            }
            return;
        }

        // Toggle unequip if pressing the exact same key
        if (currentEquippedIndex == arrayIndex)
        {
            currentEquippedIndex = -1; 
            UpdateHighlights();
            Debug.Log("<color=gray>UNEQUIPPED:</color> Hands are empty.");
            return;
        }

        // Equip the new item
        currentEquippedIndex = arrayIndex;
        UpdateHighlights();

        ItemData equippedItem = quickSlots[arrayIndex].containedItem.itemData;
        Debug.Log($"<color=green>EQUIPPED:</color> {equippedItem.itemName}");
    }

    private void SyncHUD()
    {
        for (int i = 0; i < quickSlots.Length; i++)
        {
            if (i >= hudIcons.Length || hudIcons[i] == null) continue;

            // If the physical inventory slot actually contains a dragged item...
            if (quickSlots[i] != null && quickSlots[i].containedItem != null && quickSlots[i].containedItem.itemData != null)
            {
                Sprite assignedIcon = quickSlots[i].containedItem.itemData.icon;

                if (assignedIcon != null)
                {
                    // Item has a valid picture! Show it.
                    hudIcons[i].sprite = assignedIcon;
                    hudIcons[i].color = Color.white; // Remove transparency
                    hudIcons[i].enabled = true;
                }
                else
                {
                    // The item data exists, but you forgot to assign a picture to it in the Inspector!
                    hudIcons[i].sprite = null;
                    hudIcons[i].color = Color.clear; // Force it to be completely invisible
                    hudIcons[i].enabled = false;
                }
            }
            else
            {
                // The physical slot is totally empty. Hide everything.
                hudIcons[i].sprite = null;
                hudIcons[i].color = Color.clear; // Force it to be completely invisible
                hudIcons[i].enabled = false;

                // SAFETY: If you were holding this item, but dragged it out, unequip it!
                if (currentEquippedIndex == i)
                {
                    currentEquippedIndex = -1;
                    UpdateHighlights();
                    Debug.Log("<color=gray>UNEQUIPPED:</color> Item removed from hotbar.");
                }
            }
        }
    }
    // Helper method to keep your UI frames synced up perfectly
    private void UpdateHighlights()
    {
        for (int i = 0; i < quickSlots.Length; i++)
        {
            bool isEquipped = (i == currentEquippedIndex);

            // 1. Highlight the physical inventory slot
            if (quickSlots[i] != null)
            {
                quickSlots[i].SetHighlight(isEquipped);
            }

            // 2. Highlight the HUD slot by turning the Outline component ON or OFF
            if (i < hudOutlines.Length && hudOutlines[i] != null)
            {
                hudOutlines[i].enabled = isEquipped;
            }
        }
    }
}
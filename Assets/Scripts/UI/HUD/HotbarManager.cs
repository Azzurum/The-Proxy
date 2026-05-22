using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Coordinates the synchronization between the physical quick-slot interactions and the persistent HUD visualization.
/// </summary>
public class HotbarManager : MonoBehaviour
{
    public static HotbarManager Instance;

    [Header("Physical Inventory Slots")]
    [Tooltip("The physical HotbarSlot components located in the M.E.T. Rig.")]
    public HotbarSlot[] quickSlots = new HotbarSlot[3];

    [Header("HUD Sync References")]
    [Tooltip("The image elements rendering the items on the non-inventory UI HUD.")]
    public Image[] hudIcons = new Image[3];        
    [Tooltip("The border outlines that indicate selection state on the HUD.")]
    public Outline[] hudOutlines = new Outline[3]; 

    [HideInInspector]
    public int currentEquippedIndex = -1; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else 
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateHighlights();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) EquipSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) EquipSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) EquipSlot(3);

        SyncHUD();
    }

    /// <summary>
    /// Selects and arms the designated hotbar slot. Use slotNumber 1, 2, or 3.
    /// </summary>
    public void EquipSlot(int slotNumber)
    {
        int arrayIndex = slotNumber - 1;

        if (arrayIndex < 0 || arrayIndex >= quickSlots.Length) return;
        if (quickSlots[arrayIndex] == null) return;

        if (quickSlots[arrayIndex].containedItem == null)
        {
            if (currentEquippedIndex == arrayIndex)
            {
                currentEquippedIndex = -1; 
                UpdateHighlights();
            }
            return;
        }

        if (currentEquippedIndex == arrayIndex)
        {
            currentEquippedIndex = -1; 
            UpdateHighlights();
            return;
        }

        currentEquippedIndex = arrayIndex;
        UpdateHighlights();
    }

    private void SyncHUD()
    {
        for (int i = 0; i < quickSlots.Length; i++)
        {
            if (i >= hudIcons.Length || hudIcons[i] == null) continue;

            if (quickSlots[i] != null && quickSlots[i].containedItem != null && quickSlots[i].containedItem.itemData != null)
            {
                Sprite assignedIcon = quickSlots[i].containedItem.itemData.icon;

                if (assignedIcon != null)
                {
                    hudIcons[i].sprite = assignedIcon;
                    hudIcons[i].color = Color.white; 
                    hudIcons[i].enabled = true;
                }
                else
                {
                    hudIcons[i].sprite = null;
                    hudIcons[i].color = Color.clear; 
                    hudIcons[i].enabled = false;
                }
            }
            else
            {
                hudIcons[i].sprite = null;
                hudIcons[i].color = Color.clear; 
                hudIcons[i].enabled = false;

                if (currentEquippedIndex == i)
                {
                    currentEquippedIndex = -1;
                    UpdateHighlights();
                }
            }
        }
    }
    
    private void UpdateHighlights()
    {
        for (int i = 0; i < quickSlots.Length; i++)
        {
            bool isEquipped = (i == currentEquippedIndex);

            if (quickSlots[i] != null)
            {
                quickSlots[i].SetHighlight(isEquipped);
            }

            if (i < hudOutlines.Length && hudOutlines[i] != null)
            {
                hudOutlines[i].enabled = isEquipped;
            }
        }
    }
}
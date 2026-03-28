using UnityEngine;
using UnityEngine.UI;

public class HotbarSlot : MonoBehaviour
{
    [Header("Slot Setup")]
    public int slotNumber;
    public Image itemIcon; // The visual sprite
    public Image highlightFrame; // To show if it's currently equipped

    [Header("Assigned Data")]
    public DraggableItem assignedItem;

    public void AssignShortcut(DraggableItem item)
    {
        assignedItem = item;

        // Copy the visual from the physical item
        Image sourceImage = item.GetComponent<Image>();
        if (sourceImage != null && itemIcon != null)
        {
            itemIcon.sprite = sourceImage.sprite;
            itemIcon.color = sourceImage.color;
            itemIcon.enabled = true;
        }

        // Auto-equip if this is slot 1
        if (slotNumber == 1)
        {
            HotbarManager.Instance.EquipSlot(1);
        }
    }

    public void ClearSlot()
    {
        assignedItem = null;
        if (itemIcon != null) itemIcon.enabled = false;
        SetHighlight(false);
    }

    public void SetHighlight(bool isActive)
    {
        if (highlightFrame != null) highlightFrame.enabled = isActive;
    }
}
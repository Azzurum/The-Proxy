using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HotbarSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slot Setup")]
    public int slotNumber;
    public Image highlightFrame;

    [Header("Physical Item")]
    public DraggableItem containedItem;

    private Image backgroundImage;
    private Color normalColor;
    private Color hoverColor = new Color(0f, 1f, 0.8f, 0.3f);

    void Awake()
    {
        backgroundImage = GetComponent<Image>();
        if (backgroundImage != null) normalColor = backgroundImage.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (DraggableItem.itemBeingDragged != null && containedItem == null)
            if (backgroundImage != null) backgroundImage.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (backgroundImage != null) backgroundImage.color = normalColor;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (backgroundImage != null) backgroundImage.color = normalColor;

        DraggableItem draggedItem = DraggableItem.itemBeingDragged;

        // Only accept the drop if we are currently empty
        if (draggedItem != null && containedItem == null)
        {
            draggedItem.dropAccepted = true;
            draggedItem.parentAfterDrag = this.transform; // Make this slot the parent!
            containedItem = draggedItem;

            if (slotNumber == 1 && HotbarManager.Instance != null)
            {
                HotbarManager.Instance.EquipSlot(1);
            }
        }
    }

    public void ClearSlot()
    {
        containedItem = null;
        SetHighlight(false);

        if (HotbarManager.Instance != null)
        {
            HotbarManager.Instance.EquipSlot(slotNumber);
        }
    }

    // A quiet way to clear the item reference when a drag begins from this slot.
    public void DetachItem()
    {
        containedItem = null;
    }

    public void SetHighlight(bool isActive)
    {
        if (highlightFrame != null) highlightFrame.enabled = isActive;
    }
}
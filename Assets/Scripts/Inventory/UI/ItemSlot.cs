using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Coordinates mouse interaction and UI feedback for a specific grid slot in the inventory.
/// </summary>
public class ItemSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Image slotImage;
    private Color originalColor;
    private Dictionary<Image, Color> highlightedOriginalColors = new Dictionary<Image, Color>();
    private InventorySlot _parentSlotData;

    void Awake()
    {
        slotImage = GetComponent<Image>();
        if (slotImage != null)
        {
            originalColor = slotImage.color;
        }
        _parentSlotData = GetComponent<InventorySlot>();
    }

    /// <summary>
    /// Triggers when the user releases a draggable item over this slot, evaluating if it can be placed.
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        if (dropped == null) return;

        if (!dropped.TryGetComponent<DraggableItem>(out var draggableItem) || _parentSlotData == null || InventoryManager.Instance == null)
        {
            if (draggableItem != null) draggableItem.dropAccepted = false;
            return;
        }

        if (InventoryManager.Instance.CanDropToSlot(_parentSlotData, draggableItem))
        {
            draggableItem.parentAfterDrag = transform;
            draggableItem.dropAccepted = true;
        }
        else
        {
            draggableItem.dropAccepted = false;
        }
    }

    /// <summary>
    /// Draws a colored overlay indicating whether a dragged item can fit in the spaces originating from this slot.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        DraggableItem dragged = DraggableItem.itemBeingDragged;
        if (dragged == null || slotImage == null || InventoryManager.Instance == null || _parentSlotData == null) return;

        ItemFootprint footprint = dragged.footprint;
        if (footprint == null) footprint = new ItemFootprint(1, 1);

        bool canDrop = InventoryManager.Instance.CanDropToSlot(_parentSlotData, dragged);

        Transform grid = transform.parent;
        int columns = 5; 

        int offsetX = -Mathf.FloorToInt(footprint.width / 2f);
        int offsetY = -Mathf.FloorToInt(footprint.height / 2f);

        for (int y = 0; y < footprint.height; y++)
        {
            for (int x = 0; x < footprint.width; x++)
            {
                if (!footprint.GetCell(x, y)) continue; 

                int checkX = _parentSlotData.slotCoordinate.x + offsetX + x;
                int checkY = _parentSlotData.slotCoordinate.y + offsetY + y;

                if (checkX >= 0 && checkX < columns && checkY >= 0)
                {
                    int slotIndex = checkY * columns + checkX;
                    if (slotIndex >= 0 && slotIndex < grid.childCount)
                    {
                        Transform childSlot = grid.GetChild(slotIndex);
                        if (childSlot.TryGetComponent<Image>(out var img))
                        {
                            if (!highlightedOriginalColors.ContainsKey(img))
                            {
                                highlightedOriginalColors[img] = img.color;
                            }
                            img.color = canDrop ? Color.green : Color.red;
                        }
                    }
                }
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetHighlights();
    }

    /// <summary>
    /// Restores the original color of any slots that were modified during the drag highlight phase.
    /// </summary>
    private void ResetHighlights()
    {
        foreach (var pair in highlightedOriginalColors)
        {
            if (pair.Key != null)
            {
                pair.Key.color = pair.Value;
            }
        }
        highlightedOriginalColors.Clear();
    }

    public void ClearHighlight()
    {
        if (slotImage != null)
        {
            slotImage.color = originalColor;
        }
        foreach (var pair in highlightedOriginalColors)
        {
            if (pair.Key != null)
            {
                pair.Key.color = pair.Value;
            }
        }
        highlightedOriginalColors.Clear();
    }
}
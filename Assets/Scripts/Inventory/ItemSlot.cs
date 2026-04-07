using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class ItemSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Image slotImage;
    private Color originalColor;
    private Dictionary<Image, Color> highlightedOriginalColors = new Dictionary<Image, Color>();

    void Awake()
    {
        slotImage = GetComponent<Image>();
        if (slotImage != null)
        {
            originalColor = slotImage.color;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        if (dropped == null) return;

        DraggableItem draggableItem = dropped.GetComponent<DraggableItem>();
        if (draggableItem == null) return;

        InventorySlot slot = GetComponent<InventorySlot>();
        InventoryManager manager = FindFirstObjectByType<InventoryManager>();

        // Only accept a drop into an empty slot
        if (transform.childCount > 0)
        {
            draggableItem.dropAccepted = false;
            return;
        }

        if (manager == null || slot == null)
        {
            draggableItem.dropAccepted = false;
            return;
        }

        if (manager.CanDropToSlot(slot, draggableItem))
        {
            draggableItem.parentAfterDrag = transform;
            draggableItem.dropAccepted = true;
        }
        else
        {
            draggableItem.dropAccepted = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        DraggableItem dragged = DraggableItem.itemBeingDragged;
        if (dragged == null || slotImage == null) return;

        InventorySlot slot = GetComponent<InventorySlot>();
        InventoryManager manager = FindFirstObjectByType<InventoryManager>();
        if (manager == null || slot == null) return;

        ItemFootprint footprint = dragged.footprint;
        if (footprint == null) footprint = new ItemFootprint(1, 1);

        bool canDrop = manager.CanDropToSlot(slot, dragged);

        // Highlight the footprint
        Transform grid = transform.parent;
        int columns = 5; // Assume 5 columns for all grids
        for (int y = 0; y < footprint.height; y++)
        {
            for (int x = 0; x < footprint.width; x++)
            {
                int checkX = slot.slotCoordinate.x + x;
                int checkY = slot.slotCoordinate.y + y;
                int slotIndex = checkY * columns + checkX;
                if (slotIndex >= 0 && slotIndex < grid.childCount)
                {
                    Transform childSlot = grid.GetChild(slotIndex);
                    Image img = childSlot.GetComponent<Image>();
                    if (img != null)
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

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetHighlights();
    }

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
using UnityEngine;
using UnityEngine.EventSystems;

// We added IPointerEnterHandler to test mouse visibility
public class InventorySlot : MonoBehaviour, IDropHandler, IPointerEnterHandler
{
    // 1. This tests if the slot is visible to the mouse at all
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("Laser Test: Mouse can see the Grid Slot!");
    }

    // 2. This is the actual drop command
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            RectTransform itemRect = eventData.pointerDrag.GetComponent<RectTransform>();
            DraggableItem dragLogic = eventData.pointerDrag.GetComponent<DraggableItem>();

            // 1. First, snap the item center-to-center with the grid slot
            itemRect.position = transform.position;

            // 2. The Grid Math variables
            float gridStep = 80f; // Your 75px cell + 5px spacing
            float halfStep = gridStep / 2f; // 40px shift
            Vector2 snapOffset = Vector2.zero;

            // 3. Calculate how many grid squares this item takes up
            int cellsX = Mathf.RoundToInt(itemRect.rect.width / gridStep);
            int cellsY = Mathf.RoundToInt(itemRect.rect.height / gridStep);

            // 4. If the item is rotated sideways, swap the X and Y cell counts
            if (dragLogic != null && dragLogic.isRotated)
            {
                int temp = cellsX;
                cellsX = cellsY;
                cellsY = temp;
            }

            // 5. If the item is an EVEN number of cells (like 2), shift it by half a slot to align perfectly!
            if (cellsX % 2 == 0) snapOffset.x = halfStep;  // Shift Right
            if (cellsY % 2 == 0) snapOffset.y = -halfStep; // Shift Down

            // 6. Apply the shift
            itemRect.anchoredPosition += snapOffset;

            Debug.Log("SUCCESS: Perfect Grid Snap Applied!");
        }
    }
}
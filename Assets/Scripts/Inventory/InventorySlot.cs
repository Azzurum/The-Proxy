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

    [Header("Grid Data")]
    public Vector2Int slotCoordinate; // X is Column (0-9), Y is Row (0-9, where 0 is bottom)

    // 2. This is the actual drop command
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            RectTransform itemRect = eventData.pointerDrag.GetComponent<RectTransform>();
            DraggableItem dragLogic = eventData.pointerDrag.GetComponent<DraggableItem>();

            // 1. Center the item on the dropped slot
            itemRect.position = transform.position;

            // 2. Grid Math Variables
            float gridStep = 80f; // 75px cell + 5px spacing
            float halfStep = gridStep / 2f;
            Vector2 snapOffset = Vector2.zero;

            // 3. Calculate Item Size in Cells
            int cellsX = Mathf.RoundToInt(itemRect.rect.width / gridStep);
            int cellsY = Mathf.RoundToInt(itemRect.rect.height / gridStep);

            if (dragLogic != null && dragLogic.isRotated)
            {
                int temp = cellsX;
                cellsX = cellsY;
                cellsY = temp;
            }

            // 4. Default Alignment (Shift right/down if the item size is even)
            if (cellsX % 2 == 0) snapOffset.x = halfStep;
            if (cellsY % 2 == 0) snapOffset.y = -halfStep;

            // 5. BOUNDARY CLAMPING MATH
            int actualLeftCol = slotCoordinate.x;
            int actualTopRow = slotCoordinate.y;

            // A. Prevent hanging off the Right Edge (Max Column is 9)
            int rightEdge = actualLeftCol + cellsX - 1;
            if (rightEdge > 9)
            {
                int overflowX = rightEdge - 9;
                actualLeftCol -= overflowX; // Update the memory
                snapOffset.x -= (overflowX * gridStep); // Physically bump it left
            }

            // B. Prevent hanging off the Bottom Edge (Min Row is 0)
            int bottomEdge = actualTopRow - cellsY + 1;
            if (bottomEdge < 0)
            {
                int underflowY = 0 - bottomEdge;
                actualTopRow += underflowY; // Update the memory
                snapOffset.y += (underflowY * gridStep); // Physically bump it up
            }

            // 6. Apply the final visual shift
            itemRect.anchoredPosition += snapOffset;

            // 7. Send the corrected data to the backend Manager
            InventoryManager manager = FindFirstObjectByType<InventoryManager>();
            if (manager != null)
            {
                // The technical anchor is the Bottom-Left most cell it occupies
                Vector2Int anchorCoordinate = new Vector2Int(actualLeftCol, actualTopRow - cellsY + 1);

                // Only register if we aren't hovering over the trash slot
                manager.RegisterItemPlacement(eventData.pointerDrag, anchorCoordinate, dragLogic != null && dragLogic.isRotated);

                Debug.Log($"SUCCESS: Perfect Snap! Clamped Anchor: Column {anchorCoordinate.x}, Row {anchorCoordinate.y}");
            }
        }
    }
}
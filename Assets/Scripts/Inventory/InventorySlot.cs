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

            float gridStep = 80f;
            float halfStep = gridStep / 2f;
            Vector2 snapOffset = Vector2.zero;

            int cellsX = Mathf.RoundToInt(itemRect.rect.width / gridStep);
            int cellsY = Mathf.RoundToInt(itemRect.rect.height / gridStep);

            if (dragLogic != null && dragLogic.isRotated)
            {
                int temp = cellsX;
                cellsX = cellsY;
                cellsY = temp;
            }

            if (cellsX % 2 == 0) snapOffset.x = halfStep;
            if (cellsY % 2 == 0) snapOffset.y = -halfStep;

            int actualLeftCol = slotCoordinate.x;
            int actualTopRow = slotCoordinate.y;

            int rightEdge = actualLeftCol + cellsX - 1;
            if (rightEdge > 9)
            {
                int overflowX = rightEdge - 9;
                actualLeftCol -= overflowX;
                snapOffset.x -= (overflowX * gridStep);
            }

            int bottomEdge = actualTopRow - cellsY + 1;
            if (bottomEdge < 0)
            {
                int underflowY = 0 - bottomEdge;
                actualTopRow += underflowY;
                snapOffset.y += (underflowY * gridStep);
            }

            // Calculate theoretical anchor BEFORE snapping
            Vector2Int anchorCoordinate = new Vector2Int(actualLeftCol, actualTopRow - cellsY + 1);

            InventoryManager manager = FindFirstObjectByType<InventoryManager>();
            if (manager != null)
            {
                // THE GATEKEEPER CHECK: Is the space free?
                if (!manager.IsSpaceFree(anchorCoordinate, cellsX, cellsY, eventData.pointerDrag))
                {
                    // Space is blocked! Send the battery back to where it came from.
                    if (dragLogic != null) dragLogic.ReturnToOrigin();
                    Debug.LogWarning("Drop Rejected: Space is occupied by Corruption or another item.");
                    return; // Stop the code here!
                }

                // If we pass the Gatekeeper, physically center and clamp the item
                itemRect.position = transform.position;
                itemRect.anchoredPosition += snapOffset;

                // Register the new position in the backend
                manager.RegisterItemPlacement(eventData.pointerDrag, anchorCoordinate, cellsX, cellsY, dragLogic != null && dragLogic.isRotated);
            }
        }
    }
}
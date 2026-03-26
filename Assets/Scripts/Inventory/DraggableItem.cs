using UnityEngine;
using UnityEngine.EventSystems; // Required for UI drag interfaces

// We add the IBeginDrag, IDrag, and IEndDrag interfaces to listen to the mouse
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Drag State")]
    public Transform originalParent;

    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;

        // Move the item to the very top layer of the UI so it doesn't drag *underneath* the grid
        transform.SetAsLastSibling();

        // Turn off raycasts so the mouse can detect the grid slots behind this item
        canvasGroup.blocksRaycasts = false;

        // Make the item slightly transparent while dragging
        canvasGroup.alpha = 0.7f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Make the item's position exactly match the mouse cursor
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Restore solid collision and opacity when you let go of the mouse click
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
    }
}
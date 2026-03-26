using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector2 originalPosition;
    private Transform originalParent;

    [Header("Item State")]
    public bool isRotated = false; // Tracks the state for the InventoryManager later [cite: 123]
    private bool isDragging = false; // Safety lock so you can't rotate items sitting on the grid

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    void Update()
    {
        // If we are currently holding the item and press 'R', rotate it!
        if (isDragging && Input.GetKeyDown(KeyCode.R))
        {
            RotateItem();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true; // Unlock rotation

        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.7f;
        originalParent = transform.parent;
        originalPosition = rectTransform.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false; // Lock rotation

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
    }

    private void RotateItem()
    {
        isRotated = !isRotated; // Toggle the internal state

        // Visually rotate the item -90 degrees on the Z axis
        rectTransform.Rotate(0, 0, -90f);

        Debug.Log("Item Rotated! Current isRotated state: " + isRotated);
    }

    // This triggers automatically if the M.E.T. Rig is closed (Canvas disabled)
    void OnDisable()
    {
        if (isDragging)
        {
            ForceCancelDrag();
        }
    }

    private void ForceCancelDrag()
    {
        isDragging = false;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        // Snap it back to its original location
        if (originalParent != null)
        {
            transform.SetParent(originalParent);
            rectTransform.anchoredPosition = originalPosition;
        }

        Debug.Log("Drag forcefully cancelled via Tab. Item reset.");
    }
}
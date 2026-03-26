using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Canvas canvas;

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
}
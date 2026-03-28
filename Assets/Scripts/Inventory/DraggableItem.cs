using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("Item Data")]
    public ItemData itemData;

    [Header("Item Details")]
    public string itemName = "Unknown Object";
    [TextArea] public string itemDescription = "A strange item with unknown properties.";

    [Header("Item Footprint")]
    public int sizeX = 1;
    public int sizeY = 2;
    public bool isRotated = false;

    [Header("Layout")]
    public float cellSize = 80f;

    private bool isDragging = false;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Canvas canvas;
    private InventoryManager inventoryManager;

    // --- RESTORED LIVE MEMORY (Accessible by the Manager) ---
    public static DraggableItem itemBeingDragged;
    public bool ejectedFromRig = false;
    public InventoryGrid originalGrid;
    public Vector2 originalLocalPosition;
    public Vector2Int originalAnchorCoord;

    private Transform originalParent;
    private bool originalIsRotated;
    private int originalSizeX;
    private int originalSizeY;
    private Quaternion originalRotation;
    private bool fromHotbar = false;
    private HotbarSlot originalHotbarSlot;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>().rootCanvas;

        if (itemData != null)
        {
            itemName = itemData.itemName;
            itemDescription = itemData.itemDescription;
            sizeX = itemData.size.x;
            sizeY = itemData.size.y;
        }

        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(sizeX * cellSize, sizeY * cellSize);

        LayoutElement le = GetComponent<LayoutElement>();
        if (le == null) le = gameObject.AddComponent<LayoutElement>();
        le.ignoreLayout = true;
    }

    void Update()
    {
        if (isDragging && Input.GetKeyDown(KeyCode.R)) RotateItem();
    }

    // --- INSPECTION CLICK ---
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isDragging && ItemInspector.Instance != null)
        {
            ItemInspector.Instance.InspectItem(this);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        itemBeingDragged = this; // Restored
        ejectedFromRig = false;  // Restored

        originalLocalPosition = rectTransform.localPosition;

        originalGrid = GetComponentInParent<InventoryGrid>();
        if (originalGrid != null)
        {
            foreach (var item in originalGrid.activeItems)
            {
                if (item.uiObject == gameObject)
                {
                    originalAnchorCoord = item.position;
                    break;
                }
            }
            originalGrid.RemoveItem(gameObject);
        }
        else
        {
            // Check if from hotbar
            HotbarSlot slot = GetComponentInParent<HotbarSlot>();
            if (slot != null)
            {
                fromHotbar = true;
                originalHotbarSlot = slot;
            }
        }

        originalParent = transform.parent;
        originalIsRotated = isRotated;
        originalSizeX = sizeX;
        originalSizeY = sizeY;
        originalRotation = rectTransform.rotation;

        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();

        Vector3 lp = rectTransform.localPosition; lp.z = 0; rectTransform.localPosition = lp;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.7f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        // --- HOTBAR SHORTCUT CHECK ---
        GameObject droppedOn = eventData.pointerCurrentRaycast.gameObject;
        if (droppedOn != null)
        {
            HotbarSlot hotbarSlot = droppedOn.GetComponentInParent<HotbarSlot>();
            if (hotbarSlot != null)
            {
                // Move the item to hotbar
                if (originalGrid != null) originalGrid.RemoveItem(gameObject);
                hotbarSlot.AssignShortcut(this);
                transform.SetParent(hotbarSlot.transform, true);
                rectTransform.localPosition = Vector2.zero;
                canvasGroup.alpha = 0f; // Hide the draggable item
                itemBeingDragged = null;
                fromHotbar = false;
                originalHotbarSlot = null;
                return;
            }
        }

        InventoryGrid[] allGrids = FindObjectsByType<InventoryGrid>(FindObjectsSortMode.None);
        InventoryGrid bestGrid = null;

        foreach (var grid in allGrids)
        {
            if (grid.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(grid.GetComponent<RectTransform>(), Input.mousePosition, eventData.pressEventCamera))
            {
                bestGrid = grid;
                break;
            }
        }

        if (bestGrid != null)
        {
            Vector2 localPos = bestGrid.transform.InverseTransformPoint(rectTransform.position);

            float gridBottomLeftX = -(bestGrid.gridWidth * bestGrid.cellSize) / 2f;
            float gridBottomLeftY = -(bestGrid.gridHeight * bestGrid.cellSize) / 2f;

            float itemBottomLeftX = localPos.x - (sizeX * bestGrid.cellSize) / 2f;
            float itemBottomLeftY = localPos.y - (sizeY * bestGrid.cellSize) / 2f;

            int anchorX = Mathf.RoundToInt((itemBottomLeftX - gridBottomLeftX) / bestGrid.cellSize);
            int anchorY = Mathf.RoundToInt((itemBottomLeftY - gridBottomLeftY) / bestGrid.cellSize);

            if (anchorX < 0) anchorX = 0;
            if (anchorX + sizeX > bestGrid.gridWidth) anchorX = bestGrid.gridWidth - sizeX;
            int bottomActiveRow = bestGrid.gridHeight - bestGrid.activeHeight;
            if (anchorY < bottomActiveRow) anchorY = bottomActiveRow;
            if (anchorY + sizeY > bestGrid.gridHeight) anchorY = bestGrid.gridHeight - sizeY;

            Vector2Int bestAnchor = new Vector2Int(anchorX, anchorY);

            if (bestGrid.IsSpaceFree(bestAnchor, sizeX, sizeY, gameObject))
            {
                foreach (var g in allGrids) g.RemoveItem(gameObject);

                transform.SetParent(bestGrid.transform, true);
                rectTransform.localPosition = bestGrid.GetSnapPosition(bestAnchor, sizeX, sizeY);

                rectTransform.localScale = Vector3.one;
                Vector3 lp = rectTransform.localPosition; lp.z = 0; rectTransform.localPosition = lp;

                bestGrid.RegisterItem(gameObject, bestAnchor, sizeX, sizeY, isRotated);

                if (inventoryManager != null && originalGrid != null && originalGrid == inventoryManager.externalStorageGrid && originalGrid != bestGrid && originalGrid.activeItems.Count == 0)
                {
                    originalGrid.gameObject.SetActive(false);
                }

                if (fromHotbar)
                {
                    originalHotbarSlot.ClearSlot();
                    canvasGroup.alpha = 1f; // Make visible again
                }

                itemBeingDragged = null; // Restored
                return;
            }
            else Debug.LogWarning("Drop Rejected: Space is occupied!");
        }

        ReturnToOrigin();
        itemBeingDragged = null; // Restored
        fromHotbar = false;
        originalHotbarSlot = null;
    }

    private void RotateItem()
    {
        isRotated = !isRotated;
        int tempSize = sizeX;
        sizeX = sizeY;
        sizeY = tempSize;
        rectTransform.Rotate(0, 0, -90f);
    }

    public void ReturnToOrigin()
    {
        // Restored check for rig ejection
        if (ejectedFromRig)
        {
            if (inventoryManager != null) inventoryManager.DiscardItemToWorld(gameObject);
            return;
        }

        isRotated = originalIsRotated;
        sizeX = originalSizeX;
        sizeY = originalSizeY;
        rectTransform.rotation = originalRotation;

        if (originalGrid != null)
        {
            originalGrid.gameObject.SetActive(true);
            transform.SetParent(originalGrid.transform, true);
            rectTransform.localScale = Vector3.one;
            transform.SetAsLastSibling();

            originalGrid.RegisterItem(gameObject, originalAnchorCoord, sizeX, sizeY, isRotated);
            rectTransform.localPosition = originalLocalPosition;
        }
        else if (fromHotbar)
        {
            // Return to hotbar
            transform.SetParent(originalHotbarSlot.transform, true);
            rectTransform.localPosition = Vector2.zero;
            canvasGroup.alpha = 0f; // Hide again
        }

        Vector3 lp = rectTransform.localPosition; lp.z = 0; rectTransform.localPosition = lp;
    }
}
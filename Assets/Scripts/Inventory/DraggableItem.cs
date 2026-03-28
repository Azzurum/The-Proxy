using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Item Footprint")]
    public int sizeX = 1;
    public int sizeY = 2;
    public bool isRotated = false;

    private bool isDragging = false;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Canvas canvas;

    // --- LIVE MEMORY (Accessible by the Manager) ---
    public static DraggableItem itemBeingDragged;
    public InventoryGrid originalGrid;
    public Vector2 originalLocalPosition;
    public Vector2Int originalAnchorCoord;
    public bool ejectedFromRig = false; // Tracks if it gets pushed out while holding it

    private Transform originalParent;
    private bool originalIsRotated;
    private int originalSizeX;
    private int originalSizeY;
    private Quaternion originalRotation;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>().rootCanvas;

        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(sizeX * 80f, sizeY * 80f);

        LayoutElement le = GetComponent<LayoutElement>();
        if (le == null) le = gameObject.AddComponent<LayoutElement>();
        le.ignoreLayout = true;
    }

    void Update()
    {
        if (isDragging && Input.GetKeyDown(KeyCode.R)) RotateItem();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        itemBeingDragged = this;
        ejectedFromRig = false;

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

            float gridBottomLeftX = -(bestGrid.gridWidth * 80f) / 2f;
            float gridBottomLeftY = -(bestGrid.gridHeight * 80f) / 2f;

            float itemBottomLeftX = localPos.x - (sizeX * 80f) / 2f;
            float itemBottomLeftY = localPos.y - (sizeY * 80f) / 2f;

            int anchorX = Mathf.RoundToInt((itemBottomLeftX - gridBottomLeftX) / 80f);
            int anchorY = Mathf.RoundToInt((itemBottomLeftY - gridBottomLeftY) / 80f);

            // Boundary Clamping
            if (anchorX < 0) anchorX = 0;
            if (anchorX + sizeX > bestGrid.gridWidth) anchorX = bestGrid.gridWidth - sizeX;
            int bottomActiveRow = bestGrid.gridHeight - bestGrid.activeHeight;
            if (anchorY < bottomActiveRow) anchorY = bottomActiveRow;
            if (anchorY + sizeY > bestGrid.gridHeight) anchorY = bestGrid.gridHeight - sizeY;

            Vector2Int bestAnchor = new Vector2Int(anchorX, anchorY);

            // Drop Validation
            if (bestGrid.IsSpaceFree(bestAnchor, sizeX, sizeY, gameObject))
            {
                foreach (var g in allGrids) g.RemoveItem(gameObject);

                transform.SetParent(bestGrid.transform, true);
                rectTransform.localPosition = bestGrid.GetSnapPosition(bestAnchor, sizeX, sizeY);

                rectTransform.localScale = Vector3.one;
                Vector3 lp = rectTransform.localPosition; lp.z = 0; rectTransform.localPosition = lp;

                bestGrid.RegisterItem(gameObject, bestAnchor, sizeX, sizeY, isRotated);

                InventoryManager mgr = FindFirstObjectByType<InventoryManager>();
                if (originalGrid != null && originalGrid == mgr.externalStorageGrid && originalGrid != bestGrid && originalGrid.activeItems.Count == 0)
                {
                    originalGrid.gameObject.SetActive(false);
                }

                itemBeingDragged = null;
                return;
            }
            else Debug.LogWarning("Drop Rejected: Space is occupied!");
        }

        ReturnToOrigin();
        itemBeingDragged = null;
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
        // 1. Check if the grid pushed it entirely off the screen while we were holding it
        if (ejectedFromRig)
        {
            InventoryManager mgr = FindFirstObjectByType<InventoryManager>();
            if (mgr != null) mgr.DiscardItemToWorld(gameObject);
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

            // Returns to the NEW LIVE MEMORY coordinate
            originalGrid.RegisterItem(gameObject, originalAnchorCoord, sizeX, sizeY, isRotated);
            rectTransform.localPosition = originalLocalPosition;
        }

        Vector3 lp = rectTransform.localPosition; lp.z = 0; rectTransform.localPosition = lp;
    }
}
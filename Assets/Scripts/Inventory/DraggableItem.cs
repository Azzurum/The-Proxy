using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(LayoutElement))]
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("Item Data")]
    public ItemData itemData;
    public string itemName = "Unknown Object";
    [TextArea] public string itemDescription = "A strange item with unknown properties.";

    [Header("Item Footprint")]
    public ItemFootprint footprint;
    public bool isRotated = false;

    [Header("Layout")]
    public float cellSize = 80f;

    [HideInInspector] public Transform parentAfterDrag;
    [HideInInspector] public static DraggableItem itemBeingDragged;
    public bool ejectedFromRig = false;

    [HideInInspector] public Transform originalParent;
    [HideInInspector] public Vector3 originalLocalPosition;
    [HideInInspector] public bool dropAccepted = false;
    private Vector3 initialCanvasPosition;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Canvas canvas;
    private LayoutElement layoutElement;
    private Coroutine animateCoroutine;
    private UIItem uiItem;
    private Text rotationHintText;
    public bool showRotationHint = true;
    public string rotationHintLabel = "R";

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>()?.rootCanvas;
        layoutElement = GetComponent<LayoutElement>();
        uiItem = GetComponent<UIItem>();
        
        if (layoutElement != null)
        {
            layoutElement.ignoreLayout = true;
        }

        // Dynamically steal the exact cell size from the grid as soon as the object is created
        GridLayoutGroup grid = GetComponentInParent<GridLayoutGroup>();
        if (grid != null) cellSize = grid.cellSize.x;

        CreateRotationHint();
    }

    void Start()
    {
        if (itemData != null)
        {
            itemName = itemData.itemName;
            itemDescription = itemData.itemDescription;
        }

        UpdateVisualSize();
    }

    void Update()
    {
        if (itemBeingDragged == this && Input.GetKeyDown(KeyCode.R)) RotateItem();
    }

    public void SetFootprint(ItemFootprint newFootprint)
    {
        footprint = newFootprint;
        UpdateVisualSize();
    }

    public void SetCellSize(float newCellSize)
    {
        cellSize = newCellSize;
        UpdateVisualSize();
    }

    public void UpdateVisualSize()
    {
        if (footprint == null)
        {
            footprint = new ItemFootprint(1, 1);
        }

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null) return;
        }

        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(footprint.width * cellSize, footprint.height * cellSize);

        ApplyRotationVisual();
    }

    private void RotateItem()
    {
        isRotated = !isRotated;
        footprint = footprint.GetRotated();
        UpdateVisualSize();
    }

    private void ApplyRotationVisual()
    {
        if (uiItem != null && uiItem.displayImage != null)
        {
            uiItem.displayImage.rectTransform.localEulerAngles = new Vector3(0f, 0f, isRotated ? 90f : 0f);
        }
    }

    public void OnPointerClick(PointerEventData eventData) {}

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (itemData != null && itemData.itemID == "CRPT") return; // Block corruption drag

        itemBeingDragged = this;
        ejectedFromRig = false;
        dropAccepted = false;
        parentAfterDrag = transform.parent;
        originalParent = transform.parent;
        originalLocalPosition = rectTransform.localPosition;
        initialCanvasPosition = canvas.transform.position;

        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();

        // Update data after parenting so original slot is empty
        InventoryManager manager = FindFirstObjectByType<InventoryManager>();
        if (manager != null) manager.SyncDataFromUI();

        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.8f;

        SetRotationHintVisible(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (itemData != null && itemData.itemID == "CRPT") return;

        // With pivot at center, position directly at mouse, adjusted for canvas movement
        rectTransform.position = Input.mousePosition + (canvas.transform.position - initialCanvasPosition);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (itemData != null && itemData.itemID == "CRPT") return;

        itemBeingDragged = null;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        ClearAllSlotHighlights();
        SetRotationHintVisible(false);

        if (ejectedFromRig)
        {
            InventoryManager manager = FindFirstObjectByType<InventoryManager>();
            if (manager != null) manager.DiscardItemToWorld(gameObject);
            return;
        }

        if (dropAccepted && parentAfterDrag != null)
        {
            StartSmoothPlacement(parentAfterDrag);
        }
        else
        {
            StartRejectedReturn();
        }
    }

    private Vector3 GetTargetLocalPositionForParent(Transform targetParent)
    {
        float halfCell = cellSize / 2f;
        return new Vector3((footprint.width - 1) * halfCell, -(footprint.height - 1) * halfCell, 0f);
    }

    private void StartSmoothPlacement(Transform targetParent)
    {
        if (animateCoroutine != null) StopCoroutine(animateCoroutine);
        animateCoroutine = StartCoroutine(SmoothMoveToParent(targetParent, GetTargetLocalPositionForParent(targetParent), 0.18f));
    }

    private void StartSmoothReturn()
    {
        if (originalParent == null)
        {
            originalParent = transform.parent;
        }
        if (animateCoroutine != null) StopCoroutine(animateCoroutine);
        animateCoroutine = StartCoroutine(SmoothMoveToParent(originalParent, originalLocalPosition, 0.18f));
    }

    private void StartRejectedReturn()
    {
        if (originalParent == null)
        {
            originalParent = transform.parent;
        }
        if (animateCoroutine != null) StopCoroutine(animateCoroutine);
        animateCoroutine = StartCoroutine(RejectedReturnCoroutine(originalParent, originalLocalPosition, 0.22f));
    }

    private System.Collections.IEnumerator RejectedReturnCoroutine(Transform targetParent, Vector3 targetLocalPosition, float duration)
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null) yield break;
        }

        Vector3 startWorld = rectTransform.position;
        transform.SetParent(targetParent, true);
        rectTransform.position = startWorld;

        Vector3 startLocal = rectTransform.localPosition;
        Color originalColor = Color.white;
        if (uiItem != null && uiItem.backgroundImage != null)
        {
            originalColor = uiItem.backgroundImage.color;
        }

        float shakeMagnitude = cellSize * 0.08f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / duration);
            float shake = Mathf.Sin(timer * 40f) * shakeMagnitude * (1f - t);
            rectTransform.localPosition = Vector3.Lerp(startLocal, targetLocalPosition, t) + new Vector3(shake, Mathf.Abs(shake) * 0.5f, 0f);

            if (uiItem != null && uiItem.backgroundImage != null)
            {
                float pulse = Mathf.Sin(timer * 20f) * 0.5f + 0.5f;
                uiItem.backgroundImage.color = Color.Lerp(originalColor, Color.red, pulse * 0.5f);
            }

            yield return null;
        }

        rectTransform.localPosition = targetLocalPosition;
        if (uiItem != null && uiItem.backgroundImage != null)
        {
            uiItem.backgroundImage.color = originalColor;
        }

        animateCoroutine = null;

        InventoryManager inventoryManager = FindFirstObjectByType<InventoryManager>();
        if (inventoryManager != null)
        {
            inventoryManager.SyncDataFromUI();
        }
    }

    private System.Collections.IEnumerator SmoothMoveToParent(Transform targetParent, Vector3 targetLocalPosition, float duration)
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null) yield break;
        }

        Vector3 startWorld = rectTransform.position;
        transform.SetParent(targetParent, true);
        rectTransform.position = startWorld;

        Vector3 startLocal = rectTransform.localPosition;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / duration);
            rectTransform.localPosition = Vector3.Lerp(startLocal, targetLocalPosition, t);
            yield return null;
        }

        rectTransform.localPosition = targetLocalPosition;
        animateCoroutine = null;

        InventoryManager inventoryManager = FindFirstObjectByType<InventoryManager>();
        if (inventoryManager != null)
        {
            inventoryManager.SyncDataFromUI();
        }
    }

    /// <summary>
    /// Returns the list of grid cells occupied by this item at a given position
    /// </summary>
    public List<Vector2Int> GetOccupiedCells(Vector2Int gridPosition)
    {
        List<Vector2Int> occupied = new List<Vector2Int>();
        List<Vector2Int> footprintCells = footprint.GetOccupiedCells();
        
        foreach (Vector2Int cell in footprintCells)
        {
            occupied.Add(gridPosition + cell);
        }
        
        return occupied;
    }

    private void CreateRotationHint()
    {
        if (!showRotationHint) return;
        if (rotationHintText != null) return;

        Transform existing = transform.Find("RotationHint");
        if (existing != null)
        {
            rotationHintText = existing.GetComponent<Text>();
        }

        if (rotationHintText == null)
        {
            GameObject hintObject = new GameObject("RotationHint", typeof(RectTransform));
            hintObject.transform.SetParent(transform, false);
            RectTransform hintRect = hintObject.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(1f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0f);
            hintRect.pivot = new Vector2(1f, 0f);
            hintRect.anchoredPosition = new Vector2(-6f, 6f);
            hintRect.sizeDelta = new Vector2(32f, 24f);

            rotationHintText = hintObject.AddComponent<Text>();
            rotationHintText.raycastTarget = false;
            rotationHintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rotationHintText.fontSize = 18;
            rotationHintText.fontStyle = FontStyle.Bold;
            rotationHintText.alignment = TextAnchor.LowerRight;
            rotationHintText.color = new Color(1f, 1f, 1f, 0.92f);
            rotationHintText.text = rotationHintLabel;
            rotationHintText.enabled = false;
        }
    }

    private void SetRotationHintVisible(bool visible)
    {
        if (rotationHintText == null)
        {
            CreateRotationHint();
        }
        if (rotationHintText != null)
        {
            rotationHintText.enabled = visible;
        }
    }

    private void ClearAllSlotHighlights()
    {
        InventoryManager manager = FindFirstObjectByType<InventoryManager>();
        if (manager == null) return;

        // Clear highlights on all grids
        ClearGridHighlights(manager.gridLeft);
        ClearGridHighlights(manager.gridRight);
        if (manager.gridExt != null) ClearGridHighlights(manager.gridExt);
    }

    private void ClearGridHighlights(Transform grid)
    {
        if (grid == null) return;
        foreach (Transform slot in grid)
        {
            ItemSlot itemSlot = slot.GetComponent<ItemSlot>();
            if (itemSlot != null)
            {
                itemSlot.ClearHighlight();
            }
        }
    }
}
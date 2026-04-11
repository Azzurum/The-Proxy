using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("Item Data")]
    public ItemData itemData;
    public string itemName = "Unknown Object";
    [TextArea] public string itemDescription = "A strange item with unknown properties.";

    [Header("Item Footprint")]
    public ItemFootprint footprint;
    public bool isRotated = false;

    public int sizeX => footprint != null ? footprint.width : 1;
    public int sizeY => footprint != null ? footprint.height : 1;

    [Header("Layout")]
    public float cellSize = 80f;

    [HideInInspector] public Transform parentAfterDrag;
    [HideInInspector] public static DraggableItem itemBeingDragged;
    public bool ejectedFromRig = false;

    [HideInInspector] public Transform originalParent;
    [HideInInspector] public Vector2 originalAnchoredPosition;
    [HideInInspector] public bool dropAccepted = false;

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
        
        if (layoutElement != null) layoutElement.ignoreLayout = true;
        CreateRotationHint();
    }

    void Start()
    {
        if (itemData != null)
        {
            itemName = itemData.itemName;
            itemDescription = itemData.itemDescription;
            isRotated = itemData.isRotated; 
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
        ItemFootprint baseFp = itemData != null ? itemData.GetFootprint() : new ItemFootprint(1, 1);
        if (rectTransform == null) { rectTransform = GetComponent<RectTransform>(); if (rectTransform == null) return; }

        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(baseFp.width * cellSize, baseFp.height * cellSize);
        rectTransform.localEulerAngles = new Vector3(0f, 0f, isRotated ? -90f : 0f);

        footprint = isRotated ? baseFp.GetRotated() : baseFp;

        if (itemBeingDragged != this)
        {
            // Lock strict scale and Top-Left grid anchors ONLY when resting
            rectTransform.localScale = Vector3.one;
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);

            float visualWidth = footprint.width * cellSize;
            float visualHeight = footprint.height * cellSize;
            rectTransform.anchoredPosition = new Vector2(visualWidth / 2f, -(visualHeight / 2f));
        }
    }

    private void RotateItem()
    {
        isRotated = !isRotated;
        if (itemData != null) itemData.isRotated = isRotated; 
        
        UpdateVisualSize();

        if (itemBeingDragged == this)
        {
            UpdateDragPosition(null);
        }
    }

    public void OnPointerClick(PointerEventData eventData) {}

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (itemData != null && itemData.itemID == "CRPT") return; 

        itemBeingDragged = this;
        ejectedFromRig = false;
        dropAccepted = false;
        parentAfterDrag = transform.parent;
        originalParent = transform.parent;
        originalAnchoredPosition = rectTransform.anchoredPosition; 

        // 1. Reparent using TRUE so Unity automatically adjusts localScale to prevent the giant "Balloon" effect
        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();
        
        // 2. FIX: Temporarily swap anchors to Center (0.5) so the Canvas Mouse Math perfectly aligns without teleporting!
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

        Vector3 cleanPos = rectTransform.localPosition;
        cleanPos.z = 0f; 
        rectTransform.localPosition = cleanPos;
        
        UpdateDragPosition(eventData); 

        InventoryManager manager = FindAnyObjectByType<InventoryManager>();
        if (manager != null) manager.SyncDataFromUI();

        Canvas myCanvas = GetComponent<Canvas>();
        if (myCanvas != null) myCanvas.sortingOrder = 20;

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.8f;
        SetRotationHintVisible(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (itemData != null && itemData.itemID == "CRPT") return;
        UpdateDragPosition(eventData);
    }

    private void UpdateDragPosition(PointerEventData eventData)
    {
        if (canvas == null) return;
        
        Vector2 pointerPos = eventData != null ? eventData.position : (Vector2)Input.mousePosition;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)canvas.transform,
            pointerPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out Vector2 localPoint))
        {
            rectTransform.anchoredPosition = localPoint;
        }
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
            InventoryManager manager = FindAnyObjectByType<InventoryManager>();
            if (manager != null) manager.DiscardItemToWorld(gameObject);
            return;
        }

        if (dropAccepted && parentAfterDrag != null) 
        {
            InventorySlot hoverSlot = parentAfterDrag.GetComponent<InventorySlot>();
            if (hoverSlot != null && footprint != null)
            {
                int offsetX = -Mathf.FloorToInt(footprint.width / 2f);
                int offsetY = -Mathf.FloorToInt(footprint.height / 2f);
                
                int targetX = hoverSlot.slotCoordinate.x + offsetX;
                int targetY = hoverSlot.slotCoordinate.y + offsetY;
                
                Transform gridTransform = hoverSlot.transform.parent;
                int cols = 5; 
                
                int targetIndex = targetY * cols + targetX;
                if (targetIndex >= 0 && targetIndex < gridTransform.childCount)
                {
                    parentAfterDrag = gridTransform.GetChild(targetIndex);
                }
            }
            StartSmoothPlacement(parentAfterDrag);
        }
        else StartRejectedReturn();
    }

    private Vector2 GetTargetAnchoredPosition() 
    { 
        float visualWidth = footprint.width * cellSize;
        float visualHeight = footprint.height * cellSize;
        return new Vector2(visualWidth / 2f, -(visualHeight / 2f)); 
    }

    private void StartSmoothPlacement(Transform targetParent)
    {
        if (animateCoroutine != null) StopCoroutine(animateCoroutine);
        animateCoroutine = StartCoroutine(SmoothMoveToParent(targetParent, GetTargetAnchoredPosition(), 0.18f));
    }

    private void StartSmoothReturn()
    {
        if (originalParent == null) originalParent = transform.parent;
        if (animateCoroutine != null) StopCoroutine(animateCoroutine);
        animateCoroutine = StartCoroutine(SmoothMoveToParent(originalParent, originalAnchoredPosition, 0.18f));
    }

    private void StartRejectedReturn()
    {
        if (originalParent == null) originalParent = transform.parent;
        if (animateCoroutine != null) StopCoroutine(animateCoroutine);
        animateCoroutine = StartCoroutine(RejectedReturnCoroutine(originalParent, originalAnchoredPosition, 0.22f));
    }

    private System.Collections.IEnumerator RejectedReturnCoroutine(Transform targetParent, Vector2 targetAnchoredPosition, float duration)
    {
        if (rectTransform == null) yield break;

        // Reparent using TRUE so it doesn't visually jump
        transform.SetParent(targetParent, true); 
        
        // Restore strict Top-Left grid anchors BEFORE reading coordinates
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);

        Vector2 startAnchored = rectTransform.anchoredPosition; 
        Vector3 startScale = rectTransform.localScale; 

        Vector3 cleanPos = rectTransform.localPosition;
        cleanPos.z = 0f;
        rectTransform.localPosition = cleanPos;

        Color originalColor = Color.white;
        if (uiItem != null && uiItem.backgroundImage != null) originalColor = uiItem.backgroundImage.color;

        float shakeMagnitude = cellSize * 0.08f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / duration);
            float shake = Mathf.Sin(timer * 40f) * shakeMagnitude * (1f - t);
            
            rectTransform.anchoredPosition = Vector2.Lerp(startAnchored, targetAnchoredPosition, t) + new Vector2(shake, Mathf.Abs(shake) * 0.5f);
            
            // Gently snap scale back to exactly 1 over the animation
            rectTransform.localScale = Vector3.Lerp(startScale, Vector3.one, t);

            if (uiItem != null && uiItem.backgroundImage != null)
            {
                float pulse = Mathf.Sin(timer * 20f) * 0.5f + 0.5f;
                uiItem.backgroundImage.color = Color.Lerp(originalColor, Color.red, pulse * 0.5f);
            }
            yield return null;
        }

        rectTransform.anchoredPosition = targetAnchoredPosition;
        rectTransform.localScale = Vector3.one; 
        if (uiItem != null && uiItem.backgroundImage != null) uiItem.backgroundImage.color = originalColor;
        
        UpdateVisualSize(); 
        animateCoroutine = null;

        Canvas myCanvas = GetComponent<Canvas>();
        InventorySlot slot = targetParent.GetComponent<InventorySlot>();
        if (myCanvas != null && slot != null) myCanvas.sortingOrder = (slot.gridRegion == InventorySlot.GridRegion.External) ? 1 : 5;

        InventoryManager inventoryManager = FindAnyObjectByType<InventoryManager>();
        if (inventoryManager != null) inventoryManager.SyncDataFromUI();
    }

    private System.Collections.IEnumerator SmoothMoveToParent(Transform targetParent, Vector2 targetAnchoredPosition, float duration)
    {
        if (rectTransform == null) yield break;

        // Reparent using TRUE so it doesn't visually jump
        transform.SetParent(targetParent, true);
        
        // Restore strict Top-Left grid anchors BEFORE reading coordinates
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);

        Vector2 startAnchored = rectTransform.anchoredPosition;
        Vector3 startScale = rectTransform.localScale;

        Vector3 cleanPos = rectTransform.localPosition;
        cleanPos.z = 0f;
        rectTransform.localPosition = cleanPos;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / duration);
            
            rectTransform.anchoredPosition = Vector2.Lerp(startAnchored, targetAnchoredPosition, t);
            
            // Gently snap scale back to exactly 1 over the animation
            rectTransform.localScale = Vector3.Lerp(startScale, Vector3.one, t);
            
            yield return null;
        }

        rectTransform.anchoredPosition = targetAnchoredPosition;
        rectTransform.localScale = Vector3.one; 
        
        UpdateVisualSize(); 
        animateCoroutine = null;

        Canvas myCanvas = GetComponent<Canvas>();
        InventorySlot slot = targetParent.GetComponent<InventorySlot>();
        if (myCanvas != null && slot != null) myCanvas.sortingOrder = (slot.gridRegion == InventorySlot.GridRegion.External) ? 1 : 5;

        InventoryManager inventoryManager = FindAnyObjectByType<InventoryManager>();
        if (inventoryManager != null) inventoryManager.SyncDataFromUI();
    }

    public List<Vector2Int> GetOccupiedCells(Vector2Int gridPosition)
    {
        List<Vector2Int> occupied = new List<Vector2Int>();
        if (footprint == null) return occupied;

        int offsetX = -Mathf.FloorToInt(footprint.width / 2f);
        int offsetY = -Mathf.FloorToInt(footprint.height / 2f);

        List<Vector2Int> footprintCells = footprint.GetOccupiedCells();
        foreach (Vector2Int cell in footprintCells) 
        {
            occupied.Add(new Vector2Int(gridPosition.x + offsetX + cell.x, gridPosition.y + offsetY + cell.y));
        }
        return occupied;
    }

    private void CreateRotationHint()
    {
        if (!showRotationHint) return;
        if (rotationHintText != null) return;

        Transform existing = transform.Find("RotationHint");
        if (existing != null) rotationHintText = existing.GetComponent<Text>();

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
        if (rotationHintText == null) CreateRotationHint();
        if (rotationHintText != null) rotationHintText.enabled = visible;
    }

    private void ClearAllSlotHighlights()
    {
        InventoryManager manager = FindAnyObjectByType<InventoryManager>();
        if (manager == null) return;
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
            if (itemSlot != null) itemSlot.ClearHighlight();
        }
    }
}
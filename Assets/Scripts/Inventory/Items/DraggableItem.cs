using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Manages the behavior of a UI item that can be clicked, dragged, rotated, and dropped into inventory slots.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler
{
    [Header("Item Data")]
    [Tooltip("The ScriptableObject that defines this item's properties.")]
    public ItemData itemData;
    [Tooltip("Display name of the item.")]
    public string itemName = "Unknown Object";
    [Tooltip("Flavor text or description for the item inspector.")]
    [TextArea] public string itemDescription = "A strange item with unknown properties.";

    [Header("Item Footprint")]
    [Tooltip("The spatial layout of the item on the grid.")]
    public ItemFootprint footprint;
    [Tooltip("Is the item currently rotated 90 degrees?")]
    public bool isRotated = false;

    public int sizeX => footprint != null ? footprint.width : 1;
    public int sizeY => footprint != null ? footprint.height : 1;

    [Header("Layout")]
    [Tooltip("The size of a single grid cell, used for calculating visual dimensions.")]
    public float cellSize = 80f;

    [Header("Drag State (Runtime)")]
    [Tooltip("If true, the item will be destroyed and spawned in the world on drop.")]
    public bool ejectedFromRig = false;
    [Tooltip("Should the 'Press R to Rotate' hint be shown during drag?")]
    public bool showRotationHint = true;
    [Tooltip("The text to display for the rotation hint.")]
    public string rotationHintLabel = "R";

    [HideInInspector] public Transform parentAfterDrag;
    [HideInInspector] public static DraggableItem itemBeingDragged;
    [HideInInspector] public Transform originalParent;
    [HideInInspector] public Vector2 originalAnchoredPosition;
    [HideInInspector] public bool dropAccepted = false;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Canvas rootCanvas;
    private Coroutine animateCoroutine;
    private UIItem uiItem;
    private Text rotationHintText;
    private Vector3 _dragOffset;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        
        // This component is added by UIItem, but we can ensure it's ignored here.
        if (TryGetComponent<LayoutElement>(out var layoutElement))
        {
            layoutElement.ignoreLayout = true;
        }

        uiItem = GetComponent<UIItem>();

        CreateRotationHint();
    }

    void OnDestroy()
    {
        // SAFETY: Prevents a dangling static reference if the item is destroyed or the scene unloads mid-drag.
        if (itemBeingDragged == this)
        {
            itemBeingDragged = null;
        }
    }

    void Start()
    {
        if (itemData != null)
        {
            itemName = itemData.itemName;
            itemDescription = itemData.description;
            isRotated = itemData.isRotated;
        }
        UpdateVisualSize();
    }

    void Update()
    {
        if (itemBeingDragged == this && Input.GetKeyDown(KeyCode.R)) RotateItem();
    }

    /// <summary>
    /// Assigns a new footprint to this item and updates its visual representation.
    /// </summary>
    public void SetFootprint(ItemFootprint newFootprint)
    {
        footprint = newFootprint;
        UpdateVisualSize();
    }

    /// <summary>
    /// Updates the cell size used for layout calculations and refreshes the item's visual representation.
    /// </summary>
    public void SetCellSize(float newCellSize)
    {
        cellSize = newCellSize;
        UpdateVisualSize();
    }

    /// <summary>
    /// Recalculates and applies the item's size, rotation, and pivot based on its data and parent container.
    /// </summary>
    public void UpdateVisualSize()
    {
        ItemFootprint baseFp = itemData != null ? itemData.GetFootprint() : new ItemFootprint(1, 1);
        if (rectTransform == null) { rectTransform = GetComponent<RectTransform>(); if (rectTransform == null) return; }

        footprint = isRotated ? baseFp.GetRotated() : baseFp;

        // Determine if the item is currently in a hotbar slot.
        bool inHotbar = false;
        if (transform.parent != null && transform.parent.GetComponent<HotbarSlot>() != null) inHotbar = true;
        
        // --- FIX: Force full size while dragging so you can clearly see the real footprint! ---
        if (itemBeingDragged == this) inHotbar = false;

        // --- DEDICATED RAYCAST HITBOX FIX ---
        // We create a dedicated, invisible child Image to act purely as a reliable click/drag surface!
        // This prevents the root Image from being accidentally disabled, ensuring right-clicks and hotbar drags ALWAYS work.
        Transform hitboxTrans = transform.Find("DragHitbox");
        Image hitbox = null;
        if (hitboxTrans == null)
        {
            GameObject go = new GameObject("DragHitbox", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            go.transform.SetAsLastSibling(); // Render on top to catch clicks first
            hitbox = go.GetComponent<Image>();
            hitbox.color = new Color(0, 0, 0, 0); // Invisible
            
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        else
        {
            hitbox = hitboxTrans.GetComponent<Image>();
            hitboxTrans.SetAsLastSibling();
        }
        hitbox.raycastTarget = true;
        hitbox.enabled = true; 
        // ------------------------------------

        if (inHotbar)
        {
            float activeCellSize = cellSize > 10f ? cellSize : 75f; // Failsafe against 0 scale
            rectTransform.sizeDelta = new Vector2(activeCellSize, activeCellSize);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.localEulerAngles = Vector3.zero;

            if (uiItem != null) uiItem.SetTetrisGridVisibility(false);

            if (itemBeingDragged != this)
            {
                // Center the item within the hotbar slot.
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.localScale = Vector3.one;
            }
            return;
        }

        // Standard grid item layout logic.
        if (uiItem != null) uiItem.SetTetrisGridVisibility(true);
        
        rectTransform.sizeDelta = new Vector2(baseFp.width * cellSize, baseFp.height * cellSize);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.localEulerAngles = new Vector3(0f, 0f, isRotated ? -90f : 0f);

        if (itemBeingDragged != this)
        {
            rectTransform.localScale = Vector3.one;
            rectTransform.anchorMin = new Vector2(0f, 1f); // Top-left anchor for grid slots.
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

    /// <summary>
    /// Handles left-click for inspection and right-click for item usage.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Left-click sends the item to the inspection panel.
            if (UIInspectorManager.Instance != null && itemData != null)
                UIInspectorManager.Instance.InspectItem(itemData);

            if (InventoryManager.Instance != null && itemData != null)
                InventoryManager.Instance.SetInspectionIcon(itemData.icon);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Right-click attempts to use the item (moved to PointerDown to bypass Unity's drag-cancellation of clicks!)
            ItemUsageManager usageManager = FindAnyObjectByType<ItemUsageManager>();
            if (usageManager != null && itemData != null)
            {
                usageManager.ExecuteItem(itemData, gameObject);
            }
        }
    }

    /// <summary>
    /// Called when a drag operation begins. Caches original state and prepares the item for floating.
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        // Only allow dragging with the Left Mouse Button! (Fixes right-click consumable bug)
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (itemData != null && itemData.itemID == "CRPT") return;

        itemBeingDragged = this;
        ejectedFromRig = false;
        dropAccepted = false;
        parentAfterDrag = transform.parent;
        originalParent = transform.parent;
        originalAnchoredPosition = rectTransform.anchoredPosition;

        // Notify the source hotbar slot that the item is being moved.
        if (originalParent != null)
        {
            HotbarSlot slot = originalParent.GetComponent<HotbarSlot>();
            if (slot != null)
            {
                slot.DetachItem();
            }
        }

        transform.SetParent(rootCanvas.transform, true);
        transform.SetAsLastSibling();

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

        Vector3 cleanPos = rectTransform.localPosition;
        cleanPos.z = 0f;
        rectTransform.localPosition = cleanPos;
        rectTransform.localScale = Vector3.one; // Force it back to full size immediately when leaving the hotbar!

        UpdateVisualSize();
        UpdateDragPosition(eventData);

        if (InventoryManager.Instance != null) InventoryManager.Instance.SyncDataFromUI();

        Canvas myCanvas = GetComponent<Canvas>();
        if (myCanvas != null) myCanvas.sortingOrder = 40; // High default float priority during drag

        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;

        canvasGroup.alpha = 0.8f;
        SetRotationHintVisible(true);
    }

    /// <summary>
    /// Called every frame during a drag operation to update the item's position.
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (itemData != null && itemData.itemID == "CRPT") return;
        UpdateDragPosition(eventData);
    }

    /// <summary>
    /// Updates the item's position to follow the cursor during a drag.
    /// </summary>
    private void UpdateDragPosition(PointerEventData eventData)
    {
        if (rootCanvas == null) return;

        Vector2 pointerPos = eventData != null ? eventData.position : (Vector2)Input.mousePosition;
            Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                (RectTransform)rootCanvas.transform, pointerPos, cam, out Vector3 worldPoint))
            {
                transform.position = worldPoint + _dragOffset;
                
                Vector3 localPos = transform.localPosition;
                localPos.z = 0f;
                transform.localPosition = localPos;
        }
    }

    /// <summary>
    /// Called when a drag operation ends. Determines whether to place the item or return it.
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (itemData != null && itemData.itemID == "CRPT") return;

        itemBeingDragged = null;

        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;

        canvasGroup.alpha = 1f;

        ClearAllSlotHighlights();
        SetRotationHintVisible(false);

            // --- FAILSAFE: Manually accept Hotbar drops in case Unity's EventSystem dropped the event ---
            if (!dropAccepted && eventData != null && eventData.pointerEnter != null)
            {
                HotbarSlot hoveredHotbar = eventData.pointerEnter.GetComponentInParent<HotbarSlot>();
                if (hoveredHotbar != null)
                {
                    parentAfterDrag = hoveredHotbar.transform;
                    dropAccepted = true;
                }
            }

        // --- FIX: Detect if the item was dropped completely off the UI to eject it into the world! ---
        if (!dropAccepted)
        {
            bool droppedOnValidGrid = false;
            if (eventData != null && eventData.pointerEnter != null)
            {
                // Check if we dropped it anywhere inside an inventory or hotbar panel
                if (eventData.pointerEnter.GetComponentInParent<InventorySlot>() != null || 
                    eventData.pointerEnter.GetComponentInParent<HotbarSlot>() != null ||
                    eventData.pointerEnter.GetComponentInParent<DraggableItem>() != null)
                {
                    droppedOnValidGrid = true;
                }

                // --- DISCARD AREA FIX ---
                // If you drop the item on a custom UI panel named "Discard", force the ejection!
                Transform currentTransform = eventData.pointerEnter.transform;
                while (currentTransform != null)
                {
                    string hoverName = currentTransform.name.ToLower();
                    if (hoverName.Contains("discard") || hoverName.Contains("trash") || hoverName.Contains("eject") || hoverName.Contains("world") || hoverName.Contains("drop"))
                    {
                        droppedOnValidGrid = false;
                        break;
                    }
                    currentTransform = currentTransform.parent;
                }
            }

            if (!droppedOnValidGrid) ejectedFromRig = true;
        }

        // If the item was dragged off the grid, spawn it in the world.
        if (ejectedFromRig)
        {
            // Failsafe Warning: If the item vanishes but doesn't spawn in the world, tell the developer exactly why!
            if (itemData != null && itemData.worldPrefab == null)
            {
                Debug.LogError($"<color=red>CANNOT SPAWN ITEM:</color> '{itemData.itemName}' does not have a World Prefab assigned in its ItemData! The UI item was deleted but nothing spawned.");
            }

            if (itemData != null && itemData.worldPrefab != null)
            {
                if (WorldItemSpawner.Instance != null)
                {
                    WorldItemSpawner.Instance.EjectItem(itemData);
                }
                else
                {
                    // BULLETPROOF FALLBACK: If the Spawner is missing from the scene entirely, spawn it manually!
                    Debug.LogWarning($"<color=yellow>[WARNING]</color> No WorldItemSpawner found in the scene! Spawning '{itemData.itemName}' using automatic fallback.");
                    
                    PlayerController player = FindAnyObjectByType<PlayerController>();
                    Vector3 spawnPos = player != null ? player.transform.position : Vector3.zero;
                    
                    GameObject droppedItem = Instantiate(itemData.worldPrefab, spawnPos, Quaternion.identity);
                    PhysicalItem pi = droppedItem.GetComponent<PhysicalItem>();
                    if (pi != null)
                    {
                        Vector3 targetPos = spawnPos + (Vector3)Random.insideUnitCircle * 2f;
                        pi.TriggerDropAnimation(spawnPos, targetPos);
                    }
                }
            }
            Destroy(gameObject); // Destroy the UI representation!

            // Instantly update the backend data so the game knows the item is gone!
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.SyncDataFromUI();
                InventoryManager.Instance.OnItemDroppedSignal();
            }
            return;
        }

        if (dropAccepted && parentAfterDrag != null)
        {
            HotbarSlot hotbarSlot = parentAfterDrag.GetComponent<HotbarSlot>();
            if (hotbarSlot != null)
            {
                // Finalize the drop into the hotbar slot.
                hotbarSlot.containedItem = this;
                this.dropAccepted = true;

                if (HotbarManager.Instance != null)
                {
                    // Dynamically find which slot it was dropped into instead of hardcoding 0!
                    for (int i = 0; i < HotbarManager.Instance.quickSlots.Length; i++)
                    {
                        if (HotbarManager.Instance.quickSlots[i] == hotbarSlot)
                        {
                            HotbarManager.Instance.EquipSlot(i);
                            break;
                        }
                    }
                }

                StartSmoothPlacement(parentAfterDrag);
                return;
            }

            InventorySlot hoverSlot = parentAfterDrag.GetComponent<InventorySlot>();
            if (hoverSlot != null && footprint != null)
            {
                // Calculate the correct anchor slot based on the item's center and footprint.
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
        else 
        {
            // FIX: Restore the item to the hotbar data if the drop was rejected so it isn't wiped!
            if (originalParent != null)
            {
                HotbarSlot originalHotbar = originalParent.GetComponent<HotbarSlot>();
                if (originalHotbar != null) originalHotbar.containedItem = this;
            }
            StartRejectedReturn();
        }
    }

    /// <summary>
    /// Calculates the final local position for the item within its new parent slot.
    /// </summary>
    private Vector2 GetTargetAnchoredPosition()
    {
        if (parentAfterDrag != null && parentAfterDrag.GetComponent<HotbarSlot>() != null)
        {
            return Vector2.zero;
        }

        // For grid items, calculate the offset based on the visual size to align the pivot correctly.
        float visualWidth = footprint.width * cellSize;
        float visualHeight = footprint.height * cellSize;
        return new Vector2(visualWidth / 2f, -(visualHeight / 2f));
    }

    /// <summary>
    /// Starts a coroutine to smoothly animate the item into its new parent.
    /// </summary>
    private void StartSmoothPlacement(Transform targetParent)
    {
        if (animateCoroutine != null) StopCoroutine(animateCoroutine);
        animateCoroutine = StartCoroutine(SmoothMoveToParent(targetParent, GetTargetAnchoredPosition(), 0.18f));
    }

    /// <summary>
    /// Starts a coroutine to animate the item returning to its original position after a failed drop.
    /// </summary>
    private void StartRejectedReturn()
    {
        if (originalParent == null) originalParent = transform.parent;
        if (animateCoroutine != null) StopCoroutine(animateCoroutine);
        animateCoroutine = StartCoroutine(RejectedReturnCoroutine(originalParent, originalAnchoredPosition, 0.22f));
    }

    /// <summary>
    /// Coroutine to animate the item returning to its original slot with a "shake" effect.
    /// </summary>
    private System.Collections.IEnumerator RejectedReturnCoroutine(Transform targetParent, Vector2 targetAnchoredPosition, float duration)
    {
        if (rectTransform == null) yield break;

        transform.SetParent(targetParent, true);

        if (targetParent.GetComponent<HotbarSlot>() != null)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        }
        else
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
        }

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

        // --- DYNAMIC LAYER SORTING FIX ---
        Canvas myCanvas = GetComponent<Canvas>();
        InventorySlot slot = targetParent.GetComponent<InventorySlot>();
        if (myCanvas != null)
        {
            Canvas rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            int baseOrder = (rootCanvas != null) ? rootCanvas.sortingOrder : 0;
            if (slot != null) {
                myCanvas.sortingOrder = (slot.gridRegion == InventorySlot.GridRegion.External) ? (baseOrder + 1) : (baseOrder + 5);
            } else {
                myCanvas.sortingOrder = baseOrder + 6; // Sit nicely in the hotbar!
            }
        }
    }

    /// <summary>
    /// Coroutine to smoothly move and re-parent the item to a new slot.
    /// </summary>
    private System.Collections.IEnumerator SmoothMoveToParent(Transform targetParent, Vector2 targetAnchoredPosition, float duration)
    {
        if (rectTransform == null) yield break;

        transform.SetParent(targetParent, true);

        if (targetParent.GetComponent<HotbarSlot>() != null)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        }
        else
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
        }

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
            rectTransform.localScale = Vector3.Lerp(startScale, Vector3.one, t);

            yield return null;
        }

        rectTransform.anchoredPosition = targetAnchoredPosition;
        rectTransform.localScale = Vector3.one;

        UpdateVisualSize();
        animateCoroutine = null;

        // --- DYNAMIC LAYER SORTING FIX ---
        Canvas myCanvas = GetComponent<Canvas>();
        InventorySlot slot = targetParent.GetComponent<InventorySlot>();
        if (myCanvas != null)
        {
            Canvas rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            int baseOrder = (rootCanvas != null) ? rootCanvas.sortingOrder : 0;
            if (slot != null) {
                myCanvas.sortingOrder = (slot.gridRegion == InventorySlot.GridRegion.External) ? (baseOrder + 1) : (baseOrder + 5);
            } else {
                myCanvas.sortingOrder = baseOrder + 6; // Sit nicely in the hotbar!
            }
        }

        if (InventoryManager.Instance != null) InventoryManager.Instance.SyncDataFromUI();
    }

    /// <summary>
    /// Calculates the list of grid coordinates this item would occupy if placed at a given anchor point.
    /// </summary>
    public List<Vector2Int> GetOccupiedCells(Vector2Int gridPosition)
    {
        List<Vector2Int> occupied = new List<Vector2Int>();
        if (footprint == null) return occupied;

        int offsetX = -Mathf.FloorToInt(footprint.width / 2f); // Centering offset
        int offsetY = -Mathf.FloorToInt(footprint.height / 2f);

        List<Vector2Int> footprintCells = footprint.GetOccupiedCells();
        foreach (Vector2Int cell in footprintCells)
        {
            occupied.Add(new Vector2Int(gridPosition.x + offsetX + cell.x, gridPosition.y + offsetY + cell.y));
        }
        return occupied;
    }

    /// <summary>
    /// Creates the small "R" text object used to indicate that the item can be rotated.
    /// </summary>
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
        if (InventoryManager.Instance == null) return;
        ClearGridHighlights(InventoryManager.Instance.gridLeft);
        ClearGridHighlights(InventoryManager.Instance.gridRight);
        if (InventoryManager.Instance.gridExt != null) ClearGridHighlights(InventoryManager.Instance.gridExt);
    }

    private void ClearGridHighlights(Transform grid)
    {
        if (grid == null) return;
        foreach (Transform slotTransform in grid)
        {
            if (slotTransform.TryGetComponent<ItemSlot>(out var itemSlot))
            {
                itemSlot.ClearHighlight();
            }
        }
    }

    /// <summary>
    /// Forcibly cancels an active drag, returning the item to its original position instantly.
    /// </summary>
    public void AbortDrag()
    {
        if (itemBeingDragged != this) return;

        itemBeingDragged = null;

        DraggableItem[] allItems = FindObjectsByType<DraggableItem>(FindObjectsInactive.Exclude);
        foreach (DraggableItem item in allItems)
        {
            if (item.canvasGroup != null) item.canvasGroup.blocksRaycasts = true;
        }

        canvasGroup.alpha = 1f;
        ClearAllSlotHighlights();
        SetRotationHintVisible(false);

        if (animateCoroutine != null)
        {
            StopCoroutine(animateCoroutine);
            animateCoroutine = null;
        }

        if (originalParent != null)
        {
            HotbarSlot originalHotbar = originalParent.GetComponent<HotbarSlot>();
            if (originalHotbar != null) originalHotbar.containedItem = this;

            transform.SetParent(originalParent, true);

            if (originalParent.GetComponent<HotbarSlot>() != null)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            }
            else
            {
                rectTransform.anchorMin = new Vector2(0f, 1f);
                rectTransform.anchorMax = new Vector2(0f, 1f);
            }

            Vector3 cleanPos = rectTransform.localPosition;
            cleanPos.z = 0f;
            rectTransform.localPosition = cleanPos;

            rectTransform.anchoredPosition = originalAnchoredPosition;
            rectTransform.localScale = Vector3.one;
        }

        UpdateVisualSize();
    }
}
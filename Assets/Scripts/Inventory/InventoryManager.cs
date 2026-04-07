using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class InventoryManager : MonoBehaviour
{
    [Header("Data & Layout Architecture")]
    public InventoryState inventoryState;
    public Transform gridLeft;
    public Transform gridRight;
    public Transform gridExt;
    public GameObject emptySlotPrefab;
    public GameObject filledItemPrefab;

    [Header("Grid Sizing")]
    public float cellSizeOverride = 0f; // Set to 0 to auto-calculate from prefab or grid size, otherwise specify a fixed size
    private float lastKnownCellSize = 80f;
    private bool gridRefreshPending = false;

    [Header("World Spawning")]
    public Transform playerTransform;
    public GameObject physicalBatteryPrefab;

    [Header("Corruption Setup")]
    public ItemData corruptionData;

    [Header("MOTHER-v4 System Shock")]
    public float shockInterval = 30f;
    private float shockTimer;
    private MetRigManager metRigManager;

    [Header("Signal Broadcasting")]
    public UnityEvent onItemDropped; // Broadcasts the ProxyAI alert and visual glitch

    [Header("System Constants")]
    public bool isSystemActive = true;
    public Slider systemShockProgressBar;

    [Header("Crush Penalties")]
    private int crushTier = 0;
    private float crushTimer = 0f;
    private float[] crushDurations = { 5f, 10f }; // Tier 1 and 2 durations, tier 3 permanent

    void Start()
    {
        // SAFETY CHECK: Ensure the lists are exactly the right size so the UI never crashes
        while (inventoryState.mainGridSlots.Count < 100) inventoryState.mainGridSlots.Add(null);
        while (inventoryState.extGridSlots.Count < 25) inventoryState.extGridSlots.Add(null);
        while (inventoryState.hotbarSlots.Count < 3) inventoryState.hotbarSlots.Add(null);

        metRigManager = FindFirstObjectByType<MetRigManager>();
        shockInterval = 30f; 
        shockTimer = shockInterval;
        Debug.Log($"Inventory auto-corruption interval set to {shockInterval} seconds.");

        RefreshAllGrids(); // Draw the UI initially
    }

    void Update()
    {
        // 1. MOTHER-v4 Shock Timer Logic
        if (isSystemActive && metRigManager != null && metRigManager.isRigOpen)
        {
            shockTimer -= Time.deltaTime;
            if (systemShockProgressBar != null) systemShockProgressBar.value = 1f - (shockTimer / shockInterval);

            if (shockTimer <= 0f)
            {
                ResolveCorruptionTick();
                shockTimer = shockInterval;
            }
        }
        else if (systemShockProgressBar != null)
        {
            systemShockProgressBar.value = 1f - (shockTimer / shockInterval);
        }

        // 2. Crush Penalties Logic
        if (crushTimer > 0)
        {
            crushTimer -= Time.deltaTime;
            if (crushTimer <= 0)
            {
                crushTier = Mathf.Max(0, crushTier - 1);
                Debug.Log($"Crush Penalty Tier degraded to {crushTier}");
                if (crushTier > 0)
                {
                    crushTimer = crushDurations[Mathf.Min(crushTier - 1, crushDurations.Length - 1)];
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.C)) ExecuteCleanProtocol();
    }

    // ================= DATA-DRIVEN CORRUPTION SYSTEM =================
    
    public void ResolveCorruptionTick()
    {   
        SyncDataFromUI();
        Debug.Log("SYSTEM SHOCK: Corruption Advancing...");

        int columns = 5;

        // 1. Eject Top Row Items from Left (indices 0-4) and Right (indices 50-54)
        for (int i = 0; i < columns; i++)
        {
            EjectItemIfValid(inventoryState.mainGridSlots[i]);          // Left Visor Top Row
            EjectItemIfValid(inventoryState.mainGridSlots[i + 50]);     // Right Visor Top Row
        }

        // 2. Shift all data UP by 1 row (5 indices)
        // Unity GridLayout starts Index 0 at Top-Left. So pushing "UP" means moving to a lower index.
        for (int i = 0; i < 45; i++)
        {
            inventoryState.mainGridSlots[i] = inventoryState.mainGridSlots[i + columns]; // Shift Left Visor
            inventoryState.mainGridSlots[i + 50] = inventoryState.mainGridSlots[i + 50 + columns]; // Shift Right Visor
        }

        // 3. Spawn new Corruption in the Bottom Row (indices 45-49 and 95-99)
        for (int i = 45; i < 50; i++)
        {
            inventoryState.mainGridSlots[i] = corruptionData;
            inventoryState.mainGridSlots[i + 50] = corruptionData;
        }

        CheckForGameOver();
        RefreshAllGrids(); // Visually update the UI to match the new data state!
    }

    public void ExecuteCleanProtocol()
    {
        bool didCleanAnything = false;
        int columns = 5;

        // Find and delete corruption on the bottom rows
        for (int i = 45; i < 50; i++)
        {
            if (inventoryState.mainGridSlots[i] == corruptionData) { inventoryState.mainGridSlots[i] = null; didCleanAnything = true; }
            if (inventoryState.mainGridSlots[i + 50] == corruptionData) { inventoryState.mainGridSlots[i + 50] = null; didCleanAnything = true; }
        }

        if (didCleanAnything)
        {
            // Shift everything DOWN by 1 row
            for (int i = 44; i >= 0; i--)
            {
                inventoryState.mainGridSlots[i + columns] = inventoryState.mainGridSlots[i];
                inventoryState.mainGridSlots[i + 50 + columns] = inventoryState.mainGridSlots[i + 50];
            }
            
            // Clear the top rows
            for (int i = 0; i < columns; i++)
            {
                inventoryState.mainGridSlots[i] = null;
                inventoryState.mainGridSlots[i + 50] = null;
            }

            RefreshAllGrids();
        }
    }

    private void EjectItemIfValid(ItemData itemData)
    {
        if (itemData != null && itemData != corruptionData)
        {
            // If it's a valid item (not corruption), eject it into the world physically
            if (physicalBatteryPrefab != null && playerTransform != null)
            {
                Instantiate(physicalBatteryPrefab, playerTransform.position, Quaternion.identity);
                Debug.Log($"Item {itemData.itemName} ejected from rig!");
            }
        }
    }

    // ================= VISUAL REFRESH LOGIC =================

    public void RefreshAllGrids()
    {
        if (!IsGridVisible())
        {
            gridRefreshPending = true;
            return;
        }

        gridRefreshPending = false;

        // 1. Destroy old UI objects
        foreach (Transform child in gridLeft) Destroy(child.gameObject);
        foreach (Transform child in gridRight) Destroy(child.gameObject);
        if(gridExt != null) foreach (Transform child in gridExt) Destroy(child.gameObject);

        // 2. Rebuild UI from Data State with manual positioning
        RefreshGrid(gridLeft, inventoryState.mainGridSlots, 5, 10, 0); // Left: 5x10
        RefreshGrid(gridRight, inventoryState.mainGridSlots, 5, 10, 50); // Right: 5x10, offset 50
        if (gridExt != null)
        {
            RefreshGrid(gridExt, inventoryState.extGridSlots, 5, 5, 0); // Ext: 5x5
        }
    }

    private bool IsGridVisible()
    {
        return (gridLeft != null && gridLeft.gameObject.activeInHierarchy)
            || (gridRight != null && gridRight.gameObject.activeInHierarchy)
            || (gridExt != null && gridExt.gameObject.activeInHierarchy);
    }

    public void RefreshAllGridsIfPending()
    {
        if (gridRefreshPending)
        {
            Debug.Log("InventoryManager: pending refresh triggered when UI became visible.");
            RefreshAllGrids();
        }
    }

    void RefreshGrid(Transform gridTransform, List<ItemData> dataList, int columns, int rows, int dataOffset)
    {
        float cellSize = 80f;
        RectTransform slotTemplateRect = emptySlotPrefab ? emptySlotPrefab.GetComponent<RectTransform>() : null;

        if (cellSizeOverride > 0f)
        {
            cellSize = cellSizeOverride;
            if (slotTemplateRect != null)
            {
                float prefabCellSize = slotTemplateRect.sizeDelta.x;
                if (prefabCellSize <= 0f) prefabCellSize = slotTemplateRect.rect.width;
                if (prefabCellSize > 0f && Mathf.Abs(prefabCellSize - cellSizeOverride) > 1f)
                {
                    Debug.LogWarning($"InventoryManager: cellSizeOverride ({cellSizeOverride}) does not match emptySlotPrefab width ({prefabCellSize}). This mismatch can cause visual scaling issues.");
                }
            }
        }
        else
        {
            float prefabCellSize = -1f;
            if (slotTemplateRect != null)
            {
                prefabCellSize = slotTemplateRect.sizeDelta.x;
                if (prefabCellSize <= 0f) prefabCellSize = slotTemplateRect.rect.width;
            }

            if (prefabCellSize > 0f && prefabCellSize < 500f)
            {
                cellSize = prefabCellSize;
            }
            else
            {
                RectTransform gridRect = gridTransform as RectTransform;
                if (gridRect != null && gridRect.gameObject.activeInHierarchy)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);
                    float widthCell = gridRect.rect.width / columns;
                    float heightCell = gridRect.rect.height / rows;
                    float computedCell = Mathf.Min(widthCell, heightCell);
                    if (computedCell > 0f && computedCell < 500f)
                    {
                        cellSize = computedCell;
                    }
                }
            }

            if (cellSize <= 0f || cellSize > 500f)
            {
                if (lastKnownCellSize > 0f && lastKnownCellSize <= 500f)
                {
                    cellSize = lastKnownCellSize;
                }
                else
                {
                    cellSize = 80f;
                }
            }
        }

        cellSize = Mathf.Clamp(cellSize, 20f, 250f);
        lastKnownCellSize = cellSize;

        Debug.Log($"InventoryManager.RefreshGrid({gridTransform.name}): cellSize={cellSize:F1}, override={cellSizeOverride}, prefab={slotTemplateRect?.sizeDelta.x:F1}, lastKnown={lastKnownCellSize:F1}");

        float startX = - (columns * cellSize / 2f) + cellSize / 2f;
        float startY = (rows * cellSize / 2f) - cellSize / 2f;

        for (int i = 0; i < dataList.Count - dataOffset && i < columns * rows; i++)
        {
            int x = i % columns;
            int y = i / columns;
            Vector3 pos = new Vector3(startX + x * cellSize, startY - y * cellSize, 0);

            GameObject slotObj = Instantiate(emptySlotPrefab, gridTransform);
            RectTransform slotRect = slotObj.GetComponent<RectTransform>();
            if (slotRect != null)
            {
                slotRect.localScale = Vector3.one;
                slotRect.anchorMin = new Vector2(0.5f, 0.5f);
                slotRect.anchorMax = new Vector2(0.5f, 0.5f);
                slotRect.pivot = new Vector2(0.5f, 0.5f);
                slotRect.sizeDelta = new Vector2(cellSize, cellSize);
                slotRect.localPosition = pos;
            }
            else
            {
                slotObj.transform.localPosition = pos;
            }

            InventorySlot slotLogic = slotObj.GetComponent<InventorySlot>();
            if (slotLogic != null)
            {
                slotLogic.slotCoordinate = new Vector2Int(x, y);
                if (gridTransform == gridLeft) slotLogic.gridRegion = InventorySlot.GridRegion.MainLeft;
                else if (gridTransform == gridRight) slotLogic.gridRegion = InventorySlot.GridRegion.MainRight;
                else if (gridTransform == gridExt) slotLogic.gridRegion = InventorySlot.GridRegion.External;
            }

            ItemData data = dataList[i + dataOffset];
            if (data != null)
            {
                GameObject itemObj = Instantiate(filledItemPrefab, slotObj.transform);
                itemObj.transform.localScale = Vector3.one;
                itemObj.transform.localRotation = Quaternion.identity;
                UIItem uiItem = itemObj.GetComponent<UIItem>();
                if (uiItem != null)
                {
                    uiItem.Initialize(data, cellSize);
                    RectTransform itemRect = itemObj.GetComponent<RectTransform>();
                    if (itemRect != null)
                    {
                        itemRect.localScale = Vector3.one;
                        Debug.Log($"InventoryManager.RefreshGrid: spawned item {data.itemName} at grid={gridTransform.name} slot={x},{y} rectSize={itemRect.sizeDelta} localPos={itemRect.localPosition}");
                    }
                }
            }
        }
    }

    // ================= GAMEPLAY CHECKS =================

    private bool IsInventoryFullyCorrupted()
    {
        int corruptionCount = 0;
        foreach (ItemData item in inventoryState.mainGridSlots)
        {
            if (item == corruptionData) corruptionCount++;
        }
        return corruptionCount >= 100; // All slots in left and right are corrupted
    }

    private void CheckForGameOver()
    {
        if (!IsInventoryFullyCorrupted()) return;

        Debug.LogError("SYSTEM FAILURE: Inventory fully corrupted. GAME OVER.");
        GameOverManager gameOver = FindFirstObjectByType<GameOverManager>();
        if (gameOver != null)
        {
            gameOver.TriggerGameOver();
        }
        else
        {
            Time.timeScale = 0f;
        }
    }

    private void EscalateCrushPenaltyTimer()
    {
        crushTier = Mathf.Min(crushTier + 1, 3);
        if (crushTier <= 2) crushTimer = crushDurations[crushTier - 1];
        Debug.Log($"Crush Penalty Tier {crushTier} activated!");
    }

    public void OnItemDroppedSignal()
    {
        // This is called by your Drag/Drop scripts to trigger the Proxy AI
        onItemDropped?.Invoke();
    }

    public bool CanDropToSlot(InventorySlot slot, DraggableItem draggedItem)
    {
        if (slot == null || draggedItem == null || draggedItem.itemData == null) return false;

        ItemFootprint footprint = draggedItem.footprint;
        if (footprint == null) footprint = new ItemFootprint(1, 1);

        return IsSpaceFreeForFootprint(slot, footprint);
    }

    private bool IsSpaceFreeForFootprint(InventorySlot anchorSlot, ItemFootprint footprint)
    {
        int startX = anchorSlot.slotCoordinate.x;
        int startY = anchorSlot.slotCoordinate.y;

        Debug.Log($"Checking footprint at ({startX}, {startY}) for region {anchorSlot.gridRegion}, size {footprint.width}x{footprint.height}");

        switch (anchorSlot.gridRegion)
        {
            case InventorySlot.GridRegion.MainLeft:
                for (int y = 0; y < footprint.height; y++)
                {
                    for (int x = 0; x < footprint.width; x++)
                    {
                        int checkX = startX + x;
                        int checkY = startY + y;
                        if (checkX < 0 || checkX >= 5 || checkY < 0 || checkY >= 10)
                        {
                            Debug.Log($"Out of bounds at ({checkX}, {checkY})");
                            return false;
                        }
                        int index = checkY * 5 + checkX;
                        if (inventoryState.mainGridSlots[index] != null)
                        {
                            Debug.Log($"Slot {index} occupied by {inventoryState.mainGridSlots[index].itemName}");
                            return false;
                        }
                    }
                }
                Debug.Log("Space is free");
                return true;
            case InventorySlot.GridRegion.MainRight:
                for (int y = 0; y < footprint.height; y++)
                {
                    for (int x = 0; x < footprint.width; x++)
                    {
                        int checkX = startX + x;
                        int checkY = startY + y;
                        if (checkX < 0 || checkX >= 5 || checkY < 0 || checkY >= 10)
                        {
                            Debug.Log($"Out of bounds at ({checkX}, {checkY})");
                            return false;
                        }
                        int index = (checkY * 5 + checkX) + 50;
                        if (inventoryState.mainGridSlots[index] != null)
                        {
                            Debug.Log($"Slot {index} occupied by {inventoryState.mainGridSlots[index].itemName}");
                            return false;
                        }
                    }
                }
                Debug.Log("Space is free");
                return true;
            case InventorySlot.GridRegion.External:
                for (int y = 0; y < footprint.height; y++)
                {
                    for (int x = 0; x < footprint.width; x++)
                    {
                        int checkX = startX + x;
                        int checkY = startY + y;
                        if (checkX < 0 || checkX >= 5 || checkY < 0 || checkY >= 5)
                        {
                            Debug.Log($"Out of bounds at ({checkX}, {checkY})");
                            return false;
                        }
                        int index = checkY * 5 + checkX;
                        if (inventoryState.extGridSlots[index] != null)
                        {
                            Debug.Log($"Slot {index} occupied by {inventoryState.extGridSlots[index].itemName}");
                            return false;
                        }
                    }
                }
                Debug.Log("Space is free");
                return true;
            default:
                return false;
        }
    }

    public int CrushTier => crushTier;
    public bool HasHallucinations => crushTier >= 2;

    // ====================================================================
    // LEGACY BRIDGE METHODS (Fixes CS1061 compilation errors)
    // These translate old script calls into the new Data-Driven system.
    // ====================================================================

    // 1. Fix for MetRigManager, PlayerController, ProxyAI
    public void AddCorruptionRow()
    {
        ResolveCorruptionTick();
    }

    // 2. Fix for DraggableItem and TrashSlot
    public void DiscardItemToWorld(GameObject uiItemObj)
    {
        UIItem uiItem = uiItemObj.GetComponent<UIItem>();
        if (uiItem != null && uiItem.myData != null)
        {
            // Spawn the physical item
            if (physicalBatteryPrefab != null && playerTransform != null)
            {
                Instantiate(physicalBatteryPrefab, playerTransform.position, Quaternion.identity);
            }
            
            // Find it in the Data State and delete it
            for (int i = 0; i < inventoryState.mainGridSlots.Count; i++)
                if (inventoryState.mainGridSlots[i] == uiItem.myData) { inventoryState.mainGridSlots[i] = null; break; }
            for (int i = 0; i < inventoryState.extGridSlots.Count; i++)
                if (inventoryState.extGridSlots[i] == uiItem.myData) { inventoryState.extGridSlots[i] = null; break; }
            
            RefreshAllGrids();
        }
        if (uiItemObj != null) Destroy(uiItemObj);
    }

    // 3. Fix for DraggableItem checking the external grid
    public Transform externalStorageGrid => gridExt;

    // 4. Fix for PlayerInteraction picking up items
    public bool TryPickupItem(GameObject uiPrefabToSpawn, int sizeX, int sizeY, bool isQuestItem = false)
    {
        // Find the first empty slot in the External Grid
        for(int i = 0; i < inventoryState.extGridSlots.Count; i++)
        {
            if(inventoryState.extGridSlots[i] == null)
            {
                // Create a temporary data file so the UI doesn't crash
                ItemData tempItem = ScriptableObject.CreateInstance<ItemData>();
                tempItem.itemID = "NEW";
                inventoryState.extGridSlots[i] = tempItem;
                RefreshAllGrids();
                return true;
            }
        }
        return false;
    }

    // 5. Fix for PlayerInteraction consuming batteries
    public bool TryConsumeBatteries(int amountRequired)
    {
        int count = 0;
        foreach (var item in inventoryState.mainGridSlots)
        {
            // Note: Make sure your ItemData for Battery has exactly "BATT" as its itemID!
            if (item != null && item.itemID == "BATT") count++; 
        }

        if (count >= amountRequired)
        {
            int removed = 0;
            for (int i = 0; i < inventoryState.mainGridSlots.Count; i++)
            {
                if (inventoryState.mainGridSlots[i] != null && inventoryState.mainGridSlots[i].itemID == "BATT")
                {
                    inventoryState.mainGridSlots[i] = null;
                    removed++;
                    if (removed >= amountRequired) break;
                }
            }
            RefreshAllGrids();
            return true;
        }
        return false;
    }

    public void SyncDataFromUI()
    {
        // 1. Clear old data
        for (int i = 0; i < 100; i++) inventoryState.mainGridSlots[i] = null;
        if (gridExt != null) for (int i = 0; i < 25; i++) inventoryState.extGridSlots[i] = null;

        // 2. Scrape Left Visor
        int leftChildCount = gridLeft != null ? gridLeft.childCount : 0;
        int leftSlots = Mathf.Min(leftChildCount, 50);
        for (int i = 0; i < leftSlots; i++) {
            Transform slot = gridLeft.GetChild(i);
            if (slot.childCount > 0) {
                UIItem item = slot.GetChild(0).GetComponent<UIItem>();
                if (item != null) inventoryState.mainGridSlots[i] = item.myData;
            }
        }
        
        // 3. Scrape Right Visor
        int rightChildCount = gridRight != null ? gridRight.childCount : 0;
        int rightSlots = Mathf.Min(rightChildCount, 50);
        for (int i = 0; i < rightSlots; i++) {
            Transform slot = gridRight.GetChild(i);
            if (slot.childCount > 0) {
                UIItem item = slot.GetChild(0).GetComponent<UIItem>();
                if (item != null) inventoryState.mainGridSlots[i + 50] = item.myData;
            }
        }

        // 4. Scrape Ext Node
        if (gridExt != null) {
            int extChildCount = gridExt.childCount;
            int extSlots = Mathf.Min(extChildCount, 25);
            for (int i = 0; i < extSlots; i++) {
                Transform slot = gridExt.GetChild(i);
                if (slot.childCount > 0) {
                    UIItem item = slot.GetChild(0).GetComponent<UIItem>();
                    if (item != null) inventoryState.extGridSlots[i] = item.myData;
                }
            }
        }
    }
}
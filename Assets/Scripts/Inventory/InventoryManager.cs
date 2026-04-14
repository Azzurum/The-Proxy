using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;

public class InventoryManager : MonoBehaviour
{
    [Header("Data & Layout Architecture")]
    public InventoryState inventoryState;
    public Transform gridLeft;
    public Transform gridRight;
    public Transform gridExt;
    public GameObject emptySlotPrefab;
    public GameObject filledItemPrefab;
    public GameObject uiCorruptionPrefab;

    [Header("Grid Sizing")]
    public float cellSizeOverride = 0f; 
    private bool gridRefreshPending = false;

    [Header("World Spawning")]
    public Transform playerTransform;
    public GameObject physicalBatteryPrefab;

    [Header("Corruption Setup")]
    public ItemData corruptionData;

    [Header("MOTHER-v4 System Shock")]
    public float shockInterval = 60f; // UPDATED TO 60 SECONDS
    private float shockTimer;
    private MetRigManager metRigManager;

    [Header("Signal Broadcasting")]
    public UnityEvent onItemDropped; 

    // --- Health Event System ---
    public event Action<float> OnHealthStateChanged;
    // The broadcast megaphone for UI

    [Header("System Constants")]
    public bool isSystemActive = true;
    public Slider systemShockProgressBar;

    [Header("Crush Penalties")]
    private int crushTier = 0;
    private float crushTimer = 0f;
    private float[] crushDurations = { 5f, 10f }; 

    [Header("Game Over State")]
    private bool isGameOverSequenceStarted = false;

    void Start()
    {
        while (inventoryState.mainGridSlots.Count < 100) inventoryState.mainGridSlots.Add(null);
        while (inventoryState.extGridSlots.Count < 25) inventoryState.extGridSlots.Add(null);
        while (inventoryState.hotbarSlots.Count < 3) inventoryState.hotbarSlots.Add(null);

        metRigManager = FindAnyObjectByType<MetRigManager>();
        shockInterval = 60f; // UPDATED TO 60 SECONDS
        shockTimer = shockInterval;
        
        RefreshAllGrids();
        BroadcastHealthState(); // Ensure UI gets the initial health on boot
    }

    void Update()
    {
        // FIXED: Removed 'metRigManager.isRigOpen' so this timer runs everywhere, constantly!
        if (isSystemActive)
        {
            shockTimer -= Time.deltaTime;
            
            if (systemShockProgressBar != null) 
            {
                systemShockProgressBar.value = 1f - (shockTimer / shockInterval);
            }

            if (shockTimer <= 0f)
            {
                ResolveCorruptionTick();
                shockTimer = shockInterval;
            }
        }

        if (crushTimer > 0)
        {
            crushTimer -= Time.deltaTime;
            if (crushTimer <= 0)
            {
                crushTier = Mathf.Max(0, crushTier - 1);
                if (crushTier > 0) crushTimer = crushDurations[Mathf.Min(crushTier - 1, crushDurations.Length - 1)];
            }
        }

        if (Input.GetKeyDown(KeyCode.C)) ExecuteCleanProtocol();
    }

    public void ResolveCorruptionTick()
    {   
        SyncDataFromUI();
        int columns = 5;

        for (int i = 0; i < columns; i++)
        {
            EjectItemIfValid(inventoryState.mainGridSlots[i]);
            EjectItemIfValid(inventoryState.mainGridSlots[i + 50]);     
        }

        for (int i = 0; i < 45; i++)
        {
            inventoryState.mainGridSlots[i] = inventoryState.mainGridSlots[i + columns];
            inventoryState.mainGridSlots[i + 50] = inventoryState.mainGridSlots[i + 50 + columns]; 
        }

        for (int i = 45; i < 50; i++)
        {
            inventoryState.mainGridSlots[i] = corruptionData;
            inventoryState.mainGridSlots[i + 50] = corruptionData;
        }

        CheckForGameOver();
        RefreshAllGrids(); 
        BroadcastHealthState();
        // Update health after taking damage
    }

    public void ExecuteCleanProtocol()
    {
        bool didCleanAnything = false;
        int columns = 5;

        for (int i = 45; i < 50; i++)
        {
            if (inventoryState.mainGridSlots[i] == corruptionData) { inventoryState.mainGridSlots[i] = null;
                didCleanAnything = true; }
            if (inventoryState.mainGridSlots[i + 50] == corruptionData) { inventoryState.mainGridSlots[i + 50] = null;
                didCleanAnything = true; }
        }

        if (didCleanAnything)
        {
            for (int i = 44; i >= 0; i--)
            {
                inventoryState.mainGridSlots[i + columns] = inventoryState.mainGridSlots[i];
                inventoryState.mainGridSlots[i + 50 + columns] = inventoryState.mainGridSlots[i + 50];
            }
            for (int i = 0; i < columns; i++)
            {
                inventoryState.mainGridSlots[i] = null;
                inventoryState.mainGridSlots[i + 50] = null;
            }
            RefreshAllGrids();
            BroadcastHealthState();
            // Update health after healing/purging
        }
    }

    // Mathematically calculate health and broadcast it to the Face and EKG
    public void BroadcastHealthState()
    {
        if (inventoryState == null || corruptionData == null) return;
        int corruptionCount = 0;
        int maxSlots = 100;

        foreach (ItemData item in inventoryState.mainGridSlots)
        {
            if (item == corruptionData) corruptionCount++;
        }

        float corruptionPercentage = (float)corruptionCount / maxSlots;
        float currentHealthPercentage = Mathf.Clamp01(1f - corruptionPercentage);
        OnHealthStateChanged?.Invoke(currentHealthPercentage);

        if (UI_ParasiteOverride.Instance != null)
        {
            UI_ParasiteOverride.Instance.SetExactStacks(corruptionCount);
        }
    }

    private void EjectItemIfValid(ItemData itemData)
    {
        if (itemData != null && itemData != corruptionData)
        {
            if (physicalBatteryPrefab != null && playerTransform != null)
            {
                Instantiate(physicalBatteryPrefab, playerTransform.position, Quaternion.identity);
            }
        }
    }

    public void RefreshAllGrids()
    {
        if (!IsGridVisible())
        {
            gridRefreshPending = true;
            return;
        }

        gridRefreshPending = false;

        foreach (Transform child in gridLeft) Destroy(child.gameObject);
        foreach (Transform child in gridRight) Destroy(child.gameObject);
        if(gridExt != null) foreach (Transform child in gridExt) Destroy(child.gameObject);

        RefreshGrid(gridLeft, inventoryState.mainGridSlots, 5, 10, 0);
        RefreshGrid(gridRight, inventoryState.mainGridSlots, 5, 10, 50); 
        if (gridExt != null) RefreshGrid(gridExt, inventoryState.extGridSlots, 5, 5, 0);
    }

    private bool IsGridVisible()
    {
        return (gridLeft != null && gridLeft.gameObject.activeInHierarchy)
            ||
            (gridRight != null && gridRight.gameObject.activeInHierarchy)
            ||
            (gridExt != null && gridExt.gameObject.activeInHierarchy);
    }

    public void RefreshAllGridsIfPending()
    {
        if (gridRefreshPending) RefreshAllGrids();
    }

    void RefreshGrid(Transform gridTransform, List<ItemData> dataList, int columns, int rows, int dataOffset)
    {
        float currentCellSize = 75f;
        GridLayoutGroup layout = gridTransform.GetComponent<GridLayoutGroup>();
        if (layout != null) currentCellSize = layout.cellSize.x;
        else if (cellSizeOverride > 0f) currentCellSize = cellSizeOverride;
        HashSet<ItemData> spawnedUniqueItems = new HashSet<ItemData>();

        for (int i = 0; i < dataList.Count - dataOffset && i < columns * rows; i++)
        {
            int x = i % columns;
            int y = i / columns;

            GameObject slotObj = Instantiate(emptySlotPrefab, gridTransform);
            RectTransform slotRect = slotObj.GetComponent<RectTransform>();
            if (slotRect != null)
            {
                slotRect.localScale = Vector3.one;
                slotRect.localRotation = Quaternion.identity;
                slotRect.pivot = new Vector2(0f, 1f); 
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
                if (data != corruptionData)
                {
                    if (spawnedUniqueItems.Contains(data)) continue;
                    spawnedUniqueItems.Add(data);
                }

                GameObject prefabToSpawn = (data == corruptionData && uiCorruptionPrefab != null) ?
                uiCorruptionPrefab : filledItemPrefab;
                GameObject itemObj = Instantiate(prefabToSpawn, slotObj.transform);
                RectTransform itemRect = itemObj.GetComponent<RectTransform>();
                if (itemRect != null)
                {
                    itemRect.localScale = Vector3.one;
                    itemRect.localRotation = Quaternion.identity;
                }

                Canvas itemCanvas = itemObj.GetComponent<Canvas>();
                if (itemCanvas != null)
                {
                    itemCanvas.overrideSorting = true;
                    itemCanvas.sortingOrder = (gridTransform == gridExt) ? 1 : 5;
                }

                UIItem uiItem = itemObj.GetComponent<UIItem>();
                if (uiItem != null) uiItem.Initialize(data, currentCellSize);
                
                DraggableItem dragItem = itemObj.GetComponent<DraggableItem>();
                if (dragItem != null) {
                    dragItem.cellSize = currentCellSize;
                    dragItem.UpdateVisualSize();
                }
            }
        }
    }

    private bool IsInventoryFullyCorrupted()
    {
        int corruptionCount = 0;
        foreach (ItemData item in inventoryState.mainGridSlots)
            if (item == corruptionData) corruptionCount++;
        return corruptionCount >= 100;
    }

    private void CheckForGameOver()
    {
        // If it's not fully corrupted, OR we are already dying, do nothing.
        if (!IsInventoryFullyCorrupted() || isGameOverSequenceStarted) return;
        
        // Lock the sequence so it doesn't trigger again!
        isGameOverSequenceStarted = true;
        // Start the dramatic timed sequence
        StartCoroutine(GameOverSequenceRoutine());
    }

    private System.Collections.IEnumerator GameOverSequenceRoutine()
    {
        // 1. Is the player currently looking at the M.E.T. Rig?
        if (metRigManager != null && metRigManager.isRigOpen)
        {
            // Force them to stare at the fully corrupted inventory and flatline for 1.5 seconds
            yield return new WaitForSeconds(1.5f);
            // Forcefully close the inventory using the new method we just added!
            metRigManager.CloseRig();
        }

        // 2. Now trigger the actual Game Over animation/screen
        GameOverManager gameOver = FindAnyObjectByType<GameOverManager>();
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
    }

    public void OnItemDroppedSignal() { onItemDropped?.Invoke();
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
        int offsetX = -Mathf.FloorToInt(footprint.width / 2f);
        int offsetY = -Mathf.FloorToInt(footprint.height / 2f);

        int startX = anchorSlot.slotCoordinate.x + offsetX;
        int startY = anchorSlot.slotCoordinate.y + offsetY;
        int w = footprint != null ? footprint.width : 1;
        int h = footprint != null ? footprint.height : 1;
        int maxCols = 5;
        int maxRows = anchorSlot.gridRegion == InventorySlot.GridRegion.External ? 5 : 10;
        int offset = anchorSlot.gridRegion == InventorySlot.GridRegion.MainRight ? 50 : 0;
        List<ItemData> targetGrid = anchorSlot.gridRegion == InventorySlot.GridRegion.External ? inventoryState.extGridSlots : inventoryState.mainGridSlots;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int cx = startX + x;
                int cy = startY + y;

                if (cx < 0 || cx >= maxCols || cy < 0 || cy >= maxRows) return false;
                if (targetGrid[offset + (cy * maxCols) + cx] != null) return false;
            }
        }
        return true;
    }

    public int CrushTier => crushTier;
    public bool HasHallucinations => crushTier >= 2;
    public void AddCorruptionRow() { ResolveCorruptionTick(); }

    public void DiscardItemToWorld(GameObject itemUI)
    {
        DraggableItem dragItem = itemUI.GetComponent<DraggableItem>();
        if (dragItem != null && dragItem.itemData != null && dragItem.itemData.worldPrefab != null)
        {
            Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
            Vector3 spawnPos = player != null ? player.position + (Vector3.right * 2f) : Vector3.zero;
            
            Instantiate(dragItem.itemData.worldPrefab, spawnPos, Quaternion.identity);
        }

        Destroy(itemUI);
        SyncDataFromUI();
    }

    public Transform externalStorageGrid => gridExt;
    public bool TryPickupItem(GameObject uiPrefabToSpawn, int sizeX, int sizeY, bool isQuestItem = false)
    {
        for(int i = 0; i < inventoryState.extGridSlots.Count; i++)
        {
            if(inventoryState.extGridSlots[i] == null)
            {
                ItemData tempItem = ScriptableObject.CreateInstance<ItemData>();
                tempItem.itemID = "NEW";
                inventoryState.extGridSlots[i] = tempItem;
                RefreshAllGrids();
                return true;
            }
        }
        return false;
    }

    public bool TryConsumeBatteries(int amountRequired)
    {
        int count = 0;
        foreach (var item in inventoryState.mainGridSlots) if (item != null && item.itemID == "BATT") count++;
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
        // THE FIX: If the grid graphics are hidden and out of date, do NOT scrape them!
        // This protects your true background data from being overwritten.
        if (gridRefreshPending) return;

        for (int i = 0; i < 100; i++) inventoryState.mainGridSlots[i] = null;
        if (gridExt != null) for (int i = 0; i < 25; i++) inventoryState.extGridSlots[i] = null;
        ScrapeGrid(gridLeft, inventoryState.mainGridSlots, 0, 5, 10);
        ScrapeGrid(gridRight, inventoryState.mainGridSlots, 50, 5, 10);
        if (gridExt != null) ScrapeGrid(gridExt, inventoryState.extGridSlots, 0, 5, 5);
        // Calculate and broadcast health one final time just in case manual dragging caused corruption changes
        BroadcastHealthState();
    }

    private void ScrapeGrid(Transform gridTransform, List<ItemData> targetList, int dataOffset, int cols, int rows)
    {
        if (gridTransform == null) return;
        for (int i = 0; i < gridTransform.childCount; i++)
        {
            Transform slot = gridTransform.GetChild(i);
            if (slot.childCount > 0)
            {
                Transform itemObj = slot.GetChild(0);
                DraggableItem dragItem = itemObj.GetComponent<DraggableItem>();
                UIItem uiItem = itemObj.GetComponent<UIItem>();

                if (dragItem != null && uiItem != null && uiItem.myData != null)
                {
                    int startX = i % cols;
                    int startY = i / cols;
                    
                    int w = dragItem.footprint != null ? dragItem.footprint.width : dragItem.sizeX;
                    int h = dragItem.footprint != null ? dragItem.footprint.height : dragItem.sizeY;
                    w = Mathf.Max(1, w);
                    h = Mathf.Max(1, h);
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                
                            int cx = startX + x;
                            int cy = startY + y;
                            if (cx < cols && cy < rows)
                            {
                                int index = dataOffset + (cy * cols) + cx;
                                targetList[index] = uiItem.myData;
                            }
                        }
                    }
                }
                else
                {
     
                    int startX = i % cols;
                    int startY = i / cols;
                    int index = dataOffset + (startY * cols) + startX;
                    if (index >= 0 && index < targetList.Count)
                    {
                        targetList[index] = corruptionData;
                    }
                }
            }
        }
    }
}
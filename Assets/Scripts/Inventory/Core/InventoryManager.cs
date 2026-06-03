using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using UnityEngine.SceneManagement;

/// <summary>
/// The central hub for managing player inventory data, grid UI, and core mechanics like Corruption.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Data & Layout Architecture")]
    [Tooltip("The ScriptableObject asset that holds the runtime inventory state.")]
    public InventoryState inventoryState;
    [Tooltip("The parent Transform for the left side of the main inventory grid.")]
    public Transform gridLeft;
    [Tooltip("The parent Transform for the right side of the main inventory grid.")]
    public Transform gridRight;
    [Tooltip("The parent Transform for the external grid (Buffer or Locker).")]
    public Transform gridExt;
    [Tooltip("The prefab for an empty, unoccupied grid slot.")]
    public GameObject emptySlotPrefab;
    [Tooltip("The prefab for a UI item that occupies one or more grid slots.")]
    public GameObject filledItemPrefab;

    [Tooltip("Drag all your ItemData ScriptableObjects here so the game can load them by ID")]
    public List<ItemData> itemDatabase = new List<ItemData>();

    [Header("Grid Sizing")]
    [Tooltip("If the GridLayoutGroup is missing, use this value for cell size calculations.")]
    public float cellSizeOverride = 0f; 
    [Tooltip("A flag indicating that the grid UI needs to be redrawn.")]
    public bool gridRefreshPending = false;

    [Header("Corruption Setup")]
    [Tooltip("The ItemData representing a single block of corruption.")]
    public ItemData corruptionData;

    [Header("MOTHER-v4 System Shock")]
    [Tooltip("The base time in seconds between corruption ticks.")]
    public float shockInterval = 60f;
    [Tooltip("The current countdown timer for the next corruption tick.")]
    public float shockTimer;
    private MetRigManager metRigManager;

    [Header("Signal Broadcasting")]
    [Tooltip("A UnityEvent fired when an item is dropped from the inventory.")]
    public UnityEvent onItemDropped; 

    [Header("Inspection UI")]
    [Tooltip("The UI Image element that displays the sprite of the currently inspected item.")]
    public UnityEngine.UI.Image uiInspectIcon;

    /// <summary>Fired when the player's health percentage changes due to corruption. Passes a float (0.0 to 1.0).</summary>
    public event Action<float> OnHealthStateChanged;
    /// <summary>Fired just before a new row of corruption is added to the grid.</summary>
    public event Action OnCorruptionTick;

    [Header("System Constants")]
    [Tooltip("A master switch to disable corruption timers and other active processes.")]
    public bool isSystemActive = true;

    [Header("Crush Penalties")]
    private int crushTier = 0;
    private float crushTimer = 0f;
    private float[] crushDurations = { 5f, 10f }; 

    [Header("Game Over State")]
    [Tooltip("If true, the game over sequence will not trigger even if the inventory is full of corruption.")]
    public bool suppressGameOver = false;
    private bool isGameOverSequenceStarted = false;

    [Header("Locker State")]
    [Tooltip("Is the player currently viewing a locker's contents in the external grid?")]
    public bool isInteractingWithLocker = false;
    private LockerStorage activeLocker = null;

    private void Awake()
    {
        // Standard singleton pattern.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // To prevent the original ScriptableObject asset from being modified during play, create a runtime clone.
        if (inventoryState != null) inventoryState = inventoryState.GetRuntimeClone();
    }

    void Start()
    {
        // Auto-wire references if they were not set in the inspector.
        if (gridLeft == null || gridRight == null)
        {
            GridLayoutGroup[] grids = FindObjectsByType<GridLayoutGroup>(FindObjectsInactive.Include);
            foreach (var grid in grids)
            {
                string gName = grid.name.ToLower();
                if (!gName.Contains("grid")) continue; // Ignore other UI layout groups

                if (gName.Contains("left")) gridLeft = grid.transform;
                else if (gName.Contains("right")) gridRight = grid.transform;
                else if (gName.Contains("ext")) gridExt = grid.transform;
            }
        }

        if (uiInspectIcon == null)
        {
            Image[] allImages = FindObjectsByType<Image>(FindObjectsInactive.Include);
            foreach (var img in allImages)
            {
                if (img.name.ToLower().Contains("inspect")) { uiInspectIcon = img; break; }
            }
        }

        // Ensure the data lists are initialized to their correct sizes.
        while (inventoryState.mainGridSlots.Count < 100) inventoryState.mainGridSlots.Add(null);
        while (inventoryState.matterBufferSlots.Count < 25) inventoryState.matterBufferSlots.Add(null);
        
        // The external grid defaults to showing the player's matter buffer.
        inventoryState.extGridSlots = inventoryState.matterBufferSlots;

        while (inventoryState.hotbarSlots.Count < 3) inventoryState.hotbarSlots.Add(null);

        metRigManager = FindAnyObjectByType<MetRigManager>();

        shockTimer = shockInterval;
        
        RefreshAllGrids();
        BroadcastHealthState();
        ClearInspectionScreen();
        if (InventorySaveHandler.Instance != null) InventorySaveHandler.Instance.LoadWorldItems();
    }

    void Update()
    {
        if (isSystemActive)
        {
            // Corruption builds faster while the M.E.T. Rig UI is open.
            float tickMultiplier = (metRigManager != null && metRigManager.isRigOpen) ? 2.5f : 1.0f;
            shockTimer -= Time.deltaTime * tickMultiplier;

            if (shockTimer <= 0f)
            {
                ResolveCorruptionTick();
                shockTimer = shockInterval;
            }
        }

        // Handle the countdown for crush penalty tiers.
        if (crushTimer > 0)
        {
            crushTimer -= Time.deltaTime;

            if (crushTimer <= 0)
            {
                crushTier = Mathf.Max(0, crushTier - 1);

                if (crushTier > 0) crushTimer = crushDurations[Mathf.Min(crushTier - 1, crushDurations.Length - 1)];
            }
        }

        // Debug key to manually trigger the clean protocol.
        if (Input.GetKeyDown(KeyCode.C)) ExecuteCleanProtocol();
    }

    /// <summary>
    /// The core logic for adding a new row of corruption, shifting all items up, and ejecting overflow.
    /// </summary>
    public void ResolveCorruptionTick()
    {   
        // Prevent data corruption by aborting any item drag before modifying the grid data.
        if (DraggableItem.itemBeingDragged != null) DraggableItem.itemBeingDragged.AbortDrag();

        OnCorruptionTick?.Invoke();

        SyncDataFromUI();

        int columns = 5;
        bool[] memoryMask = new bool[100];

        // Eject any items that are in the top row (indices 0-4) of each grid half.
        for (int i = 0; i < 5; i++)
        {
            ExtractAndEject(i, memoryMask);
            ExtractAndEject(i + 50, memoryMask);
        }

        // Shift all items in the main grid data UP by one row (towards index 0).
        for (int i = 5; i < 50; i++)
        {
            inventoryState.mainGridSlots[i - columns] = inventoryState.mainGridSlots[i];
            inventoryState.mainGridSlots[i + 50 - columns] = inventoryState.mainGridSlots[i + 50]; 
        }

        // Fill the newly created empty bottom row (indices 45-49) with corruption data.
        for (int i = 45; i < 50; i++)
        {
            inventoryState.mainGridSlots[i] = corruptionData;
            inventoryState.mainGridSlots[i + 50] = corruptionData;
        }

        CheckForGameOver();
        RefreshAllGrids(); 
        BroadcastHealthState();
        if (InventorySaveHandler.Instance != null) InventorySaveHandler.Instance.SaveWorldItems();
    }

    /// <summary>
    /// Safely identifies the footprint of an item touching the ceiling, ejects it once, and clears its footprint to prevent duplication.
    /// </summary>
    private void ExtractAndEject(int index, bool[] memoryMask)
    {
        if (memoryMask[index]) return;

        ItemData item = inventoryState.mainGridSlots[index];
        if (item == null || item == corruptionData) return;

        EjectItemIfValid(item);

        ItemFootprint fp = item.GetFootprint();
        bool isRotated = false;

        // Infer rotation based on neighboring identical items
        if (fp != null)
        {
            if (fp.width == 1 && fp.height > 1) 
            {
                int rightIndex = index + 1;
                if (index % 5 < 4 && rightIndex < 100 && inventoryState.mainGridSlots[rightIndex] == item) isRotated = true; 
            }
            else if (fp.width > 1 && fp.height == 1)
            {
                int bottomIndex = index + 5;
                if (bottomIndex < 100 && inventoryState.mainGridSlots[bottomIndex] == item) isRotated = true; 
            }
        }

        int w = fp != null ? fp.width : 1;
        int h = fp != null ? fp.height : 1;
        if (isRotated) { int temp = w; w = h; h = temp; } 

        ItemFootprint activeFootprint = isRotated && fp != null ? fp.GetRotated() : fp;

        int startX = index % 5;
        int startY = (index % 50) / 5;
        int offset = index >= 50 ? 50 : 0;

        // Mask out and nullify the entire footprint so it doesn't get chopped in half during the shift
        for (int fy = 0; fy < h; fy++)
        {
            for (int fx = 0; fx < w; fx++)
            {
                if (activeFootprint != null && !activeFootprint.GetCell(fx, fy)) continue;

                int cx = startX + fx;
                int cy = startY + fy;

                if (cx < 5 && cy < 10)
                {
                    int targetIndex = offset + (cy * 5) + cx;
                    memoryMask[targetIndex] = true;
                    inventoryState.mainGridSlots[targetIndex] = null; 
                }
            }
        }
    }

    /// <summary>
    /// Removes the bottom-most row of corruption and shifts all items down to fill the space.
    /// </summary>
    public void ExecuteCleanProtocol()
    {
        if (DraggableItem.itemBeingDragged != null) DraggableItem.itemBeingDragged.AbortDrag();

        bool didCleanAnything = false;
        int columns = 5;

        // Clear any corruption found in the bottom row (indices 45-49).
        for (int i = 45; i < 50; i++)
        {
            if (inventoryState.mainGridSlots[i] == corruptionData) { inventoryState.mainGridSlots[i] = null; didCleanAnything = true; }
            if (inventoryState.mainGridSlots[i + 50] == corruptionData) { inventoryState.mainGridSlots[i + 50] = null; didCleanAnything = true; }
        }

        if (didCleanAnything)
        {
            // Shift all items down by one row (towards index 49).
            // Start from the bottom to prevent propagating nulls all the way to the top!
            for (int i = 44; i >= 0; i--)
            {
                inventoryState.mainGridSlots[i + columns] = inventoryState.mainGridSlots[i];
                inventoryState.mainGridSlots[i + 50 + columns] = inventoryState.mainGridSlots[i + 50];
            }
            // Clear the top row (indices 0-4)
            for (int i = 0; i < 5; i++)
            {
                inventoryState.mainGridSlots[i] = null;
                inventoryState.mainGridSlots[i + 50] = null;
            }
            RefreshAllGrids();
            BroadcastHealthState();
        }
    }

    /// <summary>
    /// Calculates the current health based on corruption percentage and invokes the OnHealthStateChanged event.
    /// </summary>
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

    /// <summary>
    /// Checks if an item is valid (not null, not corruption) and ejects it into the world.
    /// </summary>
    private void EjectItemIfValid(ItemData itemData)
    {
        if (itemData != null && itemData != corruptionData)
        {
            if (WorldItemSpawner.Instance != null) WorldItemSpawner.Instance.EjectItem(itemData);
        }
    }

    /// <summary>
    /// Redraws all inventory grid UIs based on the current data in `inventoryState`.
    /// </summary>
    public void RefreshAllGrids()
    {
        if (!IsGridVisible())
        {
            gridRefreshPending = true;
            return;
        }

        gridRefreshPending = false;

        RefreshGrid(gridLeft, inventoryState.mainGridSlots, 5, 10, 0);
        RefreshGrid(gridRight, inventoryState.mainGridSlots, 5, 10, 50); 
        if (gridExt != null) RefreshGrid(gridExt, inventoryState.extGridSlots, 5, 5, 0);
    }

    /// <summary>
    /// Checks if any of the main inventory grid UI objects are currently active and visible.
    /// </summary>
    private bool IsGridVisible()
    {
        return (gridLeft != null && gridLeft.gameObject.activeInHierarchy)
            || (gridRight != null && gridRight.gameObject.activeInHierarchy)
            || (gridExt != null && gridExt.gameObject.activeInHierarchy);
    }

    /// <summary>
    /// If a grid refresh was previously queued, this function executes it.
    /// </summary>
    public void RefreshAllGridsIfPending()
    {
        if (gridRefreshPending) RefreshAllGrids();
    }

    /// <summary>
    /// The core drawing function that populates a single grid UI with slots and items.
    /// </summary>
    void RefreshGrid(Transform gridTransform, List<ItemData> dataList, int columns, int rows, int dataOffset)
    {
        float currentCellSize = 75f;

        GridLayoutGroup layout = gridTransform.GetComponent<GridLayoutGroup>();
        if (layout != null) currentCellSize = layout.cellSize.x;
        else if (cellSizeOverride > 0f) currentCellSize = cellSizeOverride;

        bool[] spawnedMask = new bool[columns * rows];

        // Iterate through the data list to create or update UI elements.
        for (int i = 0; i < dataList.Count - dataOffset && i < columns * rows; i++)
        {
            int x = i % columns;
            int y = i / columns;

            Transform slotTransform;
            if (i < gridTransform.childCount)
            {
                slotTransform = gridTransform.GetChild(i);
                // Reuse the existing slot for performance, but clear any old items first.
                for (int c = slotTransform.childCount - 1; c >= 0; c--) Destroy(slotTransform.GetChild(c).gameObject);
            }
            else
            {
                GameObject slotObj = Instantiate(emptySlotPrefab, gridTransform);
                slotTransform = slotObj.transform;
                
                RectTransform slotRect = slotObj.GetComponent<RectTransform>();
                if (slotRect != null) { slotRect.localScale = Vector3.one; slotRect.localRotation = Quaternion.identity; slotRect.pivot = new Vector2(0f, 1f); }
                
                InventorySlot slotLogic = slotObj.GetComponent<InventorySlot>();
                if (slotLogic != null)
                {
                    slotLogic.slotCoordinate = new Vector2Int(x, y);
                    if (gridTransform == gridLeft) slotLogic.gridRegion = InventorySlot.GridRegion.MainLeft;
                    else if (gridTransform == gridRight) slotLogic.gridRegion = InventorySlot.GridRegion.MainRight;
                    else if (gridTransform == gridExt) slotLogic.gridRegion = InventorySlot.GridRegion.External;
                }
            }

            ItemData data = dataList[i + dataOffset];

            if (data != null)
            {
                bool isRotated = false;
                
                if (data != corruptionData)
                {
                    // Skip this slot if an item starting in a previous slot already occupies it.
                    if (spawnedMask[y * columns + x]) continue;

                    ItemFootprint fp = data.GetFootprint();
                    if (fp != null)
                    {
                        if (fp.width == 1 && fp.height > 1) 
                        {
                            if (x + 1 < columns && i + dataOffset + 1 < dataList.Count && dataList[i + dataOffset + 1] == data) isRotated = true;
                        }
                        else if (fp.width > 1 && fp.height == 1)
                        {
                            if (y + 1 < rows && i + dataOffset + columns < dataList.Count && dataList[i + dataOffset + columns] == data) isRotated = true;
                        }
                    }

                    int w = fp != null ? fp.width : 1;
                    int h = fp != null ? fp.height : 1;
                    if (isRotated) { int temp = w; w = h; h = temp; }

                    // Mark all cells covered by this item's footprint as occupied.
                    for (int fy = 0; fy < h; fy++)
                    {
                        for (int fx = 0; fx < w; fx++)
                        {
                            int cy = y + fy;
                            int cx = x + fx;
                            if (cx < columns && cy < rows)
                            {
                                spawnedMask[cy * columns + cx] = true;
                            }
                        }
                    }
                }

                GameObject itemObj = Instantiate(filledItemPrefab, slotTransform);
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

                if (uiItem != null) uiItem.Initialize(data, currentCellSize, isRotated);
                
                DraggableItem dragItem = itemObj.GetComponent<DraggableItem>();

                if (dragItem != null) {
                    dragItem.cellSize = currentCellSize;
                    dragItem.UpdateVisualSize();
                }
            }
        }
    }

    /// <summary>
    /// Checks if all 100 slots of the main grid are filled with corruption.
    /// </summary>
    private bool IsInventoryFullyCorrupted()
    {
        int corruptionCount = 0;
        foreach (ItemData item in inventoryState.mainGridSlots)
            if (item == corruptionData) corruptionCount++;
        return corruptionCount >= 100;
    }

    /// <summary>
    /// Checks if the game over condition has been met and starts the sequence if it has.
    /// </summary>
    private void CheckForGameOver()
    {
        if (suppressGameOver) return;
        if (!IsInventoryFullyCorrupted() || isGameOverSequenceStarted) return;
        
        isGameOverSequenceStarted = true;
        StartCoroutine(GameOverSequenceRoutine());
    }

    /// <summary>
    /// A timed coroutine that handles the visual and logical flow of the game over sequence.
    /// </summary>
    private System.Collections.IEnumerator GameOverSequenceRoutine()
    {
        if (metRigManager != null && metRigManager.isRigOpen)
        {
            yield return new WaitForSeconds(1.5f);
            metRigManager.CloseRig();
        }

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

    /// <summary>
    /// A public method to be called by other scripts to invoke the onItemDropped event.
    /// </summary>
    public void OnItemDroppedSignal() { onItemDropped?.Invoke(); }

    /// <summary>
    /// Determines if a dragged item can be legally placed in a target slot.
    /// </summary>
    public bool CanDropToSlot(InventorySlot slot, DraggableItem draggedItem)
    {
        if (slot == null || draggedItem == null || draggedItem.itemData == null) return false;
        
        // Prevent dropping items into the external grid if it's not connected to a locker.
        if (slot.gridRegion == InventorySlot.GridRegion.External && !isInteractingWithLocker)
        {
            return false; // Read-Only Mode!
        }

        ItemFootprint footprint = draggedItem.footprint;
        if (footprint == null) footprint = new ItemFootprint(1, 1);
        return IsSpaceFreeForFootprint(slot, footprint);
    }

    /// <summary>
    /// Checks if the area required by an item's footprint is completely empty in the target grid.
    /// </summary>
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
                // Ignore empty cells within a complex footprint (e.g., L-shaped items).
                if (!footprint.GetCell(x, y)) continue;

                int cx = startX + x;
                int cy = startY + y;

                if (cx < 0 || cx >= maxCols || cy < 0 || cy >= maxRows) return false;
                if (targetGrid[offset + (cy * maxCols) + cx] != null) return false;
            }
        }
        return true;
    }

    /// <summary>The current tier of the crush penalty (0-3).</summary>
    public int CrushTier => crushTier;
    /// <summary>Public accessor to trigger a corruption tick manually.</summary>
    public void AddCorruptionRow() { ResolveCorruptionTick(); }

    /// <summary>A public accessor for the external storage grid transform.</summary>
    public Transform externalStorageGrid => gridExt;
    
    /// <summary>
    /// Attempts to find space for and add a new item to the external matter buffer.
    /// </summary>
    /// <returns>True if the item was successfully picked up, false otherwise.</returns>
    public bool TryPickupItem(ItemData itemToPickup)
    {
        if (itemToPickup == null) return false;

        ItemFootprint fp = itemToPickup.GetFootprint();
        int w = fp != null ? fp.width : 1;
        int h = fp != null ? fp.height : 1;

        if (AttemptPlacement(itemToPickup, w, h, inventoryState.extGridSlots, 5, 5, 0)) 
        {
            return true;
        }

        Debug.Log("<color=red>External Tray Full! No space for " + itemToPickup.itemName + "</color>");
        return false;
    }

    /// <summary>
    /// Scans a target grid to find the first available space that fits an item's dimensions and places it there.
    /// </summary>
    private bool AttemptPlacement(ItemData item, int w, int h, List<ItemData> targetGrid, int maxCols, int maxRows, int gridOffset)
    {
        for (int y = 0; y <= maxRows - h; y++)
        {
            for (int x = 0; x <= maxCols - w; x++)
            {
                bool spaceFree = true;

                // Check if the specific footprint shape is clear
                for (int cy = 0; cy < h; cy++)
                {
                    for (int cx = 0; cx < w; cx++)
                    {
                        if (item.GetFootprint() != null && !item.GetFootprint().GetCell(cx, cy)) continue;
                        int index = gridOffset + ((y + cy) * maxCols) + (x + cx);
                        if (targetGrid[index] != null)
                        {
                            spaceFree = false;
                            break;
                        }
                    }
                    if (!spaceFree) break;
                }

                // If a valid spot was found, write the item data to the grid and exit.
                if (spaceFree)
                {
                    for (int cy = 0; cy < h; cy++)
                    {
                        for (int cx = 0; cx < w; cx++)
                        {
                            if (item.GetFootprint() != null && !item.GetFootprint().GetCell(cx, cy)) continue;
                            int index = gridOffset + ((y + cy) * maxCols) + (x + cx);
                            targetGrid[index] = item;
                        }
                    }
                    RefreshAllGrids();
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if there is enough space in the external buffer for an item without actually adding it.
    /// </summary>
    public bool CanFitItemToExternalTray(ItemData itemToPickup)
    {
        if (itemToPickup == null) return false;

        ItemFootprint fp = itemToPickup.GetFootprint();
        int w = fp != null ? fp.width : 1;
        int h = fp != null ? fp.height : 1;

        return CheckPlacementSpace(itemToPickup, w, h, inventoryState.extGridSlots, 5, 5, 0);
    }

    /// <summary>
    /// A non-destructive check to see if a valid placement spot exists in a target grid.
    /// </summary>
    private bool CheckPlacementSpace(ItemData item, int w, int h, List<ItemData> targetGrid, int maxCols, int maxRows, int gridOffset)
    {
        for (int y = 0; y <= maxRows - h; y++)
        {
            for (int x = 0; x <= maxCols - w; x++)
            {
                bool spaceFree = true;
                for (int cy = 0; cy < h; cy++)
                {
                    for (int cx = 0; cx < w; cx++)
                    {
                        if (item.GetFootprint() != null && !item.GetFootprint().GetCell(cx, cy)) continue;
                        int index = gridOffset + ((y + cy) * maxCols) + (x + cx);
                        if (targetGrid[index] != null) { spaceFree = false; break; }
                    }
                    if (!spaceFree) break;
                }
                if (spaceFree) return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Connects the external grid UI to a specific locker's storage data.
    /// </summary>
    public void OpenLocker(LockerStorage locker)
    {
        SyncDataFromUI();
        
        activeLocker = locker;
        // Point the external grid's data source to the locker's data list.
        inventoryState.extGridSlots = locker.gridSlots;
        isInteractingWithLocker = true;

        if (gridExt != null && gridExt.parent != null)
        {
            gridExt.parent.gameObject.SetActive(true);
            RefreshAllGrids();
        }
    }

    /// <summary>
    /// Disconnects the external grid from a locker and reverts it to the player's matter buffer.
    /// </summary>
    public void DisconnectFromLocker()
    {
        SyncDataFromUI();
        inventoryState.extGridSlots = inventoryState.matterBufferSlots;
        isInteractingWithLocker = false;
        
        if (activeLocker != null)
        {
            activeLocker.SaveLockerState();
            activeLocker = null;
        }
    }

    /// <summary>
    /// Checks if the player's external matter buffer contains any items.
    /// </summary>
    public bool HasItemsInExternalStorage()
    {
        if (inventoryState == null || inventoryState.extGridSlots == null) return false;
        foreach (var item in inventoryState.extGridSlots)
        {
            if (item != null) return true;
        }
        return false;
    }

    /// <summary>
    /// Scans all inventories for a specific number of batteries and consumes them if found.
    /// </summary>
    public bool TryConsumeBatteries(int amountRequired)
    {
        int count = 0;
        foreach (var item in inventoryState.mainGridSlots) if (item != null && item.itemID == "BATT") count++;
        foreach (var item in inventoryState.matterBufferSlots) if (item != null && item.itemID == "BATT") count++;
        
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
            if (removed < amountRequired)
            {
                for (int i = 0; i < inventoryState.matterBufferSlots.Count; i++)
                {
                    if (inventoryState.matterBufferSlots[i] != null && inventoryState.matterBufferSlots[i].itemID == "BATT")
                    {
                        inventoryState.matterBufferSlots[i] = null;
                        removed++;
                        if (removed >= amountRequired) break;
                    }
                }
            }
            RefreshAllGrids();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Reads the current state of the UI grids and writes it back to the `inventoryState` data lists.
    /// </summary>
    public void SyncDataFromUI()
    {
        // CRITICAL FIX: To prevent completely wiping the inventory array, abort if the grid is physically hidden or pending a refresh.
        if (gridRefreshPending || !IsGridVisible()) return;

        // (The global AbortDrag check was removed from here because it instantly cancelled dragging! 
        // It is now safely handled explicitly in SaveLoadManager when actually saving.)

        for (int i = 0; i < 100; i++) inventoryState.mainGridSlots[i] = null;
        if (gridExt != null) for (int i = 0; i < 25; i++) inventoryState.extGridSlots[i] = null;

        ScrapeGrid(gridLeft, inventoryState.mainGridSlots, 0, 5, 10);
        ScrapeGrid(gridRight, inventoryState.mainGridSlots, 50, 5, 10);
        if (gridExt != null) ScrapeGrid(gridExt, inventoryState.extGridSlots, 0, 5, 5);
        
        if (HotbarManager.Instance != null && inventoryState.hotbarSlots != null && inventoryState.hotbarSlots.Count >= 3)
        {
            for (int i = 0; i < 3; i++)
            {
                if (HotbarManager.Instance.quickSlots.Length > i && HotbarManager.Instance.quickSlots[i] != null && HotbarManager.Instance.quickSlots[i].containedItem != null)
                {
                    inventoryState.hotbarSlots[i] = HotbarManager.Instance.quickSlots[i].containedItem.itemData;
                }
                else
                {
                    inventoryState.hotbarSlots[i] = null;
                }
            }
        }

        BroadcastHealthState();
    }

    /// <summary>
    /// Iterates through a UI grid's children to determine which items are in which slots and updates the data list.
    /// </summary>
    private void ScrapeGrid(Transform gridTransform, List<ItemData> targetList, int dataOffset, int cols, int rows)
    {
        if (gridTransform == null) return;
        
        for (int i = 0; i < gridTransform.childCount; i++)
        {
            Transform slot = gridTransform.GetChild(i);
            
            for (int c = 0; c < slot.childCount; c++)
            {
                Transform itemObj = slot.GetChild(c);
                
                // Use TryGetComponent for performance and safety.
                if (!itemObj.TryGetComponent(out UIItem uiItem) || uiItem.myData == null) continue;
                itemObj.TryGetComponent(out DraggableItem dragItem);
                
                if (true) // Keeping block scope to minimize diff lines
                {
                    int startX = i % cols;
                    int startY = i / cols;
                    
                    int w = 1;
                    int h = 1;
                    ItemFootprint fp = null;

                    if (dragItem != null)
                    {
                        fp = dragItem.footprint;
                        w = fp != null ? fp.width : dragItem.sizeX;
                        h = fp != null ? fp.height : dragItem.sizeY;
                    }
                    else
                    {
                        fp = uiItem.myData.GetFootprint();
                        w = fp != null ? fp.width : 1;
                        h = fp != null ? fp.height : 1;
                    }
                    
                    w = Mathf.Max(1, w);
                    h = Mathf.Max(1, h);
                    
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            // For items with complex shapes, only write data to the occupied cells of the footprint.
                            if (fp != null && !fp.GetCell(x, y)) continue;

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
            }
        }
    }

    /// <summary>
    /// Sets the sprite of the main inspection window icon.
    /// </summary>
    /// <param name="itemSprite">The sprite to display.</param>
    public void SetInspectionIcon(Sprite itemSprite)
    {
        if (uiInspectIcon != null && itemSprite != null)
        {
            uiInspectIcon.sprite = itemSprite;
            uiInspectIcon.color = Color.white; 
        }
    }

    /// <summary>
    /// Clears the inspection window, making it blank and transparent.
    /// </summary>
    public void ClearInspectionScreen()
    {
        if (uiInspectIcon != null)
        {
            uiInspectIcon.sprite = null;
            uiInspectIcon.color = new Color(0, 0, 0, 0); 
        }
    }

    /// <summary>Gets the current number of full corruption rows (10 blocks per row).</summary>
    public int CurrentCorruptionRows
    {
        get
        {
            int count = 0;
            if (inventoryState != null && inventoryState.mainGridSlots != null)
            {
                foreach (ItemData item in inventoryState.mainGridSlots)
                {
                    if (item == corruptionData) count++;
                }
            }
            return count / 10;
        }
    }

    /// <summary>
    /// Gets the total corruption percentage as a value between 0.0 and 1.0.
    /// </summary>
    public float GetCorruptionPercentage()
    {
        int count = 0;
        foreach (ItemData item in inventoryState.mainGridSlots)
        {
            if (item == corruptionData) count++;
        }
        return Mathf.Clamp01(count / 100f);
    }

    /// <summary>
    /// Checks if an item with the specified ID exists in any player inventory.
    /// </summary>
    public bool HasItem(string itemID)
    {
        if (string.IsNullOrEmpty(itemID)) return false;

        // Check main M.E.T. Rig grid.
        foreach (var item in inventoryState.mainGridSlots)
        {
            if (item != null && item.itemID == itemID) return true;
        }
        // Check external buffer (where picked up items go)
        foreach (var item in inventoryState.matterBufferSlots)
        {
            if (item != null && item.itemID == itemID) return true;
        }
        return false;
    }

    /// <summary>
    /// Finds and removes the first instance of an item with the specified ID from any player inventory.
    /// </summary>
    public bool ConsumeItem(string itemID)
    {
        if (string.IsNullOrEmpty(itemID)) return false;

        // Prioritize consuming from the main grid first.
        for (int i = 0; i < inventoryState.mainGridSlots.Count; i++)
        {
            if (inventoryState.mainGridSlots[i] != null && inventoryState.mainGridSlots[i].itemID == itemID)
            {
                inventoryState.mainGridSlots[i] = null;
                RefreshAllGrids();
                return true;
            }
        }
        
        // If not found, consume from the external buffer.
        for (int i = 0; i < inventoryState.matterBufferSlots.Count; i++)
        {
            if (inventoryState.matterBufferSlots[i] != null && inventoryState.matterBufferSlots[i].itemID == itemID)
            {
                inventoryState.matterBufferSlots[i] = null;
                RefreshAllGrids();
                return true;
            }
        }

        return false;
    }
}
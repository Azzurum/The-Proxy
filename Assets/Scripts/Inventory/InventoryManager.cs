using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

public class InventoryManager : MonoBehaviour
{
    [Header("Grid Configuration")]
    public int gridWidth = 10; // 10 columns 
    public int gridHeight = 10; // Row Index 0 to 9, where 9 is the top 
    [Header("Visual UI Setup")]
    public GameObject slotPrefab;
    public Transform gridContainer;
    [Header("World Spawning")]
    public Transform playerTransform; // Drag Player_Kaelen here in the Inspector
    public GameObject physicalBatteryPrefab; // The physical item to spawn on the ground
    [Header("Corruption Setup")]
    public GameObject corruptionPrefab;
    [Header("Pickup Setup")]
    public GameObject uiBatteryPrefab; // The 1x2 UI item to spawn
    [Header("Active Data")]
    public List<InventoryItem> activeItems = new List<InventoryItem>(); // This list tracks every item currently materialized in Kaelen's M.E.T. Rig
    [Header("MOTHER-v4 System Shock")]
    public float shockInterval = 10f; // Seconds between each corruption wave
    private float shockTimer;
    public bool isSystemActive = true; // A switch to pause the timer if needed

    void Start()
    {
        GenerateVisualGrid();
        shockTimer = shockInterval;
    }

    private void GenerateVisualGrid()
    {
        int totalSlots = gridWidth * gridHeight;
        for (int i = 0; i < totalSlots; i++)
        {
            GameObject newSlot = Instantiate(slotPrefab, gridContainer);

            // Calculate the exact Column (X) and Row (Y)
            int col = i % gridWidth;
            // UI generates top-to-bottom, but our logic requires Row 0 to be the bottom
            int row = (gridHeight - 1) - (i / gridWidth);

            // Assign the coordinate to the slot's memory
            newSlot.GetComponent<InventorySlot>().slotCoordinate = new Vector2Int(col, row);
        }
    }

    // Notice we added cellsX and cellsY so the backend knows the physical size of the item!
    public void RegisterItemPlacement(GameObject uiItem, Vector2Int anchorCoordinate, int cellsX, int cellsY, bool isRotated)
    {
        // 1. CLEANUP: If we just moved this item from another slot, erase its old memory first!
        activeItems.RemoveAll(item => item.uiObject == uiItem);

        // 2. Register the new data
        InventoryItem newData = new InventoryItem();
        newData.position = anchorCoordinate;
        newData.size = new Vector2Int(cellsX, cellsY); // Save its bounding box
        newData.isRotated = isRotated;
        newData.uiObject = uiItem;

        activeItems.Add(newData);
    }

    public void ResolveCorruptionTick()
    {
        // 1. SHIFT EVERYTHING UP
        foreach (var item in activeItems)
        {
            item.position.y += 1;

            if (item.uiObject != null)
            {
                RectTransform rect = item.uiObject.GetComponent<RectTransform>();
                rect.anchoredPosition += new Vector2(0, 80f);
            }
        }

        // 2. CEILING EJECTION CHECK
        // We loop backwards because we are deleting things from the list as we go
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            InventoryItem item = activeItems[i];

            // Calculate the absolute top row this specific item occupies
            int topEdge = item.position.y + item.size.y - 1;

            // If the top edge is pushed past Row 9...
            if (topEdge > 9)
            {
                // A. If it's a helpful item (like our Battery), eject it to the physical world
                if (!item.isCorruption && item.uiObject != null)
                {
                    if (physicalBatteryPrefab != null && playerTransform != null)
                    {
                        Instantiate(physicalBatteryPrefab, playerTransform.position, Quaternion.identity);
                        Debug.LogWarning("WARNING: Storage breach! Item forcibly ejected from M.E.T. Rig!");
                    }
                }
                // B. If it's just Corruption hitting the ceiling, we just let it despawn quietly

                // Destroy the UI visual
                if (item.uiObject != null) Destroy(item.uiObject);

                // Delete the data from memory
                activeItems.RemoveAt(i);
            }
        }

        // 3. SPAWN NEW CORRUPTION AT THE BOTTOM
        SpawnCorruptionAtRowZero();
    }

    void Update()
    {
        // 1. The Automated Timer
        if (isSystemActive)
        {
            shockTimer -= Time.deltaTime;

            if (shockTimer <= 0f)
            {
                Debug.LogWarning("SYSTEM SHOCK: MOTHER-v4 is corrupting the grid!");
                ResolveCorruptionTick(); // Trigger the wave!

                shockTimer = shockInterval; // Reset the clock
            }
        }

        // 2. Keep your 'C' key for testing the Clean Protocol!
        if (Input.GetKeyDown(KeyCode.C))
        {
            ExecuteCleanProtocol();
        }
    }

    public void ExecuteClean()
    {
        // 1. Remove the bottom row of corruption
        RemoveBottomCorruptionRow();

        // 2. Data Gravity Reversion: Sort items by their Y position (bottom to top)
        // This ensures lower items drop first, clearing space for the higher items to follow.
        activeItems.Sort((a, b) => a.position.y.CompareTo(b.position.y));

        // 3. Snap items down
        foreach (var item in activeItems)
        {
            ApplyGravityDrop(item);
        }

        // 4. Clear any penalties if the top row is safe again
        ResetCrushPenaltyIfClear();
    }

    public void ExecuteCleanProtocol()
    {
        bool didCleanAnything = false;

        // 1. VAPORIZE ROW 0
        // We scan backwards so we can safely delete items from the list as we find them
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            InventoryItem item = activeItems[i];

            // If it is a Corruption block AND it is on the bottom row...
            if (item.isCorruption && item.position.y == 0)
            {
                // Destroy the visual red block
                if (item.uiObject != null) Destroy(item.uiObject);

                // Remove the data from the manager's memory
                activeItems.RemoveAt(i);
                didCleanAnything = true;
            }
        }

        // 2. GRAVITY SHIFT
        // If we successfully deleted row 0, everything else falls down to fill the gap
        if (didCleanAnything)
        {
            foreach (var item in activeItems)
            {
                // Math: Shift the data down by 1 row
                item.position.y -= 1;

                // Visuals: Physically move the UI object DOWN by one grid step (80 pixels)
                if (item.uiObject != null)
                {
                    RectTransform rect = item.uiObject.GetComponent<RectTransform>();
                    rect.anchoredPosition -= new Vector2(0, 80f);
                }
            }
            Debug.Log("CLEAN PROTOCOL EXECUTED: Row 0 vaporized. Gravity shift applied.");
        }
        else
        {
            Debug.Log("CLEAN PROTOCOL FAILED: No corruption detected on Row 0.");
        }
    }

    public void DiscardItemToWorld(GameObject uiItem)
    {
        // 1. NEW: Erase the battery from the backend memory BEFORE destroying it!
        activeItems.RemoveAll(item => item.uiObject == uiItem);

        // 2. Spawn the physical item at Kaelen's feet
        if (physicalBatteryPrefab != null && playerTransform != null)
        {
            Instantiate(physicalBatteryPrefab, playerTransform.position, Quaternion.identity);
            Debug.Log("Item ejected from M.E.T. Rig into the physical world.");
        }

        // 3. Destroy the dragged UI element permanently
        Destroy(uiItem);
    }

    // --- Helper Methods (Stubs to prevent errors until we build them) ---

    private void EscalateCrushPenaltyTimer()
    {
        Debug.Log("System Crush! Escalating penalty tier.");
    }

    private void RespawnItemAtOriginalSpawn(InventoryItem item)
    {
        Debug.Log("Quest item pushed out! Respawning in world.");
    }

    private void EjectItemToWorld(InventoryItem item)
    {
        Debug.Log("Item ejected from M.E.T. Rig into the physical world.");
    }

    private void SpawnCorruptionAtRowZero()
    {
        // NEW: Tell Unity to include "Inactive" objects in its search!
        InventorySlot[] allSlots = FindObjectsByType<InventorySlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        // Loop through all 10 columns (X: 0 to 9)
        for (int x = 0; x < gridWidth; x++)
        {
            Vector2Int spawnCoord = new Vector2Int(x, 0); // Always Row 0

            // Find the specific visual slot that matches this coordinate
            foreach (var slot in allSlots)
            {
                if (slot.slotCoordinate == spawnCoord)
                {
                    // 1. Spawn the visual red block on top of the slot
                    GameObject newBlock = Instantiate(corruptionPrefab, gridContainer.parent);
                    newBlock.transform.position = slot.transform.position;

                    // 2. Register it in the backend data as Corruption
                    InventoryItem badData = new InventoryItem();
                    badData.position = spawnCoord;
                    badData.size = new Vector2Int(1, 1);
                    badData.uiObject = newBlock;
                    badData.isCorruption = true; // Flag it!

                    activeItems.Add(badData);
                    break;
                }
            }
        }

        Debug.Log("SYSTEM SHOCK: Row 0 filled with Corruption!");
    }

    public bool TryPickupBattery()
    {
        // 1. THE GHOST SWEEPER: Force-clear any destroyed UI items from memory before doing math
        activeItems.RemoveAll(item => item.uiObject == null);

        int sizeX = 1;
        int sizeY = 2; // Battery size

        // 2. BULLETPROOF SEARCH: Find slots directly inside the container, even if the Canvas is disabled!
        InventorySlot[] allSlots = gridContainer.GetComponentsInChildren<InventorySlot>(true);

        if (allSlots.Length == 0)
        {
            Debug.LogError("CRITICAL BUG: The Manager cannot find the Grid Slots. Check your Grid Container reference!");
            return false;
        }

        // 3. Scan the grid bottom-to-top, left-to-right
        for (int y = 0; y <= 10 - sizeY; y++)
        {
            for (int x = 0; x <= 10 - sizeX; x++)
            {
                Vector2Int testCoord = new Vector2Int(x, y);

                // Ask the Gatekeeper if this specific spot is empty
                if (IsSpaceFree(testCoord, sizeX, sizeY, null))
                {
                    // Space found! Find the matching visual slot.
                    foreach (var slot in allSlots)
                    {
                        if (slot.slotCoordinate == testCoord)
                        {
                            // Spawn the UI Battery
                            GameObject newItem = Instantiate(uiBatteryPrefab, gridContainer.parent);

                            // Center it, then apply the 40px downward shift (for 2-cell tall items)
                            newItem.transform.position = slot.transform.position;
                            // Center it on the bottom slot, then shift it UP by 40px so it covers the slot above it!
                            newItem.transform.position = slot.transform.position;
                            newItem.GetComponent<RectTransform>().anchoredPosition += new Vector2(0, 40f);

                            // Register it permanently into the backend math
                            RegisterItemPlacement(newItem, testCoord, sizeX, sizeY, false);

                            Debug.Log($"<color=green>SUCCESS: Battery vacuumed into Column {testCoord.x}, Row {testCoord.y}</color>");
                            return true; // Stop looking!
                        }
                    }
                }
            }
        }

        Debug.LogWarning($"PICKUP FAILED: Tracking {activeItems.Count} items. No 1x2 space found.");
        return false;
    }

    private void RemoveBottomCorruptionRow()
    {
        Debug.Log("Standard Clean executed: Row 0 Corruption removed.");
    }

    private void ApplyGravityDrop(InventoryItem item)
    {
        // We will write the complex collision math for this later.
        // For now, it just acknowledges the drop.
        Debug.Log("Applying Data Gravity: Item snaps down to lowest available slot.");
    }

    private void ResetCrushPenaltyIfClear()
    {
        Debug.Log("Top row checked. Crush penalties reset if clear.");
    }

    // The Gatekeeper: Checks if every single cell an item wants to occupy is empty
    public bool IsSpaceFree(Vector2Int anchor, int width, int height, GameObject itemBeingMoved)
    {
        // Loop through the theoretical grid space the item is hovering over
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int targetCell = new Vector2Int(anchor.x + x, anchor.y + y);

                // Check this cell against every item and corruption block on the board
                foreach (var item in activeItems)
                {
                    // Ignore the item we are currently holding, so it doesn't collide with itself!
                    if (item.uiObject == itemBeingMoved) continue;

                    // Check if our target cell falls inside this existing item's boundaries
                    bool overlapsX = targetCell.x >= item.position.x && targetCell.x < (item.position.x + item.size.x);
                    bool overlapsY = targetCell.y >= item.position.y && targetCell.y < (item.position.y + item.size.y);

                    if (overlapsX && overlapsY)
                    {
                        return false; // COLLISION DETECTED! Space is blocked.
                    }
                }
            }
        }
        return true; // The entire space is completely free
    }
}
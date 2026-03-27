using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    [Header("Grid Configuration")]
    public int gridWidth = 10;
    public int gridHeight = 10;

    [Header("Visual UI Setup")]
    public GameObject slotPrefab;
    public Transform gridContainer;

    [Header("World Spawning")]
    public Transform playerTransform;
    public GameObject physicalBatteryPrefab;

    [Header("Corruption Setup")]
    public GameObject corruptionPrefab;

    [Header("Pickup Setup")]
    public GameObject uiBatteryPrefab;

    [Header("Active Data")]
    public List<InventoryItem> activeItems = new List<InventoryItem>();

    [Header("MOTHER-v4 System Shock")]
    public float shockInterval = 10f;
    private float shockTimer;
    public bool isSystemActive = true;
    public Slider systemShockProgressBar;

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

            int col = i % gridWidth;
            int row = (gridHeight - 1) - (i / gridWidth);

            newSlot.GetComponent<InventorySlot>().slotCoordinate = new Vector2Int(col, row);
        }
    }

    public void RegisterItemPlacement(GameObject uiItem, Vector2Int anchorCoordinate, int cellsX, int cellsY, bool isRotated)
    {
        // 1. Erase old memory if moved
        activeItems.RemoveAll(item => item.uiObject == uiItem);

        // 2. Register new placement
        InventoryItem newData = new InventoryItem();
        newData.position = anchorCoordinate;
        newData.size = new Vector2Int(cellsX, cellsY);
        newData.isRotated = isRotated;
        newData.uiObject = uiItem;

        activeItems.Add(newData);
    }

    void Update()
    {
        // 1. Automated System Shock Timer
        if (isSystemActive)
        {
            shockTimer -= Time.deltaTime;

            if (systemShockProgressBar != null)
            {
                systemShockProgressBar.value = 1f - (shockTimer / shockInterval);
            }

            if (shockTimer <= 0f)
            {
                Debug.LogWarning("SYSTEM SHOCK: MOTHER-v4 is corrupting the grid!");
                ResolveCorruptionTick();
                shockTimer = shockInterval;
            }
        }

        // 2. Manual Clean Protocol Trigger
        if (Input.GetKeyDown(KeyCode.C))
        {
            ExecuteCleanProtocol();
        }
    }

    public void ResolveCorruptionTick()
    {
        // 1. Shift everything UP
        foreach (var item in activeItems)
        {
            item.position.y += 1;

            if (item.uiObject != null)
            {
                RectTransform rect = item.uiObject.GetComponent<RectTransform>();
                rect.anchoredPosition += new Vector2(0, 80f);
            }
        }

        // 2. Ceiling Ejection Check (Row 9+)
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            InventoryItem item = activeItems[i];
            int topEdge = item.position.y + item.size.y - 1;

            if (topEdge > 9)
            {
                // Eject helpful items to the physical world
                if (!item.isCorruption && item.uiObject != null)
                {
                    if (physicalBatteryPrefab != null && playerTransform != null)
                    {
                        Instantiate(physicalBatteryPrefab, playerTransform.position, Quaternion.identity);
                        Debug.LogWarning("WARNING: Storage breach! Item forcibly ejected from M.E.T. Rig!");
                    }
                }

                // Destroy UI visual and delete from memory
                if (item.uiObject != null) Destroy(item.uiObject);
                activeItems.RemoveAt(i);
            }
        }

        // 3. Spawn new corruption at bottom
        SpawnCorruptionAtRowZero();
    }

    public void ExecuteCleanProtocol()
    {
        bool didCleanAnything = false;

        // 1. Vaporize Row 0 Corruption
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            InventoryItem item = activeItems[i];

            if (item.isCorruption && item.position.y == 0)
            {
                if (item.uiObject != null) Destroy(item.uiObject);
                activeItems.RemoveAt(i);
                didCleanAnything = true;
            }
        }

        // 2. Apply Gravity Shift (Items fall down 1 row)
        if (didCleanAnything)
        {
            foreach (var item in activeItems)
            {
                item.position.y -= 1;

                if (item.uiObject != null)
                {
                    RectTransform rect = item.uiObject.GetComponent<RectTransform>();
                    rect.anchoredPosition -= new Vector2(0, 80f);
                }
            }
            Debug.Log("CLEAN PROTOCOL EXECUTED: Gravity shift applied.");
        }
    }

    public void DiscardItemToWorld(GameObject uiItem)
    {
        activeItems.RemoveAll(item => item.uiObject == uiItem);

        if (physicalBatteryPrefab != null && playerTransform != null)
        {
            Instantiate(physicalBatteryPrefab, playerTransform.position, Quaternion.identity);
        }

        Destroy(uiItem);
    }

    private void SpawnCorruptionAtRowZero()
    {
        InventorySlot[] allSlots = FindObjectsByType<InventorySlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int x = 0; x < gridWidth; x++)
        {
            Vector2Int spawnCoord = new Vector2Int(x, 0);

            foreach (var slot in allSlots)
            {
                if (slot.slotCoordinate == spawnCoord)
                {
                    GameObject newBlock = Instantiate(corruptionPrefab, gridContainer.parent);
                    newBlock.transform.position = slot.transform.position;

                    InventoryItem badData = new InventoryItem();
                    badData.position = spawnCoord;
                    badData.size = new Vector2Int(1, 1);
                    badData.uiObject = newBlock;
                    badData.isCorruption = true;

                    activeItems.Add(badData);
                    break;
                }
            }
        }
    }

    public bool TryPickupItem(GameObject uiPrefabToSpawn, int sizeX, int sizeY, bool isQuestItem = false)
    {
        activeItems.RemoveAll(item => item.uiObject == null);

        InventorySlot[] allSlots = gridContainer.GetComponentsInChildren<InventorySlot>(true);
        if (allSlots.Length == 0) return false;

        // Scan the grid bottom-to-top, left-to-right
        for (int y = 0; y <= gridHeight - sizeY; y++)
        {
            for (int x = 0; x <= gridWidth - sizeX; x++)
            {
                Vector2Int testCoord = new Vector2Int(x, y);

                // Ask the Gatekeeper if this massive footprint is entirely empty
                if (IsSpaceFree(testCoord, sizeX, sizeY, null))
                {
                    foreach (var slot in allSlots)
                    {
                        if (slot.slotCoordinate == testCoord)
                        {
                            GameObject newItem = Instantiate(uiPrefabToSpawn, gridContainer.parent);

                            // Center the object on the bottom-left anchor slot
                            newItem.transform.position = slot.transform.position;

                            // MATH: Shift the UI up and right based on how many extra cells it takes up (40px per extra cell)
                            float shiftX = (sizeX - 1) * 40f;
                            float shiftY = (sizeY - 1) * 40f;
                            newItem.GetComponent<RectTransform>().anchoredPosition += new Vector2(shiftX, shiftY);

                            // Register it into the backend math
                            RegisterItemPlacement(newItem, testCoord, sizeX, sizeY, false);

                            // If it's a quest item, we flag it so it triggers the System Crush later!
                            activeItems[activeItems.Count - 1].isQuestItem = isQuestItem;

                            return true;
                        }
                    }
                }
            }
        }

        Debug.LogWarning($"<color=red>PICKUP FAILED:</color> Not enough contiguous space for a {sizeX}x{sizeY} item!");
        return false;
    }

    public bool IsSpaceFree(Vector2Int anchor, int width, int height, GameObject itemBeingMoved)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int targetCell = new Vector2Int(anchor.x + x, anchor.y + y);

                foreach (var item in activeItems)
                {
                    if (item.uiObject == itemBeingMoved) continue;

                    bool overlapsX = targetCell.x >= item.position.x && targetCell.x < (item.position.x + item.size.x);
                    bool overlapsY = targetCell.y >= item.position.y && targetCell.y < (item.position.y + item.size.y);

                    if (overlapsX && overlapsY) return false;
                }
            }
        }
        return true;
    }

    // --- NEW: Objective & Combat Resource Management ---
    public bool TryConsumeBatteries(int amountRequired)
    {
        List<InventoryItem> foundBatteries = new List<InventoryItem>();

        foreach (var item in activeItems)
        {
            // If it's not corruption, assume it's a battery
            if (!item.isCorruption)
            {
                foundBatteries.Add(item);
            }
        }

        if (foundBatteries.Count >= amountRequired)
        {
            for (int i = 0; i < amountRequired; i++)
            {
                InventoryItem batteryToBurn = foundBatteries[i];
                if (batteryToBurn.uiObject != null) Destroy(batteryToBurn.uiObject);
                activeItems.Remove(batteryToBurn);
            }
            return true; // Successfully consumed
        }

        return false; // Not enough batteries
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Text;

/// <summary>
/// Manages the serialization and deserialization of the spatial inventory grid, converting complex shapes to flat data.
/// </summary>
public class InventorySaveHandler : MonoBehaviour
{
    public static InventorySaveHandler Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Mathematically maps the layout of the UI grids and creates a list of items and their rotation states.
    /// </summary>
    public List<SavedGridItem> ExportInventoryForSave()
    {
        List<SavedGridItem> savedItems = new List<SavedGridItem>();
        InventoryManager inv = InventoryManager.Instance;
        
        bool[,] memoryMask = new bool[18, 10];

        ItemData GetItemAtGlobal(int x, int y)
        {
            if (x < 0 || y < 0 || y >= 10 || x >= 18) return null;
            if (x < 5) return inv.inventoryState.mainGridSlots[(y * 5) + x];
            if (x < 10) return inv.inventoryState.mainGridSlots[50 + (y * 5) + (x - 5)];
            if (x < 15 && y < 5 && inv.inventoryState.extGridSlots != null) return inv.inventoryState.extGridSlots[(y * 5) + (x - 10)];
            if (x >= 15 && x < 18 && y == 0 && inv.inventoryState.hotbarSlots != null) return inv.inventoryState.hotbarSlots[x - 15];
            return null;
        }

        for (int gy = 0; gy < 10; gy++)
        {
            for (int gx = 0; gx < 18; gx++)
            {
                if (memoryMask[gx, gy]) continue;

                ItemData item = GetItemAtGlobal(gx, gy);
                if (item == null || item == inv.corruptionData) continue;
                if (string.IsNullOrEmpty(item.itemID)) continue;

                ItemFootprint fp = item.GetFootprint();
                bool isRotated = false;

                if (fp != null)
                {
                    if (fp.width == 1 && fp.height > 1) 
                    {
                        ItemData rightItem = GetItemAtGlobal(gx + 1, gy);
                        if (rightItem == item) isRotated = true; 
                    }
                    else if (fp.width > 1 && fp.height == 1)
                    {
                        ItemData bottomItem = GetItemAtGlobal(gx, gy + 1);
                        if (bottomItem == item) isRotated = true; 
                    }
                }

                savedItems.Add(new SavedGridItem(item.itemID, gx, gy, isRotated));

                int minX, maxX, minY, maxY;
                if (gx < 5)      { minX = 0;  maxX = 4;  minY = 0; maxY = 9; } // Left Main Grid
                else if (gx < 10){ minX = 5;  maxX = 9;  minY = 0; maxY = 9; } // Right Main Grid
                else if (gx < 15){ minX = 10; maxX = 14; minY = 0; maxY = 4; } // Ext Storage Grid
                else             { minX = 15; maxX = 17; minY = 0; maxY = 0; } // Hotbar Slots

                int w = fp != null ? fp.width : 1;
                int h = fp != null ? fp.height : 1;
                
                if (isRotated) { int temp = w; w = h; h = temp; } 

                ItemFootprint activeFootprint = isRotated && fp != null ? fp.GetRotated() : fp;

                for (int fy = 0; fy < h; fy++)
                {
                    for (int fx = 0; fx < w; fx++)
                    {
                        if (activeFootprint != null && !activeFootprint.GetCell(fx, fy)) continue;

                        int targetX = gx + fx;
                        int targetY = gy + fy;

                        if (targetX >= minX && targetX <= maxX && targetY >= minY && targetY <= maxY)
                        {
                            memoryMask[targetX, targetY] = true;
                        }
                    }
                }
            }
        }

        return savedItems;
    }

    /// <summary>
    /// Destroys current UI layouts and mathematically rebuilds the physical layout of the loaded inventory.
    /// </summary>
    public void LoadInventoryFromSave(List<SavedGridItem> savedItems, float savedCorruptionPct)
    {
        InventoryManager inv = InventoryManager.Instance;
        float currentCellSize = 75f;
        if (inv.cellSizeOverride > 0f) currentCellSize = inv.cellSizeOverride;

        void BuildGrid(Transform grid, int cols, int rows, InventorySlot.GridRegion region)
        {
            if (grid == null) return;
            
            for (int i = 0; i < cols * rows; i++)
            {
                Transform slotTransform;
                if (i < grid.childCount)
                {
                    slotTransform = grid.GetChild(i);
                    for (int c = slotTransform.childCount - 1; c >= 0; c--) Destroy(slotTransform.GetChild(c).gameObject);
                }
                else
                {
                    GameObject slotObj = Instantiate(inv.emptySlotPrefab, grid);
                    slotTransform = slotObj.transform;
                    
                    RectTransform slotRect = slotObj.GetComponent<RectTransform>();
                    if (slotRect != null) { slotRect.localScale = Vector3.one; slotRect.localRotation = Quaternion.identity; slotRect.pivot = new Vector2(0f, 1f); }
                    
                    InventorySlot slotLogic = slotObj.GetComponent<InventorySlot>();
                    if (slotLogic != null)
                    {
                        slotLogic.slotCoordinate = new Vector2Int(i % cols, i / cols);
                        slotLogic.gridRegion = region;
                    }
                }
            }
        }

        BuildGrid(inv.gridLeft, 5, 10, InventorySlot.GridRegion.MainLeft);
        BuildGrid(inv.gridRight, 5, 10, InventorySlot.GridRegion.MainRight);
        if (inv.gridExt != null) BuildGrid(inv.gridExt, 5, 5, InventorySlot.GridRegion.External);

        foreach (SavedGridItem savedItem in savedItems)
        {
            ItemData foundData = inv.itemDatabase.Find(x => x.itemID == savedItem.itemID);
            if (foundData != null)
            {
                Transform targetSlot = null;
                if (savedItem.gridPosX < 5) targetSlot = inv.gridLeft.GetChild((savedItem.gridPosY * 5) + savedItem.gridPosX);
                else if (savedItem.gridPosX < 10) targetSlot = inv.gridRight.GetChild((savedItem.gridPosY * 5) + (savedItem.gridPosX - 5));
                else if (inv.gridExt != null && savedItem.gridPosX < 15) targetSlot = inv.gridExt.GetChild((savedItem.gridPosY * 5) + (savedItem.gridPosX - 10));
                else if (savedItem.gridPosX >= 15 && savedItem.gridPosX < 18)
                {
                    int hotbarIndex = savedItem.gridPosX - 15;
                    if (HotbarManager.Instance != null && HotbarManager.Instance.quickSlots.Length > hotbarIndex)
                    {
                        targetSlot = HotbarManager.Instance.quickSlots[hotbarIndex].transform;
                    }
                    if (inv.inventoryState.hotbarSlots != null && inv.inventoryState.hotbarSlots.Count > hotbarIndex)
                    {
                        inv.inventoryState.hotbarSlots[hotbarIndex] = foundData;
                    }
                    
                    if (targetSlot == null) continue;
                }

                if (targetSlot != null)
                {
                    GameObject newObj = Instantiate(inv.filledItemPrefab, targetSlot);
                    
                    RectTransform itemRect = newObj.GetComponent<RectTransform>();
                    if (itemRect != null) { itemRect.localScale = Vector3.one; itemRect.localRotation = Quaternion.identity; }
                    
                    Canvas itemCanvas = newObj.GetComponent<Canvas>();
                    if (itemCanvas != null) { itemCanvas.overrideSorting = true; itemCanvas.sortingOrder = (targetSlot.parent == inv.gridExt) ? 1 : 5; }

                    UIItem uiItem = newObj.GetComponent<UIItem>();
                    if (uiItem != null) uiItem.Initialize(foundData, currentCellSize, savedItem.isRotated); 

                    DraggableItem dragItem = newObj.GetComponent<DraggableItem>();
                    if (dragItem != null) {
                        dragItem.cellSize = currentCellSize;
                        dragItem.UpdateVisualSize();
                    }

                    if (targetSlot.GetComponent<HotbarSlot>() != null && dragItem != null)
                    {
                        targetSlot.GetComponent<HotbarSlot>().containedItem = dragItem;
                    }
                }
            }
            else Debug.LogWarning($"<color=red>LOAD ERROR:</color> Item ID {savedItem.itemID} missing from Database!");
        }

        int corruptionBlocksToSpawn = Mathf.FloorToInt(savedCorruptionPct * 100f);
        int spawned = 0;
        
        for (int row = 9; row >= 0 && spawned < corruptionBlocksToSpawn; row--)
        {
            for (int col = 0; col < 5 && spawned < corruptionBlocksToSpawn; col++)
            {
                Transform slot = inv.gridLeft.GetChild((row * 5) + col);
                if (slot.childCount == 0)
                {
                    GameObject crptObj = Instantiate(inv.filledItemPrefab, slot);
                    UIItem uiItem = crptObj.GetComponent<UIItem>();
                    if (uiItem != null) uiItem.Initialize(inv.corruptionData, currentCellSize);
                    spawned++;
                }
            }
            for (int col = 0; col < 5 && spawned < corruptionBlocksToSpawn; col++)
            {
                Transform slot = inv.gridRight.GetChild((row * 5) + col);
                if (slot.childCount == 0)
                {
                    GameObject crptObj = Instantiate(inv.filledItemPrefab, slot);
                    UIItem uiItem = crptObj.GetComponent<UIItem>();
                    if (uiItem != null) uiItem.Initialize(inv.corruptionData, currentCellSize);
                    spawned++;
                }
            }
        }

        inv.gridRefreshPending = false;  
        inv.SyncDataFromUI();            
    }

    /// <summary>
    /// Scans the current scene for items dropped on the floor and serializes their positions to PlayerPrefs.
    /// </summary>
    public void SaveWorldItems(GameObject ignoreObj = null)
    {
        PhysicalItem[] itemsOnFloor = FindObjectsByType<PhysicalItem>(FindObjectsInactive.Exclude);
        StringBuilder sb = new StringBuilder();

        foreach (PhysicalItem pi in itemsOnFloor)
        {
            if (pi == null || pi.itemData == null || pi.gameObject == ignoreObj) continue;

            Vector3 pos = pi.IsBouncing ? pi.TargetPosition : pi.transform.position;
            float rotZ = pi.transform.eulerAngles.z;
            sb.Append($"{pi.itemData.itemID},{pos.x.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)},{pos.y.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)},{pos.z.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)},{rotZ.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)};");
        }

        string sceneName = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString("WorldItems_" + sceneName, sb.ToString());
        PlayerPrefs.SetInt("WorldItemsSaved_" + sceneName, 1);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Instantiates world prefabs based on the serialized PlayerPrefs data for the current scene.
    /// </summary>
    public void LoadWorldItems()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (PlayerPrefs.GetInt("WorldItemsSaved_" + sceneName, 0) == 0) return;

        PhysicalItem[] defaultItems = FindObjectsByType<PhysicalItem>(FindObjectsInactive.Exclude);
        foreach (PhysicalItem pi in defaultItems) Destroy(pi.gameObject);

        string saveString = PlayerPrefs.GetString("WorldItems_" + sceneName, "");
        if (string.IsNullOrEmpty(saveString)) return; 

        string[] items = saveString.Split(';');
        foreach (string itemDataString in items)
        {
            if (string.IsNullOrEmpty(itemDataString)) continue;

            string[] data = itemDataString.Split(',');
            if (data.Length >= 4)
            {
                string id = data[0];
                if (float.TryParse(data[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x) && float.TryParse(data[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y) && float.TryParse(data[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float z))
                {
                    ItemData foundItem = InventoryManager.Instance.itemDatabase.Find(i => i.itemID == id);
                    if (foundItem != null && foundItem.worldPrefab != null)
                    {
                        GameObject spawned = Instantiate(foundItem.worldPrefab, new Vector3(x, y, z), Quaternion.identity);
                        
                        if (data.Length >= 5 && float.TryParse(data[4], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float savedRotZ))
                            spawned.transform.rotation = Quaternion.Euler(0, 0, savedRotZ);
                            
                        PhysicalItem pi = spawned.GetComponent<PhysicalItem>();
                        if (pi == null) pi = spawned.GetComponentInChildren<PhysicalItem>();
                        if (pi != null) pi.itemData = foundItem;
                    }
                }
            }
        }
    }
}
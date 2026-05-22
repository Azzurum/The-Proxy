using UnityEngine;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Controls physical locker storage units in the world, mapping random loot or saved items to the 5x5 external grid.
/// </summary>
public class LockerStorage : MonoBehaviour
{
    [Header("Locker Settings")]
    [Tooltip("A unique identifier string used to save and load this locker's contents across sessions.")]
    public string lockerID; 

    [Header("Loot Configuration (5x5 Grid)")]
    [Tooltip("Drag items into these 25 slots to design the exact layout of this locker's loot!")]
    public List<ItemData> gridSlots = new List<ItemData>(25);

    [Header("Randomized Loot")]
    [Tooltip("If true, the locker will ignore manually placed items and generate random loot on start.")]
    public bool randomizeLoot = false;
    [Tooltip("The minimum number of random items to spawn.")]
    public int minItems = 1;
    [Tooltip("The maximum number of random items to spawn.")]
    public int maxItems = 3;
    [Tooltip("The pool of item definitions to randomly select from.")]
    public List<ItemData> lootPool = new List<ItemData>();

    [Header("Debug")]
    [Tooltip("Check this to ignore Save Data and force generate new loot every time the game starts. (Uncheck for final build!)")]
    public bool forceRerollLoot = false;

    void Awake()
    {
        while (gridSlots.Count < 25) gridSlots.Add(null);
    }

    void Start()
    {
        StartCoroutine(DelayedInitialization());
    }

    private System.Collections.IEnumerator DelayedInitialization()
    {
        yield return null; 
        
        if (string.IsNullOrEmpty(lockerID))
        {
            Debug.LogWarning($"<color=yellow>LOCKER WARNING:</color> A Locker on this map has an empty LockerID! It will not save.");
        }

        if (forceRerollLoot || !LoadLockerState())
        {
            if (randomizeLoot && lootPool != null && lootPool.Count > 0)
            {
                for (int i = 0; i < gridSlots.Count; i++) gridSlots[i] = null;

                int amountToSpawn = Random.Range(minItems, maxItems + 1);

                for (int i = 0; i < amountToSpawn; i++)
                {
                    ItemData randomItem = null;
                    int safetyCheck = 0;
                    while (randomItem == null && safetyCheck < 10) { randomItem = lootPool[Random.Range(0, lootPool.Count)]; safetyCheck++; }
                    
                    if (randomItem != null) TryPlaceRandomly(randomItem);
                }
            }
            
            SaveLockerState();
        }
    }

    /// <summary>
    /// Serializes the locker's 25 grid slots into a string and saves it via PlayerPrefs.
    /// </summary>
    public void SaveLockerState()
    {
        if (string.IsNullOrEmpty(lockerID)) return;
        
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < gridSlots.Count; i++)
        {
            if (gridSlots[i] != null) sb.Append($"{gridSlots[i].itemID},{gridSlots[i].isRotated}");
            else sb.Append("NONE,False");
            
            if (i < gridSlots.Count - 1) sb.Append(";"); 
        }
        
        PlayerPrefs.SetString("Locker_" + lockerID, sb.ToString());
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Attempts to restore the locker's contents from saved PlayerPrefs data.
    /// </summary>
    public bool LoadLockerState()
    {
        if (string.IsNullOrEmpty(lockerID) || !PlayerPrefs.HasKey("Locker_" + lockerID)) return false;

        string saveString = PlayerPrefs.GetString("Locker_" + lockerID);
        string[] slots = saveString.Split(';');

        if (InventoryManager.Instance == null) return false;

        for (int i = 0; i < gridSlots.Count && i < slots.Length; i++)
        {
            string[] data = slots[i].Split(',');
            string id = data[0];
            bool isRotated = false;
            if (data.Length > 1) bool.TryParse(data[1], out isRotated);

            if (id == "NONE") gridSlots[i] = null;
            else
            {
                ItemData foundItem = InventoryManager.Instance.itemDatabase.Find(x => x.itemID == id);
                if (foundItem != null)
                {
                    gridSlots[i] = foundItem;
                }
                else gridSlots[i] = null;
            }
        }
        return true;
    }

    private bool TryPlaceRandomly(ItemData item)
    {
        if (item == null) return false;

        ItemFootprint fp = item.GetFootprint();
        int w = fp != null ? fp.width : 1;
        int h = fp != null ? fp.height : 1;

        for (int attempt = 0; attempt < 20; attempt++)
        {
            int startX = Random.Range(0, Mathf.Max(1, 6 - w));
            int startY = Random.Range(0, Mathf.Max(1, 6 - h));

            bool spaceFree = true;

            for (int cy = 0; cy < h; cy++)
            {
                for (int cx = 0; cx < w; cx++)
                {
                    if (fp != null && !fp.GetCell(cx, cy)) continue;

                    int index = (startY + cy) * 5 + (startX + cx);
                    if (gridSlots[index] != null) spaceFree = false;
                }
            }

            if (spaceFree)
            {
                for (int cy = 0; cy < h; cy++)
                {
                    for (int cx = 0; cx < w; cx++)
                    {
                        if (fp != null && !fp.GetCell(cx, cy)) continue;

                        int index = (startY + cy) * 5 + (startX + cx);
                        gridSlots[index] = item;
                    }
                }
                return true; 
            }
        }
        return false; 
    }
}
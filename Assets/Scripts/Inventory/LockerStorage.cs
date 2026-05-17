using UnityEngine;
using System.Collections.Generic;

public class LockerStorage : MonoBehaviour
{
    [Header("Locker Settings")]
    public string lockerID; // Unique ID for a Save/Load system later!

    [Header("Loot Configuration (5x5 Grid)")]
    [Tooltip("Drag items into these 25 slots to design the exact layout of this locker's loot!")]
    public List<ItemData> gridSlots = new List<ItemData>(25);

    [Header("Randomized Loot")]
    [Tooltip("If true, the locker will ignore manually placed items and generate random loot on start.")]
    public bool randomizeLoot = false;
    public int minItems = 1;
    public int maxItems = 3;
    public List<ItemData> lootPool = new List<ItemData>();

    void Awake()
    {
        // Ensure the list is exactly 25 slots so the UI grid math never breaks
        while (gridSlots.Count < 25) gridSlots.Add(null);
    }

    void Start()
    {
        if (string.IsNullOrEmpty(lockerID))
        {
            Debug.LogWarning($"<color=yellow>LOCKER WARNING:</color> A Locker on this map has an empty LockerID! It will not save.");
        }

        // 1. Try to load the locker's state from the hard drive.
        // If no save exists (first time playing), it will return false and run the generation block.
        if (!LoadLockerState())
        {
            // If the box is checked, generate random loot!
            if (randomizeLoot && lootPool != null && lootPool.Count > 0)
            {
                // Clear any pre-existing items first
                for (int i = 0; i < gridSlots.Count; i++) gridSlots[i] = null;

                int amountToSpawn = Random.Range(minItems, maxItems + 1);

                for (int i = 0; i < amountToSpawn; i++)
                {
                    ItemData randomItem = lootPool[Random.Range(0, lootPool.Count)];
                    TryPlaceRandomly(randomItem);
                }
            }
            
            // Save this newly generated (or manually placed) setup so it remembers it forever!
            SaveLockerState();
        }
    }

    public void SaveLockerState()
    {
        if (string.IsNullOrEmpty(lockerID)) return;
        
        string saveString = "";
        for (int i = 0; i < gridSlots.Count; i++)
        {
            if (gridSlots[i] != null) saveString += $"{gridSlots[i].itemID},{gridSlots[i].isRotated}";
            else saveString += "NONE,False";
            
            if (i < gridSlots.Count - 1) saveString += ";"; // Separate each slot with a semicolon
        }
        
        PlayerPrefs.SetString("Locker_" + lockerID, saveString);
        PlayerPrefs.Save();
    }

    public bool LoadLockerState()
    {
        if (string.IsNullOrEmpty(lockerID) || !PlayerPrefs.HasKey("Locker_" + lockerID)) return false;

        string saveString = PlayerPrefs.GetString("Locker_" + lockerID);
        string[] slots = saveString.Split(';');

        InventoryManager inventoryManager = FindAnyObjectByType<InventoryManager>();
        if (inventoryManager == null) return false;

        for (int i = 0; i < gridSlots.Count && i < slots.Length; i++)
        {
            string[] data = slots[i].Split(',');
            string id = data[0];
            bool isRotated = false;
            if (data.Length > 1) bool.TryParse(data[1], out isRotated);

            if (id == "NONE") gridSlots[i] = null;
            else
            {
                ItemData foundItem = inventoryManager.itemDatabase.Find(x => x.itemID == id);
                if (foundItem != null)
                {
                    foundItem.isRotated = isRotated;
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

        // Try 20 times to find a random empty spot that fits this item's shape
        for (int attempt = 0; attempt < 20; attempt++)
        {
            // Pick a random anchor coordinate
            int startX = Random.Range(0, 6 - w);
            int startY = Random.Range(0, 6 - h);

            bool spaceFree = true;

            // Check if the item's specific footprint collides with anything
            for (int cy = 0; cy < h; cy++)
            {
                for (int cx = 0; cx < w; cx++)
                {
                    if (fp != null && !fp.GetCell(cx, cy)) continue;

                    int index = (startY + cy) * 5 + (startX + cx);
                    if (gridSlots[index] != null) spaceFree = false;
                }
            }

            // Lock it into the locker's memory if the space is totally clear!
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
                return true; // Successfully placed!
            }
        }
        return false; // Failed to find room
    }
}
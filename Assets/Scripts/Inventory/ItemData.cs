using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemFootprint
{
    public int width = 1;
    public int height = 1;
    public bool[] cells; // Flattened 1D array: index = y * width + x
    
    public ItemFootprint()
    {
        cells = new bool[1] { true }; // Default 1x1
    }

    public ItemFootprint(int w, int h)
    {
        width = w;
        height = h;
        cells = new bool[w * h];
        for (int i = 0; i < cells.Length; i++) cells[i] = true; // Default all cells filled
    }

    public bool GetCell(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return false;
        return cells[y * width + x];
    }

    public void SetCell(int x, int y, bool value)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        cells[y * width + x] = value;
    }

    public List<Vector2Int> GetOccupiedCells()
    {
        List<Vector2Int> occupied = new List<Vector2Int>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (GetCell(x, y))
                    occupied.Add(new Vector2Int(x, y));
            }
        }
        return occupied;
    }

    public ItemFootprint GetRotated()
    {
        ItemFootprint rotated = new ItemFootprint(height, width);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (GetCell(x, y))
                {
                    int newX = height - 1 - y;
                    int newY = x;
                    rotated.SetCell(newX, newY, true);
                }
            }
        }
        return rotated;
    }
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Core Identification")]
    public string itemID; // Unique identifier (e.g., "BATT", "KEY")
    public string itemName = "UNKNOWN DATA"; // Single definition for Title

    [Header("Properties")]
    public float mass; // In kg (e.g., 1.2f)
    public string status; // e.g., "Volatile", "Sterile", "Corrupted"
    public string substats = "VOL: -- // WGT: --"; // For Inspector Substats
    
    [Header("Visuals & World")]
    public Sprite icon; // UI sprite
    public GameObject worldPrefab; // Physical item to drop

    [Header("Inspector UI Content")]
    [TextArea(3, 6)] 
    public string description = "Awaiting I/O..."; // The main body text

    [Header("Log Console Content")]
    public string logLabel = "SYSTEM LOG"; // The label above the log
    [TextArea(3, 6)] 
    public string logContent = "No log data found."; // The main log text

    [Header("Custom Footprint Matrix")]
    public ItemFootprint footprint; // Define complex shapes here
    
    [Header("Runtime Memory")]
    public bool isRotated = false; // Stores rotation state across inventory opens

    void OnEnable()
    {
        // SAFETY FIX: Prevents items from staying rotated when the game restarts
        isRotated = false;

        if (footprint == null || footprint.cells == null || footprint.cells.Length == 0)
        {
            footprint = new ItemFootprint(1, 1);
        }
    }

    public ItemFootprint GetFootprint()
    {
        if (footprint == null || footprint.cells == null || footprint.cells.Length == 0)
        {
            footprint = new ItemFootprint(1, 1);
        }
        return footprint;
    }
}
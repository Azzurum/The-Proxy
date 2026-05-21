using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents the spatial geometry of a physical item within a 2D grid structure.
/// </summary>
[System.Serializable]
public class ItemFootprint
{
    [Tooltip("The total column width this item occupies.")]
    public int width = 1;
    [Tooltip("The total row height this item occupies.")]
    public int height = 1;
    [Tooltip("Flattened 1D array mapping solid cells in the footprint. Index = y * width + x")]
    public bool[] cells; 
    
    public ItemFootprint()
    {
        cells = new bool[1] { true }; 
    }

    public ItemFootprint(int w, int h)
    {
        width = w;
        height = h;
        cells = new bool[w * h];
        for (int i = 0; i < cells.Length; i++) cells[i] = true; 
    }

    /// <summary>Returns true if the specific local cell coordinate is physically occupied by the item.</summary>
    public bool GetCell(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return false;
        return cells[y * width + x];
    }

    /// <summary>Sets the occupancy state of a specific local cell coordinate.</summary>
    public void SetCell(int x, int y, bool value)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        cells[y * width + x] = value;
    }

    /// <summary>Retrieves a list of all local coordinates (x,y) that represent solid parts of the item.</summary>
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

    /// <summary>
    /// Creates and returns a new 90-degree rotated iteration of this footprint.
    /// </summary>
    public ItemFootprint GetRotated()
    {
        ItemFootprint rotated = new ItemFootprint(height, width);
        
        // High-performance clear to reset all cells to false before applying rotated geometry.
        System.Array.Clear(rotated.cells, 0, rotated.cells.Length);

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

/// <summary>
/// A ScriptableObject defining the core attributes, visuals, and grid behavior of an inventory item.
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Core Identification")]
    [Tooltip("Unique internal identifier code (e.g., 'BATT', 'KEY-MSTR-1').")]
    public string itemID; 
    [Tooltip("The human-readable display name for the UI.")]
    public string itemName = "UNKNOWN DATA"; 

    [Header("Properties")]
    [Tooltip("The physical mass of the item in kilograms.")]
    public float mass; 
    [Tooltip("Short descriptor text indicating current state (e.g., 'Volatile', 'Corrupted').")]
    public string status; 
    [Tooltip("Sub-stat readout shown in the item inspector interface.")]
    public string substats = "VOL: -- // WGT: --"; 
    
    [Header("Visuals & World")]
    [Tooltip("The 2D sprite used for the inventory grid icon.")]
    public Sprite icon; 
    [Tooltip("The 3D/2D physics prefab spawned when the item is ejected from the inventory.")]
    public GameObject worldPrefab; 

    [Header("Inspector UI Content")]
    [TextArea(3, 6)] 
    [Tooltip("The main flavor text or mechanical description for the UI inspector.")]
    public string description = "Awaiting I/O..."; 

    [Header("Custom Footprint Matrix")]
    [Tooltip("Defines the exact shape of this item within the inventory grid cells.")]
    public ItemFootprint footprint; 
    
    [Header("Runtime Memory")]
    [Tooltip("Runtime memory to remember if this specific item instance is rotated.")]
    public bool isRotated = false; 

    void OnEnable()
    {
        isRotated = false;

        if (footprint == null || footprint.cells == null || footprint.cells.Length == 0)
        {
            footprint = new ItemFootprint(1, 1);
        }
    }

    /// <summary>
    /// Safely returns the footprint for this item, generating a default 1x1 if missing.
    /// </summary>
    public ItemFootprint GetFootprint()
    {
        if (footprint == null || footprint.cells == null || footprint.cells.Length == 0)
        {
            footprint = new ItemFootprint(1, 1);
        }
        return footprint;
    }
}
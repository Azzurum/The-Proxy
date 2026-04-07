using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InventoryState", menuName = "Inventory/InventoryState")]
public class InventoryState : ScriptableObject
{
    [Header("Grid Dimensions")]
    public int gridWidth = 5; // For visors (5x10)
    public int gridHeight = 10;
    public int extGridWidth = 5; // For external nodes (5x5)
    public int extGridHeight = 5;

    [Header("Slot Data")]
    public List<ItemData> mainGridSlots = new List<ItemData>(); // Index 0-49 for main visors
    public List<ItemData> extGridSlots = new List<ItemData>(); // Index 0-24 for external
    public List<ItemData> hotbarSlots = new List<ItemData>(); // 3 slots

    // Initialize with empty slots if needed
    public void Initialize()
    {
        mainGridSlots = new List<ItemData>(new ItemData[gridWidth * gridHeight]);
        extGridSlots = new List<ItemData>(new ItemData[extGridWidth * extGridHeight]);
        hotbarSlots = new List<ItemData>(new ItemData[3]);
    }
}
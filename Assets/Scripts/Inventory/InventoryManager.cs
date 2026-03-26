using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("Grid Configuration")]
    public int gridWidth = 10; // 10 columns 
    public int gridHeight = 10; // Row Index 0 to 9, where 9 is the top 

    [Header("Active Data")]
    // This list tracks every item currently materialized in Kaelen's M.E.T. Rig
    [cite_start] public List<InventoryItem> activeItems = new List<InventoryItem>(); [cite: 127]

    // We will build the functions for ResolveCorruptionTick and ExecuteClean here next.
}
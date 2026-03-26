using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("Grid Configuration")]
    public int gridWidth = 10; // 10 columns 
    public int gridHeight = 10; // Row Index 0 to 9, where 9 is the top 
    [Header("Visual UI Setup")]
    public GameObject slotPrefab;
    public Transform gridContainer;

    [Header("Active Data")]
    // This list tracks every item currently materialized in Kaelen's M.E.T. Rig
    public List<InventoryItem> activeItems = new List<InventoryItem>();

    void Start()
    {
        GenerateVisualGrid();
    }

    private void GenerateVisualGrid()
    {
        int totalSlots = gridWidth * gridHeight;
        for (int i = 0; i < totalSlots; i++)
        {
            Instantiate(slotPrefab, gridContainer);
        }
    }

    public void ResolveCorruptionTick()
    {
        // 1. Shift all items up one Row Index
        foreach (var item in activeItems)
        {
            item.position.y += 1; // Shifts item up by 1 row
        }

        // 2. Resolve Collisions with Top Boundary (Row Index > 9)
        // We loop backwards when removing items from a list to avoid index errors
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            var item = activeItems[i];
            int itemTopEdge = item.position.y + item.size.y - 1;

            if (itemTopEdge > 9)
            {
                if (item.isLocked)
                {
                    // System Crush Scenario: Item halts at Row 9
                    item.position.y = 10 - item.size.y;
                    EscalateCrushPenaltyTimer();
                }
                else if (item.isQuestItem)
                {
                    // Master Keys respawn
                    RespawnItemAtOriginalSpawn(item);
                    activeItems.RemoveAt(i);
                }
                else
                {
                    // Standard items are ejected
                    EjectItemToWorld(item);
                    activeItems.RemoveAt(i);
                }
            }
        }

        // 3. Spawn the new corruption row at the bottom
        SpawnCorruptionAtRowZero();
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
        Debug.Log("New corruption row spawned at Index 0.");
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
}
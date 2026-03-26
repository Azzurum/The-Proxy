using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("Grid Configuration")]
    public int gridWidth = 10; // 10 columns 
    public int gridHeight = 10; // Row Index 0 to 9, where 9 is the top 

    [Header("Active Data")]
    // This list tracks every item currently materialized in Kaelen's M.E.T. Rig
    public List<InventoryItem> activeItems = new List<InventoryItem>();

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
}
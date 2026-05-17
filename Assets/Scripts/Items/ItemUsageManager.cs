using UnityEngine;

public class ItemUsageManager : MonoBehaviour
{
    [Header("References")]
    public InventoryManager inventoryManager;
    public GameObject physicalDecoyPrefab; // Drag your new Decoy Prefab here

    // Kaelen's top-down facing direction (update this from your movement script)
    public Vector2 playerFacingDirection = Vector2.down; 

    public void ExecuteItem(ItemData item, GameObject uiItemReference)
    {
        if (item == null) return;

        switch (item.itemID)
        {
            case "CONS-HEAT":
                UseEmergencyHeatSink(uiItemReference);
                break;

            case "TOOL-DECOY":
                PlantDecoy(uiItemReference);
                break;

            case "STUN-ARC":
            case "WEP-REPULSE":
                Debug.LogWarning("WEAPON: You must assign this to a Hotbar slot (1, 2, 3) to aim and fire it!");
                break;

            case "TOOL-WELD":
                Debug.Log("FUSION WELDER: Approach a sealed bulkhead and hold [E] to cut through.");
                break;

            case "KEY-MSTR":
                Debug.Log("MASTER KEY: Non-destructible. Must be used directly at a Security Terminal.");
                break;
        }
    }

    private void UseEmergencyHeatSink(GameObject uiItemReference)
    {
        Debug.Log("HEAT SINK USED: Venting M.E.T. Rig temperatures...");
        // Call your Emergency Clean logic here to purge corruption rows
        inventoryManager.ExecuteCleanProtocol(); 

        DestroyConsumable(uiItemReference);
    }

    private void PlantDecoy(GameObject uiItemReference)
    {
        Debug.Log("DECOY DEPLOYED: Priming 7-second fuse...");
        
        if (physicalDecoyPrefab != null)
        {
            // Spawn the decoy slightly in front of Kaelen
            Vector3 spawnPos = transform.position + (Vector3)(playerFacingDirection * 1.5f);
            Instantiate(physicalDecoyPrefab, spawnPos, Quaternion.identity);
        }

        DestroyConsumable(uiItemReference);
    }

    private void DestroyConsumable(GameObject uiItemReference)
    {
        // 1. Destroy the physical UI block from the grid
        Destroy(uiItemReference);

        // 2. Tell the InventoryManager to rescan the grid. 
        // It will see the item is missing and automatically clear the memory!
        inventoryManager.SyncDataFromUI();
    }
}
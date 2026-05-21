using UnityEngine;

/// <summary>
/// Handles physical ejection and scattering of inventory items back into the world environment.
/// </summary>
public class WorldItemSpawner : MonoBehaviour
{
    public static WorldItemSpawner Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    /// <summary>
    /// Spawns the physical world prefab representation of an item, removing it from UI data.
    /// </summary>
    public void DiscardItemToWorld(GameObject itemUI)
    {
        DraggableItem dragItem = itemUI.GetComponent<DraggableItem>();
        
        if (dragItem != null && dragItem.itemData != null)
        {
            if (dragItem.itemData.worldPrefab == null)
            {
                Debug.LogWarning($"<color=red>CANNOT DROP ITEM:</color> '{dragItem.itemData.itemName}' does not have a World Prefab assigned in its ItemData! The item has been deleted.");
            }
            else
            {
                Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
                Vector3 basePos = player != null ? player.position : Vector3.zero;
                Vector3 spawnPos = GetScatterPosition(basePos);
                
                GameObject dropped = Instantiate(dragItem.itemData.worldPrefab, basePos, Quaternion.identity);
                PhysicalItem pi = dropped.GetComponent<PhysicalItem>();
                if (pi == null) pi = dropped.GetComponentInChildren<PhysicalItem>();
                if (pi != null) 
                {
                    pi.itemData = dragItem.itemData;
                    pi.TriggerDropAnimation(basePos, spawnPos);
                }

                if (UIPickupLog.Instance != null) UIPickupLog.Instance.AddLog(dragItem.itemData.itemName ?? dragItem.itemData.itemID, Color.gray, "Dropped");
            }
        }

        Destroy(itemUI);
        if (InventoryManager.Instance != null) InventoryManager.Instance.SyncDataFromUI();
        if (InventorySaveHandler.Instance != null) InventorySaveHandler.Instance.SaveWorldItems();
    }

    /// <summary>
    /// Safely creates a new physical item near the player based on supplied item data.
    /// </summary>
    public void EjectItem(ItemData itemData)
    {
        if (itemData != null && InventoryManager.Instance != null && itemData != InventoryManager.Instance.corruptionData)
        {
            if (itemData.worldPrefab != null)
            {
                Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
                Vector3 startPos = player != null ? player.position : Vector3.zero;
                Vector3 spawnPos = GetScatterPosition(startPos);
                
                GameObject ejected = Instantiate(itemData.worldPrefab, startPos, Quaternion.identity);
                PhysicalItem pi = ejected.GetComponent<PhysicalItem>();
                if (pi == null) pi = ejected.GetComponentInChildren<PhysicalItem>();
                if (pi != null) 
                {
                    pi.itemData = itemData;
                    pi.TriggerDropAnimation(startPos, spawnPos);
                }

                if (UIPickupLog.Instance != null) UIPickupLog.Instance.AddLog(itemData.itemName ?? itemData.itemID, new Color(1f, 0.4f, 0f), "Ejected");
            }
        }
    }

    /// <summary>
    /// Calculates a valid, collision-free drop position near the provided center point.
    /// </summary>
    public Vector3 GetScatterPosition(Vector3 center)
    {
        float dropRadius = 1.2f;
        float itemSize = 0.3f;
        
        ContactFilter2D filter = ContactFilter2D.noFilter;
        Collider2D[] hits = new Collider2D[10];

        for (int i = 0; i < 15; i++)
        {
            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * dropRadius;
            Vector3 testPos = center + (Vector3)randomOffset;
            
            int hitCount = Physics2D.OverlapCircle(testPos, itemSize, filter, hits);
            bool isSpaceFree = true;

            for (int j = 0; j < hitCount; j++)
            {
                Collider2D hit = hits[j];
                if (hit.CompareTag("Interactable") || hit.CompareTag("MasterKey")) { isSpaceFree = false; break; }
                if (!hit.isTrigger && !hit.CompareTag("Player")) { isSpaceFree = false; break; }
            }

            if (isSpaceFree) return testPos;
        }
        return center + (Vector3)(UnityEngine.Random.insideUnitCircle * 0.2f);
    }
}
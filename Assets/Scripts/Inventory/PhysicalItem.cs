using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PhysicalItem : MonoBehaviour
{
    [Header("Item Definition")]
    [Tooltip("Drag the matching ItemData ScriptableObject here")]
    public ItemData itemData;

    // Call this method from whatever script handles Kaelen's interaction (e.g., when you press 'E')
    public void InteractToPickup()
    {
        InventoryManager manager = FindAnyObjectByType<InventoryManager>();
        
        if (manager != null && itemData != null)
        {
            // Try to shove it into the External Storage
            bool pickupSuccessful = manager.TryPickupItem(itemData);

            if (pickupSuccessful)
            {
                // Destroy the physical object in the world if successfully picked up
                Destroy(gameObject);
            }
            else
            {
                // Optional: Play a "buzzer" sound or show UI text saying "Inventory Full!"
                Debug.Log("Cannot pick up " + itemData.itemName + " - No space in M.E.T. Rig!");
            }
        }
    }
}
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRadius = 1.5f; // Radius for picking up items or using the generator

    [Header("Inventory Data")]
    public ItemData batteryData;   // NEW: We pass the raw data now!
    public ItemData masterKeyData; // NEW: We pass the raw data now!

    void Update()
    {
        // Pick up items or use generator
        if (Input.GetKeyDown(KeyCode.E))
        {
            AttemptPickup();
        }
    }

    private void AttemptPickup()
    {
        Collider2D[] nearbyObjects = Physics2D.OverlapCircleAll(transform.position, interactRadius);

        foreach (var obj in nearbyObjects)
        {
            InventoryManager manager = FindAnyObjectByType<InventoryManager>();
            if (manager == null) return;

            // SCENARIO A: Pick up a Battery
            if (obj.CompareTag("Interactable"))
            {
                PhysicalItem pi = obj.GetComponent<PhysicalItem>();
                ItemData itemToPickup = (pi != null && pi.itemData != null) ? pi.itemData : batteryData;

                if (manager.TryPickupItem(itemToPickup))
                {
                    Debug.Log($"<color=yellow>[DEBUG]</color> Picked up {itemToPickup.itemName ?? itemToPickup.itemID}.");
                    Destroy(obj.gameObject);
                    return;
                }
                else
                {
                    Debug.Log($"<color=yellow>[DEBUG]</color> Failed to pick up {itemToPickup.itemName ?? itemToPickup.itemID}. External inventory full or missing space.");
                }
            }
            // SCENARIO B: Pick up a Master Key
            else if (obj.CompareTag("MasterKey"))
            {
                PhysicalItem pi = obj.GetComponent<PhysicalItem>();
                ItemData itemToPickup = (pi != null && pi.itemData != null) ? pi.itemData : masterKeyData;

                if (manager.TryPickupItem(itemToPickup))
                {
                    Debug.Log($"<color=magenta>MASTER KEY ACQUIRED:</color> Fits perfectly. [DEBUG] {itemToPickup.itemID}");
                    Destroy(obj.gameObject);
                    return;
                }
                else
                {
                    Debug.Log($"<color=yellow>[DEBUG]</color> Failed to pick up Master Key. External inventory full or missing space.");
                }
            }
            // SCENARIO C: The Generator Win Condition
            else if (obj.CompareTag("Generator"))
            {
                if (manager.TryConsumeBatteries(3))
                {
                    TriggerVictory();
                    return;
                }
            }
        }
    }

    private void TriggerVictory()
    {
        Debug.Log("MISSION ACCOMPLISHED: The Proxy has been neutralized!");

        // Find the hidden Victory UI Canvas
        GameObject victoryScreen = GameObject.Find("Canvas_Victory");
        if (victoryScreen == null)
        {
            // Fallback search if the canvas is turned off
            victoryScreen = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include)
                            .gameObject.transform.Find("Canvas_Victory")?.gameObject;
        }

        // Show the screen and freeze time
        if (victoryScreen != null)
        {
            victoryScreen.SetActive(true);
        }
        Time.timeScale = 0f;
    }

    // Draws a visible red circle in the Scene view to show Kaelen's interaction reach
    private void OnDrawGizmosSelected()
    {
        // Red circle for interact radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
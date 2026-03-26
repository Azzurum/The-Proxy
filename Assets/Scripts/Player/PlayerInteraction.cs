using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRadius = 1.5f; // How close Kaelen needs to be to the item

    void Update()
    {
        // X-RAY 1: Did Unity even register the button press?
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("<color=yellow>X-RAY 1: 'E' key was pressed!</color>");
            AttemptPickup();
        }
    }

    private void AttemptPickup()
    {
        Collider2D[] nearbyObjects = Physics2D.OverlapCircleAll(transform.position, interactRadius);

        // X-RAY 2: Did the physics circle actually touch anything at all?
        Debug.Log($"<color=orange>X-RAY 2: Circle cast complete. Found {nearbyObjects.Length} objects in radius.</color>");

        foreach (var obj in nearbyObjects)
        {
            // X-RAY 3: What EXACTLY did the circle touch?
            Debug.Log($"<color=cyan>X-RAY 3: Touched object named '{obj.name}'. Its tag is exactly: '{obj.tag}'</color>");

            if (obj.CompareTag("Interactable"))
            {
                InventoryManager manager = FindFirstObjectByType<InventoryManager>();
                if (manager != null)
                {
                    if (manager.TryPickupBattery())
                    {
                        Destroy(obj.gameObject);
                        Debug.Log("<color=green>X-RAY 4: Battery successfully vacuumed up!</color>");
                        return;
                    }
                }
            }
        }
    }

    // Optional: This draws a visible red circle in your Scene view so you can see Kaelen's reach!
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRadius = 1.5f; // Radius for picking up items or using the generator

    [Header("Combat Settings")]
    public float stunnerRange = 5f; // Max reach of the ARC-Pulse stunner

    // NEW REPULSOR SETTINGS
    public float repulsorRange = 3f; // You have to let it get dangerously close!
    public float repulsorForce = 7f; // Pushes it back 7 units (Middle of GDD's 6-8 rule)
    public float repulsorCooldown = 10f;
    private float currentRepulsorCooldown = 0f;

    [Header("Inventory Prefabs")]
    public GameObject uiBatteryPrefab;
    public GameObject uiMasterKeyPrefab;

    void Update()
    {
        // TICK DOWN THE COOLDOWN TIMER
        if (currentRepulsorCooldown > 0)
        {
            currentRepulsorCooldown -= Time.deltaTime;
        }

        // Pick up items or use generator
        if (Input.GetKeyDown(KeyCode.E))
        {
            AttemptPickup();
        }

        // Fire Stunner
        if (Input.GetKeyDown(KeyCode.F))
        {
            FireStunner();
        }

        // FIRE THE K-80 REPULSOR
        if (Input.GetKeyDown(KeyCode.R))
        {
            FireRepulsor();
        }
    }

    private void FireRepulsor()
    {
        // 1. Check if the weapon is ready
        if (currentRepulsorCooldown > 0)
        {
            Debug.LogWarning($"<color=red>K-80 REPULSOR NOT READY:</color> Recharging... ({Mathf.Ceil(currentRepulsorCooldown)}s remaining)");
            return;
        }

        // 2. Find the monster and check the distance
        ProxyAI proxy = FindFirstObjectByType<ProxyAI>();
        if (proxy != null)
        {
            Debug.Log("<color=green>K-80 REPULSOR FIRED!</color> Massive pneumatic blast emitted.");
            currentRepulsorCooldown = repulsorCooldown; // Start 10s cooldown

            // COMBAT AWARENESS: The blast reveals Kaelen's location!
            proxy.OnCombatAction(transform.position);

            float distance = Vector2.Distance(transform.position, proxy.transform.position);

            // The Repulsor has a shorter range than the Stunner!
            if (distance <= repulsorRange)
            {
                proxy.ApplyRepulsor(transform.position, repulsorForce);
            }
            else
            {
                Debug.Log("<color=yellow>K-80 MISS:</color> The Proxy was too far away!");
            }
        }
    }

    private void AttemptPickup()
    {
        Collider2D[] nearbyObjects = Physics2D.OverlapCircleAll(transform.position, interactRadius);

        foreach (var obj in nearbyObjects)
        {
            InventoryManager manager = FindFirstObjectByType<InventoryManager>();
            if (manager == null) return;

            // SCENARIO A: Pick up a Battery (Size: 1x2)
            if (obj.CompareTag("Interactable"))
            {
                if (manager.TryPickupItem(uiBatteryPrefab, 1, 2, false))
                {
                    Destroy(obj.gameObject);
                    return;
                }
            }
            // SCENARIO B: Pick up a Master Key (Size: 3x3)
            else if (obj.CompareTag("MasterKey"))
            {
                if (manager.TryPickupItem(uiMasterKeyPrefab, 3, 3, true))
                {
                    Debug.Log("<color=magenta>MASTER KEY ACQUIRED:</color> 9 slots consumed.");
                    Destroy(obj.gameObject);
                    return;
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
            victoryScreen = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include)
                            .gameObject.transform.Find("Canvas_Victory")?.gameObject;
        }

        // Show the screen and freeze time
        if (victoryScreen != null)
        {
            victoryScreen.SetActive(true);
        }
        Time.timeScale = 0f;
    }

    private void FireStunner()
    {
        InventoryManager manager = FindFirstObjectByType<InventoryManager>();
        if (manager == null) return;

        // 1. Consume 1 battery to power the weapon
        if (manager.TryConsumeBatteries(1))
        {
            Debug.Log("ARC-PULSE FIRED! 1 Battery consumed.");

            // 2. Check if the Proxy is close enough
            ProxyAI proxy = FindFirstObjectByType<ProxyAI>();
            if (proxy != null)
            {
                // COMBAT AWARENESS: The shot reveals Kaelen's location!
                proxy.OnCombatAction(transform.position);

                float distance = Vector2.Distance(transform.position, proxy.transform.position);
                if (distance <= stunnerRange)
                {
                    // 3. Hit! Stun the monster
                    proxy.ApplyStun();
                }
                else
                {
                    Debug.Log("ARC-PULSE MISSED: Proxy is out of range!");
                }
            }
        }
        else
        {
            Debug.LogWarning("WEAPON FAILED: No Batteries in M.E.T. Rig!");
        }
    }

    // Draws a visible red circle in the Scene view to show Kaelen's interaction reach
    private void OnDrawGizmosSelected()
    {
        // Red circle for interact radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, interactRadius);

        // Blue circle for Repulsor range
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, repulsorRange);
    }
}
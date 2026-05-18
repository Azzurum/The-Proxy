using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRadius = 1.5f; // Radius for picking up items or using the generator

    [Header("UI Prompts")]
    public FloatingPrompt interactionPrompt; 
    public Vector3 promptOffset = new Vector3(0f, 1f, 0f); // How high above the item it floats
    private GameObject closestInteractable = null;

    [Header("Audio SFX")]
    public AudioSource audioSource;
    public AudioClip sfxPickup;
    public AudioClip sfxInventoryFull;
    public AudioClip sfxLockerOpen;

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        // AUTO-WIRING: Find the floating interaction prompt UI
        if (interactionPrompt == null)
        {
            interactionPrompt = FindAnyObjectByType<FloatingPrompt>(FindObjectsInactive.Include);
        }
    }

    void Update()
    {
        FindClosestInteractable();

        // Pick up items or use generator
        if (Input.GetKeyDown(KeyCode.E))
        {
            AttemptPickup();
        }
    }

    private void FindClosestInteractable()
    {
        Collider2D[] nearbyObjects = Physics2D.OverlapCircleAll(transform.position, interactRadius);
        GameObject closest = null;
        float minDistance = float.MaxValue;

        // Find the absolute closest item
        foreach (var obj in nearbyObjects)
        {
            if (obj.CompareTag("Interactable") || obj.CompareTag("MasterKey") || obj.CompareTag("Generator") || obj.CompareTag("Locker") || obj.CompareTag("LockedDoor"))
            {
                float dist = Vector2.Distance(transform.position, obj.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = obj.gameObject;
                }
            }
        }
        
        // If the closest item changes (or we walked away from it)
        if (closest != closestInteractable)
        {
            closestInteractable = closest;
            
            if (closestInteractable != null && interactionPrompt != null)
            {
                Color promptColor = Color.white;

                // Check if it's an item and if we have space for it
                if (closestInteractable.CompareTag("Interactable") || closestInteractable.CompareTag("MasterKey"))
                {
                    PhysicalItem pi = closestInteractable.GetComponent<PhysicalItem>();
                    if (pi == null) pi = closestInteractable.GetComponentInParent<PhysicalItem>();
                    
                    if (pi != null && pi.itemData != null)
                    {
                        InventoryManager manager = FindAnyObjectByType<InventoryManager>();
                        if (manager != null && !manager.CanFitItemToExternalTray(pi.itemData))
                        {
                            promptColor = Color.red; // Color it red if the inventory buffer is full!
                        }
                    }
                }
                else if (closestInteractable.CompareTag("LockedDoor"))
                {
                    // Always show locked doors as white, the feedback will be red text if failed.
                    promptColor = Color.white;
                }

                interactionPrompt.SetPromptColor(promptColor);

                // Lock the prompt to the new item and fade it in
                interactionPrompt.SetDynamicTarget(closestInteractable.transform, promptOffset);
                interactionPrompt.ShowPrompt();
            }
            else if (interactionPrompt != null)
            {
                // Walked away from everything, fade it out
                interactionPrompt.HidePrompt();
            }
        }
    }

    private void AttemptPickup()
    {
        if (closestInteractable == null) return; // Nothing to interact with.

        InventoryManager manager = FindAnyObjectByType<InventoryManager>();
        if (manager == null) return;

        GameObject obj = closestInteractable;

        // SCENARIO A: Pick up a Battery
        if (obj.CompareTag("Interactable"))
        {
            PhysicalItem pi = obj.GetComponent<PhysicalItem>();
            if (pi == null) pi = obj.GetComponentInParent<PhysicalItem>();
            
            if (pi == null || pi.itemData == null)
            {
                Debug.LogError($"<color=red>[ERROR]</color> The object '{obj.name}' is tagged 'Interactable' but is missing the PhysicalItem script or its ItemData is empty! Cannot pick up.");
                return;
            }

            if (manager.TryPickupItem(pi.itemData))
            {
                if (audioSource != null) audioSource.PlayOneShot(sfxPickup != null ? sfxPickup : ProceduralAudioGen.GenerateAscendingChime());
                Debug.Log($"<color=yellow>[DEBUG]</color> Picked up {pi.itemData.itemName ?? pi.itemData.itemID}.");
                
                if (UIPickupLog.Instance != null) UIPickupLog.Instance.AddLog(pi.itemData.itemName ?? pi.itemData.itemID, Color.yellow);

                if (interactionPrompt != null) interactionPrompt.HidePrompt();
                closestInteractable = null; // Reset the tracker
                
                manager.SaveWorldItems(pi.gameObject); // Save the room and tell it to ignore this object!
                Destroy(pi.gameObject); // Make sure we destroy the root item object!
            }
            else
            {
                if (audioSource != null) audioSource.PlayOneShot(sfxInventoryFull != null ? sfxInventoryFull : ProceduralAudioGen.GenerateErrorBuzz());
                Debug.Log($"<color=yellow>[DEBUG]</color> Failed to pick up {pi.itemData.itemName ?? pi.itemData.itemID}. External inventory full or missing space.");
            }
        }
        // SCENARIO B: Pick up a Master Key
        else if (obj.CompareTag("MasterKey"))
        {
            PhysicalItem pi = obj.GetComponent<PhysicalItem>();
            if (pi == null) pi = obj.GetComponentInParent<PhysicalItem>();
            
            if (pi == null || pi.itemData == null)
            {
                Debug.LogError($"<color=red>[ERROR]</color> The object '{obj.name}' is tagged 'MasterKey' but is missing the PhysicalItem script or its ItemData is empty! Cannot pick up.");
                return;
            }

            if (manager.TryPickupItem(pi.itemData))
            {
                if (audioSource != null) audioSource.PlayOneShot(sfxPickup != null ? sfxPickup : ProceduralAudioGen.GenerateAscendingChime());
                Debug.Log($"<color=magenta>MASTER KEY ACQUIRED:</color> Fits perfectly. [DEBUG] {pi.itemData.itemID}");
                
                if (UIPickupLog.Instance != null) UIPickupLog.Instance.AddLog(pi.itemData.itemName ?? pi.itemData.itemID, Color.magenta);

                if (interactionPrompt != null) interactionPrompt.HidePrompt();
                closestInteractable = null; // Reset the tracker
                
                manager.SaveWorldItems(pi.gameObject); // Save the room and tell it to ignore this object!
                Destroy(pi.gameObject); // Make sure we destroy the root item object!
            }
            else
            {
                if (audioSource != null) audioSource.PlayOneShot(sfxInventoryFull != null ? sfxInventoryFull : ProceduralAudioGen.GenerateErrorBuzz());
                Debug.Log($"<color=yellow>[DEBUG]</color> Failed to pick up Master Key. External inventory full or missing space.");
            }
        }
        // SCENARIO C: The Generator Win Condition
        else if (obj.CompareTag("Generator"))
        {
            if (manager.TryConsumeBatteries(3))
            {
                TriggerVictory();
            }
        }
        // SCENARIO D: The Physical Locker Storage
        else if (obj.CompareTag("Locker"))
        {
            // 1. Play the locker door opening animation
            if (audioSource != null) audioSource.PlayOneShot(sfxLockerOpen != null ? sfxLockerOpen : ProceduralAudioGen.GenerateClick(300f, 0.3f));

            Animator anim = obj.GetComponent<Animator>();
            if (anim != null) anim.SetTrigger("OpenLocker");

            // 2. Open the Player's M.E.T. Rig
            MetRigManager rigManager = FindAnyObjectByType<MetRigManager>();
            if (rigManager != null && !rigManager.isRigOpen)
            {
                rigManager.OpenRig(); 
            }

            // 3. Connect the Locker's memory to the UI!
            LockerStorage locker = obj.GetComponent<LockerStorage>();
            if (locker != null)
            {
                manager.OpenLocker(locker);
            }
            else
            {
                Debug.LogWarning("This Locker is missing a LockerStorage script!");
            }
        }
        // SCENARIO E: A Locked Door
        else if (obj.CompareTag("LockedDoor"))
        {
            LockedDoor door = obj.GetComponent<LockedDoor>();
            if (door == null) door = obj.GetComponentInParent<LockedDoor>();

            if (door != null)
            {
                door.AttemptUnlock();
            }
            else
            {
                Debug.LogError($"<color=red>[ERROR]</color> The object '{obj.name}' is tagged 'LockedDoor' but is missing the LockedDoor.cs script!");
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
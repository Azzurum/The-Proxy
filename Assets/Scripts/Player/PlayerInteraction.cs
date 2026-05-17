using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRadius = 1.5f; // Radius for picking up items or using the generator

    [Header("Inventory Data")]
    public ItemData batteryData;   // NEW: We pass the raw data now!
    public ItemData masterKeyData; // NEW: We pass the raw data now!

    [Header("Audio SFX")]
    public AudioSource audioSource;
    public AudioClip sfxPickup;
    public AudioClip sfxInventoryFull;
    public AudioClip sfxLockerOpen;

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

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
                if (pi == null) pi = obj.GetComponentInParent<PhysicalItem>();
                
                ItemData itemToPickup = (pi != null && pi.itemData != null) ? pi.itemData : batteryData;

                if (manager.TryPickupItem(itemToPickup))
                {
                    if (audioSource != null) audioSource.PlayOneShot(sfxPickup != null ? sfxPickup : ProceduralAudioGen.GenerateAscendingChime());
                    Debug.Log($"<color=yellow>[DEBUG]</color> Picked up {itemToPickup.itemName ?? itemToPickup.itemID}.");
                    Destroy(obj.gameObject);
                    return;
                }
                else
                {
                    if (audioSource != null) audioSource.PlayOneShot(sfxInventoryFull != null ? sfxInventoryFull : ProceduralAudioGen.GenerateErrorBuzz());
                    Debug.Log($"<color=yellow>[DEBUG]</color> Failed to pick up {itemToPickup.itemName ?? itemToPickup.itemID}. External inventory full or missing space.");
                }
            }
            // SCENARIO B: Pick up a Master Key
            else if (obj.CompareTag("MasterKey"))
            {
                PhysicalItem pi = obj.GetComponent<PhysicalItem>();
                if (pi == null) pi = obj.GetComponentInParent<PhysicalItem>();
                
                ItemData itemToPickup = (pi != null && pi.itemData != null) ? pi.itemData : masterKeyData;

                if (manager.TryPickupItem(itemToPickup))
                {
                    if (audioSource != null) audioSource.PlayOneShot(sfxPickup != null ? sfxPickup : ProceduralAudioGen.GenerateAscendingChime());
                    Debug.Log($"<color=magenta>MASTER KEY ACQUIRED:</color> Fits perfectly. [DEBUG] {itemToPickup.itemID}");
                    Destroy(obj.gameObject);
                    return;
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
                    return;
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
                return;
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
using UnityEngine;

public class ItemUsageManager : MonoBehaviour
{
    [Header("References")]
    public InventoryManager inventoryManager;
    public GameObject physicalDecoyPrefab; // Drag your new Decoy Prefab here

    [Header("Audio SFX")]
    public AudioSource audioSource;
    public AudioClip sfxUseHeatSink;
    public AudioClip sfxError;

    private Vector2 lastFacingDirection = Vector2.down;

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (inventoryManager == null) inventoryManager = FindAnyObjectByType<InventoryManager>();
    }

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
                if (audioSource != null) audioSource.PlayOneShot(sfxError != null ? sfxError : ProceduralAudioGen.GenerateErrorBuzz());
                Debug.LogWarning("WEAPON: You must assign this to a Hotbar slot (1, 2, 3) to aim and fire it!");
                break;

            case "TOOL-WELD":
                if (audioSource != null) audioSource.PlayOneShot(sfxError != null ? sfxError : ProceduralAudioGen.GenerateErrorBuzz());
                Debug.Log("FUSION WELDER: Approach a sealed bulkhead and hold [E] to cut through.");
                break;

            case "KEY-MSTR":
                if (audioSource != null) audioSource.PlayOneShot(sfxError != null ? sfxError : ProceduralAudioGen.GenerateErrorBuzz());
                Debug.Log("MASTER KEY: Non-destructible. Must be used directly at a Security Terminal.");
                break;
        }
    }

    private void UseEmergencyHeatSink(GameObject uiItemReference)
    {
        if (audioSource != null) audioSource.PlayOneShot(sfxUseHeatSink != null ? sfxUseHeatSink : ProceduralAudioGen.GenerateHiss(2f));
        Debug.Log("HEAT SINK USED: Venting M.E.T. Rig temperatures...");
        // Call your Emergency Clean logic here to purge corruption rows
        inventoryManager.ExecuteCleanProtocol(); 

        DestroyConsumable(uiItemReference);
    }

    private void PlantDecoy(GameObject uiItemReference)
    {
        Debug.Log("DECOY DEPLOYED: Priming 7-second fuse...");

        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null && pc.animator != null)
        {
            float x = pc.animator.GetFloat("Horizontal");
            float y = pc.animator.GetFloat("Vertical");
            if (x != 0 || y != 0) lastFacingDirection = new Vector2(x, y).normalized;
        }
        
        if (physicalDecoyPrefab != null)
        {
            // Spawn the decoy slightly in front of Kaelen
            Vector3 spawnPos = transform.position + (Vector3)(lastFacingDirection * 1.5f);
            Instantiate(physicalDecoyPrefab, spawnPos, Quaternion.identity);
        }

        DestroyConsumable(uiItemReference);
    }

    private void DestroyConsumable(GameObject uiItemReference)
    {
        // Detach from parent so it is immediately removed from the grid hierarchy
        if (uiItemReference != null) uiItemReference.transform.SetParent(null);

        // 1. Destroy the physical UI block from the grid
        Destroy(uiItemReference);

        // 2. Tell the InventoryManager to rescan the grid. 
        // It will see the item is missing and automatically clear the memory!
        inventoryManager.SyncDataFromUI();
    }
}
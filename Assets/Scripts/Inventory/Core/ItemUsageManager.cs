using UnityEngine;

/// <summary>
/// Handles the execution of consumable and equippable items from the inventory.
/// </summary>
public class ItemUsageManager : MonoBehaviour
{
    public static ItemUsageManager Instance { get; private set; }

    [Header("References")]
    [Tooltip("The prefab spawned when the player deploys a decoy device.")]
    public GameObject physicalDecoyPrefab;

    [Header("Audio SFX")]
    [Tooltip("Audio source for playing item usage sounds.")]
    public AudioSource audioSource;
    [Tooltip("Sound played when the emergency heat sink is used.")]
    public AudioClip sfxUseHeatSink;
    [Tooltip("Sound played when an item cannot be used.")]
    public AudioClip sfxError;

    private Vector2 _lastFacingDirection = Vector2.down;
    private PlayerController _playerController;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        _playerController = FindAnyObjectByType<PlayerController>();
    }

    /// <summary>
    /// Evaluates and executes the logic associated with a specific item's ID.
    /// </summary>
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
                PlayErrorSound();
                break;

            case "TOOL-WELD":
                PlayErrorSound();
                break;

            case "KEY-MSTR":
                PlayErrorSound();
                break;
        }
    }

    private void UseEmergencyHeatSink(GameObject uiItemReference)
    {
        if (audioSource != null) audioSource.PlayOneShot(sfxUseHeatSink != null ? sfxUseHeatSink : ProceduralAudioGen.GenerateHiss(2f));
        
        if (InventoryManager.Instance != null) InventoryManager.Instance.ExecuteCleanProtocol(); 

        DestroyConsumable(uiItemReference);
    }

    private void PlantDecoy(GameObject uiItemReference)
    {
        if (_playerController != null && _playerController.animator != null)
        {
            float x = _playerController.animator.GetFloat("Horizontal");
            float y = _playerController.animator.GetFloat("Vertical");
            if (Mathf.Abs(x) > 0.01f || Mathf.Abs(y) > 0.01f) 
            {
                _lastFacingDirection = new Vector2(x, y).normalized;
            }
        }
        
        if (physicalDecoyPrefab != null)
        {
            Vector3 spawnPos = transform.position + (Vector3)(_lastFacingDirection * 1.5f);
            Instantiate(physicalDecoyPrefab, spawnPos, Quaternion.identity);
        }

        DestroyConsumable(uiItemReference);
    }

    private void DestroyConsumable(GameObject uiItemReference)
    {
        if (uiItemReference != null) uiItemReference.transform.SetParent(null);
        Destroy(uiItemReference);

        if (InventoryManager.Instance != null) InventoryManager.Instance.SyncDataFromUI();
    }

    private void PlayErrorSound()
    {
        if (audioSource != null) audioSource.PlayOneShot(sfxError != null ? sfxError : ProceduralAudioGen.GenerateErrorBuzz());
    }
}
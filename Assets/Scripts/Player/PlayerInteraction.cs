using UnityEngine;

/// <summary>
/// Manages proximity-based player interactions with the environment, items, and terminals.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("The maximum distance from Kaelen that objects can be interacted with.")]
    [SerializeField] private float interactRadius = 1.5f; 

    [Header("UI Prompts")]
    [Tooltip("How high above the target interactable object the UI prompt should float.")]
    [SerializeField] private Vector3 promptOffset = new Vector3(0f, 1f, 0f);
    
    [Tooltip("The floating UI text prompt (e.g., 'Press E') that appears over items.")]
    [SerializeField] private FloatingPrompt interactionPrompt; 

    [Header("Audio SFX")]
    [Tooltip("Played when an item is successfully added to the M.E.T. Rig buffer.")]
    [SerializeField] private AudioClip sfxPickup;
    
    [Tooltip("Played when the player attempts to pick up an item but the buffer is full.")]
    [SerializeField] private AudioClip sfxInventoryFull;
    
    [Tooltip("Played when Kaelen forces open a crew locker.")]
    [SerializeField] private AudioClip sfxLockerOpen;

    private AudioSource _audioSource;
    private GameObject _closestInteractable = null;
    private readonly Collider2D[] _overlapResults = new Collider2D[10];
    private ContactFilter2D _contactFilter;
    
    // Cached component references
    private QuestTracker _questTracker;
    private MetRigManager _metRigManager;
    
    private float _interactScanTimer = 0f;
    private const float INTERACT_SCAN_INTERVAL = 0.1f;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();

        // Use the static property for a non-allocating filter.
        _contactFilter = ContactFilter2D.noFilter;

        if (interactionPrompt == null)
        {
            interactionPrompt = FindAnyObjectByType<FloatingPrompt>(FindObjectsInactive.Include);
            if (interactionPrompt == null) Debug.LogWarning("<color=yellow>[WARNING]</color> No FloatingPrompt found in the scene! The 'E' interact prompt will not appear.");
        }
    }

    private void Update()
    {
        _interactScanTimer -= Time.deltaTime;
        if (_interactScanTimer <= 0f)
        {
            FindClosestInteractable();
            _interactScanTimer = INTERACT_SCAN_INTERVAL;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            AttemptPickup();
        }
    }

    /// <summary>
    /// Scans the immediate vicinity for interactable objects, caches the closest valid target, and manages prompt visibility.
    /// </summary>
    private void FindClosestInteractable()
    {
        // Memory-safe overlap validation loop
        int hitCount = Physics2D.OverlapCircle(transform.position, interactRadius, _contactFilter, _overlapResults);
        
        GameObject closest = null;
        float minDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            GameObject obj = _overlapResults[i].gameObject;

            if (IsInteractable(obj))
            {
                // We check the physical distance from the player to the object's closest point
                float dist = Vector2.Distance(transform.position, _overlapResults[i].ClosestPoint(transform.position));
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = obj;
                }
            }
        }
        
        if (closest != _closestInteractable)
        {
            _closestInteractable = closest;
            UpdateInteractionPrompt();
        }
    }

    /// <summary>
    /// Evaluates the currently targeted interactable and updates the UI prompt's color and position.
    /// </summary>
    private void UpdateInteractionPrompt()
    {
        if (interactionPrompt == null) return;

        if (_closestInteractable != null)
        {
            Color promptColor = Color.white;

            // If it's a physical item, check if the external tray is full and color the prompt red if it is
            PhysicalItem pi = _closestInteractable.GetComponentInParent<PhysicalItem>();
            if (pi != null && pi.itemData != null && InventoryManager.Instance != null)
            {
                if (!InventoryManager.Instance.CanFitItemToExternalTray(pi.itemData))
                {
                    promptColor = Color.red; 
                }
            }

            interactionPrompt.SetPromptColor(promptColor);
            interactionPrompt.SetDynamicTarget(_closestInteractable.transform, promptOffset);
            interactionPrompt.ShowPrompt();
        }
        else
        {
            interactionPrompt.HidePrompt();
        }
    }

    /// <summary>
    /// Determines if an object is interactable via the IInteractable interface or legacy tags.
    /// </summary>
    private bool IsInteractable(GameObject obj)
    {
        // Modern approach: Does it have the interface?
        if (obj.TryGetComponent<IInteractable>(out var interactable) && interactable.CanInteract())
        {
            return true;
        }
        
        // Check parent for interface just in case the collider is on a child object
        if (obj.transform.parent != null && obj.transform.parent.TryGetComponent<IInteractable>(out var parentInteractable) && parentInteractable.CanInteract())
        {
            return true;
        }

        return IsLegacyInteractable(obj) || (obj.transform.parent != null && IsLegacyInteractable(obj.transform.parent.gameObject));
    }

    private bool IsLegacyInteractable(GameObject obj)
    {
        return obj.CompareTag("Interactable") || obj.CompareTag("MasterKey") ||
               obj.CompareTag("Generator") || obj.CompareTag("Locker") || 
               obj.CompareTag("LockedDoor");
    }

    /// <summary>
    /// Routes the interaction logic to the appropriate sub-system based on the targeted object's tag.
    /// </summary>
    private void AttemptPickup()
    {
        if (_closestInteractable == null) return; 

        GameObject obj = _closestInteractable;

        // 1. Try the Modern Interface Approach First
        if (obj.TryGetComponent<IInteractable>(out var interactable))
        {
            interactable.Interact(this.gameObject);
            return;
        }
        else if (obj.transform.parent != null && obj.transform.parent.TryGetComponent<IInteractable>(out var parentInteractable))
        {
            parentInteractable.Interact(this.gameObject);
            return;
        }

        // 2. Fallback to Legacy Tag Routing
        if (obj.CompareTag("Interactable") || obj.CompareTag("MasterKey"))
        {
            HandleItemPickup(obj);
        }
        else if (obj.CompareTag("Generator"))
        {
            HandleGeneratorInteraction();
        }
        else if (obj.CompareTag("Locker"))
        {
            HandleLockerInteraction(obj);
        }
        else if (obj.CompareTag("LockedDoor"))
        {
            HandleDoorInteraction(obj);
        }
    }

    /// <summary>
    /// Attempts to digitize a physical item into the M.E.T. Rig's external buffer.
    /// </summary>
    private void HandleItemPickup(GameObject itemObject)
    {
        if (InventoryManager.Instance == null) return;

        PhysicalItem pi = itemObject.GetComponentInParent<PhysicalItem>();
        if (pi == null || pi.itemData == null) return;

        if (InventoryManager.Instance.TryPickupItem(pi.itemData))
        {
            PlayAudio(sfxPickup, ProceduralAudioGen.GenerateAscendingChime());
            
            if (UIPickupLog.Instance != null) UIPickupLog.Instance.AddLog(pi.itemData.itemName ?? pi.itemData.itemID, itemObject.CompareTag("MasterKey") ? Color.magenta : Color.yellow);

            if (interactionPrompt != null) interactionPrompt.HidePrompt();
            _closestInteractable = null; 
            
            if (InventorySaveHandler.Instance != null) InventorySaveHandler.Instance.SaveWorldItems(pi.gameObject);
            
            Destroy(pi.gameObject);
        }
        else
        {
            PlayAudio(sfxInventoryFull, ProceduralAudioGen.GenerateErrorBuzz());
        }
    }

    /// <summary>
    /// Evaluates if the player meets the requirements to power the generator.
    /// </summary>
    private void HandleGeneratorInteraction()
    {
        if (InventoryManager.Instance != null && InventoryManager.Instance.TryConsumeBatteries(3))
        {
            TriggerVictory();
        }
    }

    /// <summary>
    /// Opens the specified crew locker and links its data to the external inventory grid.
    /// </summary>
    private void HandleLockerInteraction(GameObject lockerObject)
    {
        // Cache tracker reference on first use.
        if (_questTracker == null) _questTracker = FindAnyObjectByType<QuestTracker>();
        if (_questTracker != null && _questTracker.GetCurrentObjective() < 3)
        {
            PlayAudio(sfxInventoryFull, ProceduralAudioGen.GenerateErrorBuzz());
            if (UIPickupLog.Instance != null) UIPickupLog.Instance.AddLog("Authorization Required", Color.red, "LOCKED");
            return;
        }

        // Force the Locker to emit 3D sound from its exact location instead of the player!
        AudioSource lockerAudio = lockerObject.GetComponent<AudioSource>();
        if (lockerAudio == null)
        {
            lockerAudio = lockerObject.AddComponent<AudioSource>();
            lockerAudio.spatialBlend = 1f;
            lockerAudio.rolloffMode = AudioRolloffMode.Linear;
            lockerAudio.minDistance = 2f;
            lockerAudio.maxDistance = 15f;
        }
        lockerAudio.PlayOneShot(sfxLockerOpen != null ? sfxLockerOpen : ProceduralAudioGen.GenerateTrayLatch(true));

        if (lockerObject.TryGetComponent<Animator>(out Animator anim))
        {
            anim.SetTrigger("OpenLocker");
        }

        // Cache rig manager reference on first use.
        if (_metRigManager == null) _metRigManager = FindAnyObjectByType<MetRigManager>();
        if (_metRigManager != null && !_metRigManager.isRigOpen)
        {
            _metRigManager.OpenRig(); 
            if (_questTracker != null) _questTracker.AdvanceObjective(4, "Equip the Fusion Welder in your Hotbar");

            if (_metRigManager.terminalOverlayUI != null)
            {
                Transform tourOverlay = _metRigManager.terminalOverlayUI.transform.Find("MET_Rig_Tour_Overlay");
                if (tourOverlay != null) 
                {
                    tourOverlay.gameObject.SetActive(true);
                    CanvasGroup cg = tourOverlay.GetComponent<CanvasGroup>();
                    if (cg == null) cg = tourOverlay.gameObject.AddComponent<CanvasGroup>();
                    cg.blocksRaycasts = false; // FAILSAFE: Ensure this graphic doesn't block you from dragging items!
                    cg.interactable = false;

                    // BULLETPROOF FIX: Physically strip raycasts from all tutorial graphics so they CANNOT absorb your mouse clicks!
                    foreach (var graphic in tourOverlay.GetComponentsInChildren<UnityEngine.UI.Graphic>(true)) 
                    {
                        graphic.raycastTarget = false;
                    }
                }
            }
        }

        if (lockerObject.TryGetComponent<LockerStorage>(out LockerStorage locker) && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OpenLocker(locker);
        }
    }

    /// <summary>
    /// Prompts the door script to evaluate if the player has the correct credentials.
    /// </summary>
    private void HandleDoorInteraction(GameObject doorObject)
    {
        LockedDoor door = doorObject.GetComponentInParent<LockedDoor>();
        if (door != null) door.AttemptUnlock();
    }

    /// <summary>
    /// Freezes the game state and displays the victory sequence canvas.
    /// </summary>
    private void TriggerVictory()
    {
        GameObject victoryScreen = GameObject.Find("Canvas_Victory");
        if (victoryScreen == null)
        {
            Canvas fallbackCanvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (fallbackCanvas != null)
            {
                Transform vTransform = fallbackCanvas.transform.Find("Canvas_Victory");
                if (vTransform != null) victoryScreen = vTransform.gameObject;
            }
        }

        if (victoryScreen != null) victoryScreen.SetActive(true);
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Plays an audio clip with a fallback to procedural generation if the clip is unassigned.
    /// </summary>
    private void PlayAudio(AudioClip clip, AudioClip proceduralFallback = null)
    {
        if (_audioSource == null) return;
        _audioSource.PlayOneShot(clip != null ? clip : proceduralFallback);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
using UnityEngine;

/// <summary>
/// Manages the state machine for the jammed tutorial door, requiring dialogue completion and the Fusion Welder tool.
/// </summary>
public class JammedDoor : MonoBehaviour
{
    [Header("UI Prompt")]
    [Tooltip("The canvas containing the interaction prompt graphic.")]
    public GameObject interactionPromptCanvas;
    [Tooltip("The UI Image used as a radial fill to show hold progress.")]
    public UnityEngine.UI.Image fillRing;

    [Header("Dialogue Interaction")]
    [Tooltip("Reference to the dialogue engine.")]
    public DialogueEngine dialogueEngine;
    [Tooltip("The dialogue sequence played upon the first failed interaction.")]
    public DialogueNode[] doorDialogueNodes;

    [Header("Hold Settings")]
    [Tooltip("Duration in seconds the interaction key must be held to progress.")]
    public float holdDuration = 1.0f;

    [Header("Visual Effects")]
    [Tooltip("The Particle System prefab representing welding sparks.")]
    public ParticleSystem weldingSparksFX;

    private float _holdTimer = 0f;
    private AudioSource _audioSource;
    private AudioSource _weldSource;
    private bool _isPlayerInZone = false;
    private bool _hasTriggeredMonologue = false;
    private bool _isDoorOpen = false;
    private GameObject _cachedPlayer;
    private QuestTracker _questTracker;
    private HotbarManager _hotbarManager;
    private float _interactionCooldown = 0f;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _questTracker = FindAnyObjectByType<QuestTracker>();
        _hotbarManager = FindAnyObjectByType<HotbarManager>();

        // Generate a dedicated 3D Audio Source exclusively for the welding sparks!
        _weldSource = gameObject.AddComponent<AudioSource>();
        _weldSource.spatialBlend = 1f;
        _weldSource.minDistance = 2f;
        _weldSource.maxDistance = 15f;
        _weldSource.loop = true;
        _weldSource.clip = ProceduralAudioGen.GenerateSparkCrackle(0.5f);

        if (interactionPromptCanvas != null)
            interactionPromptCanvas.SetActive(false);
    }

    private void Update()
    {
        // Completely ignore interactions and reset timers if a dialogue is currently active!
        if (DialogueEngine.isDialogueActive)
        {
            _interactionCooldown = 0.2f;
            _holdTimer = 0f;
            if (fillRing != null) fillRing.fillAmount = 0f;
            if (weldingSparksFX != null) weldingSparksFX.Stop();
            if (_weldSource != null) _weldSource.Stop();
            return;
        }

        if (_interactionCooldown > 0f) { _interactionCooldown -= Time.deltaTime; return; }

        if (_isPlayerInZone && !_isDoorOpen)
        {
            if (!_hasTriggeredMonologue)
            {
                if (Input.GetKey(KeyCode.E) || Input.GetButton("Submit"))
                {
                    _holdTimer += Time.deltaTime;
                    if (fillRing != null) fillRing.fillAmount = _holdTimer / holdDuration;

                    if (_holdTimer >= holdDuration)
                    {
                        TriggerDoorFailure();
                    }
                }
                else
                {
                    _holdTimer = 0f;
                    if (fillRing != null) fillRing.fillAmount = 0f;
                }
            }
            else
            {
                bool holdingWelder = false;

                if (_hotbarManager != null && _hotbarManager.currentEquippedIndex >= 0 && _hotbarManager.currentEquippedIndex < _hotbarManager.quickSlots.Length)
                {
                    HotbarSlot currentSlot = _hotbarManager.quickSlots[_hotbarManager.currentEquippedIndex];
                    if (currentSlot != null && currentSlot.containedItem != null)
                    {
                        ItemData activeItem = currentSlot.containedItem.itemData;
                        if (activeItem != null && activeItem.itemID == "TOOL-WELD")
                        {
                            holdingWelder = true;
                        }
                    }
                }

                if (holdingWelder)
                {
                    if (Input.GetKey(KeyCode.E) || Input.GetButton("Submit"))
                    {
                        _holdTimer += Time.deltaTime;
                        if (fillRing != null) fillRing.fillAmount = _holdTimer / holdDuration;

                        if (weldingSparksFX != null && !weldingSparksFX.isPlaying)
                        {
                            weldingSparksFX.Play();
                        if (_weldSource != null) _weldSource.Play();
                        }

                        if (_holdTimer >= holdDuration)
                        {
                            ExecuteWeldBypass();
                        }
                    }
                    else
                    {
                        _holdTimer = 0f;
                        if (fillRing != null) fillRing.fillAmount = 0f;
                        if (weldingSparksFX != null) weldingSparksFX.Stop();
                    if (_weldSource != null) _weldSource.Stop();
                    }
                }
                else
                {
                    // Player has triggered monologue but is NOT holding the welder. Give feedback!
                    if (Input.GetKeyDown(KeyCode.E) || Input.GetButtonDown("Submit"))
                    {
                        if (_audioSource != null) _audioSource.PlayOneShot(ProceduralAudioGen.GenerateErrorBuzz(150f, 0.3f));
                        if (UIPickupLog.Instance != null) UIPickupLog.Instance.AddLog("Requires Fusion Welder", Color.red, "JAMMED");
                    }
                }
            }
        }
    }

    private void TriggerDoorFailure()
    {
        _holdTimer = 0f;
        if (fillRing != null) fillRing.fillAmount = 0f;
        if (_audioSource != null && _audioSource.clip != null) _audioSource.Play();

        if (doorDialogueNodes == null || doorDialogueNodes.Length == 0) AutoFillDialogue();

        if (dialogueEngine != null && doorDialogueNodes != null && doorDialogueNodes.Length > 0)
        {
            _hasTriggeredMonologue = true;
            if (interactionPromptCanvas != null) interactionPromptCanvas.SetActive(false);

            if (_cachedPlayer != null)
            {
                PlayerController pc = _cachedPlayer.GetComponent<PlayerController>();
                if (pc != null) pc.isRooted = true;
            }

            dialogueEngine.gameObject.SetActive(true);
            dialogueEngine.StartDialogue(doorDialogueNodes);

            if (_questTracker != null)
            {
                _questTracker.AdvanceObjective(2, "Access the Sync-Terminal");
            }
        }
    }

    private void ExecuteWeldBypass()
    {
        _holdTimer = 0f;
        if (fillRing != null) fillRing.fillAmount = 0f;
        _isDoorOpen = true;

        if (weldingSparksFX != null) weldingSparksFX.Stop();
        if (_weldSource != null) _weldSource.Stop();

        if (TryGetComponent<Animator>(out var doorAnim))
        {
            doorAnim.SetTrigger("OpenDoor");
        }
        else
        {
            gameObject.SetActive(false);
        }

        if (_audioSource != null)
        {
            _audioSource.PlayOneShot(ProceduralAudioGen.GenerateAscendingChime());
        }

        DoorTransition transition = GetComponentInChildren<DoorTransition>(true);
        if (transition != null)
        {
            transition.gameObject.SetActive(true);
        }

        if (_questTracker != null)
        {
            _questTracker.AdvanceObjective(6, "Investigate the broken network signal...");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !_isDoorOpen)
        {
            _isPlayerInZone = true;
            _cachedPlayer = other.gameObject;
            _holdTimer = 0f;
            if (interactionPromptCanvas != null) interactionPromptCanvas.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInZone = false;
            _cachedPlayer = null;
            _holdTimer = 0f;
            if (fillRing != null) fillRing.fillAmount = 0f;
            if (weldingSparksFX != null) weldingSparksFX.Stop();
            if (_weldSource != null) _weldSource.Stop();
            if (interactionPromptCanvas != null) interactionPromptCanvas.SetActive(false);
        }
    }

    [ContextMenu("Auto-Fill Door Dialogue")]
    private void AutoFillDialogue()
    {
        doorDialogueNodes = new DialogueNode[]
        {
            new DialogueNode { speakerName = "KAELEN", dialogueText = "The main blast door is completely jammed shut. The manual release is dead." },
            new DialogueNode { speakerName = "KAELEN", dialogueText = "I should check the Sync-Terminal over there to see if the crew left a maintenance log or a workaround." }
        };
    }
}
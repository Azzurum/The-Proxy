using UnityEngine;

/// <summary>
/// Manages the state machine for the jammed tutorial door, requiring dialogue completion and the Fusion Welder tool.
/// </summary>
public class JammedDoor : MonoBehaviour
{
    [Header("UI Prompt")]
    [Tooltip("The canvas containing the interaction prompt graphic.")]
    public GameObject interactionPromptCanvas;

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
    private bool _isPlayerInZone = false;
    private bool _hasTriggeredMonologue = false;
    private bool _isDoorOpen = false;
    private GameObject _cachedPlayer;
    private QuestTracker _questTracker;
    private HotbarManager _hotbarManager;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _questTracker = FindAnyObjectByType<QuestTracker>();
        _hotbarManager = FindAnyObjectByType<HotbarManager>();

        if (interactionPromptCanvas != null)
            interactionPromptCanvas.SetActive(false);
    }

    private void Update()
    {
        if (_isPlayerInZone && !_isDoorOpen)
        {
            if (!_hasTriggeredMonologue)
            {
                if (Input.GetKey(KeyCode.E) || Input.GetButton("Submit"))
                {
                    _holdTimer += Time.deltaTime;
                    if (_holdTimer >= holdDuration)
                    {
                        TriggerDoorFailure();
                    }
                }
                if (Input.GetKeyUp(KeyCode.E)) _holdTimer = 0f;
            }
            else if (_questTracker != null && _questTracker.GetCurrentObjective() == 5)
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

                if (Input.GetKey(KeyCode.E) && holdingWelder)
                {
                    _holdTimer += Time.deltaTime;

                    if (weldingSparksFX != null && !weldingSparksFX.isPlaying)
                    {
                        weldingSparksFX.Play();
                    }

                    if (_holdTimer >= holdDuration)
                    {
                        ExecuteWeldBypass();
                    }
                }
                else
                {
                    if (Input.GetKeyUp(KeyCode.E) || !holdingWelder)
                    {
                        _holdTimer = 0f;
                        if (weldingSparksFX != null) weldingSparksFX.Stop();
                    }
                }
            }
        }
    }

    private void TriggerDoorFailure()
    {
        _holdTimer = 0f;
        if (_audioSource != null && _audioSource.clip != null) _audioSource.Play();

        if (dialogueEngine != null && doorDialogueNodes != null && doorDialogueNodes.Length > 0)
        {
            _hasTriggeredMonologue = true;
            if (interactionPromptCanvas != null) interactionPromptCanvas.SetActive(false);

            if (_cachedPlayer != null) _cachedPlayer.SetActive(false);

            dialogueEngine.StartDialogue(doorDialogueNodes);
        }
    }

    private void ExecuteWeldBypass()
    {
        _holdTimer = 0f;
        _isDoorOpen = true;

        if (weldingSparksFX != null) weldingSparksFX.Stop();

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
            if (weldingSparksFX != null) weldingSparksFX.Stop();
            if (interactionPromptCanvas != null) interactionPromptCanvas.SetActive(false);
        }
    }
}
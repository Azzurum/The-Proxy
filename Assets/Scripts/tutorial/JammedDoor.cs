using UnityEngine;

public class JammedDoor : MonoBehaviour
{
    [Header("UI Prompt")]
    public GameObject interactionPromptCanvas;

    [Header("Dialogue Interaction")]
    public DialogueEngine dialogueEngine;
    public DialogueNode[] doorDialogueNodes;

    [Header("Hold Settings")]
    public float holdDuration = 1.0f;
    private float holdTimer = 0f;

    [Header("Phase 4: FX & Progression")]
    [Tooltip("Drag a Particle System prefab here to create welding sparks!")]
    public ParticleSystem weldingSparksFX;

    private AudioSource audioSource;
    private bool isPlayerInZone = false;
    private bool hasTriggeredMonologue = false;
    private bool isDoorOpen = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (interactionPromptCanvas != null)
            interactionPromptCanvas.SetActive(false);
    }

    void Update()
    {
        if (isPlayerInZone && !isDoorOpen)
        {
            QuestTracker tracker = FindObjectOfType<QuestTracker>();

            // --- STATE A: FIRST TIME TRYING DOOR (PHASE 2) ---
            if (!hasTriggeredMonologue)
            {
                if (Input.GetKey(KeyCode.E) || Input.GetButton("Submit"))
                {
                    holdTimer += Time.deltaTime;
                    if (holdTimer >= holdDuration)
                    {
                        TriggerDoorFailure();
                    }
                }
                if (Input.GetKeyUp(KeyCode.E)) holdTimer = 0f;
            }
            // --- STATE B: WELDING THE DOOR OPEN (PHASE 4) ---
            else if (tracker != null && tracker.GetCurrentObjective() == 5)
            {
                // 1. Look at your team's Hotbar Manager
                HotbarManager hotbar = FindAnyObjectByType<HotbarManager>();

                // 2. Check if the player actually has the welder equipped!
                bool holdingWelder = false;

                // Uses your team's standard array .Length boundary checks!
                if (hotbar != null && hotbar.currentEquippedIndex >= 0 && hotbar.currentEquippedIndex < hotbar.quickSlots.Length)
                {
                    // Grab your team's custom HotbarSlot script
                    HotbarSlot currentSlot = hotbar.quickSlots[hotbar.currentEquippedIndex];

                    // Look deep into the slot -> containedItem -> itemData reference!
                    if (currentSlot != null && currentSlot.containedItem != null)
                    {
                        ItemData activeItem = currentSlot.containedItem.itemData;
                        if (activeItem != null && activeItem.itemID == "TOOL-WELD")
                        {
                            holdingWelder = true;
                        }
                    }
                }

                // If holding down E AND the welder is in Kaelen's hand
                if (Input.GetKey(KeyCode.E) && holdingWelder)
                {
                    holdTimer += Time.deltaTime;

                    // Trigger the visual spark system!
                    if (weldingSparksFX != null && !weldingSparksFX.isPlaying)
                    {
                        weldingSparksFX.Play();
                    }

                    if (holdTimer >= holdDuration)
                    {
                        ExecuteWeldBypass(tracker);
                    }
                }
                else
                {
                    // If they release E OR scroll off the welder item slot, cut the sparks immediately
                    if (Input.GetKeyUp(KeyCode.E) || !holdingWelder)
                    {
                        holdTimer = 0f;
                        if (weldingSparksFX != null) weldingSparksFX.Stop();
                    }
                }
            }
        }
    }

    private void TriggerDoorFailure()
    {
        holdTimer = 0f;
        if (audioSource != null && audioSource.clip != null) audioSource.Play();

        if (dialogueEngine != null && doorDialogueNodes != null && doorDialogueNodes.Length > 0)
        {
            hasTriggeredMonologue = true;
            if (interactionPromptCanvas != null) interactionPromptCanvas.SetActive(false);

            GameObject player = GameObject.Find("Player_Kaelen");
            if (player == null) player = GameObject.FindWithTag("Player");
            if (player != null) player.SetActive(false);

            dialogueEngine.StartDialogue(doorDialogueNodes);
        }
    }

    private void ExecuteWeldBypass(QuestTracker tracker)
    {
        holdTimer = 0f;
        isDoorOpen = true;

        if (weldingSparksFX != null) weldingSparksFX.Stop();

        // 1. Fire your door opening animation
        Animator doorAnim = GetComponent<Animator>();
        if (doorAnim != null)
        {
            doorAnim.SetTrigger("OpenDoor");
        }
        else
        {
            // Fallback if no Animator component is on the door asset yet
            gameObject.SetActive(false);
        }

        // 2. Play a high-fidelity success chime/hiss
        if (audioSource != null)
        {
            audioSource.PlayOneShot(ProceduralAudioGen.GenerateAscendingChime());
        }

        DoorTransition transition = GetComponentInChildren<DoorTransition>(true);
        if (transition != null)
        {
            transition.gameObject.SetActive(true);
        }

        // 3. Move the tracker forward to Phase 5: The Symbiote's Introduction!
        if (tracker != null)
        {
            tracker.AdvanceObjective(6, "Investigate the broken network signal...");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isDoorOpen)
        {
            isPlayerInZone = true;
            holdTimer = 0f;
            if (interactionPromptCanvas != null) interactionPromptCanvas.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = false;
            holdTimer = 0f;
            if (weldingSparksFX != null) weldingSparksFX.Stop();
            if (interactionPromptCanvas != null) interactionPromptCanvas.SetActive(false);
        }
    }
}
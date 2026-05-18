using UnityEngine;

public class JammedDoor : MonoBehaviour
{
    [Header("UI Prompt")]
    public GameObject interactionPromptCanvas;

    [Header("Dialogue Interaction")]
    public DialogueEngine dialogueEngine;
    public DialogueNode[] doorDialogueNodes;

    [Header("Hold Settings")]
    public float holdDuration = 1.0f; // How long to hold E (1 second)
    private float holdTimer = 0f;

    private AudioSource audioSource;
    private bool isPlayerInZone = false;
    private bool hasTriggeredMonologue = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (interactionPromptCanvas != null)
            interactionPromptCanvas.SetActive(false);
    }

    void Update()
    {
        if (isPlayerInZone && !hasTriggeredMonologue)
        {
            if (Input.GetKey(KeyCode.E))
            {
                holdTimer += Time.deltaTime;
                Debug.Log("Holding E... current time: " + holdTimer); // ALERT 1

                if (holdTimer >= holdDuration)
                {
                    Debug.Log("Hold complete! Running TriggerDoorFailure()..."); // ALERT 2
                    TriggerDoorFailure();
                }
            }

            if (Input.GetKeyUp(KeyCode.E))
            {
                holdTimer = 0f;
            }
        }
    }

    private void TriggerDoorFailure()
    {
        holdTimer = 0f; // Reset timer safely

        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }

        if (dialogueEngine != null && doorDialogueNodes != null && doorDialogueNodes.Length > 0)
        {
            hasTriggeredMonologue = true;

            if (interactionPromptCanvas != null) interactionPromptCanvas.SetActive(false);

            // Safer Player search: First try name, then try Tag!
            GameObject player = GameObject.Find("Player_Kaelen");
            if (player == null) player = GameObject.FindWithTag("Player");

            if (player != null) player.SetActive(false);

            dialogueEngine.StartDialogue(doorDialogueNodes);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !hasTriggeredMonologue)
        {
            isPlayerInZone = true;
            holdTimer = 0f; // Reset
            if (interactionPromptCanvas != null)
                interactionPromptCanvas.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = false;
            holdTimer = 0f;
            if (interactionPromptCanvas != null)
                interactionPromptCanvas.SetActive(false);
        }
    }
}
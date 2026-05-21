using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles specific NPC dialogue branching, such as the initial mission briefing and refusal sequences.
/// </summary>
public class NPCInteraction : MonoBehaviour
{
    [Header("Conversations")]
    [Tooltip("The dialogue sequence played during the initial briefing.")]
    public DialogueNode[] conversation;
    [Tooltip("The dialogue sequence played if the player chooses to refuse the mission.")]
    public DialogueNode[] firedConversation;

    [Header("UI Connections")]
    [Tooltip("Reference to the floating interaction prompt UI.")]
    public FloatingPrompt interactionPrompt;
    [Tooltip("Reference to the Dialogue Engine in the scene.")]
    public DialogueEngine dialogueEngine;
    [Tooltip("Reference to the Game Over screen used for the Refusal ending.")]
    public GameOverScreen gameOverScreen;

    private bool _isPlayerInRange = false;
    private bool _isTalking = false;

    void Update()
    {
        if (_isPlayerInRange && !_isTalking && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return)))
        {
            StartBriefing();
        }

        if (_isTalking && !DialogueEngine.isDialogueActive)
        {
            _isTalking = false;
        }
    }

    private void StartBriefing()
    {
        if (conversation == null || conversation.Length == 0) return;

        _isTalking = true;
        if (interactionPrompt != null) interactionPrompt.HidePrompt();

        if (dialogueEngine != null) dialogueEngine.StartDialogue(conversation, true);
    }

    /// <summary>
    /// Triggered by the UI accept button. Closes dialogue and transitions to the main gameplay scene.
    /// </summary>
    public void AcceptDirective()
    {
        if (dialogueEngine.choicePanel != null) dialogueEngine.choicePanel.SetActive(false);
        dialogueEngine.EndDialogue();
        
        SceneManager.LoadScene("level_1");
    }

    /// <summary>
    /// Triggered by the UI refuse button. Plays the termination dialogue and initiates the 'Coward's Ending'.
    /// </summary>
    public void RefuseDirective()
    {
        if (dialogueEngine.choicePanel != null) dialogueEngine.choicePanel.SetActive(false);
        
        dialogueEngine.StartDialogue(firedConversation, false);
        StartCoroutine(WaitForGameOver());
    }

    private System.Collections.IEnumerator WaitForGameOver()
    {
        yield return new WaitUntil(() => !DialogueEngine.isDialogueActive);
        
        if (gameOverScreen != null)
        {
            gameOverScreen.TriggerGameOver();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !_isTalking)
        {
            _isPlayerInRange = true;
            if (interactionPrompt != null) interactionPrompt.ShowPrompt();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            _isPlayerInRange = false;
            _isTalking = false;
            if (interactionPrompt != null) interactionPrompt.HidePrompt();
        }
    }
}

/// <summary>
/// Represents a single discrete unit of text to be displayed by the Dialogue Engine.
/// </summary>
[System.Serializable]
public class DialogueNode
{
    [Tooltip("The display name of the character speaking this line.")]
    public string speakerName;
    [Tooltip("The actual text content to be typed out on screen.")]
    [TextArea(3, 5)]
    public string dialogueText;
}
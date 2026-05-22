using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles specific NPC dialogue branching, such as the initial mission briefing and refusal sequences.
/// </summary>
public class NPCInteraction : MonoBehaviour, IInteractable
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

    private bool _isTalking = false;
    private PlayerController _lockedPlayer;

    void Update()
    {
        if (_isTalking && !DialogueEngine.isDialogueActive)
        {
            _isTalking = false;
        }
    }

    public bool CanInteract()
    {
        return !_isTalking;
    }

    public void Interact(GameObject interactor)
    {
        _lockedPlayer = interactor.GetComponent<PlayerController>();
        if (_lockedPlayer != null) _lockedPlayer.isRooted = true;

        StartBriefing();
    }

    private void StartBriefing()
    {
        if (conversation == null || conversation.Length == 0) return;

        _isTalking = true;
        if (interactionPrompt != null) interactionPrompt.HidePrompt();

        if (dialogueEngine != null) 
        {
            dialogueEngine.gameObject.SetActive(true);
            dialogueEngine.StartDialogue(conversation, true);
        }
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
        
        if (dialogueEngine != null)
        {
            dialogueEngine.gameObject.SetActive(true);
            dialogueEngine.StartDialogue(firedConversation, false);
        }
        StartCoroutine(WaitForGameOver());
    }

    private System.Collections.IEnumerator WaitForGameOver()
    {
        yield return new WaitUntil(() => !DialogueEngine.isDialogueActive);
        
        if (gameOverScreen != null)
        {
            // FAILSAFE: Ensure parent is awake before showing the screen!
            if (gameOverScreen.transform.parent != null && !gameOverScreen.transform.parent.gameObject.activeSelf)
            {
                gameOverScreen.transform.parent.gameObject.SetActive(true);
            }
            
            gameOverScreen.gameObject.SetActive(true);
            gameOverScreen.TriggerGameOver();
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
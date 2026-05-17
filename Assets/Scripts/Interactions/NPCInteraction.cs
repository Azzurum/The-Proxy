using UnityEngine;
using UnityEngine.SceneManagement;

public class NPCInteraction : MonoBehaviour
{
    [Header("Conversations")]
    public DialogueNode[] conversation;
    public DialogueNode[] firedConversation;

    [Header("UI Connections")]
    public FloatingPrompt interactionPrompt;
    public DialogueEngine dialogueEngine;
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

        // Passing true shows the choice menu at the end
        if (dialogueEngine != null) dialogueEngine.StartDialogue(conversation, true);
    }

    // Called via UI Button Event on Btn_Accept
    public void AcceptDirective()
    {
        if (dialogueEngine.choicePanel != null) dialogueEngine.choicePanel.SetActive(false);
        dialogueEngine.EndDialogue();
        
        // Note: Change to the exact name of your ship scene
        SceneManager.LoadScene("level_1");
    }

    // Called via UI Button Event on Btn_Refuse
    public void RefuseDirective()
    {
        if (dialogueEngine.choicePanel != null) dialogueEngine.choicePanel.SetActive(false);
        
        // Passing false prevents the choice menu from showing again
        dialogueEngine.StartDialogue(firedConversation, false);
        StartCoroutine(WaitForGameOver());
    }

    private System.Collections.IEnumerator WaitForGameOver()
    {
        // Wait until the dialogue completely finishes typing and closes
        yield return new WaitUntil(() => !DialogueEngine.isDialogueActive);
        
        Debug.Log("ENDING 4 UNLOCKED: Terminated by Aether-Core.");
        
        // Trigger the cinematic fade!
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

[System.Serializable]
public class DialogueNode
{
    public string speakerName;
    [TextArea(3, 5)]
    public string dialogueText;
}
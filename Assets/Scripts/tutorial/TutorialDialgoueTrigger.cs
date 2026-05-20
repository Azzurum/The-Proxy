using UnityEngine;

public class TutorialDialogueTrigger : MonoBehaviour
{
    [Header("Engine Reference")]
    public DialogueEngine dialogueEngine;

    [Header("Opening Scene Dialogue")]
    // This safely hooks right into the original DialogueNode struct your team made!
    public DialogueNode[] introductionConversation;

    // This is the function our Master Timeline Director will call!
    public void TriggerKaelenDialogue()
    {
        if (dialogueEngine != null && introductionConversation != null && introductionConversation.Length > 0)
        {
            // 1. Lock the player so they can't move during the intro
            PlayerController pc = FindAnyObjectByType<PlayerController>();
            if (pc != null) pc.enabled = false;

            // 2. Make sure the Dialogue UI is turned on
            if (!dialogueEngine.gameObject.activeSelf) dialogueEngine.gameObject.SetActive(true);

            // 3. Play the conversation
            dialogueEngine.StartDialogue(introductionConversation, false);
        }
        else
        {
            Debug.LogWarning("Dialogue Trigger missing an engine reference or lines!");
        }
    }
}
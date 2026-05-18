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
            dialogueEngine.StartDialogue(introductionConversation, false);
        }
        else
        {
            Debug.LogWarning("Dialogue Trigger missing an engine reference or lines!");
        }
    }
}
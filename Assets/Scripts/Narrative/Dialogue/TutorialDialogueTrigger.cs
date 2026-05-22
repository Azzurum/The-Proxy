using UnityEngine;

/// <summary>
/// Coordinates the initial tutorial dialogue sequence, locking player input during execution.
/// </summary>
public class TutorialDialogueTrigger : MonoBehaviour
{
    [Header("Engine Reference")]
    [Tooltip("Reference to the dialogue engine.")]
    public DialogueEngine dialogueEngine;

    [Header("Opening Scene Dialogue")]
    [Tooltip("The dialogue sequence nodes to be played.")]
    public DialogueNode[] introductionConversation;
    
    private PlayerController _playerController;

    private void Start()
    {
        _playerController = FindAnyObjectByType<PlayerController>();
    }

    /// <summary>
    /// Triggered by a timeline or external event to begin the introductory conversation.
    /// </summary>
    public void TriggerKaelenDialogue()
    {
        if (dialogueEngine != null && introductionConversation != null && introductionConversation.Length > 0)
        {
            if (_playerController != null) _playerController.enabled = false;

            if (!dialogueEngine.gameObject.activeSelf) dialogueEngine.gameObject.SetActive(true);

            dialogueEngine.StartDialogue(introductionConversation, false);
        }
    }
}
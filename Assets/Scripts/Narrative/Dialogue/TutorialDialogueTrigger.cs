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
    private bool _hasTriggered = false;

    private void Start()
    {
        // Safe search in case Kaelen is hidden by the Director
        _playerController = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
    }

    /// <summary>
    /// Triggered by a timeline or external event to begin the introductory conversation.
    /// </summary>
    public void TriggerKaelenDialogue()
    {
        if (_hasTriggered) return; // FAILSAFE: Prevent this sequence from ever playing twice!

        if (dialogueEngine != null && introductionConversation != null && introductionConversation.Length > 0)
        {
            _hasTriggered = true;
            if (_playerController != null) _playerController.isRooted = true;

            if (!dialogueEngine.gameObject.activeSelf) dialogueEngine.gameObject.SetActive(true);

            dialogueEngine.StartDialogue(introductionConversation, false);
        }
    }
}
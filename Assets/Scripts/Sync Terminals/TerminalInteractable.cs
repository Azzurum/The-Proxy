using UnityEngine;

/// <summary>
/// Serves as the physical interaction volume for activating a Sync Terminal UI.
/// </summary>
public class TerminalInteractable : MonoBehaviour
{
    [Header("Terminal Data")]
    [Tooltip("The text data to display when the player accesses this terminal.")]
    public TerminalLogData assignedLog;
    [Tooltip("Reference to the terminal UI manager.")]
    public SyncTerminalUI uiManager;

    [Header("Visuals")]
    [Tooltip("Reference to the floating interaction prompt.")]
    public FloatingPrompt promptText;

    private bool _isPlayerNear = false;
    private GameObject _playerRef;
    private QuestTracker _questTracker;

    private void Start()
    {
        _questTracker = FindAnyObjectByType<QuestTracker>();
    }

    private void Update()
    {
        if (_questTracker == null || _questTracker.GetCurrentObjective() < 2)
            return;

        if (_isPlayerNear && Input.GetKeyDown(KeyCode.E) && !uiManager.terminalCanvas.gameObject.activeInHierarchy)
        {
            uiManager.OpenTerminal(assignedLog, _playerRef);

            if (promptText != null)
                promptText.HidePrompt();

        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            _isPlayerNear = true;
            _playerRef = collision.gameObject;

            if (_questTracker != null && _questTracker.GetCurrentObjective() == 2)
            {
                if (promptText != null)
                    promptText.ShowPrompt();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            _isPlayerNear = false;
            _playerRef = null;

            if (promptText != null)
                promptText.HidePrompt();
        }
    }
}
using UnityEngine;

public class TerminalInteractable : MonoBehaviour
{
    [Header("Terminal Data")]
    public TerminalLogData assignedLog;
    public SyncTerminalUI uiManager;

    [Header("Visuals")]
    public FloatingPrompt promptText;

    private bool _isPlayerNear = false;
    private GameObject _playerRef;

    private void Update()
    {
        QuestTracker tracker = FindObjectOfType<QuestTracker>();
        if (tracker == null || tracker.GetCurrentObjective() < 2)
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

            QuestTracker tracker = FindObjectOfType<QuestTracker>();
            if (tracker != null && tracker.GetCurrentObjective() == 2)
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
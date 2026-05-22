using UnityEngine;

/// <summary>
/// Serves as the physical interaction volume for activating a Sync Terminal UI.
/// </summary>
public class TerminalInteractable : MonoBehaviour, IInteractable
{
    [Header("Terminal Data")]
    [Tooltip("The text data to display when the player accesses this terminal.")]
    public TerminalLogData assignedLog;
    [Tooltip("Reference to the terminal UI manager.")]
    public SyncTerminalUI uiManager;

    [Header("Visuals")]
    [Tooltip("Reference to the floating interaction prompt.")]
    public FloatingPrompt promptText;

    private QuestTracker _questTracker;

    private void Start()
    {
        _questTracker = FindAnyObjectByType<QuestTracker>();
    }

    public bool CanInteract()
    {
        return _questTracker != null && _questTracker.GetCurrentObjective() >= 2 && !uiManager.terminalCanvas.gameObject.activeInHierarchy;
    }

    public void Interact(GameObject interactor)
    {
        uiManager.OpenTerminal(assignedLog, interactor);
        if (promptText != null) promptText.HidePrompt();
    }
}
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
    private AudioSource _audioSource;
    private float _pingTimer = 0f;

    private void Start()
    {
        _questTracker = FindAnyObjectByType<QuestTracker>();

        // Auto-wire the UI manager if it wasn't assigned in the Inspector
        if (uiManager == null) uiManager = FindAnyObjectByType<SyncTerminalUI>(FindObjectsInactive.Include);

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.spatialBlend = 1f; // Force 3D sound
        _audioSource.rolloffMode = AudioRolloffMode.Linear;
        _audioSource.minDistance = 2f;
        _audioSource.maxDistance = 15f;
    }

    private void Update()
    {
        // Emit a low, mechanical pinging sound when unread, acting as a breadcrumb in the dark!
        if (CanInteract())
        {
            _pingTimer -= Time.deltaTime;
            if (_pingTimer <= 0f)
            {
                if (_audioSource != null) _audioSource.PlayOneShot(ProceduralAudioGen.GenerateBeep(600f, 0.05f), 0.2f);
                _pingTimer = 2.5f; // Ping every 2.5 seconds
            }
        }
    }

    public bool CanInteract()
    {
        if (uiManager == null || uiManager.terminalCanvas == null) return false;
        return _questTracker != null && _questTracker.GetCurrentObjective() >= 2 && !uiManager.terminalCanvas.gameObject.activeInHierarchy;
    }

    public void Interact(GameObject interactor)
    {
        if (_audioSource != null) _audioSource.PlayOneShot(ProceduralAudioGen.GenerateServerRise(0.4f));

        uiManager.OpenTerminal(assignedLog, interactor);
        if (promptText != null) promptText.HidePrompt();

        // Advance the quest after reading the terminal to direct them to the locker!
        if (_questTracker != null && _questTracker.GetCurrentObjective() == 2)
        {
            _questTracker.AdvanceObjective(3, "Search the Crew Locker for a Fusion Welder");
        }
    }
}
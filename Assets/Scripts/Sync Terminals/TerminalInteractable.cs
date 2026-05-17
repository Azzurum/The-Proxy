using UnityEngine;

public class TerminalInteractable : MonoBehaviour
{
    [Header("Terminal Data")]
    public TerminalLogData assignedLog; 
    public SyncTerminalUI uiManager;    

    [Header("Visuals")]
    public FloatingPrompt promptText; // NEW: We add a slot for your floating 'E'

    private bool _isPlayerNear = false;
    private GameObject _playerRef;

    private void Update()
    {
        if (_isPlayerNear && Input.GetKeyDown(KeyCode.E) && !uiManager.terminalCanvas.gameObject.activeInHierarchy)
        {
            uiManager.OpenTerminal(assignedLog, _playerRef);
            
            // Optional Polish: Hide the 'E' while Kaelen is reading the terminal
            if (promptText != null) promptText.HidePrompt();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            _isPlayerNear = true;
            _playerRef = collision.gameObject;
            
            // NEW: Fade the prompt IN when Kaelen gets close
            if (promptText != null) promptText.ShowPrompt(); 
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            _isPlayerNear = false;
            _playerRef = null;
            
            // NEW: Fade the prompt OUT when Kaelen walks away
            if (promptText != null) promptText.HidePrompt(); 
        }
    }
}
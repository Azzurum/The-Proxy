using System.Collections;
using UnityEngine;
using TMPro;
using System.Text;

/// <summary>
/// Manages the visual interface and input handling for in-game log decryption terminals.
/// </summary>
public class SyncTerminalUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The main canvas object that contains the terminal UI.")]
    public Canvas terminalCanvas;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI metaText;
    public TextMeshProUGUI bodyText;
    
    [Header("Buttons")]
    public GameObject logOffButton;
    public GameObject nextPageButton;
    public GameObject prevPageButton;

    [Header("Decryption Settings")]
    [Tooltip("Delay in seconds between typing each standard character.")]
    public float typeSpeed = 0.02f;
    [Tooltip("Delay in seconds between scrambling letters for the glitch effect.")]
    public float scrambleSpeed = 0.012f;
    [Tooltip("Number of random glitch characters to display before showing the real character.")]
    public int maxScrambles = 3;
    
    private string glitchChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789@#$%&*+=-/\\";
    private Coroutine typingCoroutine;
    private GameObject _lockedPlayer; 

    private bool _isDecrypting = false;
    private string[] _currentPages;
    private int _pageIndex = 0;
    
    private WaitForSeconds _typeWait;
    private WaitForSeconds _scrambleWait;
    private QuestTracker _questTracker;

    private void Start()
    {
        if (terminalCanvas != null) terminalCanvas.gameObject.SetActive(false);
        
        _typeWait = new WaitForSeconds(typeSpeed);
        _scrambleWait = new WaitForSeconds(scrambleSpeed);
        _questTracker = FindAnyObjectByType<QuestTracker>();
    }

    private void Update()
    {
        if (terminalCanvas.gameObject.activeInHierarchy)
        {
            if (_isDecrypting && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
            {
                TriggerInstantSkip();
            }
            else if (!_isDecrypting && _pageIndex < _currentPages.Length - 1 && Input.GetKeyDown(KeyCode.Space))
            {
                LoadNextPage();
            }
            else if (!_isDecrypting && _pageIndex > 0 && (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.LeftArrow)))
            {
                LoadPreviousPage();
            }
        }
    }

    /// <summary>
    /// Locks the player character and begins the decryption visualization for the provided log.
    /// </summary>
    public void OpenTerminal(TerminalLogData logData, GameObject player)
    {
        _lockedPlayer = player;

        if (_lockedPlayer != null)
        {
            PlayerController movement = _lockedPlayer.GetComponent<PlayerController>();
            Rigidbody2D rb = _lockedPlayer.GetComponent<Rigidbody2D>();
            
            if (movement != null) movement.enabled = false;
            if (rb != null) 
            {
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
        }

        terminalCanvas.gameObject.SetActive(true);
        titleText.text = logData.logTitle;
        metaText.text = "AUTHOR: " + logData.author + " | DAY: " + logData.dayNumber;
        bodyText.text = "";
        
        logOffButton.SetActive(true); 
        nextPageButton.SetActive(false); 
        prevPageButton.SetActive(false);

        _currentPages = logData.logPages;
        _pageIndex = 0;

        if (_currentPages.Length > 0)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(DecryptTextRoutine(_currentPages[_pageIndex]));
        }
    }

    /// <summary>
    /// Unlocks the player and hides the terminal interface.
    /// </summary>
    public void CloseTerminal()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        _isDecrypting = false; 
        
        if (_lockedPlayer != null)
        {
            PlayerController movement = _lockedPlayer.GetComponent<PlayerController>();
            Rigidbody2D rb = _lockedPlayer.GetComponent<Rigidbody2D>();
            
            if (movement != null) movement.enabled = true;
            if (rb != null) rb.bodyType = RigidbodyType2D.Dynamic;
        }

        if (_questTracker != null)
        {
            _questTracker.AdvanceObjective(3, "Search lockers for Fusion Welder");
        }

        terminalCanvas.gameObject.SetActive(false);
    }

    /// <summary>
    /// Transitions to the next page of the log if available.
    /// </summary>
    public void LoadNextPage()
    {
        _pageIndex++;
        bodyText.text = "";
        
        UpdateButtonVisibility();

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(DecryptTextRoutine(_currentPages[_pageIndex]));
    }

    /// <summary>
    /// Transitions to the previous page of the log if available.
    /// </summary>
    public void LoadPreviousPage()
    {
        _pageIndex--;
        bodyText.text = "";

        UpdateButtonVisibility();

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(DecryptTextRoutine(_currentPages[_pageIndex]));
    }

    /// <summary>
    /// Bypasses the decryption animation and immediately displays the full page text.
    /// </summary>
    private void TriggerInstantSkip()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        
        bodyText.text = _currentPages[_pageIndex];
        _isDecrypting = false; 

        UpdateButtonVisibility();
    }

    private void UpdateButtonVisibility()
    {
        prevPageButton.SetActive(_pageIndex > 0 && !_isDecrypting);
        nextPageButton.SetActive(_pageIndex < _currentPages.Length - 1 && !_isDecrypting);
    }

    private IEnumerator DecryptTextRoutine(string fullText)
    {
        _isDecrypting = true; 
        
        nextPageButton.SetActive(false);
        prevPageButton.SetActive(false);

        StringBuilder currentString = new StringBuilder();

        for (int i = 0; i < fullText.Length; i++)
        {
            char nextChar = fullText[i];

            if (nextChar == ' ' || nextChar == '\n')
            {
                currentString.Append(nextChar);
                bodyText.text = currentString.ToString() + "█"; 
                yield return _typeWait;
                continue;
            }

            for (int s = 0; s < maxScrambles; s++)
            {
                char randomChar = glitchChars[Random.Range(0, glitchChars.Length)];
                bodyText.text = currentString.ToString() + randomChar + "█";
                yield return _scrambleWait;
            }

            currentString.Append(nextChar);
            bodyText.text = currentString.ToString() + "█";
            yield return _typeWait;
        }

        bodyText.text = currentString.ToString();
        _isDecrypting = false; 

        UpdateButtonVisibility();
    }
}
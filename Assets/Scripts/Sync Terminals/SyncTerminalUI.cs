using System.Collections;
using UnityEngine;
using TMPro;

public class SyncTerminalUI : MonoBehaviour
{
    [Header("UI References")]
    public Canvas terminalCanvas;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI metaText;
    public TextMeshProUGUI bodyText;
    
    [Header("Buttons")]
    public GameObject logOffButton;
    public GameObject nextPageButton;
    public GameObject prevPageButton; // NEW: The Previous Page Button

    [Header("Decryption Settings")]
    public float typeSpeed = 0.02f;
    public float scrambleSpeed = 0.012f;
    public int maxScrambles = 3;
    
    private string glitchChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789@#$%&*+=-/\\";
    private Coroutine typingCoroutine;
    private GameObject _lockedPlayer; 

    private bool _isDecrypting = false;
    private string[] _currentPages;
    private int _pageIndex = 0;

    private void Start()
    {
        if (terminalCanvas != null) terminalCanvas.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (terminalCanvas.gameObject.activeInHierarchy)
        {
            // Skip typing
            if (_isDecrypting && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
            {
                TriggerInstantSkip();
            }
            // Hotkey for Next Page (Spacebar)
            else if (!_isDecrypting && _pageIndex < _currentPages.Length - 1 && Input.GetKeyDown(KeyCode.Space))
            {
                LoadNextPage();
            }
            // NEW: Hotkey for Previous Page (Backspace or Left Arrow)
            else if (!_isDecrypting && _pageIndex > 0 && (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.LeftArrow)))
            {
                LoadPreviousPage();
            }
        }
    }

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
        prevPageButton.SetActive(false); // NEW: Hide Previous button on open

        _currentPages = logData.logPages;
        _pageIndex = 0;

        if (_currentPages.Length > 0)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(DecryptTextRoutine(_currentPages[_pageIndex]));
        }
    }

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

        terminalCanvas.gameObject.SetActive(false);
    }

    public void LoadNextPage()
    {
        _pageIndex++;
        bodyText.text = "";
        
        UpdateButtonVisibility(); // NEW: Handle button hiding/showing

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(DecryptTextRoutine(_currentPages[_pageIndex]));
    }

    // NEW: The method for loading the previous page
    public void LoadPreviousPage()
    {
        _pageIndex--;
        bodyText.text = "";

        UpdateButtonVisibility(); // Handle button hiding/showing

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(DecryptTextRoutine(_currentPages[_pageIndex]));
    }

    private void TriggerInstantSkip()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        
        bodyText.text = _currentPages[_pageIndex];
        _isDecrypting = false; 

        UpdateButtonVisibility(); // NEW: Handle button hiding/showing
    }

    // NEW: A clean helper method to check which buttons should be visible
    private void UpdateButtonVisibility()
    {
        // Only show Prev if we aren't on the first page
        prevPageButton.SetActive(_pageIndex > 0 && !_isDecrypting);
        
        // Only show Next if we aren't on the last page
        nextPageButton.SetActive(_pageIndex < _currentPages.Length - 1 && !_isDecrypting);
    }

    private IEnumerator DecryptTextRoutine(string fullText)
    {
        _isDecrypting = true; 
        
        // Hide navigation buttons while typing
        nextPageButton.SetActive(false);
        prevPageButton.SetActive(false);

        string currentString = "";

        for (int i = 0; i < fullText.Length; i++)
        {
            char nextChar = fullText[i];

            if (nextChar == ' ' || nextChar == '\n')
            {
                currentString += nextChar;
                bodyText.text = currentString + "█"; 
                yield return new WaitForSeconds(typeSpeed);
                continue;
            }

            for (int s = 0; s < maxScrambles; s++)
            {
                char randomChar = glitchChars[Random.Range(0, glitchChars.Length)];
                bodyText.text = currentString + randomChar + "█";
                yield return new WaitForSeconds(scrambleSpeed);
            }

            currentString += nextChar;
            bodyText.text = currentString + "█";
            yield return new WaitForSeconds(typeSpeed);
        }

        bodyText.text = currentString;
        _isDecrypting = false; 

        UpdateButtonVisibility(); // Show the correct buttons now that typing is done
    }
}
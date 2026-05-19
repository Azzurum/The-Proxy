using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

[System.Serializable]
public struct CharacterProfile
{
    public string characterName;
    public Color nameplateColor;
    public Color textColor;
}

[System.Serializable]
public class PortraitController
{
    public RectTransform rectTransform;
    public Image image;

    public Vector2 focusOffset;
    public float activeScale = 1.15f;
    public float inactiveScale = 0.85f;
    public Color activeColor = Color.white;
    public Color inactiveColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    public float lerpSpeed = 8f;

    private Vector2 basePos, targetPos;
    private Vector3 targetScale;
    private Color targetCol;

    public void Initialize()
    {
        if (rectTransform != null)
        {
            basePos = rectTransform.anchoredPosition;
            SetFocus(false);
            rectTransform.localScale = targetScale;
            if (image != null) image.color = targetCol;
        }
    }

    public void SetFocus(bool isFocused)
    {
        targetPos = isFocused ? basePos + focusOffset : basePos;
        targetScale = isFocused ? Vector3.one * activeScale : Vector3.one * inactiveScale;
        targetCol = isFocused ? activeColor : inactiveColor;
    }

    public void UpdateLerp()
    {
        if (rectTransform == null || image == null) return;
        rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, targetPos, Time.deltaTime * lerpSpeed);
        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, Time.deltaTime * lerpSpeed);
        image.color = Color.Lerp(image.color, targetCol, Time.deltaTime * lerpSpeed);
    }
}

public class DialogueEngine : MonoBehaviour
{
    [Header("Character Database")]
    public CharacterProfile[] characterProfiles;

    [Header("Portrait Controllers")]
    public PortraitController portraitLeft;
    public PortraitController portraitRight;

    [Header("UI References")]
    public GameObject dialogueCanvas;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI nameText;
    public Image nameplateBG;
    public GameObject ambientDust;
    public GameObject vignetteOverlay;

    [Header("Menus")]
    public GameObject logPanel;
    public TextMeshProUGUI logHistoryText;
    public GameObject configPanel;
    public Slider speedSlider;
    public GameObject choicePanel; 

    [Header("Controls & Settings")]
    public TextMeshProUGUI autoButtonText;
    public TextMeshProUGUI skipButtonText; 
    public float typingSpeed = 0.02f;
    public float autoPlayDelay = 1.5f;

    private DialogueNode[] currentConversation;
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    public static bool isDialogueActive = false;
    private bool isAutoMode = false;
    private bool isSkipping = false; 
    private string _conversationHistory = "";
    private bool _showChoicesAtEnd = false;
    private float _inputCooldown = 0f;
    private bool _isInitialized = false;

    void Start()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        // Immediate Failsafes for when a scene restarts
        isDialogueActive = false;
        isSkipping = false;
        isAutoMode = false;

        if (dialogueCanvas != null) dialogueCanvas.SetActive(false);
        if (logPanel != null) logPanel.SetActive(false);
        if (configPanel != null) configPanel.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);
        if (vignetteOverlay != null) vignetteOverlay.SetActive(false);

        if (speedSlider != null)
        {
            speedSlider.value = typingSpeed;
            speedSlider.onValueChanged.AddListener(UpdateTypingSpeed);
        }

        portraitLeft.Initialize();
        portraitRight.Initialize();
    }

    void Update()
    {
        portraitLeft.UpdateLerp();
        portraitRight.UpdateLerp();

        if (_inputCooldown > 0f) _inputCooldown -= Time.deltaTime;

        bool isMenuOpen = (logPanel != null && logPanel.activeInHierarchy) ||
                          (configPanel != null && configPanel.activeInHierarchy) ||
                          (choicePanel != null && choicePanel.activeInHierarchy);

        if (dialogueCanvas.activeInHierarchy && !isMenuOpen && _inputCooldown <= 0f)
        {
            bool pressedKey = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
            bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            bool clickedMouse = Input.GetMouseButtonDown(0) && !isOverUI;

            if (pressedKey || clickedMouse)
            {
                // Clicking intercepts and cancels Auto/Skip so the player can read normally again
                if (isSkipping) Button_Skip();
                else if (isAutoMode) Button_Auto();
                else AdvanceDialogue();
            }
        }
    }

    public void StartDialogue(DialogueNode[] conversation, bool showChoices = false)
    {
        if (!_isInitialized) Start(); // Ensure Start doesn't overwrite us later!

        if (conversation == null || conversation.Length == 0) return;

        _inputCooldown = 0.2f; // Prevent instantly skipping on the frame it opens
        _showChoicesAtEnd = showChoices;
        isDialogueActive = true;
        currentConversation = conversation;
        currentLineIndex = 0;
        _conversationHistory = "SYSTEM ARCHIVE // SESSION INITIALIZED\n\n";

        // Reset toggles cleanly
        isAutoMode = false;
        isSkipping = false;
        if (autoButtonText != null) autoButtonText.color = Color.white;
        if (skipButtonText != null) skipButtonText.color = Color.white;

        if (dialogueCanvas != null) dialogueCanvas.SetActive(true);
        if (ambientDust != null) ambientDust.SetActive(true);
        if (vignetteOverlay != null) vignetteOverlay.SetActive(true);

        portraitLeft.SetFocus(false);
        portraitRight.SetFocus(false);

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeSentence(currentConversation[currentLineIndex]));
    }

    private void AdvanceDialogue()
    {
        if (currentConversation == null) return;

        if (isTyping)
        {
            // Instantly finish the line
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            if (dialogueText != null) dialogueText.maxVisibleCharacters = dialogueText.textInfo.characterCount;
            isTyping = false;
            
            // If we are fast-forwarding, instantly jump to the next line
            if (isSkipping) StartCoroutine(SkipDelayTrigger());
            else if (isAutoMode) StartCoroutine(AutoAdvanceTimer());
        }
        else
        {
            currentLineIndex++;
            if (currentLineIndex < currentConversation.Length)
            {
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                typingCoroutine = StartCoroutine(TypeSentence(currentConversation[currentLineIndex]));
            }
            else
            {
                if (_showChoicesAtEnd && choicePanel != null)
                {
                    // Forcibly stop the skipping when choices demand an answer!
                    isSkipping = false; 
                    if (skipButtonText != null) skipButtonText.color = Color.white;
                    choicePanel.SetActive(true);
                }
                else
                {
                    EndDialogue();
                }
            }
        }
    }

    IEnumerator TypeSentence(DialogueNode node)
    {
        if (nameText != null) nameText.text = node.speakerName;
        ApplyCharacterProfile(node.speakerName);

        string cleanName = node.speakerName.Trim().ToUpper();
        if (cleanName == "KAELEN") { portraitLeft.SetFocus(true); portraitRight.SetFocus(false); }
        else if (cleanName == "SYSTEM") { portraitLeft.SetFocus(false); portraitRight.SetFocus(false); }
        else { portraitLeft.SetFocus(false); portraitRight.SetFocus(true); }

        _conversationHistory += "<color=#FFB300>> " + node.speakerName + "</color>\n" + node.dialogueText + "\n\n";

        // Detect if the speaker is screaming (MOTHER + All Caps + Contains Actual Letters)
        bool isScreaming = node.speakerName.Trim().ToUpper() == "MOTHER" &&
                           node.dialogueText.Length > 5 && 
                           node.dialogueText == node.dialogueText.ToUpper() && 
                           node.dialogueText != node.dialogueText.ToLower();

        CameraFollow cam = null;
        if (isScreaming) 
        {
            cam = FindAnyObjectByType<CameraFollow>();
            
            // Play a loud static glitch sound when they scream!
            AudioSource audio = GetComponent<AudioSource>();
            if (audio == null) audio = gameObject.AddComponent<AudioSource>();
            audio.PlayOneShot(ProceduralAudioGen.GenerateStaticGlitch(2.5f), 1.0f);
        }

        string textToType = node.dialogueText;

        // Apply "Ransom Note" Glitch Effect
        // We check for '<' to ensure we don't accidentally break any existing Rich Text tags you might add later!
        if (isScreaming && !textToType.Contains("<"))
        {
            string corruptedText = "";
            foreach (char c in textToType)
            {
                if (char.IsWhiteSpace(c)) 
                {
                    corruptedText += c;
                }
                else
                {
                    float rand = Random.value;
                    if (rand > 0.90f) corruptedText += $"<color=#ff003c><size=130%>{c}</size></color>"; // Massive Red
                    else if (rand > 0.80f) corruptedText += $"<color=#777777><size=70%>{c}</size></color>"; // Tiny Grey
                    else if (rand > 0.77f) corruptedText += $"<color=#ff003c>█</color>"; // Pure Glitch Block
                    else corruptedText += c; // Normal
                }
            }
            textToType = corruptedText;
        }

        isTyping = true;
        if (dialogueText != null)
        {
            dialogueText.text = textToType;
            dialogueText.maxVisibleCharacters = 0;
            dialogueText.ForceMeshUpdate();

            int totalVisibleCharacters = dialogueText.textInfo.characterCount;
            for (int i = 0; i <= totalVisibleCharacters; i++)
            {
                dialogueText.maxVisibleCharacters = i;
                
                // Re-trigger the shake as they type so it feels like the voice is rattling the screen!
                if (isScreaming && cam != null && i % 2 == 0)
                {
                    cam.TriggerShake(0.15f, 0.35f); // Short, violent vibration
                }

                // Drive at 20x speed if skipping is active. If screaming, randomize typing for a glitchy effect!
                float currentDelay = typingSpeed;
                if (isSkipping) currentDelay = typingSpeed / 20f;
                else if (isScreaming) 
                {
                    currentDelay = typingSpeed * Random.Range(0.5f, 3.5f);
                    if (Random.value > 0.9f) currentDelay = typingSpeed * 8f; // Occasional heavy stutter
                }
                
                yield return new WaitForSeconds(currentDelay);
            }
        }
        isTyping = false;

        // Auto-trigger next logic when typing finishes
        if (isSkipping) StartCoroutine(SkipDelayTrigger());
        else if (isAutoMode) StartCoroutine(AutoAdvanceTimer());
    }

    private void ApplyCharacterProfile(string currentSpeakerName)
    {
        bool profileFound = false;
        string cleanName = currentSpeakerName.Trim().ToUpper();

        foreach (CharacterProfile profile in characterProfiles)
        {
            if (profile.characterName.Trim().ToUpper() == cleanName)
            {
                profileFound = true;
                if (nameplateBG != null) nameplateBG.color = profile.nameplateColor;
                if (nameText != null) nameText.color = profile.textColor;
                break;
            }
        }

        if (!profileFound) { if (nameplateBG != null) nameplateBG.color = Color.black; if (nameText != null) nameText.color = Color.white; }
    }

    IEnumerator AutoAdvanceTimer() { yield return new WaitForSeconds(autoPlayDelay); AdvanceDialogue(); }
    
    // Tiny fraction of a second delay when skipping so portraits have time to visually swap
    IEnumerator SkipDelayTrigger() { yield return new WaitForSeconds(0.1f); AdvanceDialogue(); }

    [Header("Player Control Integration")]
    public GameObject playerObject;

    public void EndDialogue()
    {
        isDialogueActive = false;
        isAutoMode = false;
        isSkipping = false;
        if (dialogueCanvas != null) dialogueCanvas.SetActive(false);
        if (ambientDust != null) ambientDust.SetActive(false);
        if (vignetteOverlay != null) vignetteOverlay.SetActive(false);

        if (playerObject != null)
        {
            playerObject.SetActive(true);
            Debug.Log("Dialogue complete! Player_Kaelen is now active and free to roam.");

            // Core Camera Tracking
            CameraFollow camScript = Camera.main.GetComponent<CameraFollow>();
            if (camScript != null)
            {
                camScript.target = playerObject.transform;
                Camera.main.transform.position = playerObject.transform.position + camScript.offset;
            }
        }
    }

    // ==========================================
    // BUTTON CONTROLS
    // ==========================================
    public void Button_Skip() 
    { 
        if (!isDialogueActive) return; 
        isSkipping = !isSkipping; 

        ColorUtility.TryParseHtmlString("#FFB300", out Color amber); 
        if (skipButtonText != null) skipButtonText.color = isSkipping ? amber : Color.white;

        if (isSkipping)
        {
            isAutoMode = false;
            if (autoButtonText != null) autoButtonText.color = Color.white;
            if (!isTyping) AdvanceDialogue();
        }
    }

    public void Button_Auto() 
    { 
        if (!isDialogueActive) return; 
        isAutoMode = !isAutoMode; 
        
        ColorUtility.TryParseHtmlString("#FFB300", out Color amber); 
        if (autoButtonText != null) autoButtonText.color = isAutoMode ? amber : Color.white; 

        if (isAutoMode)
        {
            isSkipping = false;
            if (skipButtonText != null) skipButtonText.color = Color.white;
            if (!isTyping) StartCoroutine(AutoAdvanceTimer()); 
        }
    }

    public void Button_Log() { if (logPanel != null && logHistoryText != null) { logHistoryText.text = _conversationHistory; logPanel.SetActive(true); } }
    public void Button_CloseLog() { if (logPanel != null) logPanel.SetActive(false); }
    public void Button_Config() { if (configPanel != null) configPanel.SetActive(true); }
    public void Button_CloseConfig() { if (configPanel != null) configPanel.SetActive(false); }
    public void UpdateTypingSpeed(float newSpeed) { typingSpeed = newSpeed; }
}
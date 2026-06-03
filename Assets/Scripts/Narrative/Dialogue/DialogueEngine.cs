using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Text;

/// <summary>
/// Contains visual settings for a specific speaking character within the dialogue system.
/// </summary>
[System.Serializable]
public struct CharacterProfile
{
    public string characterName;
    public Color nameplateColor;
    public Color textColor;
}

/// <summary>
/// Handles the lerping, scaling, and tinting of a character portrait during dialogue.
/// </summary>
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

/// <summary>
/// The core visual novel engine driving text delivery, portrait manipulation, and choice interactions.
/// </summary>
public class DialogueEngine : MonoBehaviour
{
    [Header("Character Database")]
    [Tooltip("Setup the distinct text and nameplate colors for each speaking entity.")]
    public CharacterProfile[] characterProfiles;

    [Header("Portrait Controllers")]
    public PortraitController portraitLeft;
    public PortraitController portraitRight;

    [Header("UI References")]
    [Tooltip("The root canvas GameObject for the dialogue interface.")]
    public GameObject dialogueCanvas;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI nameText;
    public Image nameplateBG;
    public GameObject ambientDust;
    public GameObject vignetteOverlay;

    [Header("Menus")]
    [Tooltip("The UI Panel for reviewing past dialogue.")]
    public GameObject logPanel;
    public TextMeshProUGUI logHistoryText;
    [Tooltip("The UI Panel for adjusting text speed.")]
    public GameObject configPanel;
    public Slider speedSlider;
    [Tooltip("The UI Panel displaying narrative choices at the end of a sequence.")]
    public GameObject choicePanel; 

    [Header("Controls & Settings")]
    public TextMeshProUGUI autoButtonText;
    public TextMeshProUGUI skipButtonText; 
    [Tooltip("The base delay in seconds between each typed character.")]
    public float typingSpeed = 0.02f;
    [Tooltip("Delay in seconds before automatically advancing text in Auto mode.")]
    public float autoPlayDelay = 1.5f;

    [Header("Player Control Integration")]
    public GameObject playerObject;
    public GameObject questTrackerText;

    private DialogueNode[] currentConversation;
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    public static bool isDialogueActive = false;
    private bool isAutoMode = false;
    private bool isSkipping = false; 
    private StringBuilder _conversationHistory;
    private bool _showChoicesAtEnd = false;
    private float _dialogueStartTime = 0f;
    private CameraFollow _mainCameraFollow;

    void Awake()
    {
        isDialogueActive = false;
        isSkipping = false;
        isAutoMode = false;
        _conversationHistory = new StringBuilder();

        _mainCameraFollow = FindAnyObjectByType<CameraFollow>();

        // --- ROBUSTNESS FIX: Auto-wire the dialogue canvas if the prefab link was broken ---
        if (dialogueCanvas == null)
        {
            // The DialogueEngine script should be on a manager object, and the canvas is expected to be a child.
            Transform wrapper = transform.Find("Dialogue_Wrapper");
            if (wrapper != null) dialogueCanvas = wrapper.gameObject;
        }

        // At the start of any scene, the dialogue UI should always be hidden.
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

        // Process interactions only if this specific engine is active and visible.
        if (isDialogueActive && currentConversation != null && dialogueCanvas.activeInHierarchy && !IsMenuOpen() && Time.time > _dialogueStartTime + 0.1f)
        {
            bool pressedKey = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
            bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            bool clickedMouse = Input.GetMouseButtonDown(0) && !isOverUI;

            if (pressedKey || clickedMouse)
            {
                if (isSkipping) Button_Skip();
                else if (isAutoMode) Button_Auto();
                else AdvanceDialogue();
            }
        }
    }

    void OnDestroy()
    {
        isDialogueActive = false;
        isAutoMode = false;
        isSkipping = false;
    }

    public bool IsMenuOpen()
    {
        return (logPanel != null && logPanel.activeInHierarchy) ||
               (configPanel != null && configPanel.activeInHierarchy) ||
               (choicePanel != null && choicePanel.activeInHierarchy);
    }

    /// <summary>
    /// Locks gameplay controls, visualizes the dialogue UI, and begins a conversation sequence.
    /// </summary>
    public void StartDialogue(DialogueNode[] conversation, bool showChoices = false)
    {
        if (conversation == null || conversation.Length == 0) return;

        _dialogueStartTime = Time.time;
        _showChoicesAtEnd = showChoices;
        isDialogueActive = true;
        currentConversation = conversation;
        currentLineIndex = 0;
        _conversationHistory.Clear();
        _conversationHistory.Append("SYSTEM ARCHIVE // SESSION INITIALIZED\n\n");
        if (logHistoryText != null) logHistoryText.text = _conversationHistory.ToString();

        isAutoMode = false;
        isSkipping = false;
        if (autoButtonText != null) autoButtonText.color = Color.white;
        if (skipButtonText != null) skipButtonText.color = Color.white;

        // 2. FAILSAFE: Ensure the Modal_Overlay (the black background) is strictly turned off
        Transform modalOverlay = transform.Find("Modal_Overlay");
        if (modalOverlay != null) modalOverlay.gameObject.SetActive(false);

        if (dialogueCanvas != null) dialogueCanvas.SetActive(true);

        dialogueCanvas.transform.SetAsLastSibling();
        
        CanvasGroup cg = dialogueCanvas.GetComponent<CanvasGroup>();
        if (cg == null) cg = dialogueCanvas.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        portraitLeft.SetFocus(false);
        portraitRight.SetFocus(false);

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeSentence(currentConversation[currentLineIndex]));
    }

    private void AdvanceDialogue()
    {
        if (isTyping)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            if (dialogueText != null && dialogueText.textInfo != null) dialogueText.maxVisibleCharacters = dialogueText.textInfo.characterCount;
            isTyping = false;
            
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
        string speakerSafe = node.speakerName ?? "SYSTEM";
        if (nameText != null) nameText.text = speakerSafe;
        ApplyCharacterProfile(speakerSafe);

        string cleanName = speakerSafe.Trim().ToUpper();
        if (cleanName == "KAELEN") { portraitLeft.SetFocus(true); portraitRight.SetFocus(false); }
        else if (cleanName == "SYSTEM") { portraitLeft.SetFocus(false); portraitRight.SetFocus(false); }
        else { portraitLeft.SetFocus(false); portraitRight.SetFocus(true); }

        _conversationHistory.Append("<color=#FFB300>> ").Append(speakerSafe).Append("</color>\n").Append(node.dialogueText).Append("\n\n");
        if (logHistoryText != null) logHistoryText.text = _conversationHistory.ToString();

        AudioSource audio = GetComponent<AudioSource>();
        if (audio == null) audio = gameObject.AddComponent<AudioSource>();

        bool isScreaming = cleanName == "MOTHER" &&
                           node.dialogueText.Length > 5 && 
                           node.dialogueText == node.dialogueText.ToUpper() && 
                           node.dialogueText != node.dialogueText.ToLower();

        if (isScreaming) 
        {
            audio.PlayOneShot(ProceduralAudioGen.GenerateStaticGlitch(2.5f), 1.0f);
        }

        string textToType = node.dialogueText;

        if (isScreaming && !textToType.Contains("<"))
        {
            StringBuilder corruptedText = new StringBuilder();
            foreach (char c in textToType)
            {
                if (char.IsWhiteSpace(c)) corruptedText.Append(c);
                else if (Random.value > 0.90f) corruptedText.Append($"<color=#ff003c><size=130%>{c}</size></color>");
                else if (Random.value > 0.80f) corruptedText.Append($"<color=#777777><size=70%>{c}</size></color>");
                else if (Random.value > 0.77f) corruptedText.Append($"<color=#ff003c>█</color>");
                else corruptedText.Append(c);
            }
            textToType = corruptedText.ToString();
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
                
                if (isScreaming && _mainCameraFollow != null && i % 2 == 0)
                {
                    _mainCameraFollow.TriggerShake(0.15f, 0.35f); 
                }
                else if (!isScreaming && i % 2 == 0 && i > 0)
                {
                    // Play the pleasant visual novel blip on every other letter to create a perfect typing rhythm!
                    audio.PlayOneShot(ProceduralAudioGen.GenerateTextBlip(), 0.5f);
                }

                float currentDelay = typingSpeed;
                if (isSkipping) currentDelay = typingSpeed / 20f;
                else if (isScreaming) 
                {
                    currentDelay = typingSpeed * Random.Range(0.5f, 3.5f);
                    if (Random.value > 0.9f) currentDelay = typingSpeed * 8f;
                }
                
                // Zero-allocation wait to prevent GC spikes during typing
                float waitTime = currentDelay;
                while (waitTime > 0)
                {
                if (!IsMenuOpen()) waitTime -= Time.deltaTime;
                    yield return null;
                }
            }
        }
        isTyping = false;

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

    IEnumerator AutoAdvanceTimer() 
    { 
        float timer = autoPlayDelay;
        while (timer > 0)
        {
            if (!IsMenuOpen()) timer -= Time.deltaTime;
            yield return null;
        }
        AdvanceDialogue(); 
    }
    
    IEnumerator SkipDelayTrigger() 
    { 
        float timer = 0.1f;
        while (timer > 0)
        {
            if (!IsMenuOpen()) timer -= Time.deltaTime;
            yield return null;
        }
        AdvanceDialogue(); 
    }

    /// <summary>
    /// Closes the dialogue interface and restores standard gameplay controls.
    /// </summary>
    public void EndDialogue()
    {
        isDialogueActive = false;
        isAutoMode = false;
        isSkipping = false;
        currentConversation = null; 
        if (dialogueCanvas != null) dialogueCanvas.SetActive(false);

        if (playerObject != null)
        {
            playerObject.SetActive(true);
            
            PlayerController pc = playerObject.GetComponent<PlayerController>();
            if (pc != null) 
            {
                pc.enabled = true;
                pc.isRooted = false; // FAILSAFE: Universally ensure the player can move when dialogue closes!
            }

            if (questTrackerText != null) questTrackerText.SetActive(true);

            CameraFollow camScript = Camera.main.GetComponent<CameraFollow>();
            if (camScript != null)
            {
                camScript.target = playerObject.transform;
                Camera.main.transform.position = playerObject.transform.position + camScript.offset;
            }
        }

        QuestTracker tracker = FindAnyObjectByType<QuestTracker>();
        if (tracker != null)
        {
            // Reveal the HUD and the WASD quest immediately after the cinematic conversation ends!
            if (tracker.gameplayHudGroup != null) tracker.gameplayHudGroup.SetActive(true);

            if (tracker.GetCurrentObjective() == 1)
            {
                tracker.AdvanceObjective(2, "Access the Sync-Terminal");
            }
        }

    }

    public void Button_Skip() 
    { 
        if (!isDialogueActive || IsMenuOpen()) return; 
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
        if (!isDialogueActive || IsMenuOpen()) return; 
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

    public void Button_Log() { if (logPanel != null && logHistoryText != null) { logHistoryText.text = _conversationHistory.ToString(); logPanel.SetActive(true); } }
    public void Button_CloseLog() { if (logPanel != null) logPanel.SetActive(false); }
    public void Button_Config() { if (configPanel != null) configPanel.SetActive(true); }
    public void Button_CloseConfig() { if (configPanel != null) configPanel.SetActive(false); }
    public void UpdateTypingSpeed(float newSpeed) { typingSpeed = newSpeed; }
}
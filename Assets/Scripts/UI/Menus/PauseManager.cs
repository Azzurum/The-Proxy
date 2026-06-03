using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the game's pause state, including UI transitions, screen capture effects, and menu navigation.
/// </summary>
public class PauseManager : MonoBehaviour
{
    [Header("Visual Connections")]
    [Tooltip("The main CanvasGroup for the pause menu, used for fading and interaction control.")]
    public CanvasGroup pauseMenuUI;
    [Tooltip("The parent GameObject containing the shattered glass visual effect.")]
    public GameObject shatteredGlassVisuals; 
    [Tooltip("A simple black background used for fading during menu transitions.")]
    public CanvasGroup voidBackground;

    [Header("Menu Panels (State Machine)")]
    [Tooltip("The panel containing the primary pause menu buttons (Resume, Settings, Quit).")]
    public GameObject panelMainMenu;
    [Tooltip("The panel for the Save/Load interface.")]
    public GameObject panelSaveLoad;
    [Tooltip("The panel for the 'Are you sure?' quit confirmation.")]
    public GameObject panelQuitConfirm;
    
    [Header("Settings / RIG Interface UI")]
    [Tooltip("The parent GameObject for the settings menu overlay.")]
    public GameObject settingsOverlay;
    [Tooltip("The CanvasGroup for the settings panel, used for fading.")]
    public CanvasGroup settingsCanvasGroup;
    [Tooltip("The RectTransform of the settings panel, used for scaling animations.")]
    public RectTransform settingsPanelTransform;

    [Header("Audio SFX")]
    [Tooltip("The AudioSource for playing menu sound effects.")]
    public AudioSource audioSource;
    [Tooltip("The sound effect played when the glass shatters.")]
    public AudioClip SND_UI_Menu_Shatter;
    [Tooltip("The duration of the audio fade in/out.")]
    public float fadeDuration = 0.5f;     
    private float _originalVolume;        

    [Header("Game Integration")]
    [Tooltip("Gameplay UI elements (e.g., Stamina bar) to hide when the game is paused.")]
    public GameObject[] elementsToHide;
    [Tooltip("Duration of the slow-motion 'bullet time' upon resuming.")]
    public float timeReentryDuration = 0.5f;

    [Header("Script References")]
    [Tooltip("Reference to the ShatterAnimator script that controls the glass animation.")]
    public ShatterAnimator shatterAnimator;
    [Tooltip("Reference to the script that procedurally generates the glass shards.")]
    public ProceduralGlassGenerator glassGenerator;

    private RenderTexture _pauseScreenTexture;
    private bool _isPaused = false;
    private bool _isAnimating = false;
    private bool _isTransitioning = false;

    private Coroutine _audioFadeCoroutine;
    private Coroutine _timeCoroutine;
    private Coroutine _uiTransitionCoroutine;

    void Start()
    {
        // DYNAMIC FAILSAFE: Reconnect the Glass Visuals if the prefab unpacking broke the links!
        if (shatterAnimator == null) shatterAnimator = FindAnyObjectByType<ShatterAnimator>(FindObjectsInactive.Include);
        if (glassGenerator == null) glassGenerator = FindAnyObjectByType<ProceduralGlassGenerator>(FindObjectsInactive.Include);
        if (shatteredGlassVisuals == null && shatterAnimator != null) shatteredGlassVisuals = shatterAnimator.gameObject;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource != null) 
        {
            audioSource.playOnAwake = false;
            audioSource.ignoreListenerPause = true;
            _originalVolume = audioSource.volume;
            if (SND_UI_Menu_Shatter != null) SND_UI_Menu_Shatter.LoadAudioData(); 
        }

        _pauseScreenTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        _pauseScreenTexture.Create();

        if (glassGenerator != null) glassGenerator.GenerateShards();
        if (shatteredGlassVisuals != null) shatteredGlassVisuals.SetActive(false);
        
        pauseMenuUI.alpha = 1f;
        pauseMenuUI.interactable = false; 
        pauseMenuUI.blocksRaycasts = false;
        pauseMenuUI.gameObject.SetActive(false);

        // Failsafe: Ensure the void background NEVER blocks your mouse clicks!
        if (voidBackground != null) 
        {
            voidBackground.blocksRaycasts = false;
            voidBackground.interactable = false;
        }

        // Automatically wire all the UI buttons so you don't have to do it manually in the Unity Editor!
        AutoWireButton(panelMainMenu, "Btn_Continue", TogglePause);
        AutoWireButton(panelMainMenu, "Btn_SaveLoad", OpenSaveLoad);
        AutoWireButton(panelMainMenu, "Btn_Settings", OpenSettings);
        // Wire Quit directly to the exit function, bypassing the confirmation panel in case it is missing!
        AutoWireButton(panelMainMenu, "Btn_Quit", ConfirmQuit); 

        AutoWireButton(panelQuitConfirm, "Btn_Confirm", ConfirmQuit);
        AutoWireButton(panelQuitConfirm, "Btn_Cancel", ReturnToMainMenuTransition);

        AutoWireButton(panelSaveLoad, "Btn_Return", ReturnToMainMenuTransition);

        if (settingsOverlay != null)
        {
            AutoWireButton(settingsOverlay, "Panel_Switchboard/Btn_Return", CloseSettings);
            AutoWireButton(settingsOverlay, "Btn_Return", CloseSettings); // Fallback
        }

        // Force the Pause Menu to render OVER EVERYTHING (Fixes the Grid overlap issue)
        if (pauseMenuUI != null)
        {
            Canvas pauseCanvas = pauseMenuUI.GetComponent<Canvas>();
            if (pauseCanvas == null) pauseCanvas = pauseMenuUI.GetComponentInParent<Canvas>();
            if (pauseCanvas != null)
            {
                pauseCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                pauseCanvas.sortingOrder = 32767;
            }
        }
    }

    private void AutoWireButton(GameObject parentPanel, string buttonPath, UnityEngine.Events.UnityAction action)
    {
        if (parentPanel == null) return;
        Transform btnTransform = parentPanel.transform.Find(buttonPath);
        if (btnTransform != null)
        {
            UnityEngine.UI.Button btn = btnTransform.GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(action);
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) 
        {
            HandleEscapePress();
        }
    }

    void OnDestroy()
    {
        // Clean up the RenderTexture to prevent memory leaks when the scene is unloaded.
        if (_pauseScreenTexture != null)
        {
            _pauseScreenTexture.Release();
            Destroy(_pauseScreenTexture);
        }
    }

    /// <summary>
    /// Central handler for the Escape key, routing it to the correct pause, unpause, or back-menu action.
    /// </summary>
    private void HandleEscapePress()
    {
        // Lock input during transitions to prevent overlapping animations.
        if (_isAnimating || _isTransitioning) return;

        if (!_isPaused)
        {
            TogglePause();
            return;
        }

        // If a submenu (like Settings or Save) is open, go back to the main pause screen.
        if (IsSubMenuActive())
        {
            ReturnToMainMenuTransition();
            return;
        }

        // If we are on the main pause screen, unpause the game.
        if (panelMainMenu != null && panelMainMenu.activeSelf)
        {
            TogglePause();
        }
    }

    /// <summary>
    /// An emergency override to instantly resume the game, used when loading a save file from the main menu.
    /// </summary>
    public void ForceResumeGame()
    {
        _isPaused = false;
        _isAnimating = false;
        _isTransitioning = false;

        StopAllCoroutines();

        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();

        if (shatteredGlassVisuals != null) shatteredGlassVisuals.SetActive(false);
        if (pauseMenuUI != null) pauseMenuUI.gameObject.SetActive(false);
        
        ToggleGameplayUI(true);
    }

    /// <summary>
    /// Checks if any of the secondary menu panels are currently active.
    /// </summary>
    private bool IsSubMenuActive()
    {
        return (panelSaveLoad != null && panelSaveLoad.activeSelf) ||
               (settingsOverlay != null && settingsOverlay.activeSelf) ||
               (panelQuitConfirm != null && panelQuitConfirm.activeSelf);
    }

    /// <summary>
    /// Toggles the game's paused state, initiating either the pause or resume sequence.
    /// </summary>
    public void TogglePause()
    {
        if (_isAnimating) return;
        _isPaused = !_isPaused;

        if (_isPaused) StartCoroutine(ExecutePauseRoutine());
        else StartCoroutine(ExecuteResumeRoutine());
    }

    /// <summary>
    /// The coroutine that executes the multi-step pause sequence.
    /// </summary>
    private IEnumerator ExecutePauseRoutine()
    {
        _isAnimating = true;
        if (_timeCoroutine != null) StopCoroutine(_timeCoroutine);

        if (_pauseScreenTexture == null || _pauseScreenTexture.width != Screen.width || _pauseScreenTexture.height != Screen.height)
        {
            if (_pauseScreenTexture != null) { _pauseScreenTexture.Release(); Destroy(_pauseScreenTexture); }
            _pauseScreenTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            _pauseScreenTexture.Create();
        }

        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshotIntoRenderTexture(_pauseScreenTexture);

        ToggleGameplayUI(false);

        if (shatteredGlassVisuals != null)
        {
            // 1. Explicitly grab the Pause Background and push it back, but respect its custom artwork/scale!
            Transform bgTrans = shatteredGlassVisuals.transform.Find("Pause_Background");
            if (bgTrans != null)
            {
                bgTrans.gameObject.SetActive(true);
                bgTrans.localPosition = new Vector3(0f, 0f, 2f); // Push it slightly backward so it doesn't fight with the glass shards!
                Renderer bgRenderer = bgTrans.GetComponent<Renderer>();
                if (bgRenderer != null)
                {
                    bgTrans.gameObject.layer = 0; // Force to Default layer to ensure visibility
                    bgRenderer.sortingLayerName = "Default"; // Force it onto the base layer
                    bgRenderer.sortingOrder = 31998; // Ensure it sits perfectly behind the glass shards (which are 32000)
                }
            }

            // 2. Map the screenshot to the glass shards and push them in front of the background
            Renderer[] renderers = shatteredGlassVisuals.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                if (r.gameObject.name == "Pause_Background") continue;
                
                r.gameObject.layer = 0; // Force layer to Default
                r.sortingLayerName = "Default"; // Force it onto the visible base layer
                r.sortingOrder = 32000; 
                if (r is MeshRenderer shard) shard.material.mainTexture = _pauseScreenTexture; 
            }
        }

        pauseMenuUI.gameObject.SetActive(true);
        ForceResetMenuState(); 

        SyncBiometrics();

        if (audioSource != null && SND_UI_Menu_Shatter != null)
        {
            audioSource.clip = SND_UI_Menu_Shatter;
            audioSource.volume = 0; 
            audioSource.Play();
            if (_audioFadeCoroutine != null) StopCoroutine(_audioFadeCoroutine);
            _audioFadeCoroutine = StartCoroutine(FadeAudio(0f, _originalVolume, fadeDuration));
        }
    
        shatteredGlassVisuals.SetActive(true);
        shatterAnimator.InitializeShards();
        yield return null; 

        Time.timeScale = 0f;
        AudioListener.pause = true;
        shatterAnimator.PlayShatter(); 
        
        yield return new WaitForSecondsRealtime(shatterAnimator.animationDuration);
        pauseMenuUI.interactable = true;
        pauseMenuUI.blocksRaycasts = true;
        _isAnimating = false; 
    }

    /// <summary>
    /// The coroutine that executes the multi-step resume sequence.
    /// </summary>
    private IEnumerator ExecuteResumeRoutine()
    {
        _isAnimating = true;

        if (audioSource != null && audioSource.isPlaying)
        {
            if (_audioFadeCoroutine != null) StopCoroutine(_audioFadeCoroutine);
            _audioFadeCoroutine = StartCoroutine(FadeAudio(audioSource.volume, 0f, fadeDuration));
        }

        // Enter a brief "bullet time" slow-motion effect for a smooth transition back to gameplay.
        Time.timeScale = 0.05f;
        AudioListener.pause = false;
        pauseMenuUI.interactable = false;
        pauseMenuUI.blocksRaycasts = false;
        
        shatterAnimator.PlayAssemble();
        
        // SIMULTANEOUSLY fade out the UI menu while the glass assembles
        StartCoroutine(FadeCanvasGroup(pauseMenuUI, 1f, 0f, shatterAnimator.animationDuration));
        
        yield return new WaitForSecondsRealtime(shatterAnimator.animationDuration + 0.1f);
        
        shatteredGlassVisuals.SetActive(false);
        pauseMenuUI.gameObject.SetActive(false);
        pauseMenuUI.alpha = 1f; // Reset the alpha so it's visible next time you pause
        ToggleGameplayUI(true);

        // Smoothly ramp time scale from bullet time back to normal (1.0).
        if (_timeCoroutine != null) StopCoroutine(_timeCoroutine);
        _timeCoroutine = StartCoroutine(LerpTimeScale(0.05f, 1f, timeReentryDuration));

        _isAnimating = false; 
    }

    /// <summary>
    /// Toggles the visibility of designated gameplay UI elements.
    /// </summary>
    private void ToggleGameplayUI(bool state)
    {
        if (elementsToHide == null) return;
        foreach (GameObject obj in elementsToHide) 
        {
            if (obj != null) obj.SetActive(state);
        }
    }

    /// <summary>
    /// Resets the menu to its default state, showing the main panel and hiding all sub-panels.
    /// </summary>
    private void ForceResetMenuState()
    {
        if (settingsOverlay != null) settingsOverlay.SetActive(false);
        if (panelSaveLoad != null) panelSaveLoad.SetActive(false);
        if (panelQuitConfirm != null) panelQuitConfirm.SetActive(false);
        
        MoveMenuToFront(panelMainMenu);
        if (voidBackground != null) voidBackground.alpha = 0f;
    }

    /// <summary>Called by a button to open the Save/Load menu.</summary>
    public void OpenSaveLoad() => TriggerSubMenuTransition(panelSaveLoad);
    /// <summary>Called by a button to open the Quit Confirmation menu.</summary>
    public void OpenQuitConfirm() => TriggerSubMenuTransition(panelQuitConfirm);
    
    /// <summary>
    /// Called by a button to open the Settings menu with a unique animation.
    /// </summary>
    public void OpenSettings()
    {
        if (_isTransitioning) return;
        if (_uiTransitionCoroutine != null) StopCoroutine(_uiTransitionCoroutine);
        _uiTransitionCoroutine = StartCoroutine(AnimateSettingsInRoutine());
    }

    /// <summary>
    /// Called by the 'Back' button within the Settings menu.
    /// </summary>
    public void CloseSettings() => ReturnToMainMenuTransition();

    /// <summary>
    /// Initiates the cinematic transition to return to the main pause menu from a sub-menu.
    /// </summary>
    public void ReturnToMainMenuTransition()
    {
        if (_isTransitioning) return;
        if (_uiTransitionCoroutine != null) StopCoroutine(_uiTransitionCoroutine);
        _uiTransitionCoroutine = StartCoroutine(TransitionToMainMenuRoutine());
    }

    private void TriggerSubMenuTransition(GameObject targetPanel)
    {
        if (_isTransitioning) return;
        if (_uiTransitionCoroutine != null) StopCoroutine(_uiTransitionCoroutine);
        _uiTransitionCoroutine = StartCoroutine(TransitionToSubMenuRoutine(targetPanel));
    }

    /// <summary>
    /// Coroutine for the cinematic transition from the main menu to a sub-menu.
    /// </summary>
    private IEnumerator TransitionToSubMenuRoutine(GameObject subMenuPanel)
    {
        _isTransitioning = true;
        if (panelMainMenu != null) panelMainMenu.SetActive(false);

        // Fade to a black background while the glass shards blow off-screen.
        if (voidBackground != null) voidBackground.alpha = 0f; // Removed the dark overlay per your request!
        
        if (shatterAnimator != null) yield return StartCoroutine(shatterAnimator.BlowbackRoutine());

        MoveMenuToFront(subMenuPanel);
        _isTransitioning = false;
    }

    /// <summary>
    /// Coroutine for the unique animated transition into the Settings menu.
    /// </summary>
    private IEnumerator AnimateSettingsInRoutine()
    {
        _isTransitioning = true;
        if (panelMainMenu != null) panelMainMenu.SetActive(false);

        if (voidBackground != null) voidBackground.alpha = 0f; // Removed the dark overlay
        
        if (shatterAnimator != null) yield return StartCoroutine(shatterAnimator.BlowbackRoutine());

        MoveMenuToFront(settingsOverlay);

        if (settingsCanvasGroup != null && settingsPanelTransform != null)
        {
            settingsCanvasGroup.interactable = false;
            settingsCanvasGroup.blocksRaycasts = false;
            settingsCanvasGroup.alpha = 0f;
            settingsPanelTransform.localScale = new Vector3(0.95f, 0.95f, 1f); // Start slightly smaller for a "pop" effect.

            float timeElapsed = 0;
            float duration = 0.25f;

            while (timeElapsed < duration)
            {
                float t = timeElapsed / duration;
                settingsCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                float scale = Mathf.Lerp(0.95f, 1f, t);
                settingsPanelTransform.localScale = new Vector3(scale, scale, 1f);

                timeElapsed += Time.unscaledDeltaTime; 
                yield return null;
            }

            settingsCanvasGroup.alpha = 1f;
            settingsPanelTransform.localScale = Vector3.one;
            settingsCanvasGroup.interactable = true;
            settingsCanvasGroup.blocksRaycasts = true;
        }

        _isTransitioning = false;
    }

    /// <summary>
    /// Coroutine for the cinematic transition from any sub-menu back to the main pause menu.
    /// </summary>
    private IEnumerator TransitionToMainMenuRoutine()
    {
        _isTransitioning = true;

        // If the settings menu is open, play its unique "out" animation.
        if (settingsOverlay != null && settingsOverlay.activeSelf && settingsCanvasGroup != null && settingsPanelTransform != null)
        {
            settingsCanvasGroup.interactable = false;
            settingsCanvasGroup.blocksRaycasts = false;

            float timeElapsed = 0;
            float duration = 0.2f;

            while (timeElapsed < duration)
            {
                float t = timeElapsed / duration;
                settingsCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                float scale = Mathf.Lerp(1f, 0.95f, t);
                settingsPanelTransform.localScale = new Vector3(scale, scale, 1f);

                timeElapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            settingsOverlay.SetActive(false);
        }
        else
        {
            // For other menus, simply hide them.
            if (settingsOverlay != null) settingsOverlay.SetActive(false);
            if (panelSaveLoad != null) panelSaveLoad.SetActive(false);
            if (panelQuitConfirm != null) panelQuitConfirm.SetActive(false);
        }

        // 2. Cinematic Glass Restore
        if (voidBackground != null) voidBackground.alpha = 0f; // Removed the dark overlay
        
        if (shatterAnimator != null) yield return StartCoroutine(shatterAnimator.RestoreRoutine());

        MoveMenuToFront(panelMainMenu);
        _isTransitioning = false;
    }

    /// <summary>
    /// Called by the 'Quit Game' button to close the application.
    /// </summary>
    public void ConfirmQuit()
    {
        Debug.Log("<color=red>SYSTEM TERMINATED.</color> Returning to Main Menu...");
        Time.timeScale = 1f;
        AudioListener.pause = false;
        
        // Bulletproof scene loading: It will hunt for whatever your Main Menu scene is actually named!
        if (Application.CanStreamedLevelBeLoaded("MainMenu_Scene")) SceneManager.LoadScene("MainMenu_Scene");
        else if (Application.CanStreamedLevelBeLoaded("MainMenu")) SceneManager.LoadScene("MainMenu");
        else if (Application.CanStreamedLevelBeLoaded("UI_MainMenu")) SceneManager.LoadScene("UI_MainMenu");
        else SceneManager.LoadScene(0); // Failsafe: load the very first scene in Build Settings
    }

    private void MoveMenuToFront(GameObject menuPanel)
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
            menuPanel.transform.SetAsLastSibling();
            
            // DYNAMIC FAILSAFE: Find the CRT effects by script instead of name, so it works no matter what you named them!
            Canvas rootCanvas = menuPanel.GetComponentInParent<Canvas>();
            if (rootCanvas != null)
            {
                UICRTPattern[] crtPatterns = rootCanvas.GetComponentsInChildren<UICRTPattern>(true);
                foreach (UICRTPattern pattern in crtPatterns)
                {
                    pattern.gameObject.SetActive(true);
                    
                    // Push the CRT object to the absolute bottom of the hierarchy so it renders on top of the menus
                    pattern.transform.SetAsLastSibling(); 
                    if (pattern.transform.parent != null) pattern.transform.parent.SetAsLastSibling();

                    // Force it to perfectly stretch across the entire screen
                    RectTransform rect = pattern.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        rect.anchorMin = Vector2.zero;
                        rect.anchorMax = Vector2.one;
                        rect.offsetMin = Vector2.zero;
                        rect.offsetMax = Vector2.zero;
                    }

                    // Turn it on (if the player hasn't disabled it in settings)
                    bool isEnabled = PlayerPrefs.GetInt("CrtDistortion", 1) == 1;
                    UnityEngine.UI.RawImage img = pattern.GetComponent<UnityEngine.UI.RawImage>();
                    if (img != null) img.enabled = isEnabled;
                    pattern.enabled = isEnabled;
                }

                // Do the exact same for the other CRT effect script
                CRTEffects[] crtFX = rootCanvas.GetComponentsInChildren<CRTEffects>(true);
                foreach (CRTEffects fx in crtFX)
                {
                    fx.gameObject.SetActive(true);
                    fx.transform.SetAsLastSibling();
                    if (fx.transform.parent != null) fx.transform.parent.SetAsLastSibling();

                    // Turn it on (if the player hasn't disabled it in settings)
                    bool isEnabled = PlayerPrefs.GetInt("CrtDistortion", 1) == 1;
                    fx.enabled = isEnabled;
                    if (fx.flickerImage != null) fx.flickerImage.enabled = isEnabled;
                }
            }
        }
    }

    /// <summary>
    /// Syncs the player's current corruption level with the visual distortion effect on the menu buttons.
    /// </summary>
    private void SyncBiometrics()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("DIAGNOSTIC: InventoryManager reference is missing!");
            return;
        }

        float corruptionPct = InventoryManager.Instance.GetCorruptionPercentage();

        HorrorProxyButton[] buttons = pauseMenuUI.GetComponentsInChildren<HorrorProxyButton>(true);
        foreach (HorrorProxyButton btn in buttons) 
        {
            btn.corruptionPercent = corruptionPct;
            btn.SendMessage("DrawFlowingLine", SendMessageOptions.DontRequireReceiver);
        }
    }

    /// <summary>
    /// A helper coroutine to smoothly lerp the game's Time.timeScale.
    /// </summary>
    private IEnumerator LerpTimeScale(float startScale, float targetScale, float duration)
    {
        float timer = 0;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Lerp(startScale, targetScale, timer / duration);
            yield return null;
        }
        Time.timeScale = targetScale;
    }

    /// <summary>
    /// A helper coroutine to smoothly fade an AudioSource's volume.
    /// </summary>
    private IEnumerator FadeAudio(float startVol, float targetVol, float duration)
    {
        float timer = 0;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(startVol, targetVol, timer / duration);
            yield return null;
        }
        audioSource.volume = targetVol;
        if (targetVol <= 0) audioSource.Stop();
    }

    /// <summary>
    /// A helper coroutine to smoothly fade a CanvasGroup's alpha.
    /// </summary>
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float target, float duration)
    {
        float timer = 0;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(start, target, timer / duration);
            yield return null;
        }
        cg.alpha = target;
    }
}
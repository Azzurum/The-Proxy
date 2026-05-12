using UnityEngine;
using System.Collections;

public class PauseManager : MonoBehaviour
{
    #region Variables
    [Header("Visual Connections")]
    public CanvasGroup pauseMenuUI;
    public GameObject shatteredGlassVisuals; 
    public CanvasGroup voidBackground;

    [Header("Menu Panels (State Machine)")]
    public GameObject panelMainMenu;
    public GameObject panelSaveLoad;
    public GameObject panelQuitConfirm;
    
    [Header("Settings / RIG Interface UI")]
    public GameObject settingsOverlay;            // Replaced the old panelSettings
    public CanvasGroup settingsCanvasGroup;       // Controls the master fade
    public RectTransform settingsPanelTransform;  // Controls the hardware "pop" scale

    [Header("Audio SFX")]
    public AudioSource audioSource;
    public AudioClip SND_UI_Menu_Shatter;
    public float fadeDuration = 0.5f;     
    private float _originalVolume;        

    [Header("Game Integration")]
    [Tooltip("UI elements to hide (e.g., Stamina bar) during pause.")]
    public GameObject[] elementsToHide;
    [Tooltip("Duration of the slow-motion 'bullet time' upon resuming.")]
    public float timeReentryDuration = 0.5f;

    [Header("Script References")]
    public ShatterAnimator shatterAnimator;
    public ProceduralGlassGenerator glassGenerator;
    public InventoryGrid inventory;

    // Internal State
    private RenderTexture _pauseScreenTexture;
    private bool _isPaused = false;
    private bool _isAnimating = false;    // Locks input during Pause/Unpause
    private bool _isTransitioning = false;// Locks input during Menu Swaps

    // Active Coroutine Trackers (prevents overlapping animations)
    private Coroutine _audioFadeCoroutine;
    private Coroutine _timeCoroutine;
    private Coroutine _uiTransitionCoroutine;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        // 1. Setup Audio
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource != null) 
        {
            audioSource.playOnAwake = false;
            audioSource.ignoreListenerPause = true;
            _originalVolume = audioSource.volume;
            if (SND_UI_Menu_Shatter != null) SND_UI_Menu_Shatter.LoadAudioData(); 
        }

        // 2. Setup Render Texture for Glass Effect
        _pauseScreenTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        _pauseScreenTexture.Create();

        // 3. Setup Initial Glass State
        if (glassGenerator != null) glassGenerator.GenerateShards();
        if (shatteredGlassVisuals != null) shatteredGlassVisuals.SetActive(false);
        
        // 4. Setup Base UI State
        pauseMenuUI.alpha = 1f;
        pauseMenuUI.interactable = false; 
        pauseMenuUI.blocksRaycasts = false;
        pauseMenuUI.gameObject.SetActive(false);
    }

    void Update()
    {
        // Listen for the "Back" or "Pause" command
        if (Input.GetKeyDown(KeyCode.Escape)) 
        {
            HandleEscapePress();
        }
    }

    void OnDestroy()
    {
        // Clean up memory to prevent leaks
        if (_pauseScreenTexture != null)
        {
            _pauseScreenTexture.Release();
            Destroy(_pauseScreenTexture);
        }
    }
    #endregion

    #region Core Pause Logic
    // Evaluates current state and routes the Escape key press appropriately
    private void HandleEscapePress()
    {
        // Block input if the system is currently doing a cinematic shatter or menu swap
        if (_isAnimating || _isTransitioning) return;

        // If game is active, pause it
        if (!_isPaused)
        {
            TogglePause();
            return;
        }

        // If we are deep in a submenu, elegantly return to the main menu using existing cinematic
        if (IsSubMenuActive())
        {
            ReturnToMainMenuTransition();
            return;
        }

        // If we are already on the main menu, unpause the game
        if (panelMainMenu != null && panelMainMenu.activeSelf)
        {
            TogglePause();
        }
    }

    // --- NEW: FORCED STARTUP LOAD KILL-SWITCH ---
    public void ForceResumeGame()
    {
        // 1. Reset all internal state locks
        _isPaused = false;
        _isAnimating = false;
        _isTransitioning = false;

        // 2. Kill any active animations, audio fades, or time transitions
        StopAllCoroutines();

        // 3. Force Time and Audio back to normal instantly
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();

        // 4. Hard-reset all visuals and UI components
        if (shatteredGlassVisuals != null) shatteredGlassVisuals.SetActive(false);
        if (pauseMenuUI != null) pauseMenuUI.gameObject.SetActive(false);
        
        // 5. Restore active gameplay HUD
        ToggleGameplayUI(true);
    }

    private bool IsSubMenuActive()
    {
        return (panelSaveLoad != null && panelSaveLoad.activeSelf) ||
               (settingsOverlay != null && settingsOverlay.activeSelf) ||
               (panelQuitConfirm != null && panelQuitConfirm.activeSelf);
    }

    public void TogglePause()
    {
        if (_isAnimating) return;
        _isPaused = !_isPaused;

        if (_isPaused) StartCoroutine(ExecutePauseRoutine());
        else StartCoroutine(ExecuteResumeRoutine());
    }

    private IEnumerator ExecutePauseRoutine()
    {
        _isAnimating = true;
        if (_timeCoroutine != null) StopCoroutine(_timeCoroutine);

        // 1. Capture Screen
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshotIntoRenderTexture(_pauseScreenTexture);

        // 2. Hide Gameplay UI
        ToggleGameplayUI(false);

        // 3. Map Screen Texture to Shards
        if (shatteredGlassVisuals != null)
        {
            MeshRenderer[] shardRenderers = shatteredGlassVisuals.GetComponentsInChildren<MeshRenderer>(true);
            foreach (MeshRenderer shard in shardRenderers)
            {
                if (shard.gameObject.name == "Pause_Background") continue;
                shard.material.mainTexture = _pauseScreenTexture; 
            }
        }

        // 4. Initialize Base Pause UI
        pauseMenuUI.gameObject.SetActive(true);
        ForceResetMenuState(); 

        // 5. Update Biometric UI
        SyncBiometrics();

        // 6. Play Audio
        if (audioSource != null && SND_UI_Menu_Shatter != null)
        {
            audioSource.clip = SND_UI_Menu_Shatter;
            audioSource.volume = 0; 
            audioSource.Play();
            if (_audioFadeCoroutine != null) StopCoroutine(_audioFadeCoroutine);
            _audioFadeCoroutine = StartCoroutine(FadeAudio(0f, _originalVolume, fadeDuration));
        }
    
        // 7. Fire Shatter Animation
        shatteredGlassVisuals.SetActive(true);
        shatterAnimator.InitializeShards();
        yield return null; 

        Time.timeScale = 0f;
        AudioListener.pause = true;
        shatterAnimator.PlayShatter(); 
        
        // 8. Unlock Input
        yield return new WaitForSecondsRealtime(shatterAnimator.animationDuration);
        pauseMenuUI.interactable = true;
        pauseMenuUI.blocksRaycasts = true;
        _isAnimating = false; 
    }

    private IEnumerator ExecuteResumeRoutine()
    {
        _isAnimating = true;

        // 1. Fade Audio Out
        if (audioSource != null && audioSource.isPlaying)
        {
            if (_audioFadeCoroutine != null) StopCoroutine(_audioFadeCoroutine);
            _audioFadeCoroutine = StartCoroutine(FadeAudio(audioSource.volume, 0f, fadeDuration));
        }

        // 2. Lock Input & Enter Bullet-Time
        Time.timeScale = 0.05f;
        AudioListener.pause = false;
        pauseMenuUI.interactable = false;
        pauseMenuUI.blocksRaycasts = false;
        
        // 3. Fire Assemble Animation
        shatterAnimator.PlayAssemble();
        yield return new WaitForSecondsRealtime(shatterAnimator.animationDuration + 0.1f);
        
        // 4. Cleanup UI & Restore Gameplay
        shatteredGlassVisuals.SetActive(false);
        pauseMenuUI.gameObject.SetActive(false);
        ToggleGameplayUI(true);

        // 5. Ramp Time Back to Normal
        if (_timeCoroutine != null) StopCoroutine(_timeCoroutine);
        _timeCoroutine = StartCoroutine(LerpTimeScale(0.05f, 1f, timeReentryDuration));

        _isAnimating = false; 
    }

    private void ToggleGameplayUI(bool state)
    {
        if (elementsToHide == null) return;
        foreach (GameObject obj in elementsToHide) 
        {
            if (obj != null) obj.SetActive(state);
        }
    }
    #endregion

    #region UI Panel State Machine
    // Instantly resets everything (Called on Pause)
    private void ForceResetMenuState()
    {
        if (settingsOverlay != null) settingsOverlay.SetActive(false);
        if (panelSaveLoad != null) panelSaveLoad.SetActive(false);
        if (panelQuitConfirm != null) panelQuitConfirm.SetActive(false);
        
        if (panelMainMenu != null) panelMainMenu.SetActive(true);
        if (voidBackground != null) voidBackground.alpha = 0f;
    }

    // --- Button Triggers ---
    public void OpenSaveLoad() => TriggerSubMenuTransition(panelSaveLoad);
    public void OpenQuitConfirm() => TriggerSubMenuTransition(panelQuitConfirm);
    
    // Explicit trigger for the Settings Prefab logic
    public void OpenSettings()
    {
        if (_isTransitioning) return;
        if (_uiTransitionCoroutine != null) StopCoroutine(_uiTransitionCoroutine);
        _uiTransitionCoroutine = StartCoroutine(AnimateSettingsInRoutine());
    }

    // Explicit trigger for the Settings Prefab Return button
    public void CloseSettings() => ReturnToMainMenuTransition();

    public void ReturnToMainMenuTransition()
    {
        if (_isTransitioning) return;
        if (_uiTransitionCoroutine != null) StopCoroutine(_uiTransitionCoroutine);
        _uiTransitionCoroutine = StartCoroutine(TransitionToMainMenuRoutine());
    }

    // --- Cinematic Transition Logic ---
    private void TriggerSubMenuTransition(GameObject targetPanel)
    {
        if (_isTransitioning) return;
        if (_uiTransitionCoroutine != null) StopCoroutine(_uiTransitionCoroutine);
        _uiTransitionCoroutine = StartCoroutine(TransitionToSubMenuRoutine(targetPanel));
    }

    private IEnumerator TransitionToSubMenuRoutine(GameObject subMenuPanel)
    {
        _isTransitioning = true;
        if (panelMainMenu != null) panelMainMenu.SetActive(false);

        // Fade to void while glass blows away
        if (voidBackground != null) StartCoroutine(FadeCanvasGroup(voidBackground, 0f, 1f, 0.2f));
        if (shatterAnimator != null) yield return StartCoroutine(shatterAnimator.BlowbackRoutine());

        if (subMenuPanel != null) subMenuPanel.SetActive(true);
        _isTransitioning = false;
    }

    // NEW: Handles the cinematic glass blowback AND the premium UI fade-in
    private IEnumerator AnimateSettingsInRoutine()
    {
        _isTransitioning = true;
        if (panelMainMenu != null) panelMainMenu.SetActive(false);

        // 1. Trigger the cinematic shattered glass blowback
        if (voidBackground != null) StartCoroutine(FadeCanvasGroup(voidBackground, 0f, 1f, 0.2f));
        if (shatterAnimator != null) yield return StartCoroutine(shatterAnimator.BlowbackRoutine());

        // 2. Turn on the Settings Overlay and start the premium scale/fade pop
        if (settingsOverlay != null) settingsOverlay.SetActive(true);

        if (settingsCanvasGroup != null && settingsPanelTransform != null)
        {
            settingsCanvasGroup.interactable = false;
            settingsCanvasGroup.blocksRaycasts = false;
            settingsCanvasGroup.alpha = 0f;
            settingsPanelTransform.localScale = new Vector3(0.95f, 0.95f, 1f);

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

    private IEnumerator TransitionToMainMenuRoutine()
    {
        _isTransitioning = true;

        // 1. If Settings is currently open, perform the premium UI fade-out first
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
            // Just close the other standard menus instantly
            if (settingsOverlay != null) settingsOverlay.SetActive(false);
            if (panelSaveLoad != null) panelSaveLoad.SetActive(false);
            if (panelQuitConfirm != null) panelQuitConfirm.SetActive(false);
        }

        // 2. Cinematic Glass Restore
        if (voidBackground != null) StartCoroutine(FadeCanvasGroup(voidBackground, 1f, 0f, 0.25f));
        if (shatterAnimator != null) yield return StartCoroutine(shatterAnimator.RestoreRoutine());

        // 3. Restore the Main Menu buttons
        if (panelMainMenu != null) panelMainMenu.SetActive(true);
        _isTransitioning = false;
    }

    public void ConfirmQuit()
    {
        Debug.Log("<color=red>SYSTEM TERMINATED.</color> Exiting application...");
        Application.Quit();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
    #endregion

    #region Helpers & Diagnostics
    private void SyncBiometrics()
    {
        if (inventory == null)
        {
            Debug.LogWarning("DIAGNOSTIC: InventoryGrid reference is missing!");
            return;
        }

        float corruptionCount = inventory.GetTotalCorruptedSlots(); 
        float corruptionPct = Mathf.Clamp01(corruptionCount / 100f);

        HorrorProxyButton[] buttons = pauseMenuUI.GetComponentsInChildren<HorrorProxyButton>(true);
        foreach (HorrorProxyButton btn in buttons) 
        {
            btn.corruptionPercent = corruptionPct;
            btn.SendMessage("DrawFlowingLine", SendMessageOptions.DontRequireReceiver);
        }
    }

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
    #endregion
}
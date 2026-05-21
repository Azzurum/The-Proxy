using UnityEngine;
using System.Collections;

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
            MeshRenderer[] shardRenderers = shatteredGlassVisuals.GetComponentsInChildren<MeshRenderer>(true);
            foreach (MeshRenderer shard in shardRenderers)
            {
                if (shard.gameObject.name == "Pause_Background") continue;
                shard.material.mainTexture = _pauseScreenTexture; 
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
        yield return new WaitForSecondsRealtime(shatterAnimator.animationDuration + 0.1f);
        
        shatteredGlassVisuals.SetActive(false);
        pauseMenuUI.gameObject.SetActive(false);
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
        
        if (panelMainMenu != null) panelMainMenu.SetActive(true);
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
        if (voidBackground != null) StartCoroutine(FadeCanvasGroup(voidBackground, 0f, 1f, 0.2f));
        if (shatterAnimator != null) yield return StartCoroutine(shatterAnimator.BlowbackRoutine());

        if (subMenuPanel != null) subMenuPanel.SetActive(true);
        _isTransitioning = false;
    }

    /// <summary>
    /// Coroutine for the unique animated transition into the Settings menu.
    /// </summary>
    private IEnumerator AnimateSettingsInRoutine()
    {
        _isTransitioning = true;
        if (panelMainMenu != null) panelMainMenu.SetActive(false);

        if (voidBackground != null) StartCoroutine(FadeCanvasGroup(voidBackground, 0f, 1f, 0.2f));
        if (shatterAnimator != null) yield return StartCoroutine(shatterAnimator.BlowbackRoutine());

        if (settingsOverlay != null) settingsOverlay.SetActive(true);

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
        if (voidBackground != null) StartCoroutine(FadeCanvasGroup(voidBackground, 1f, 0f, 0.25f));
        if (shatterAnimator != null) yield return StartCoroutine(shatterAnimator.RestoreRoutine());

        if (panelMainMenu != null) panelMainMenu.SetActive(true);
        _isTransitioning = false;
    }

    /// <summary>
    /// Called by the 'Quit Game' button to close the application.
    /// </summary>
    public void ConfirmQuit()
    {
        Debug.Log("<color=red>SYSTEM TERMINATED.</color> Exiting application...");
        Application.Quit();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
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
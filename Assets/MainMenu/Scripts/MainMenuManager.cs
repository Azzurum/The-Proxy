using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; 

/// <summary>
/// Handles all Main Menu navigation, UI transitions, and system boot sequences.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Save/Load Memory UI")]
    [Tooltip("The main container for the Save/Load interface.")]
    public RectTransform panelSaveLoad;
    [Tooltip("The dark overlay behind the Save/Load menu.")]
    public GameObject darkBlocker;
    
    [Header("Settings / RIG Interface UI")]
    [Tooltip("The container holding the main menu text buttons.")]
    public GameObject menuMatrix;      
    [Tooltip("The master object for the Settings Overlay prefab.")]
    public GameObject settingsOverlay; 
    [Tooltip("Controls the master opacity fade for the settings menu.")]
    public CanvasGroup settingsCanvasGroup;
    [Tooltip("The physical switchboard panel that scales up during boot.")]
    public RectTransform settingsPanelTransform; 

    // ==========================================
    // INITIALIZATION & SCENE LOADING
    // ==========================================

    public void StartNewRun()
    {
        Debug.Log("SYSTEM BOOT: Loading First Level...");
        SceneManager.LoadScene("MainGame"); 
    }

    // ==========================================
    // MEMORY ACCESS (SAVE / LOAD)
    // ==========================================

    public void OpenLoadGame()
    {
        Debug.Log("ACCESSING MEMORY: Opening Load Menu...");
        
        if (panelSaveLoad != null) panelSaveLoad.gameObject.SetActive(true);
        if (darkBlocker != null) darkBlocker.SetActive(true);
    }

    public void CloseLoadGame()
    {
        Debug.Log("CLOSING MEMORY...");
        StartCoroutine(CloseMenuRoutine());
    }

    private IEnumerator CloseMenuRoutine()
    {
        // 1. Trigger custom exit animations (Uses the ?. operator to prevent errors if missing)
        if (panelSaveLoad != null) panelSaveLoad.GetComponent<UIPanelAnimator>()?.SlideOut();
        if (darkBlocker != null) darkBlocker.GetComponent<UIBlockerAnimator>()?.FadeOut();

        // 2. Wait 0.25 seconds for the animations to finish (unaffected by Time.timeScale)
        yield return new WaitForSecondsRealtime(0.25f);

        // 3. Fully disable the objects once they are off-screen
        if (panelSaveLoad != null) panelSaveLoad.gameObject.SetActive(false); 
        if (darkBlocker != null) darkBlocker.SetActive(false); 
    }

    // ==========================================
    // RIG CALIBRATION (SETTINGS)
    // ==========================================

    public void OpenSettings()
    {
        Debug.Log("CALIBRATING: Booting RIG Interface...");
        StartCoroutine(AnimateSettingsIn());
    }

    public void CloseSettings()
    {
        Debug.Log("CALIBRATION COMPLETE: Shutting down interface...");
        StartCoroutine(AnimateSettingsOut());
    }

    private IEnumerator AnimateSettingsIn()
    {
        // 1. Hide main menu text, activate the settings overlay object
        if (menuMatrix != null) menuMatrix.SetActive(false);
        if (settingsOverlay != null) settingsOverlay.SetActive(true);

        // Safety check to prevent crashing if references aren't assigned in the Inspector
        if (settingsCanvasGroup == null || settingsPanelTransform == null)
        {
            Debug.LogWarning("Settings animation components missing! Snapping menu open instantly.");
            yield break;
        }

        // 2. Lock UI interaction during the transition so the player can't click invisible buttons
        settingsCanvasGroup.interactable = false;
        settingsCanvasGroup.blocksRaycasts = false;
        
        // 3. Set starting state: completely transparent and slightly scaled down (95%)
        settingsCanvasGroup.alpha = 0f;
        settingsPanelTransform.localScale = new Vector3(0.95f, 0.95f, 1f);

        // 4. Perform the smooth Lerp transition
        float timeElapsed = 0;
        float duration = 0.25f;

        while (timeElapsed < duration)
        {
            // Calculate progress as a percentage (0.0 to 1.0)
            float t = timeElapsed / duration;
            
            settingsCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            float scale = Mathf.Lerp(0.95f, 1f, t);
            settingsPanelTransform.localScale = new Vector3(scale, scale, 1f);

            // Use unscaledDeltaTime so the menu still animates even if the game is Paused
            timeElapsed += Time.unscaledDeltaTime; 
            yield return null;
        }

        // 5. Ensure final values are perfectly set to 100% and unlock interaction
        settingsCanvasGroup.alpha = 1f;
        settingsPanelTransform.localScale = Vector3.one;
        settingsCanvasGroup.interactable = true;
        settingsCanvasGroup.blocksRaycasts = true;
    }

    private IEnumerator AnimateSettingsOut()
    {
        if (settingsCanvasGroup == null || settingsPanelTransform == null)
        {
            if (settingsOverlay != null) settingsOverlay.SetActive(false);
            if (menuMatrix != null) menuMatrix.SetActive(true);
            yield break;
        }

        // 1. Lock UI interaction immediately upon clicking return
        settingsCanvasGroup.interactable = false;
        settingsCanvasGroup.blocksRaycasts = false;

        // 2. Perform the reverse transition (slightly faster at 0.2s for a snappier exit)
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

        // 3. Hide the overlay entirely and restore the main menu text matrix
        if (settingsOverlay != null) settingsOverlay.SetActive(false);
        if (menuMatrix != null) menuMatrix.SetActive(true);
    }

    // ==========================================
    // SYSTEM SHUTDOWN
    // ==========================================

    public void ExitGame()
    {
        Debug.Log("SYSTEM SHUTDOWN: Quitting Game...");
        
        // This pre-processor directive allows the Quit button to work inside the Unity Editor
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles the visual fade-out sequence and restart logic when the player fails the game.
/// </summary>
public class GameOverScreen : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The CanvasGroup component used to fade the entire game over screen.")]
    public CanvasGroup canvasGroup;
    [Tooltip("How long in seconds it takes for the screen to fully fade to black.")]
    public float fadeDuration = 2.5f;

    void Awake()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// Activates the UI element and begins the fade-to-black coroutine.
    /// </summary>
    public void TriggerGameOver()
    {
        gameObject.SetActive(true); 

        if (canvasGroup != null)
        {
            StartCoroutine(FadeToBlack());
        }
    }

    private IEnumerator FadeToBlack()
    {
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true; 
        
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            yield return null;
        }

        if (canvasGroup != null) {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true; 
        }
    }

    /// <summary>
    /// Reloads the currently active scene, functionally restarting the game from the last checkpoint or beginning.
    /// </summary>
    public void Button_RestartShift()
    {
        // Reset timescale to 1 before reloading, otherwise the new scene might be permanently frozen!
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
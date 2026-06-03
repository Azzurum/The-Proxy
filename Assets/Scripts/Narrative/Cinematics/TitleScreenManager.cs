using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Coordinates transitions from the Main Menu into the active gameplay scenes.
/// </summary>
public class TitleScreenManager : MonoBehaviour
{
    [Header("Scene Configuration")]
    [Tooltip("The exact name of the scene to load when clicking New Game.")]
    public string newGameSceneName = "Intro_EarthOffice";

    private void Start()
    {
        StartCoroutine(FadeInSequence());
    }

    private System.Collections.IEnumerator FadeInSequence()
    {
        GameObject fadeObj = new GameObject("FadeInCanvas");
        Canvas canvas = fadeObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;
        
        UnityEngine.UI.Image fadeImage = fadeObj.AddComponent<UnityEngine.UI.Image>();
        fadeImage.rectTransform.anchorMin = Vector2.zero;
        fadeImage.rectTransform.anchorMax = Vector2.one;
        fadeImage.rectTransform.offsetMin = Vector2.zero;
        fadeImage.rectTransform.offsetMax = Vector2.zero;
        fadeImage.color = new Color(0, 0, 0, 1); // Start pitch black
        fadeImage.raycastTarget = false;
        
        float duration = 1.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeImage.color = new Color(0, 0, 0, 1f - Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        
        Destroy(fadeObj); // Clean up the canvas when done!
    }

    /// <summary>
    /// Triggers the transition to the specified introductory scene.
    /// </summary>
    public void StartNewGame()
    {
        StartCoroutine(TransitionSequence());
    }

    private System.Collections.IEnumerator TransitionSequence()
    {
        // Creates a smooth fade-to-black when clicking New Game on the Main Menu
        GameObject fadeObj = new GameObject("FadeCanvas");
        Canvas canvas = fadeObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;
        
        UnityEngine.UI.Image fadeImage = fadeObj.AddComponent<UnityEngine.UI.Image>();
        fadeImage.rectTransform.anchorMin = Vector2.zero;
        fadeImage.rectTransform.anchorMax = Vector2.one;
        fadeImage.rectTransform.offsetMin = Vector2.zero;
        fadeImage.rectTransform.offsetMax = Vector2.zero;
        fadeImage.color = new Color(0, 0, 0, 0);
        fadeImage.raycastTarget = true;
        
        float duration = 1.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeImage.color = new Color(0, 0, 0, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        
        SceneManager.LoadScene(newGameSceneName);
    }
}
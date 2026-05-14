using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverScreen : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup canvasGroup;
    public float fadeDuration = 2.5f;

    void Start()
    {
        // Ensure the screen is invisible and unclickable when the game starts
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void TriggerGameOver()
    {
        // THE FIX: Force the object to turn on so the Coroutine is allowed to run!
        gameObject.SetActive(true); 

        if (canvasGroup != null)
        {
            StartCoroutine(FadeToBlack());
        }
    }

    private IEnumerator FadeToBlack()
    {
        // Block player clicks while it fades
        canvasGroup.blocksRaycasts = true; 
        
        float elapsedTime = 0f;

        // Smoothly increase the alpha to 1 (solid black)
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true; // Turn the Restart button on!
    }

    // Connect this to your Btn_Restart!
    public void Button_RestartShift()
    {
        // This grabs whatever scene you are currently in and reloads it fresh
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
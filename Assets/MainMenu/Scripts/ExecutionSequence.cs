using UnityEngine;
using UnityEngine.UI;
using System.Collections;
// using UnityEngine.SceneManagement; // Uncomment this later to load your actual game scene!

public class ExecutionSequence : MonoBehaviour
{
    public static ExecutionSequence Instance;

    [Header("UI Elements")]
    public CanvasGroup menuMatrixGroup;
    public Image flashbangOverlay;
    public Image centerTear; // Optional: to fade out the tear

    [Header("Audio")]
    public AudioSource fatalAudio; // Drop your heavy bass sound here

    private bool isExecuting = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void TriggerExecution()
    {
        // Prevent clicking multiple times
        if (isExecuting) return;
        isExecuting = true;

        // Start the cinematic timeline
        StartCoroutine(ExecutionRoutine());
    }

    IEnumerator ExecutionRoutine()
    {
        // 1. MAX STRESS & FATAL AUDIO
        StressSystem.Instance.SetTargetStress(1.0f);
        if (fatalAudio != null) fatalAudio.Play();

        // 2. FADE OUT THE MENU (0.5 seconds)
        float t = 0;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            menuMatrixGroup.alpha = 1f - (t / 0.5f);
            if (centerTear != null) centerTear.color = new Color(1, 1, 1, menuMatrixGroup.alpha);
            yield return null;
        }
        
        // Disable the menu entirely so we can't hover anymore
        menuMatrixGroup.gameObject.SetActive(false);

        // 3. THE FLASHBANG STRIKE (Instant White)
        flashbangOverlay.color = new Color(1, 1, 1, 1);
        yield return new WaitForSeconds(0.1f); // Hold the flash for a split second

        // 4. THE CRUSH (Fade from White to Pitch Black over 2 seconds)
        t = 0;
        while (t < 2.0f)
        {
            t += Time.deltaTime;
            flashbangOverlay.color = Color.Lerp(Color.white, Color.black, t / 2.0f);
            yield return null;
        }

        // Wait a moment in the dark for dramatic effect
        yield return new WaitForSeconds(1.0f);

        Debug.Log("SYSTEM EXECUTED. READY TO LOAD LEVEL.");
        // SceneManager.LoadScene("YourLevelName"); // This is how you will load the game later!
    }
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class CreditsDirector : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform creditsScrollTarget; 
    public TextMeshProUGUI creditsText;
    public Image fadeOverlay;
    
    [Header("Settings")]
    public float scrollSpeed = 45f;
    public float startDelay = 2f;
    public float endDelay = 3f;
    public string mainMenuSceneName = "MainMenu"; // Make sure this matches your main menu!

    private AudioSource musicSource;
    private bool isSkipping = false;
    private bool isTransitioning = false;

    void Start()
    {
        StartCoroutine(CreditsSequence());
    }

    void Update()
    {
        if (isTransitioning) return;

        // Hold Space, Enter, or Left Click to fast-forward
        if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Return) || Input.GetMouseButton(0))
        {
            isSkipping = true;
        }
        else
        {
            isSkipping = false;
        }

        // Press Escape to skip entirely
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            StopAllCoroutines();
            StartCoroutine(FadeAndExit());
        }
    }

    private IEnumerator CreditsSequence()
    {
        // 1. Initial Setup
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            fadeOverlay.color = Color.black;
        }

        if (creditsScrollTarget != null)
        {
            // FOOLPROOF MATH: Force the anchor and pivot to Bottom-Center so the math always works
            creditsScrollTarget.anchorMin = new Vector2(0.5f, 0f);
            creditsScrollTarget.anchorMax = new Vector2(0.5f, 0f);
            creditsScrollTarget.pivot = new Vector2(0.5f, 0f);
            
            float contentHeight = 3000f;
            if (creditsText != null) 
            {
                creditsText.ForceMeshUpdate();
                contentHeight = creditsText.preferredHeight;
            }

            // Start the text exactly below the bottom edge of the screen
            creditsScrollTarget.anchoredPosition = new Vector2(0f, -contentHeight - 50f);
        }

        // Procedural Ambient Music (Deep, space-like drone)
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.clip = ProceduralAudioGen.GenerateHiss(10f); // Creates a deep void hum
        musicSource.pitch = 0.3f;
        musicSource.loop = true;
        musicSource.volume = 0f;
        musicSource.Play();

        // 2. Fade In
        float timer = 0f;
        while (timer < 3f)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, 0.6f, timer / 3f);
            if (fadeOverlay != null) fadeOverlay.color = Color.Lerp(Color.black, Color.clear, timer / 3f);
            yield return null;
        }
        if (fadeOverlay != null) fadeOverlay.gameObject.SetActive(false);

        yield return new WaitForSeconds(startDelay);

        // 3. Scroll the Text
        if (creditsScrollTarget != null && creditsText != null)
        {
            // Get the actual height of the canvas to know when the text has fully left the top of the screen
            float canvasHeight = Screen.height;
            Canvas canvas = creditsScrollTarget.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                if (canvasRect != null) canvasHeight = canvasRect.rect.height;
            }

            float targetY = canvasHeight + 100f;

            while (creditsScrollTarget.anchoredPosition.y < targetY)
            {
                float currentSpeed = isSkipping ? scrollSpeed * 5f : scrollSpeed;
                creditsScrollTarget.anchoredPosition += Vector2.up * currentSpeed * Time.deltaTime;
                yield return null;
            }
        }

        yield return new WaitForSeconds(endDelay);

        // 4. Fade Out
        yield return StartCoroutine(FadeAndExit());
    }

    private IEnumerator FadeAndExit()
    {
        isTransitioning = true;

        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            float timer = 0f;
            while (timer < 3f)
            {
                timer += Time.deltaTime;
                fadeOverlay.color = Color.Lerp(Color.clear, Color.black, timer / 3f);
                if (musicSource != null) musicSource.volume = Mathf.Lerp(0.6f, 0f, timer / 3f);
                yield return null;
            }
        }

        // The Final Post-Credits Stinger!
        AudioSource sfx = gameObject.AddComponent<AudioSource>();
        sfx.PlayOneShot(ProceduralAudioGen.GenerateStaticGlitch(1.5f), 1f);
        
        yield return new WaitForSeconds(2f);

        // Load Main Menu or quit
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            try { SceneManager.LoadScene(mainMenuSceneName); }
            catch { Debug.LogWarning($"Could not load scene {mainMenuSceneName}. Ensure it's in Build Settings."); }
        }
    }

    [ContextMenu("Auto-Fill Masterpiece Credits")]
    private void AutoFillCredits()
    {
        if (creditsText != null)
        {
            creditsText.alignment = TextAlignmentOptions.Center;
            creditsText.lineSpacing = 15f;
            creditsText.richText = true;
            
            creditsText.text = 
                "<size=120%><color=#FFB300><b>THE PROXY</b></color></size>\n\n\n\n\n\n" +
                "<color=#aaaaaa>A SURVIVAL HORROR EXPERIENCE BY</color>\n\n\n" +
                "<b>Ken Adrien Arceno</b>\n\n" +
                "<b>Simoun Andreo Supnet</b>\n\n" +
                "<b>Kylle Jasen Punongbayan</b>\n\n" +
                "<b>Jhik Javier</b>\n\n" +
                "<b>Lane Danniesh Bonus</b>\n\n\n\n\n\n\n\n" +
                "<color=#aaaaaa>SPECIAL THANKS TO</color>\n\n" +
                "Aether-Core Logistics\n" +
                "USC Wayfarer Crew\n\n\n\n\n\n\n\n" +
                "<i>Thank you for playing.</i>\n\n\n\n\n\n\n\n\n\n" +
                "<color=#E63946><size=80%>SYSTEM.LOG // CONNECTION SEVERED.</size></color>";
                
            Debug.Log("Credits text beautifully formatted and injected!");
        }
        else
        {
            Debug.LogWarning("Please assign a TextMeshProUGUI to 'Credits Text' before auto-filling!");
        }
    }
}
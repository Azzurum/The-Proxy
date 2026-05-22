using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

/// <summary>
/// Coordinates the smooth scrolling and scene transition for the final credits sequence.
/// </summary>
public class CreditsDirector : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The RectTransform anchored at the bottom to slide upwards.")]
    public RectTransform creditsScrollTarget; 
    public TextMeshProUGUI creditsText;
    public Image fadeOverlay;
    
    [Header("Settings")]
    [Tooltip("Speed in pixels per second that the text scrolls.")]
    public float scrollSpeed = 45f;
    public float startDelay = 2f;
    public float endDelay = 3f;
    [Tooltip("The exact string name of the main menu scene.")]
    public string mainMenuSceneName = "MainMenu"; 

    private AudioSource _musicSource;
    private bool _isSkipping = false;
    private bool _isTransitioning = false;

    private void Start()
    {
        StartCoroutine(CreditsSequence());
    }

    private void Update()
    {
        if (_isTransitioning) return;

        _isSkipping = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Return) || Input.GetMouseButton(0);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            StopAllCoroutines();
            StartCoroutine(FadeAndExit());
        }
    }

    private IEnumerator CreditsSequence()
    {
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            fadeOverlay.color = Color.black;
        }

        if (creditsScrollTarget != null)
        {
            creditsScrollTarget.anchorMin = new Vector2(0.5f, 0f);
            creditsScrollTarget.anchorMax = new Vector2(0.5f, 0f);
            creditsScrollTarget.pivot = new Vector2(0.5f, 0f);
            
            float contentHeight = 3000f;
            if (creditsText != null) 
            {
                creditsText.ForceMeshUpdate();
                contentHeight = creditsText.preferredHeight;
            }

            creditsScrollTarget.anchoredPosition = new Vector2(0f, -contentHeight - 50f);
        }

        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.clip = ProceduralAudioGen.GenerateHiss(10f); 
        _musicSource.pitch = 0.3f;
        _musicSource.loop = true;
        _musicSource.volume = 0f;
        _musicSource.Play();

        float timer = 0f;
        while (timer < 3f)
        {
            timer += Time.deltaTime;
            _musicSource.volume = Mathf.Lerp(0f, 0.6f, timer / 3f);
            if (fadeOverlay != null) fadeOverlay.color = Color.Lerp(Color.black, Color.clear, timer / 3f);
            yield return null;
        }
        if (fadeOverlay != null) fadeOverlay.gameObject.SetActive(false);

        yield return new WaitForSeconds(startDelay);

        if (creditsScrollTarget != null && creditsText != null)
        {
            float canvasHeight = Screen.height;
            Canvas canvas = creditsScrollTarget.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                if (canvas.TryGetComponent<RectTransform>(out var canvasRect)) canvasHeight = canvasRect.rect.height;
            }

            float targetY = canvasHeight + 100f;

            while (creditsScrollTarget.anchoredPosition.y < targetY)
            {
                float currentSpeed = _isSkipping ? scrollSpeed * 5f : scrollSpeed;
                creditsScrollTarget.anchoredPosition += Vector2.up * currentSpeed * Time.deltaTime;
                yield return null;
            }
        }

        yield return new WaitForSeconds(endDelay);

        yield return StartCoroutine(FadeAndExit());
    }

    private IEnumerator FadeAndExit()
    {
        _isTransitioning = true;

        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            float timer = 0f;
            while (timer < 3f)
            {
                timer += Time.deltaTime;
                fadeOverlay.color = Color.Lerp(Color.clear, Color.black, timer / 3f);
                if (_musicSource != null) _musicSource.volume = Mathf.Lerp(0.6f, 0f, timer / 3f);
                yield return null;
            }
        }

        AudioSource sfx = gameObject.AddComponent<AudioSource>();
        sfx.PlayOneShot(ProceduralAudioGen.GenerateStaticGlitch(1.5f), 1f);
        
        yield return new WaitForSeconds(2f);

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
        }
    }
}
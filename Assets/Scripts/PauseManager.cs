using UnityEngine;
using System.Collections;

public class PauseManager : MonoBehaviour
{
    [Header("Visual Connections")]
    public CanvasGroup pauseMenuUI;
    public GameObject shatteredGlassVisuals;
    
    [Header("Audio SFX")]
    public AudioSource audioSource;
    public AudioClip SND_UI_Menu_Shatter; 
    public float fadeDuration = 0.5f;     
    private float _originalVolume;        

    [Header("Game Integration")]
    public GameObject[] elementsToHide;

    [Header("Scripts")]
    public ShatterAnimator shatterAnimator;
    public ProceduralGlassGenerator glassGenerator;

    private RenderTexture _pauseScreenTexture;
    private bool _isPaused = false;
    private bool _isAnimating = false; 
    private Coroutine _fadeCoroutine;

    void Start()
    {
        // 1. Prepare Audio
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource != null) 
        {
            audioSource.playOnAwake = false;
            _originalVolume = audioSource.volume;
            // Force the audio to load now, not when the menu opens
            if (SND_UI_Menu_Shatter != null) SND_UI_Menu_Shatter.LoadAudioData();
        }

        // 2. Pre-allocate the Texture (Size of the screen)
        _pauseScreenTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        _pauseScreenTexture.Create();

        // 3. Pre-generate shards
        if (glassGenerator != null) glassGenerator.GenerateShards();

        // 4. Set Initial States
        if (shatteredGlassVisuals != null) shatteredGlassVisuals.SetActive(false);
        pauseMenuUI.alpha = 0f;
        pauseMenuUI.interactable = false;
        pauseMenuUI.blocksRaycasts = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
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

        // OPTIMIZATION: Take the capture immediately before the fade wait if possible
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshotIntoRenderTexture(_pauseScreenTexture);

        foreach (GameObject obj in elementsToHide) if (obj != null) obj.SetActive(false);

        if (shatteredGlassVisuals != null)
        {
            MeshRenderer[] shardRenderers = shatteredGlassVisuals.GetComponentsInChildren<MeshRenderer>(true);
            foreach (MeshRenderer shard in shardRenderers)
            {
                if (shard.gameObject.name == "Pause_Background") continue; 
                // Using the optimized property ID is faster than string names
                shard.material.mainTexture = _pauseScreenTexture;
            }
        }

        // --- START FADE IN ---
        if (audioSource != null && SND_UI_Menu_Shatter != null)
        {
            audioSource.clip = SND_UI_Menu_Shatter;
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            audioSource.volume = 0; // Start at zero for the fade
            audioSource.Play();
            _fadeCoroutine = StartCoroutine(FadeAudio(0f, _originalVolume, fadeDuration));
        }

        shatteredGlassVisuals.SetActive(true);
        shatterAnimator.InitializeShards();
        
        // Minor Delay: Letting the GPU catch up before freezing time
        yield return null; 

        Time.timeScale = 0f;
        shatterAnimator.PlayShatter();
        
        pauseMenuUI.interactable = true;
        pauseMenuUI.blocksRaycasts = true;
        pauseMenuUI.alpha = 1f; // Ensure it's visible

        yield return new WaitForSecondsRealtime(shatterAnimator.animationDuration);
        _isAnimating = false; 
    }

    // ... (Keep ExecuteResumeRoutine and FadeAudio exactly as they were) ...
    private IEnumerator ExecuteResumeRoutine()
    {
        _isAnimating = true; 
        if (audioSource != null && audioSource.isPlaying)
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeAudio(audioSource.volume, 0f, fadeDuration));
        }

        Time.timeScale = 1f;
        pauseMenuUI.interactable = false;
        pauseMenuUI.blocksRaycasts = false;
        shatterAnimator.PlayAssemble();
        
        yield return new WaitForSeconds(shatterAnimator.animationDuration + 0.1f);
        
        shatteredGlassVisuals.SetActive(false);
        foreach (GameObject obj in elementsToHide) if (obj != null) obj.SetActive(true);

        _isAnimating = false; 
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

    void OnDestroy()
    {
        if (_pauseScreenTexture != null)
        {
            _pauseScreenTexture.Release();
            Destroy(_pauseScreenTexture);
        }
    }
}
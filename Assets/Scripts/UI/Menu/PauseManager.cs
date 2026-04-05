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
    [Tooltip("Drag UI to hide (Stamina, etc) here so it doesn't float over the glass.")]
    public GameObject[] elementsToHide;
    
    [Tooltip("How long the slow-motion 'bullet time' lasts after the menu closes.")]
    public float timeReentryDuration = 0.5f;

    [Header("Scripts")]
    public ShatterAnimator shatterAnimator;
    public ProceduralGlassGenerator glassGenerator;
    public InventoryGrid inventory; // Drag MainRig_Grid here

    private RenderTexture _pauseScreenTexture;
    private bool _isPaused = false;
    private bool _isAnimating = false; 
    private Coroutine _fadeCoroutine;
    private Coroutine _timeCoroutine; 

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource != null) 
        {
            audioSource.playOnAwake = false;
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
        
        if (_timeCoroutine != null) StopCoroutine(_timeCoroutine);

        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshotIntoRenderTexture(_pauseScreenTexture);

        foreach (GameObject obj in elementsToHide) if (obj != null) obj.SetActive(false);

        if (shatteredGlassVisuals != null)
        {
            MeshRenderer[] shardRenderers = shatteredGlassVisuals.GetComponentsInChildren<MeshRenderer>(true);
            foreach (MeshRenderer shard in shardRenderers)
            {
                if (shard.gameObject.name == "Pause_Background") continue; 
                shard.material.mainTexture = _pauseScreenTexture; 
            }
        }

        // --- BIOMETRIC LINK LOGIC ---
        pauseMenuUI.gameObject.SetActive(true); 

        if (inventory != null)
        {
            float corruptionCount = inventory.GetTotalCorruptedSlots(); 
            float corruptionPct = Mathf.Clamp01(corruptionCount / 100f);

            // 🛑 DIAGNOSTIC LINE 1: How much corruption does it see?
            Debug.Log($"<color=red>DIAGNOSTIC:</color> I see {corruptionCount} corrupted blocks!");

            HorrorProxyButton[] buttons = pauseMenuUI.GetComponentsInChildren<HorrorProxyButton>(true);
            
            // 🛑 DIAGNOSTIC LINE 2: How many buttons did it find?
            Debug.Log($"<color=cyan>DIAGNOSTIC:</color> I found {buttons.Length} HorrorProxyButtons!");

            foreach (HorrorProxyButton btn in buttons) 
            {
                btn.corruptionPercent = corruptionPct;
                btn.SendMessage("DrawFlowingLine", SendMessageOptions.DontRequireReceiver); 
            }
        }
        else
        {
            Debug.LogWarning("DIAGNOSTIC: I cannot see the InventoryGrid! The slot is empty!");
        }

        if (audioSource != null && SND_UI_Menu_Shatter != null)
        {
            audioSource.clip = SND_UI_Menu_Shatter;
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            audioSource.volume = 0; 
            audioSource.Play();
            _fadeCoroutine = StartCoroutine(FadeAudio(0f, _originalVolume, fadeDuration));
        }
    
        shatteredGlassVisuals.SetActive(true);
        shatterAnimator.InitializeShards();
        yield return null; 

        Time.timeScale = 0f;
        shatterAnimator.PlayShatter(); 
        
        yield return new WaitForSecondsRealtime(shatterAnimator.animationDuration);
        pauseMenuUI.interactable = true;
        pauseMenuUI.blocksRaycasts = true;

        _isAnimating = false; 
    }

    private IEnumerator ExecuteResumeRoutine()
    {
        _isAnimating = true; 
        
        if (audioSource != null && audioSource.isPlaying)
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeAudio(audioSource.volume, 0f, fadeDuration));
        }

        Time.timeScale = 0.05f; 
        
        pauseMenuUI.interactable = false;
        pauseMenuUI.blocksRaycasts = false;
        
        shatterAnimator.PlayAssemble();
        
        yield return new WaitForSecondsRealtime(shatterAnimator.animationDuration + 0.1f);
        
        shatteredGlassVisuals.SetActive(false);
        pauseMenuUI.gameObject.SetActive(false);
        
        foreach (GameObject obj in elementsToHide) if (obj != null) obj.SetActive(true);

        if (_timeCoroutine != null) StopCoroutine(_timeCoroutine);
        _timeCoroutine = StartCoroutine(LerpTimeScale(0.05f, 1f, timeReentryDuration));

        _isAnimating = false; 
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

    void OnDestroy()
    {
        if (_pauseScreenTexture != null)
        {
            _pauseScreenTexture.Release();
            Destroy(_pauseScreenTexture);
        }
    }
}
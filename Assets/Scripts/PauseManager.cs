using UnityEngine;
using System.Collections;

public class PauseManager : MonoBehaviour
{
    [Header("Visual Connections")]
    public CanvasGroup pauseMenuUI;
    public GameObject shatteredGlassVisuals;
    
    [Header("Game Integration")]
    [Tooltip("Drag EVERYTHING you want to disappear during pause here (Stamina Bar, HUD, etc.)")]
    public GameObject[] elementsToHide;

    [Header("Scripts")]
    public ShatterAnimator shatterAnimator;
    public ProceduralGlassGenerator glassGenerator;

    private RenderTexture _pauseScreenTexture;
    private bool _isPaused = false;
    private bool _isAnimating = false; 

    void Start()
    {
        _pauseScreenTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        _pauseScreenTexture.Create();

        if (glassGenerator != null) glassGenerator.GenerateShards();

        shatteredGlassVisuals.SetActive(false);
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

        // 1. Wait and take the picture
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshotIntoRenderTexture(_pauseScreenTexture);

        // 2. Hide ALL live UI so it doesn't float over the glass
        foreach (GameObject obj in elementsToHide)
        {
            if (obj != null) obj.SetActive(false);
        }

        // 3. Paint the shards
        if (shatteredGlassVisuals != null)
        {
            MeshRenderer[] shardRenderers = shatteredGlassVisuals.GetComponentsInChildren<MeshRenderer>(true);
            foreach (MeshRenderer shard in shardRenderers)
            {
                // Never paint the background!
                if (shard.gameObject.name == "Pause_Background") continue; 
                shard.material.SetTexture("_BaseMap", _pauseScreenTexture);
                shard.material.SetTexture("_MainTex", _pauseScreenTexture);
            }
        }

        shatteredGlassVisuals.SetActive(true);
        shatterAnimator.InitializeShards();

        Time.timeScale = 0f;
        shatterAnimator.PlayShatter();
        
        pauseMenuUI.interactable = true;
        pauseMenuUI.blocksRaycasts = true;

        yield return new WaitForSecondsRealtime(shatterAnimator.animationDuration);
        _isAnimating = false; 
    }

    private IEnumerator ExecuteResumeRoutine()
    {
        _isAnimating = true; 

        Time.timeScale = 1f;
        pauseMenuUI.interactable = false;
        pauseMenuUI.blocksRaycasts = false;
        
        shatterAnimator.PlayAssemble();
        
        yield return new WaitForSeconds(shatterAnimator.animationDuration + 0.1f);
        
        shatteredGlassVisuals.SetActive(false);
        
        // Bring all the live UI back!
        foreach (GameObject obj in elementsToHide)
        {
            if (obj != null) obj.SetActive(true);
        }

        _isAnimating = false; 
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
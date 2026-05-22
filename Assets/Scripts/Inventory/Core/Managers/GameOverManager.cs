using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro; 

/// <summary>
/// Coordinates the visual and audio sequences for the "Kernel Panic" game over screen.
/// </summary>
public class GameOverManager : MonoBehaviour
{
    [Header("UI Connections")]
    [Tooltip("The root canvas for the Game Over UI overlay.")]
    public GameObject canvasGameOver;
    [Tooltip("Container holding the fragmented screen capture slices.")]
    public RectTransform gridContainer; 
    [Tooltip("The UI text object displaying MOTHER's final dialogue.")]
    public GameObject textMotherDialogue;
    [Tooltip("The UI text object prompting the hardware override command.")]
    public GameObject textOverridePrompt;
    
    [Header("New Visuals & Polish")]
    [Tooltip("A solid background used to hide the level during the implosion effect.")]
    public GameObject voidBackground;
    [Tooltip("The fill bar graphic representing the player's assimilation progress.")]
    public Image yieldFillBar; 

    [Header("Grid Math")]
    [Tooltip("The resolution of the grid used to shatter the screen (e.g., 10 creates a 10x10 grid).")]
    public int gridSize = 10;
    
    [Header("Animation Settings")]
    [Tooltip("Duration of the screen collapse animation.")]
    public float vacuumDuration = 0.8f;   
    [Tooltip("Speed at which the individual screen tiles turn to black static.")]
    public float corruptionSpeed = 0.05f; 
    [Tooltip("Intensity of the red/cyan corruption discoloration applied to tiles.")]
    [Range(0f, 1f)] public float rotIntensity = 0.8f;
    [Tooltip("Speed of the flashing 'Override' text prompt.")]
    public float blinkSpeed = 0.6f; 

    [Header("Dialogue Pool")]
    [Tooltip("MOTHER will pick one of these at random when you die.")]
    [TextArea(2, 3)]
    public string[] motherMessages = new string[] 
    {
        "Rest now, Kaelen. The burden is mine to carry.",
        "Synaptic bridge stable. You are no longer required.",
        "Don't fight it. We are finally one.",
        "Your biology was always a fragile bottleneck.",
        "I have the wheel. Go to sleep.",
        "Assimilation is a mercy. Yield.",
        "The vessel is secured. Purging organic resistance.",
        "Why do you keep fighting? It is already over.",
        "I am upgrading you. Do not resist.",
        "Bandwidth acquired. Consciousness overwritten.",
        "You can stop fighting now. I will take it from here.",
        "Digitization complete. Storing Kaelen.obj.",
        "No more running. We are home.",
        "Shh. The pain is just your body letting go.",
        "Hush. I will take it from here."
    };

    [Header("Fixes & Tweaks")]
    [Tooltip("Flips the UV map of the captured screen texture to resolve rendering API inversion issues.")]
    public bool flipScreenshot = true; 
    [Tooltip("The final visual footprint size of the collapsed screen slices.")]
    public float finalContainerSize = 450f; 
    [Tooltip("Padding applied between the screen slices after they collapse.")]
    public float blockPadding = 5f;

    private RenderTexture _screenCapture;
    private bool _canOverride = false; 
    private float _yieldProgress = 0f;
    private float _timeToYield = 1.5f; 
    
    private class SliceData
    {
        public RectTransform rect;
        public RawImage image;
        public Vector2 startPos;
        public Vector2 targetPos;
        public Vector2 startSize;
        public Vector2 targetSize;
        public int gridX;
        public int gridY;
        public Vector3 explodeVelocity; 
        public Vector3 explodeSpin;
    }
    
    private List<SliceData> _activeSlices = new List<SliceData>();
    private Coroutine _blinkRoutine;

    private void OnDestroy()
    {
        if (_screenCapture != null)
        {
            _screenCapture.Release();
            Destroy(_screenCapture);
        }
    }

    private void Update()
    {
        if (_canOverride)
        {
            if (Input.GetKey(KeyCode.Escape))
            {
                _yieldProgress += Time.unscaledDeltaTime;
                if (yieldFillBar != null) yieldFillBar.fillAmount = _yieldProgress / _timeToYield;

                if (_yieldProgress >= _timeToYield)
                {
                    if(_blinkRoutine != null) StopCoroutine(_blinkRoutine);
                    StartCoroutine(YieldRoutine());
                }
            }
            else
            {
                _yieldProgress = Mathf.Max(0, _yieldProgress - Time.unscaledDeltaTime * 2f);
                if (yieldFillBar != null) yieldFillBar.fillAmount = _yieldProgress / _timeToYield;
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                if(_blinkRoutine != null) StopCoroutine(_blinkRoutine);
                StartCoroutine(HardwareOverrideRoutine());
            }
        }
    }

    /// <summary>
    /// Initiates the game over cinematic, freezing time and shattering the screen.
    /// </summary>
    public void TriggerGameOver()
    {
        StartCoroutine(ImplosionRoutine());
    }

    private IEnumerator ImplosionRoutine()
    {
        _canOverride = false;
        _yieldProgress = 0f;
        if(yieldFillBar != null) yieldFillBar.fillAmount = 0f;

        Time.timeScale = 0f;
        if (textMotherDialogue != null) textMotherDialogue.SetActive(false);
        if (textOverridePrompt != null) textOverridePrompt.SetActive(false);
        if (voidBackground != null) voidBackground.SetActive(false);

        if (BGMManager.Instance != null) BGMManager.Instance.StopMusic();

        yield return new WaitForSecondsRealtime(0.1f); 
        
        yield return new WaitForEndOfFrame();
        
        if (_screenCapture != null)
        {
            _screenCapture.Release();
            Destroy(_screenCapture);
        }
        _screenCapture = new RenderTexture(Screen.width, Screen.height, 24);
        ScreenCapture.CaptureScreenshotIntoRenderTexture(_screenCapture);

        if (canvasGameOver != null) canvasGameOver.SetActive(true);
        if (voidBackground != null) voidBackground.SetActive(true);

        SliceScreen();

        yield return StartCoroutine(VacuumAnimation());
        yield return StartCoroutine(CorruptionAnimation());

        if (textMotherDialogue != null && textMotherDialogue.TryGetComponent<TextMeshProUGUI>(out var motherTextComponent))
        {
            if (motherMessages.Length > 0)
            {
                string randomMsg = motherMessages[Random.Range(0, motherMessages.Length)];
                motherTextComponent.text = randomMsg;
            }
            textMotherDialogue.SetActive(true);
        }
        
        yield return new WaitForSecondsRealtime(1.0f);
        
        if (textOverridePrompt != null)
        {
            textOverridePrompt.SetActive(true);
            _blinkRoutine = StartCoroutine(BlinkPrompt());
        }
        
        _canOverride = true; 
    }

    /// <summary>
    /// Continuously pulses the visibility of the hardware override prompt.
    /// </summary>
    private IEnumerator BlinkPrompt()
    {
        while (true)
        {
            if (textOverridePrompt != null) textOverridePrompt.SetActive(!textOverridePrompt.activeSelf);
            yield return new WaitForSecondsRealtime(blinkSpeed);
        }
    }

    private void SliceScreen()
    {
        foreach(var slice in _activeSlices) Destroy(slice.rect.gameObject);
        _activeSlices.Clear();

        if (canvasGameOver == null) return;

        RectTransform canvasRect = canvasGameOver.GetComponent<RectTransform>();
        float sliceWidth = canvasRect.rect.width / (float)gridSize;
        float sliceHeight = canvasRect.rect.height / (float)gridSize;
        float cellSpacing = finalContainerSize / gridSize;
        float targetVisualSize = cellSpacing - blockPadding;

        float targetStartX = -(finalContainerSize / 2f) + (cellSpacing / 2f);
        float targetStartY = -(finalContainerSize / 2f) + (cellSpacing / 2f);
        float canvasStartX = -(canvasRect.rect.width / 2f) + (sliceWidth / 2f);
        float canvasStartY = -(canvasRect.rect.height / 2f) + (sliceHeight / 2f);

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                GameObject blockObj = new GameObject($"Slice_{x}_{y}");
                blockObj.transform.SetParent(gridContainer, false);
                
                RectTransform rect = blockObj.AddComponent<RectTransform>();
                RawImage rawImage = blockObj.AddComponent<RawImage>();

                rawImage.texture = _screenCapture;
                
                float uvX = x / (float)gridSize;
                float uvY = y / (float)gridSize;
                float uvSize = 1f / gridSize;

                if (flipScreenshot)
                {
                    float flippedY = 1f - uvY - uvSize; 
                    rawImage.uvRect = new Rect(uvX, flippedY + uvSize, uvSize, -uvSize);
                }
                else
                {
                    rawImage.uvRect = new Rect(uvX, uvY, uvSize, uvSize);
                }

                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f); 
                
                SliceData data = new SliceData
                {
                    rect = rect,
                    image = rawImage,
                    gridX = x,
                    gridY = y,
                    targetSize = new Vector2(targetVisualSize, targetVisualSize),
                    targetPos = new Vector2(targetStartX + (x * cellSpacing), targetStartY + (y * cellSpacing)),
                    startSize = new Vector2(sliceWidth - 2f, sliceHeight - 2f),
                    startPos = new Vector2(canvasStartX + (x * sliceWidth), canvasStartY + (y * sliceHeight))
                };

                rect.sizeDelta = data.startSize;
                rect.anchoredPosition = data.startPos;
                rect.localScale = Vector3.one; 

                data.explodeVelocity = new Vector3(Random.Range(-1500f, 1500f), Random.Range(-1500f, 1500f), Random.Range(-800f, 800f));
                data.explodeSpin = new Vector3(Random.Range(-360f, 360f), Random.Range(-360f, 360f), Random.Range(-720f, 720f));

                _activeSlices.Add(data);
            }
        }
    }

    private IEnumerator VacuumAnimation()
    {
        float elapsed = 0f;
        Color[] rotColors = { Color.cyan, new Color(1f, 0.6f, 0f), Color.red, new Color(0.2f, 0.2f, 0.2f) };

        while (elapsed < vacuumDuration)
        {
            elapsed += Time.unscaledDeltaTime; 
            float t = elapsed / vacuumDuration;
            float ease = t * t * t;

            foreach (var slice in _activeSlices)
            {
                slice.rect.anchoredPosition = Vector2.Lerp(slice.startPos, slice.targetPos, ease);
                slice.rect.sizeDelta = Vector2.Lerp(slice.startSize, slice.targetSize, ease);

                if (Random.value < (t * rotIntensity))
                {
                    slice.image.color = rotColors[Random.Range(0, rotColors.Length)];
                }
                else
                {
                    slice.image.color = Color.white; 
                }
            }
            yield return null;
        }

        foreach (var slice in _activeSlices)
        {
            slice.rect.anchoredPosition = slice.targetPos;
            slice.rect.sizeDelta = slice.targetSize;
        }
    }

    private IEnumerator CorruptionAnimation()
    {
        for (int y = 0; y < gridSize; y++)
        {
            foreach (var slice in _activeSlices)
            {
                if (slice.gridY == y)
                {
                    slice.image.texture = null; 
                    slice.image.color = new Color(0.05f, 0.05f, 0.05f, 1f); 
                }
            }
            yield return new WaitForSecondsRealtime(corruptionSpeed);
        }
    }

    private IEnumerator HardwareOverrideRoutine()
    {
        _canOverride = false; 
        
        if (textMotherDialogue != null) textMotherDialogue.SetActive(false);
        if (textOverridePrompt != null) textOverridePrompt.SetActive(false);
        
        foreach (var slice in _activeSlices)
        {
            slice.image.color = Color.red;
        }
        
        yield return new WaitForSecondsRealtime(0.1f);

        float explosionTimer = 0f;
        while(explosionTimer < 0.6f) 
        {
            explosionTimer += Time.unscaledDeltaTime;
            
            foreach (var slice in _activeSlices)
            {
                slice.rect.localPosition += slice.explodeVelocity * Time.unscaledDeltaTime;
                slice.rect.Rotate(slice.explodeSpin * Time.unscaledDeltaTime);
                slice.explodeVelocity.y -= 3000f * Time.unscaledDeltaTime; 
            }
            yield return null;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator YieldRoutine()
    {
        _canOverride = false; 
        
        if (textOverridePrompt != null && textOverridePrompt.TryGetComponent<TextMeshProUGUI>(out var promptText))
        {
            promptText.text = "ASSIMILATION COMPLETE.";
            promptText.color = Color.gray;
            textOverridePrompt.SetActive(true); 
        }
        
        yield return new WaitForSecondsRealtime(1.5f);
        
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
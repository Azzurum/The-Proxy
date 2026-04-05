using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro; // NEW: We must include the TextMeshPro dictionary!

public class GameOverManager : MonoBehaviour
{
    [Header("UI Connections")]
    public GameObject canvasGameOver;
    public RectTransform gridContainer; 
    public GameObject textMotherDialogue;
    public GameObject textOverridePrompt;
    
    [Header("New Visuals & Polish")]
    public GameObject voidBackground;
    public Image yieldFillBar; 

    [Header("Grid Math")]
    public int gridSize = 10;
    
    [Header("Animation Settings")]
    public float vacuumDuration = 0.8f;   
    public float corruptionSpeed = 0.05f; 
    [Range(0f, 1f)] public float rotIntensity = 0.8f;
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
    public bool flipScreenshot = true; 
    public float finalContainerSize = 450f; 
    public float blockPadding = 5f;

    private RenderTexture screenCapture;
    private bool canOverride = false; 
    private float yieldProgress = 0f;
    private float timeToYield = 1.5f; 
    
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
    
    private List<SliceData> activeSlices = new List<SliceData>();
    private Coroutine blinkRoutine;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K)) TriggerGameOver();

        if (canOverride)
        {
            if (Input.GetKey(KeyCode.Escape))
            {
                yieldProgress += Time.unscaledDeltaTime;
                yieldFillBar.fillAmount = yieldProgress / timeToYield;

                if (yieldProgress >= timeToYield)
                {
                    if(blinkRoutine != null) StopCoroutine(blinkRoutine);
                    StartCoroutine(YieldRoutine());
                }
            }
            else
            {
                yieldProgress = Mathf.Max(0, yieldProgress - Time.unscaledDeltaTime * 2f);
                yieldFillBar.fillAmount = yieldProgress / timeToYield;
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                if(blinkRoutine != null) StopCoroutine(blinkRoutine);
                StartCoroutine(HardwareOverrideRoutine());
            }
        }
    }

    public void TriggerGameOver()
    {
        StartCoroutine(ImplosionRoutine());
    }

    private IEnumerator ImplosionRoutine()
    {
        canOverride = false;
        yieldProgress = 0f;
        if(yieldFillBar != null) yieldFillBar.fillAmount = 0f;

        Time.timeScale = 0f;
        textMotherDialogue.SetActive(false);
        textOverridePrompt.SetActive(false);
        voidBackground.SetActive(false);

        yield return new WaitForSecondsRealtime(0.1f); 
        
        yield return new WaitForEndOfFrame();
        screenCapture = new RenderTexture(Screen.width, Screen.height, 24);
        ScreenCapture.CaptureScreenshotIntoRenderTexture(screenCapture);

        canvasGameOver.SetActive(true);
        voidBackground.SetActive(true);

        SliceScreen();

        yield return StartCoroutine(VacuumAnimation());
        yield return StartCoroutine(CorruptionAnimation());

        // --- THE TEXTMESHPRO FIX ---
        // We now ask Unity specifically for the TextMeshProUGUI component!
        if (motherMessages.Length > 0 && textMotherDialogue.GetComponent<TextMeshProUGUI>() != null)
        {
            string randomMsg = motherMessages[Random.Range(0, motherMessages.Length)];
            textMotherDialogue.GetComponent<TextMeshProUGUI>().text = randomMsg;
        }

        textMotherDialogue.SetActive(true);
        
        yield return new WaitForSecondsRealtime(1.0f);
        
        textOverridePrompt.SetActive(true);
        blinkRoutine = StartCoroutine(BlinkPrompt());
        
        canOverride = true; 
    }

    private IEnumerator BlinkPrompt()
    {
        while (true)
        {
            textOverridePrompt.SetActive(!textOverridePrompt.activeSelf);
            yield return new WaitForSecondsRealtime(blinkSpeed);
        }
    }

    private void SliceScreen()
    {
        foreach(var slice in activeSlices) Destroy(slice.rect.gameObject);
        activeSlices.Clear();

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

                rawImage.texture = screenCapture;
                
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
                
                SliceData data = new SliceData();
                data.rect = rect;
                data.image = rawImage;
                data.gridX = x;
                data.gridY = y;

                data.targetSize = new Vector2(targetVisualSize, targetVisualSize);
                data.targetPos = new Vector2(targetStartX + (x * cellSpacing), targetStartY + (y * cellSpacing));
                data.startSize = new Vector2(sliceWidth - 2f, sliceHeight - 2f);
                data.startPos = new Vector2(canvasStartX + (x * sliceWidth), canvasStartY + (y * sliceHeight));

                rect.sizeDelta = data.startSize;
                rect.anchoredPosition = data.startPos;
                rect.localScale = Vector3.one; 

                data.explodeVelocity = new Vector3(Random.Range(-1500f, 1500f), Random.Range(-1500f, 1500f), Random.Range(-800f, 800f));
                data.explodeSpin = new Vector3(Random.Range(-360f, 360f), Random.Range(-360f, 360f), Random.Range(-720f, 720f));

                activeSlices.Add(data);
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

            foreach (var slice in activeSlices)
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

        foreach (var slice in activeSlices)
        {
            slice.rect.anchoredPosition = slice.targetPos;
            slice.rect.sizeDelta = slice.targetSize;
        }
    }

    private IEnumerator CorruptionAnimation()
    {
        for (int y = 0; y < gridSize; y++)
        {
            foreach (var slice in activeSlices)
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
        canOverride = false; 
        
        textMotherDialogue.SetActive(false);
        textOverridePrompt.SetActive(false);
        
        foreach (var slice in activeSlices)
        {
            slice.image.color = Color.red;
        }
        
        yield return new WaitForSecondsRealtime(0.1f);

        float explosionTimer = 0f;
        while(explosionTimer < 0.6f) 
        {
            explosionTimer += Time.unscaledDeltaTime;
            
            foreach (var slice in activeSlices)
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
        canOverride = false; 
        
        // --- TEXTMESHPRO FIX FOR THE PROMPT ---
        TextMeshProUGUI promptText = textOverridePrompt.GetComponent<TextMeshProUGUI>();
        if(promptText != null)
        {
            promptText.text = "ASSIMILATION COMPLETE.";
            promptText.color = Color.gray;
        }
        
        textOverridePrompt.SetActive(true); 
        
        yield return new WaitForSecondsRealtime(1.5f);
        
        Time.timeScale = 1f;
        // SceneManager.LoadScene("MainMenu"); 
        Debug.Log("PLAYER YIELDED. LOADING MAIN MENU...");
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(RawImage))]
public class UIEKGProcedural : MonoBehaviour
{
    private RawImage ekgImage;
    private InventoryManager invManager;
    
    [Header("Procedural Visuals")]
    [Tooltip("Change this to 512 in the Inspector for a smoother, high-res stretched line!")]
    public int textureWidth = 512;
    public int textureHeight = 64;
    public int lineThickness = 1; 

    [Header("Dynamic Colors")]
    public Color stableColor = new Color(0f, 1f, 0.8f); // Cyan
    public Color warningColor = new Color(1f, 0.8f, 0f); // Yellow/Amber
    public Color criticalColor = Color.red; // Red
    public Color deadColor = new Color(0.3f, 0f, 0f); // Dark Red Flatline
    private Color targetColor;
    private Color currentColor;

    [Header("Animation Settings")]
    public float baseSpeed = 0.5f; 
    private float targetSpeed;
    private float currentSpeed;
    private float erraticJitter = 0f;

    [Header("Heart Rate Text")]
    public TextMeshProUGUI bpmText;
    private float targetBPM = 62f;
    private float currentDisplayedBPM = 62f;

    [Header("Flatline Physics")]
    private float targetAmplitude = 1f;
    private float currentAmplitude = 1f;

    void Start()
    {
        ekgImage = GetComponent<RawImage>();
        
        targetSpeed = baseSpeed;
        currentSpeed = baseSpeed;
        targetColor = stableColor;
        currentColor = stableColor;
        
        GenerateEKGTexture();

        invManager = FindAnyObjectByType<InventoryManager>();
        if (invManager != null)
        {
            invManager.OnHealthStateChanged += UpdateEKGState;
            invManager.BroadcastHealthState(); 
        }
    }

    void OnDestroy()
    {
        if (invManager != null) invManager.OnHealthStateChanged -= UpdateEKGState;
    }

    public void UpdateEKGState(float healthPercentage)
    {
        float corruptionLevel = 1f - healthPercentage; 

        // --- NEW: THE DEATH STATE ---
        if (healthPercentage <= 0f) 
        {
            targetSpeed = baseSpeed * 0.5f; // Slow down the scrolling a bit for dramatic effect
            erraticJitter = 0f; // Instantly stop shaking
            targetColor = deadColor; // Fade to dark red
            targetBPM = 0f; // Drop heart rate to zero
            targetAmplitude = 0f; // TRIGGER THE FLATLINE COLLAPSE!
        }
        else if (corruptionLevel <= 0.2f) 
        {
            targetSpeed = baseSpeed + (corruptionLevel * 0.5f);
            erraticJitter = 0f;
            targetColor = stableColor;
            targetBPM = 62f + (corruptionLevel * 80f);
            targetAmplitude = 1f;
        }
        else if (corruptionLevel <= 0.6f) 
        {
            targetSpeed = baseSpeed + (corruptionLevel * 1.5f);
            erraticJitter = 0.05f; 
            targetColor = warningColor;
            targetBPM = 62f + (corruptionLevel * 80f);
            targetAmplitude = 1f;
        }
        else 
        {
            targetSpeed = baseSpeed + (corruptionLevel * 3.0f);
            erraticJitter = 0.15f; 
            targetColor = criticalColor;
            targetBPM = 62f + (corruptionLevel * 80f);
            targetAmplitude = 1f;
        }
    }

    void Update()
    {
        // 1. Smoothly roll the BPM numbers to 0
        currentDisplayedBPM = Mathf.Lerp(currentDisplayedBPM, targetBPM, 5f * Time.deltaTime);
        if (bpmText != null) bpmText.text = Mathf.RoundToInt(currentDisplayedBPM).ToString();

        // 2. Smoothly shift colors and speed
        currentColor = Color.Lerp(currentColor, targetColor, 3f * Time.deltaTime);
        if (ekgImage != null) ekgImage.color = currentColor;
        if (bpmText != null) bpmText.color = currentColor;

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, 3f * Time.deltaTime);

        // 3. THE FLATLINE ANIMATOR
        // Only regenerate the texture if the amplitude is actively shrinking or growing
        if (Mathf.Abs(currentAmplitude - targetAmplitude) > 0.005f)
        {
            currentAmplitude = Mathf.Lerp(currentAmplitude, targetAmplitude, 4f * Time.deltaTime);
            GenerateEKGTexture();
        }
        else if (currentAmplitude != targetAmplitude)
        {
            // Snap to perfect 0 or 1 when it gets close enough to stop wasting performance
            currentAmplitude = targetAmplitude;
            GenerateEKGTexture(); 
        }

        // 4. Slide the UVs to make it scroll
        if (ekgImage != null && ekgImage.texture != null)
        {
            Rect currentUV = ekgImage.uvRect;
            currentUV.x += currentSpeed * Time.deltaTime;

            // Apply Jitter (Will be 0 if dead)
            if (erraticJitter > 0f) currentUV.y = Random.Range(-erraticJitter, erraticJitter);
            else currentUV.y = 0f;

            ekgImage.uvRect = currentUV;
        }
    }

    private void GenerateEKGTexture()
    {
        Texture2D tex = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point; 
        tex.wrapMode = TextureWrapMode.Repeat; 

        Color32[] pixels = new Color32[textureWidth * textureHeight];
        Color32 clear = new Color32(0, 0, 0, 0);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
        tex.SetPixels32(pixels);

        Color32 drawColor = Color.white; 
        int baselineY = textureHeight / 2; 

        int p_start_x = (int)(textureWidth * 0.20f);
        int p_peak_x = (int)(textureWidth * 0.23f);
        int p_end_x = (int)(textureWidth * 0.26f);
        int qrs_start_x = (int)(textureWidth * 0.38f);
        int r_peak_x = (int)(textureWidth * 0.41f); 
        int s_dip_x = (int)(textureWidth * 0.44f);
        int qrs_end_x = (int)(textureWidth * 0.47f);
        int t_start_x = (int)(textureWidth * 0.55f);
        int t_peak_x = (int)(textureWidth * 0.58f);
        int t_end_x = (int)(textureWidth * 0.61f);

        int[] yValues = new int[textureWidth];
        for (int i = 0; i < textureWidth; i++) yValues[i] = baselineY; 

        // --- DYNAMIC AMPLITUDE MATH ---
        // Multiply the height of every spike by currentAmplitude. 
        // If currentAmplitude is 0, these all equal baselineY (a flat line!)
        int pWaveY = baselineY + (int)(textureHeight * 0.10f * currentAmplitude);
        int tWaveY = baselineY + (int)(textureHeight * 0.20f * currentAmplitude);
        int qDipY = baselineY - (int)(textureHeight * 0.15f * currentAmplitude);
        int rPeakY = baselineY + (int)(textureHeight * 0.45f * currentAmplitude); 
        int sDipY = baselineY - (int)(textureHeight * 0.35f * currentAmplitude);

        for (int x = p_start_x; x <= p_peak_x; x++) { float t = (float)(x - p_start_x) / (p_peak_x - p_start_x); yValues[x] = (int)Mathf.Lerp(baselineY, pWaveY, t); }
        for (int x = p_peak_x; x <= p_end_x; x++) { float t = (float)(x - p_peak_x) / (p_end_x - p_peak_x); yValues[x] = (int)Mathf.Lerp(pWaveY, baselineY, t); }

        for (int x = t_start_x; x <= t_peak_x; x++) { float t = (float)(x - t_start_x) / (t_peak_x - t_start_x); yValues[x] = (int)Mathf.Lerp(baselineY, tWaveY, t); }
        for (int x = t_peak_x; x <= t_end_x; x++) { float t = (float)(x - t_peak_x) / (t_end_x - t_peak_x); yValues[x] = (int)Mathf.Lerp(tWaveY, baselineY, t); }
        
        for (int x = qrs_start_x; x < r_peak_x; x++) { float t = (float)(x - qrs_start_x) / (r_peak_x - qrs_start_x); yValues[x] = (int)Mathf.Lerp(qDipY, rPeakY, t); }
        yValues[r_peak_x] = rPeakY; 
        for (int x = r_peak_x + 1; x <= s_dip_x; x++) { float t = (float)(x - r_peak_x) / (s_dip_x - r_peak_x); yValues[x] = (int)Mathf.Lerp(rPeakY, sDipY, t); }
        for (int x = s_dip_x + 1; x <= qrs_end_x; x++) { float t = (float)(x - s_dip_x) / (qrs_end_x - s_dip_x); yValues[x] = (int)Mathf.Lerp(sDipY, baselineY, t); }

        int lastY = baselineY; 
        for (int x = 0; x < textureWidth; x++)
        {
            int currentY = yValues[x];
            int startY = Mathf.Min(lastY, currentY);
            int endY = Mathf.Max(lastY, currentY);

            for (int y = startY; y <= endY; y++)
            {
                for (int t = 0; t < lineThickness; t++)
                {
                    if (y + t < textureHeight) tex.SetPixel(x, y + t, drawColor);
                }
            }
            lastY = currentY; 
        }

        tex.Apply();
        ekgImage.texture = tex;
    }
}
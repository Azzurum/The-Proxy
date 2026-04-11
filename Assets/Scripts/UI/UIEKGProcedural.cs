using UnityEngine;
using UnityEngine.UI;
using TMPro; // <-- Added the TextMeshPro library!

[RequireComponent(typeof(RawImage))]
public class UIEKGProcedural : MonoBehaviour
{
    private RawImage ekgImage;
    
    [Header("Procedural Visuals")]
    [Tooltip("Width of the generated texture. Higher = smoother/longer line.")]
    public int textureWidth = 256;
    [Tooltip("Height of the generated texture. Higher = taller spikes.")]
    public int textureHeight = 64;
    public Color lineColor = Color.white;
    public int lineThickness = 2;

    [Header("Animation Settings")]
    public float baseSpeed = 0.5f; 
    private float currentSpeed;

    [Header("Heart Rate Text")]
    public TextMeshProUGUI bpmText; // <-- Changed this to TextMeshProUGUI!
    private int baseBPM = 62;

    void Awake()
    {
        ekgImage = GetComponent<RawImage>();
        currentSpeed = baseSpeed;
        
        GenerateEKGTexture();
    }

    private void GenerateEKGTexture()
    {
        Texture2D tex = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Point; 

        Color32[] pixels = new Color32[textureWidth * textureHeight];
        Color32 clear = new Color32(0, 0, 0, 0);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
        tex.SetPixels32(pixels);

        Color32 drawColor = lineColor;
        int baselineY = textureHeight / 2; 
        int lastY = baselineY;

        for (int x = 0; x < textureWidth; x++)
        {
            float normalizedX = (float)x / textureWidth;
            int currentY = baselineY;

            if (normalizedX > 0.1f && normalizedX < 0.15f) 
                currentY = baselineY + (int)(Mathf.Sin((normalizedX - 0.1f) * 20f * Mathf.PI) * textureHeight * 0.15f);
            else if (normalizedX > 0.2f && normalizedX < 0.22f) 
                currentY = baselineY - (int)(textureHeight * 0.1f);
            else if (normalizedX >= 0.22f && normalizedX < 0.26f) 
                currentY = baselineY + (int)(textureHeight * 0.4f);
            else if (normalizedX >= 0.26f && normalizedX < 0.3f) 
                currentY = baselineY - (int)(textureHeight * 0.3f);
            else if (normalizedX > 0.4f && normalizedX < 0.5f) 
                currentY = baselineY + (int)(Mathf.Sin((normalizedX - 0.4f) * 10f * Mathf.PI) * textureHeight * 0.2f);

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

    void Update()
    {
        if (ekgImage != null && ekgImage.texture != null)
        {
            Rect currentUV = ekgImage.uvRect;
            currentUV.x += currentSpeed * Time.deltaTime;
            ekgImage.uvRect = currentUV;
        }
    }

    public void SetThreatLevel(float threatPercentage)
    {
        currentSpeed = baseSpeed + (threatPercentage * 1.5f);

        if (bpmText != null)
        {
            int currentBPM = Mathf.FloorToInt(baseBPM + (threatPercentage * 80f));
            bpmText.text = currentBPM.ToString();
            
            if (threatPercentage > 0.8f) bpmText.color = Color.red;
            else bpmText.color = new Color(0f, 1f, 0.8f); 
        }
    }
}
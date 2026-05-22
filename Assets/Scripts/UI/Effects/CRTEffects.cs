using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applies visual CRT monitor effects including alpha flickering and a scrolling scanline.
/// </summary>
public class CRTEffects : MonoBehaviour
{
    [Header("CRT Flicker")]
    [Tooltip("The UI Image component used as the screen overlay.")]
    public Image flickerImage;
    [Tooltip("The minimum alpha transparency during a flicker.")]
    public float minAlpha = 0.02f; 
    [Tooltip("The maximum alpha transparency during a flicker.")]
    public float maxAlpha = 0.08f; 
    [Tooltip("The time in seconds between each flicker update.")]
    public float flickerSpeed = 0.15f; 

    [Header("Scanline Sweep")]
    [Tooltip("The RectTransform of the scanline image.")]
    public RectTransform scanlineRect;
    [Tooltip("How fast the scanline moves down the screen.")]
    public float sweepSpeed = 150f;
    [Tooltip("The Y position where the scanline starts its sweep (usually off-screen top).")]
    public float topResetY = 200f;      
    [Tooltip("The Y position where the scanline resets (usually off-screen bottom).")]
    public float bottomResetY = -1200f; 

    private float _flickerTimer;

    private void Start()
    {
        _flickerTimer = flickerSpeed;
        if (scanlineRect != null) scanlineRect.anchoredPosition = new Vector2(0, topResetY);
    }

    private void Update()
    {
        if (flickerImage != null)
        {
            _flickerTimer -= Time.unscaledDeltaTime; 
            if (_flickerTimer <= 0)
            {
                Color c = flickerImage.color;
                c.a = Random.Range(minAlpha, maxAlpha);
                flickerImage.color = c;
                
                _flickerTimer = flickerSpeed;
            }
        }

        if (scanlineRect != null)
        {
            scanlineRect.anchoredPosition += Vector2.down * sweepSpeed * Time.unscaledDeltaTime;

            if (scanlineRect.anchoredPosition.y < bottomResetY)
            {
                scanlineRect.anchoredPosition = new Vector2(0, topResetY);
            }
        }
    }
}
using UnityEngine;
using UnityEngine.UI;

public class CRTEffects : MonoBehaviour
{
    [Header("CRT Flicker")]
    public Image flickerImage;
    public float minAlpha = 0.02f; // Slight dark
    public float maxAlpha = 0.08f; // Slightly darker
    public float flickerSpeed = 0.15f; // Matches HTML 0.15s animation

    [Header("Scanline Sweep")]
    public RectTransform scanlineRect;
    public float sweepSpeed = 150f;
    public float topResetY = 200f;      // Starts slightly off-screen
    public float bottomResetY = -1200f; // Drops off the bottom of the screen

    private float _flickerTimer;

    void Start()
    {
        _flickerTimer = flickerSpeed;
        if (scanlineRect != null) scanlineRect.anchoredPosition = new Vector2(0, topResetY);
    }

    void Update()
    {
        // --- 1. THE FLICKER ---
        if (flickerImage != null)
        {
            _flickerTimer -= Time.unscaledDeltaTime; // Unscaled so it works while paused!
            if (_flickerTimer <= 0)
            {
                // Randomly shift the alpha to create a mechanical buzzing flicker
                Color c = flickerImage.color;
                c.a = Random.Range(minAlpha, maxAlpha);
                flickerImage.color = c;
                
                _flickerTimer = flickerSpeed;
            }
        }

        // --- 2. THE SCANLINE CRAWL ---
        if (scanlineRect != null)
        {
            // Move it down steadily
            scanlineRect.anchoredPosition += Vector2.down * sweepSpeed * Time.unscaledDeltaTime;

            // If it falls off the bottom of the monitor, loop it back to the top
            if (scanlineRect.anchoredPosition.y < bottomResetY)
            {
                scanlineRect.anchoredPosition = new Vector2(0, topResetY);
            }
        }
    }
}
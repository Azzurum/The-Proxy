using UnityEngine;
using UnityEngine.UI;

public class ScreenEffectManager : MonoBehaviour
{
    public static ScreenEffectManager Instance;

    [Header("Visual Warning")]
    public Image screenTint; // Assign a full-screen Image with red/black color
    public float pulseSpeed = 2f;
    public float maxAlpha = 0.5f;

    private bool isWarningActive = false;
    private float pulseTimer = 0f;
    
    private float flashTimer = 0f;
    private float flashDuration = 0f;
    private Color flashColor;
    private Color baseColor;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Cache the original warning color set in the Inspector
        if (screenTint != null) baseColor = screenTint.color;
    }

    void Update()
    {
        // 1. Flash Override
        if (flashTimer > 0)
        {
            flashTimer -= Time.deltaTime;
            float t = flashTimer / flashDuration;
            
            if (screenTint != null)
            {
                Color c = flashColor;
                c.a = Mathf.Lerp(0, flashColor.a, t); // Fade out the flash
                screenTint.color = c;
            }
            
            // Keep the heartbeat running in the background so it doesn't skip a beat
            if (isWarningActive) pulseTimer += Time.deltaTime * pulseSpeed;
            return;
        }

        // 2. Standard Warning Pulse
        if (isWarningActive)
        {
            pulseTimer += Time.deltaTime * pulseSpeed;
            float alpha = Mathf.Sin(pulseTimer) * 0.5f + 0.5f; // Oscillates 0-1
            if (screenTint != null)
            {
                Color color = baseColor;
                color.a = alpha * maxAlpha;
                screenTint.color = color;
            }
        }
        else
        {
            if (screenTint != null)
            {
                Color color = baseColor;
                color.a = 0f;
                screenTint.color = color;
            }
        }
    }

    public void SetWarning(bool active)
    {
        isWarningActive = active;
        if (!active) pulseTimer = 0f;
    }

    public void TriggerFlash(Color color, float duration)
    {
        flashColor = color;
        flashDuration = duration;
        flashTimer = duration;
    }
}
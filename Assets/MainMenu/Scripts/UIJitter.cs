using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class UIJitter : MonoBehaviour
{
    [Header("Core References")]
    [Tooltip("The duplicate line acting as the glowing background.")]
    public RectTransform glowRect;
    [Tooltip("The Image component of the glowing line.")]
    public Image glowImage;

    [Header("Jitter Settings (The Shake)")]
    public float calmJitterAmount = 1f;
    public float panicJitterAmount = 6f;
    [Tooltip("How often the UI snaps to a new position (in seconds).")]
    public float calmJitterSpeed = 0.15f;
    public float panicJitterSpeed = 0.04f;
    
    [Header("The Glitch Factor")]
    [Tooltip("Chance (0 to 1) that the line takes a massive, violent jump.")]
    [Range(0f, 1f)] public float glitchChance = 0.05f;
    public float glitchMultiplier = 2.5f;

    [Header("Ooze Settings (The Glow)")]
    public float calmOozeSpeed = 2f;
    public float panicOozeSpeed = 12f;
    
    [Space(10)]
    [Range(0f, 1f)] public float minAlpha = 0.05f;
    [Range(0f, 1f)] public float maxAlpha = 0.8f;
    
    [Space(10)]
    [Tooltip("How far the light stretches outward horizontally.")]
    public float maxPulseScale = 1.4f;

    // Internal Variables
    private RectTransform mainRect;
    private Vector2 startPos;
    private float jitterTimer;
    private float oozeTimer;

    void Start()
    {
        // Automatically grab the RectTransform this script is attached to
        mainRect = GetComponent<RectTransform>();
        startPos = mainRect.anchoredPosition;
    }

    void Update()
    {
        // 1. READ THE ROOM (Get the current stress level safely)
        float stress = (StressSystem.Instance != null) ? StressSystem.Instance.currentStress : 0.1f;

        // Calculate current speeds based on how stressed the menu is
        float currentJitterSpeed = Mathf.Lerp(calmJitterSpeed, panicJitterSpeed, stress);
        float currentJitterAmount = Mathf.Lerp(calmJitterAmount, panicJitterAmount, stress);
        float currentOozeSpeed = Mathf.Lerp(calmOozeSpeed, panicOozeSpeed, stress);

        HandleJitter(currentJitterSpeed, currentJitterAmount);
        HandleOoze(currentOozeSpeed);
    }

    private void HandleJitter(float speed, float amount)
    {
        // Use unscaledDeltaTime so the menu glitches even if the game is paused!
        jitterTimer += Time.unscaledDeltaTime;
        
        if (jitterTimer >= speed)
        {
            jitterTimer = 0f;

            // Roll the dice for a massive glitch snap
            float finalAmount = amount;
            if (Random.value <= glitchChance)
            {
                finalAmount *= glitchMultiplier;
            }

            // Jump left or right by the calculated pixels
            float randomX = Random.Range(-finalAmount, finalAmount);
            
            // Subtly jump up and down as well for more chaos
            float randomY = Random.Range(-finalAmount * 0.2f, finalAmount * 0.2f); 

            mainRect.anchoredPosition = startPos + new Vector2(randomX, randomY);
        }
    }

    private void HandleOoze(float speed)
    {
        if (glowRect == null || glowImage == null) return;

        // Advance the breathing timer
        oozeTimer += Time.unscaledDeltaTime * speed;

        // Use a Sine wave to smoothly breathe in and out (-1 to 1)
        float sineValue = Mathf.Sin(oozeTimer); 
        
        // Convert the wave into a clean 0 to 1 percentage for lerping
        float percentage = (sineValue + 1f) / 2f; 

        // 1. Throbbing Transparency
        Color c = glowImage.color;
        c.a = Mathf.Lerp(minAlpha, maxAlpha, percentage);
        glowImage.color = c;

        // 2. Throbbing Scale (Stretching outward)
        float currentScale = Mathf.Lerp(1f, maxPulseScale, percentage);
        glowRect.localScale = new Vector3(currentScale, 1f, 1f); 
    }
}
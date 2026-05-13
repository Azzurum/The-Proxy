using UnityEngine;
using UnityEngine.UI;

public class LightFlicker : MonoBehaviour
{
    [Header("References")]
    public CanvasGroup lightGroup; 
    public Image ambientFlare;     
    public CanvasGroup darknessOverlay;

    [Header("Color Settings")]
    public Color calmColor = new Color(1f, 0f, 0.2f, 0.3f); 
    public Color panicColor = new Color(1f, 0f, 0.1f, 0.8f); 

    [Header("Calm Timings (Normal)")]
    [Tooltip("Min and Max time for normal, quick flickers.")]
    public Vector2 quickFlickerDelay = new Vector2(0.05f, 0.4f);
    
    [Header("Calm Timings (The Long Hold)")]
    [Tooltip("Chance out of 1.0 that the light will stay on for a long time.")]
    [Range(0f, 1f)] public float longHoldChance = 0.05f; // 5% chance by default
    [Tooltip("Min and Max time the light stays stuck ON (e.g., 1 to 3 seconds).")]
    public Vector2 longHoldDuration = new Vector2(1f, 3f);

    [Header("Panic Timings (High Stress)")]
    [Tooltip("Min and Max time between aggressive strobe flashes.")]
    public Vector2 panicFlickerDelay = new Vector2(0.02f, 0.08f);

    private float timer;
    private float currentDelay;

    void Update()
    {
        // 1. Get the current stress safely
        float stress = (StressSystem.Instance != null) ? StressSystem.Instance.currentStress : 0.1f;

        // 2. Update the red flare color
        if (ambientFlare != null)
        {
            ambientFlare.color = Color.Lerp(calmColor, panicColor, stress);
        }

        // 3. Run the flicker clock
        timer += Time.deltaTime;
        if (timer >= currentDelay)
        {
            timer = 0f; // Reset the clock
            
            if (stress < 0.5f) ExecuteCalmFlicker();
            else ExecutePanicFlicker();
        }
    }

    private void ExecuteCalmFlicker()
    {
        // Roll the dice to see if we do a terrifying "Long Hold"
        if (Random.value <= longHoldChance)
        {
            SetLightState(1f); // Full bright flash
            
            // Pick a random time between 1 and 3 seconds
            currentDelay = Random.Range(longHoldDuration.x, longHoldDuration.y);
        }
        else
        {
            // Normal quick flickers and dark periods
            float rand = Random.value;
            if (rand > 0.8f) SetLightState(1f);        // Bright flash
            else if (rand > 0.5f) SetLightState(0.4f); // Dim spark
            else SetLightState(0f);                    // Pitch black
            
            // Pick a random quick delay
            currentDelay = Random.Range(quickFlickerDelay.x, quickFlickerDelay.y);
        }
    }

    private void ExecutePanicFlicker()
    {
        // PANIC STATE: Fast aggressive strobing
        SetLightState(Random.value > 0.3f ? 1f : 0.2f);
        currentDelay = Random.Range(panicFlickerDelay.x, panicFlickerDelay.y);
    }

    private void SetLightState(float lightIntensity)
    {
        // Set bulb brightness
        if (lightGroup != null) lightGroup.alpha = lightIntensity;

        // Invert darkness: Light at 1 = Darkness at 0
        if (darknessOverlay != null) darknessOverlay.alpha = 1f - lightIntensity;
    }
}
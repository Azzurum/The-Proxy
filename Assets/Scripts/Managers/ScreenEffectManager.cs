using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages global screen space effects like damage flashes and panic strobes.
/// </summary>
public class ScreenEffectManager : MonoBehaviour
{
    public static ScreenEffectManager Instance;

    [Header("Visual Warning")]
    [Tooltip("The full-screen UI Image used for coloring effects.")]
    public Image screenTint;
    [Tooltip("The speed at which the warning tint oscillates.")]
    public float pulseSpeed = 2f;
    [Tooltip("The maximum alpha transparency during a warning pulse.")]
    public float maxAlpha = 0.5f;

    private bool _isWarningActive = false;
    private float _pulseTimer = 0f;
    
    private float _flashTimer = 0f;
    private float _flashDuration = 0f;
    private Color _flashColor;
    private Color _baseColor;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (screenTint != null) _baseColor = screenTint.color;
    }

    private void Update()
    {
        if (_flashTimer > 0)
        {
            _flashTimer -= Time.deltaTime;
            float t = _flashTimer / _flashDuration;
            
            if (screenTint != null)
            {
                Color c = _flashColor;
                c.a = Mathf.Lerp(0, _flashColor.a, t); 
                screenTint.color = c;
            }
            
            if (_isWarningActive) _pulseTimer += Time.deltaTime * pulseSpeed;
            return;
        }

        if (_isWarningActive)
        {
            _pulseTimer += Time.deltaTime * pulseSpeed;
            float alpha = Mathf.Sin(_pulseTimer) * 0.5f + 0.5f; 
            if (screenTint != null)
            {
                Color color = _baseColor;
                color.a = alpha * maxAlpha;
                screenTint.color = color;
            }
        }
        else
        {
            if (screenTint != null)
            {
                Color color = _baseColor;
                color.a = 0f;
                screenTint.color = color;
            }
        }
    }

    /// <summary>
    /// Toggles the continuous pulsing warning state.
    /// </summary>
    public void SetWarning(bool active)
    {
        _isWarningActive = active;
        if (!active) _pulseTimer = 0f;
    }

    /// <summary>
    /// Triggers a temporary, fading screen flash of the specified color.
    /// </summary>
    public void TriggerFlash(Color color, float duration)
    {
        _flashColor = color;
        _flashDuration = duration;
        _flashTimer = duration;
    }
}
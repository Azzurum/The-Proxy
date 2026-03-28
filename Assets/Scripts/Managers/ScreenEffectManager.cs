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

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (isWarningActive)
        {
            pulseTimer += Time.deltaTime * pulseSpeed;
            float alpha = Mathf.Sin(pulseTimer) * 0.5f + 0.5f; // Oscillates 0-1
            if (screenTint != null)
            {
                Color color = screenTint.color;
                color.a = alpha * maxAlpha;
                screenTint.color = color;
            }
        }
        else
        {
            if (screenTint != null)
            {
                Color color = screenTint.color;
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
}
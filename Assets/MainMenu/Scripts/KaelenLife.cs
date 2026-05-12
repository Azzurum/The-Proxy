using UnityEngine;

public class KaelenLife : MonoBehaviour
{
    [Header("Body Parts")]
    public RectTransform chest;
    public RectTransform head;
    public RectTransform eyes; // NEW: The eye container
    
    [Header("Breathing Settings")]
    public float calmBreathSpeed = 2f;
    public float panicBreathSpeed = 8f;
    public float calmBreathAmount = 0.02f;
    public float panicBreathAmount = 0.08f;
    
    [Header("Parallax Settings")]
    public float headMoveAmount = 3f;   // Reduced from 15 so the neck doesn't break!
    public float chestMoveAmount = 1f;  // Reduced to stay attached
    public float eyeMoveAmount = 8f;    // NEW: Eyes move the most to create 3D depth
    public float headTiltAmount = 3f;   // NEW: Slight rotation for realism
    public float parallaxSmoothSpeed = 5f;

    private float breathTimer;
    private Vector2 headStartPos;
    private Vector2 chestStartPos;
    private Vector2 eyesStartPos;

    void Start()
    {
        if (head != null) headStartPos = head.anchoredPosition;
        if (chest != null) chestStartPos = chest.anchoredPosition;
        if (eyes != null) eyesStartPos = eyes.anchoredPosition;
    }

    void Update()
    {
        float stress = 0.1f;
        if (StressSystem.Instance != null)
        {
            stress = StressSystem.Instance.currentStress;
        }

        HandleBreathing(stress);
        HandleParallax();
    }

    void HandleBreathing(float stress)
    {
        if (chest == null) return;

        float currentSpeed = Mathf.Lerp(calmBreathSpeed, panicBreathSpeed, stress);
        float currentAmount = Mathf.Lerp(calmBreathAmount, panicBreathAmount, stress);

        breathTimer += Time.deltaTime * currentSpeed;
        float breathScale = 1f + (Mathf.Sin(breathTimer) * currentAmount);
        chest.localScale = new Vector3(1f, breathScale, 1f);
    }

    void HandleParallax()
    {
        Vector2 mousePos = Input.mousePosition;
        float normalizedX = (mousePos.x / Screen.width) * 2f - 1f;
        float normalizedY = (mousePos.y / Screen.height) * 2f - 1f;

        // 1. Move the Chest slightly
        if (chest != null)
        {
            Vector2 targetChestPos = chestStartPos + new Vector2(normalizedX * chestMoveAmount, 0f);
            chest.anchoredPosition = Vector2.Lerp(chest.anchoredPosition, targetChestPos, Time.deltaTime * parallaxSmoothSpeed);
        }

        // 2. Move and Tilt the Head
        if (head != null)
        {
            Vector2 targetHeadPos = headStartPos + new Vector2(normalizedX * headMoveAmount, normalizedY * (headMoveAmount * 0.5f));
            head.anchoredPosition = Vector2.Lerp(head.anchoredPosition, targetHeadPos, Time.deltaTime * parallaxSmoothSpeed);

            // Add a subtle head tilt!
            float targetRotation = -normalizedX * headTiltAmount; 
            head.localRotation = Quaternion.Lerp(head.localRotation, Quaternion.Euler(0, 0, targetRotation), Time.deltaTime * parallaxSmoothSpeed);
        }

        // 3. Move the Eyes the most (Simulates looking around inside the helmet)
        if (eyes != null)
        {
            Vector2 targetEyesPos = eyesStartPos + new Vector2(normalizedX * eyeMoveAmount, normalizedY * eyeMoveAmount);
            eyes.anchoredPosition = Vector2.Lerp(eyes.anchoredPosition, targetEyesPos, Time.deltaTime * parallaxSmoothSpeed);
        }
    }
}
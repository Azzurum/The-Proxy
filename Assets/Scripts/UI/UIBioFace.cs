using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIBioFace : MonoBehaviour
{
    [Header("Face UI References")]
    public Image faceImage;
    private RectTransform faceRect;
    private Vector2 originalPosition;
    private Vector3 originalScale;

    [Header("Kaelen's Face States")]
    public Sprite healthyFace;
    public Sprite hurtFace;
    public Sprite criticalFace;
    public Sprite deadFace;

    [Header("Juice Effects")]
    public Color damageColor = Color.red;
    private float previousHealth = 1f;
    private bool isCritical = false;
    private Coroutine shakeCoroutine;

    private InventoryManager invManager;

    void Start()
    {
        if (faceImage != null)
        {
            faceRect = faceImage.GetComponent<RectTransform>();
            originalPosition = faceRect.anchoredPosition;
            originalScale = faceRect.localScale;
        }

        invManager = FindAnyObjectByType<InventoryManager>();
        if (invManager != null)
        {
            invManager.OnHealthStateChanged += UpdateFace;
            invManager.BroadcastHealthState(); 
        }
    }

    void OnDestroy()
    {
        if (invManager != null) invManager.OnHealthStateChanged -= UpdateFace;
    }

    void Update()
    {
        // If Kaelen is in critical condition, make his face pulse like a heartbeat!
        if (isCritical && faceRect != null)
        {
            // Pulses scale between 1.0 and 1.05 based on time
            float pulse = 1f + (Mathf.Sin(Time.time * 5f) * 0.05f);
            faceRect.localScale = originalScale * pulse;
        }
        else if (faceRect != null && faceRect.localScale != originalScale)
        {
            // Reset scale when healed
            faceRect.localScale = originalScale;
        }
    }

    public void UpdateFace(float healthPercentage)
    {
        if (faceImage == null) return;

        // 1. Did we just take damage? Trigger the Shake and Flash!
        if (healthPercentage < previousHealth)
        {
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(DamageJuiceRoutine());
        }
        previousHealth = healthPercentage;

        // 2. Update the Sprite State
        if (healthPercentage <= 0f)
        {
            faceImage.sprite = deadFace;
            isCritical = false; // Stop pulsing if dead
        }
        else if (healthPercentage <= 0.4f) 
        {
            faceImage.sprite = criticalFace;
            isCritical = true; // Start the heartbeat pulse!
        }
        else if (healthPercentage <= 0.85f) 
        {
            faceImage.sprite = hurtFace;
            isCritical = false;
        }
        else
        {
            faceImage.sprite = healthyFace; 
            isCritical = false;
        }
    }

    // This Coroutine handles the rapid vibration and red flash when hit
    private IEnumerator DamageJuiceRoutine()
    {
        float elapsed = 0f;
        float duration = 0.2f; // Shake for 0.2 seconds
        float magnitude = 10f; // How violent the shake is (in pixels)

        faceImage.color = damageColor; // Flash Red

        while (elapsed < duration)
        {
            // Pick a random direction to violently shove the UI picture
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude;
            
            faceRect.anchoredPosition = originalPosition + new Vector2(offsetX, offsetY);

            elapsed += Time.deltaTime;
            
            // Fade the red color back to white smoothly over the duration
            faceImage.color = Color.Lerp(damageColor, Color.white, elapsed / duration);
            
            yield return null; // Wait for next frame
        }

        // Snap everything perfectly back into place when done
        faceRect.anchoredPosition = originalPosition;
        faceImage.color = Color.white;
    }
}
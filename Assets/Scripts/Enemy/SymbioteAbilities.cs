using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class SymbioteAbilities : MonoBehaviour
{
    [Header("World References")]
    public Transform player;
    public Transform proxy;
    public SpriteRenderer proxyBlip; // The red square on the map layer

    [Header("UI References")]
    public RectTransform edgeArrow; // The red UI arrow
    public TextMeshProUGUI telemetryText; // The text readout
    
    [Header("Settings")]
    public float pingDuration = 5f;
    public float radarRadius = 130f; // How far the arrow pushes out from the center of the UI
    public float cameraSize = 15f; // The Orthographic size of your minimap camera

    void Start()
    {
        // Hide everything on startup
        if (proxyBlip != null) proxyBlip.enabled = false;
        if (edgeArrow != null) edgeArrow.gameObject.SetActive(false);
        if (telemetryText != null) telemetryText.text = "> Sonar offline.";
    }

    void Update()
    {
        // Using 'Q' to trigger the ability
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ExecuteSonar();
        }
    }

    private void ExecuteSonar()
    {
        InventoryManager manager = FindAnyObjectByType<InventoryManager>();
        if (manager != null)
        {
            // Pay the price
            manager.AddCorruptionRow();
            
            // Stop any existing pings and start a fresh one
            StopAllCoroutines();
            StartCoroutine(SonarRoutine());
        }
    }

    private IEnumerator SonarRoutine()
    {
        float timer = 0f;

        // Ensure the arrow object is active
        if (edgeArrow != null) edgeArrow.gameObject.SetActive(true);

        // This loop runs every single frame for exactly 5 seconds
        while (timer < pingDuration)
        {
            timer += Time.deltaTime;

            if (player != null && proxy != null)
            {
                // 1. Calculate the real distance
                float distance = Vector2.Distance(player.position, proxy.position);

                // 2. Calculate the direction vector
                Vector2 direction = (proxy.position - player.position).normalized;
                
                // 3. Convert direction to an angle in degrees
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

                // 4. Check if the Proxy is close enough to be seen on the camera
                if (distance <= cameraSize)
                {
                    // ON SCREEN: Show the blip, hide the arrow
                    proxyBlip.enabled = true;
                    edgeArrow.gameObject.SetActive(false);
                    
                    if (telemetryText != null)
                    {
                        telemetryText.text = $"<color=red>WARNING: VISUAL CONTACT</color>\nDISTANCE: {distance:F1}m";
                    }
                }
                else
                {
                    // OFF SCREEN: Hide the blip, show the arrow
                    proxyBlip.enabled = false;
                    edgeArrow.gameObject.SetActive(true);

                    // Move the arrow to the edge of the minimap using trigonometry
                    float rad = angle * Mathf.Deg2Rad;
                    edgeArrow.anchoredPosition = new Vector2(Mathf.Cos(rad) * radarRadius, Mathf.Sin(rad) * radarRadius);
                    
                    // Rotate the arrow to point outward (Subtract 90 if your arrow graphic naturally points UP)
                    edgeArrow.localRotation = Quaternion.Euler(0, 0, angle - 90f);

                    // Update the terrifying text readout
                    string bearing = GetBearingString(angle);
                    if (telemetryText != null)
                    {
                        telemetryText.text = $"<color=red>WARNING: ANOMALY DETECTED</color>\nDISTANCE: {distance:F1}m\nBEARING: {bearing}";
                    }
                }
            }

            // CRITICAL: This line prevents your PC from crashing. It forces the loop to wait for the next frame.
            yield return null; 
        }

        // 5 seconds are up! Turn everything back off.
        if (proxyBlip != null) proxyBlip.enabled = false;
        if (edgeArrow != null) edgeArrow.gameObject.SetActive(false);
        if (telemetryText != null) telemetryText.text = "> Sonar offline.";
    }

    // Helper function to turn math angles into compass directions
    private string GetBearingString(float angle)
    {
        if (angle < 0) angle += 360; // Normalize to 0-360

        if (angle >= 337.5f || angle < 22.5f) return "EAST";
        if (angle >= 22.5f && angle < 67.5f) return "NORTH-EAST";
        if (angle >= 67.5f && angle < 112.5f) return "NORTH";
        if (angle >= 112.5f && angle < 157.5f) return "NORTH-WEST";
        if (angle >= 157.5f && angle < 202.5f) return "WEST";
        if (angle >= 202.5f && angle < 247.5f) return "SOUTH-WEST";
        if (angle >= 247.5f && angle < 292.5f) return "SOUTH";
        if (angle >= 292.5f && angle < 337.5f) return "SOUTH-EAST";
        
        return "UNKNOWN";
    }
}
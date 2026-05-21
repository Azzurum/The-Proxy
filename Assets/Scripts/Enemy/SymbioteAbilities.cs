using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Manages MOTHER's active symbiote abilities that Kaelen can leverage (e.g., Sonar Ping) at the cost of Corruption.
/// </summary>
public class SymbioteAbilities : MonoBehaviour
{
    [Header("World References")]
    [Tooltip("Reference to Kaelen's transform.")]
    public Transform player;
    [Tooltip("Reference to the Proxy's transform.")]
    public Transform proxy;
    [Tooltip("The red icon representing the Proxy on the physical map layer.")]
    public SpriteRenderer proxyBlip; 

    [Header("UI References")]
    [Tooltip("The red arrow indicator that locks to the edge of the radar UI.")]
    public RectTransform edgeArrow; 
    [Tooltip("Readout displaying distance and bearing text.")]
    public TextMeshProUGUI telemetryText; 
    
    [Header("Settings")]
    [Tooltip("How long the sonar sweep remains active.")]
    public float pingDuration = 5f;
    [Tooltip("Distance from the radar center point to push the directional arrow.")]
    public float radarRadius = 130f; 
    [Tooltip("Orthographic size of the minimap camera used to determine if the blip is visible.")]
    public float cameraSize = 15f; 

    void Start()
    {
        if (proxyBlip != null) proxyBlip.enabled = false;
        if (edgeArrow != null) edgeArrow.gameObject.SetActive(false);
        if (telemetryText != null) telemetryText.text = "> Sonar offline.";
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ExecuteSonar();
        }
    }

    /// <summary>
    /// Injects corruption into the player's system in exchange for a temporary radar sweep.
    /// </summary>
    private void ExecuteSonar()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddCorruptionRow();
            
            StopAllCoroutines();
            StartCoroutine(SonarRoutine());
        }
    }

    private IEnumerator SonarRoutine()
    {
        float timer = 0f;
        float _lastRecordedDistance = -1f;

        if (edgeArrow != null) edgeArrow.gameObject.SetActive(true);

        while (timer < pingDuration)
        {
            timer += Time.deltaTime;

            if (player != null && proxy != null)
            {
                float distance = Vector2.Distance(player.position, proxy.position);

                Vector2 direction = (proxy.position - player.position).normalized;
                
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

                if (distance <= cameraSize)
                {
                    proxyBlip.enabled = true;
                    edgeArrow.gameObject.SetActive(false);
                    
                    // Only rebuild UI strings if the value changes significantly to prevent excessive GC allocation.
                    if (telemetryText != null && Mathf.Abs(distance - _lastRecordedDistance) > 0.1f)
                    {
                        telemetryText.text = $"<color=red>WARNING: VISUAL CONTACT</color>\nDISTANCE: {distance:F1}m";
                        _lastRecordedDistance = distance;
                    }
                }
                else
                {
                    proxyBlip.enabled = false;
                    edgeArrow.gameObject.SetActive(true);

                    float rad = angle * Mathf.Deg2Rad;
                    edgeArrow.anchoredPosition = new Vector2(Mathf.Cos(rad) * radarRadius, Mathf.Sin(rad) * radarRadius);
                    
                    edgeArrow.localRotation = Quaternion.Euler(0, 0, angle - 90f);

                    if (telemetryText != null && Mathf.Abs(distance - _lastRecordedDistance) > 0.1f)
                    {
                        string bearing = GetBearingString(angle);
                        telemetryText.text = $"<color=red>WARNING: ANOMALY DETECTED</color>\nDISTANCE: {distance:F1}m\nBEARING: {bearing}";
                        _lastRecordedDistance = distance;
                    }
                }
            }

            yield return null; 
        }

        if (proxyBlip != null) proxyBlip.enabled = false;
        if (edgeArrow != null) edgeArrow.gameObject.SetActive(false);
        if (telemetryText != null) telemetryText.text = "> Sonar offline.";
    }

    /// <summary>
    /// Converts a mathematical angle into a readable compass heading format for UI readouts.
    /// </summary>
    private string GetBearingString(float angle)
    {
        if (angle < 0) angle += 360; 

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
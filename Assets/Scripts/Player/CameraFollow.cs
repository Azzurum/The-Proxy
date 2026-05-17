using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Targeting")]
    public Transform target; 

    // In 2D, the camera MUST stay pushed back on the Z axis
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Smoothing")]
    [Range(0.01f, 1f)]
    public float smoothTime = 0.15f; // 0.15 is the golden number for 2D!

    // Internal velocity for SmoothDamp math
    private Vector3 velocity = Vector3.zero;

    [Header("Screen Shake")]
    private float shakeTimeRemaining = 0f;
    private float currentShakeMagnitude = 0f;

    void Start()
    {
        // 1. Find the target if it wasn't assigned in the Inspector
        if (target == null)
        {
            GameObject player = GameObject.Find("Player_Kaelen");
            if (player != null) target = player.transform;
        }

        // 2. Instantly teleport camera to target
        // This prevents the "sliding" effect when the game first starts.
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }

    void LateUpdate()
    {
        if (target != null)
        {
            // Target position including our Z-depth offset
            Vector3 targetPosition = target.position + offset;

            // Smoothly glide to the target during gameplay
            Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);

            // Apply Screen Shake if active
            if (shakeTimeRemaining > 0)
            {
                shakeTimeRemaining -= Time.deltaTime;
                float x = Random.Range(-1f, 1f) * currentShakeMagnitude;
                float y = Random.Range(-1f, 1f) * currentShakeMagnitude;
                transform.position = smoothedPosition + new Vector3(x, y, 0f);
            }
            else
            {
                transform.position = smoothedPosition;
            }
        }
    }

    public void TriggerShake(float duration, float magnitude)
    {
        // Check settings to see if the player disabled screen shake (Kinetic Tremor)
        if (PlayerPrefs.GetInt("KineticTremor", 1) == 0) return;

        shakeTimeRemaining = duration;
        currentShakeMagnitude = magnitude;
    }
}
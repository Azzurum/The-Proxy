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
        if (target == null)
        {
            FindTarget();
        }
    }

    private void FindTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player_Kaelen");
        if (player == null) player = GameObject.Find("Player"); // Added another common fallback name
        
        if (player != null) 
        {
            target = player.transform;
            // Instantly teleport camera to target the moment we find them to prevent "sliding" from the void
            transform.position = target.position + offset;
        }
    }

    void LateUpdate()
    {
        // If the target is missing (or hasn't spawned into the world yet), keep looking!
        if (target == null)
            FindTarget();

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
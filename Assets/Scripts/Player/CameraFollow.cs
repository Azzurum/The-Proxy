using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Targeting")]
    public Transform target;

    // Drag your landing pad or master grid here so the camera stays centered at start!
    public Transform cutsceneStartingTarget;

    // In 2D, the camera MUST stay pushed back on the Z axis
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Smoothing")]
    [Range(0.01f, 1f)]
    public float smoothTime = 0.15f;

    private Vector3 velocity = Vector3.zero;

    [Header("Screen Shake")]
    private float shakeTimeRemaining = 0f;
    private float currentShakeMagnitude = 0f;

    void Start()
    {
        // 1. If the player isn't active yet, snap immediately to our cutscene anchor!
        if (target == null && cutsceneStartingTarget != null)
        {
            transform.position = cutsceneStartingTarget.position + offset;
        }
        else if (target == null)
        {
            FindTarget();
        }

        // 2. Instantly teleport camera to target if it's already assigned at start
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }

    void LateUpdate()
    {
        // If player woke up, switch targets automatically
        if (target == null)
        {
            FindTarget();
        }

        Vector3 targetPosition;

        // Smoothly glide to whichever target is active (Player OR Cutscene anchor)
        if (target != null)
        {
            targetPosition = target.position + offset;
        }
        else if (cutsceneStartingTarget != null)
        {
            // Keep the camera locked perfectly centered on the landing pad during the cutscene
            targetPosition = cutsceneStartingTarget.position + offset;
        }
        else
        {
            return; // No target, do nothing
        }

        // Calculate standard smooth tracking position
        Vector3 destination = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);

        // 3. Process Screen Shake / Kinetic Tremor modifications if active
        if (shakeTimeRemaining > 0f)
        {
            Vector2 randomShake = Random.insideUnitCircle * currentShakeMagnitude;
            destination.x += randomShake.x;
            destination.y += randomShake.y;

            shakeTimeRemaining -= Time.deltaTime;
        }

        transform.position = destination;
    }

    // Helper method to look for the player safely
    private void FindTarget()
    {
        GameObject player = GameObject.Find("Player_Kaelen");
        if (player != null)
        {
            target = player.transform;
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
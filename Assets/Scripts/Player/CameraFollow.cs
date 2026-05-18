using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Targeting")]
    public Transform target;

    // NEW: Drag your landing pad or master grid here so the camera stays centered at start!
    public Transform cutsceneStartingTarget;

    // In 2D, the camera MUST stay pushed back on the Z axis
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Smoothing")]
    [Range(0.01f, 1f)]
    public float smoothTime = 0.15f;

    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        // 1. If the player isn't active yet, snap immediately to our cutscene anchor!
        if (target == null && cutsceneStartingTarget != null)
        {
            transform.position = cutsceneStartingTarget.position + offset;
        }
        else if (target == null)
        {
            GameObject player = GameObject.Find("Player_Kaelen");
            if (player != null) target = player.transform;
        }

        // 2. Instantly teleport camera to target if found
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
            GameObject player = GameObject.Find("Player_Kaelen");
            if (player != null)
            {
                target = player.transform;
            }
        }

        // Smoothly glide to whichever target is active (Cutscene anchor OR Player)
        if (target != null)
        {
            Vector3 targetPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }
        else if (cutsceneStartingTarget != null)
        {
            // Keep the camera locked perfectly centered on the landing pad during the cutscene
            Vector3 targetPosition = cutsceneStartingTarget.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }
    }
}
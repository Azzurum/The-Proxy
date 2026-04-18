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
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }
    }
}
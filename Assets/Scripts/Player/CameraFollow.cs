using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Targeting")]
    public Transform target; // Drag Player_Kaelen here

    // In 2D, the camera MUST stay pushed back on the Z axis (usually -10), or it will clip through the map!
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Smoothing")]
    [Range(1f, 10f)]
    public float smoothFactor = 5f; // Higher is faster/snappier, lower is more sluggish/cinematic

    void Start()
    {
        // Auto-find Kaelen just in case you forget to drag him in the Inspector
        if (target == null)
        {
            GameObject player = GameObject.Find("Player_Kaelen");
            if (player != null) target = player.transform;
        }
    }

    // CRITICAL: We use LateUpdate instead of Update for cameras. 
    // This ensures the camera moves AFTER Kaelen has finished his movement for the frame, preventing stuttering.
    void LateUpdate()
    {
        if (target != null)
        {
            // 1. Where do we want to be? (Kaelen's position + the Z offset)
            Vector3 targetPosition = target.position + offset;

            // 2. Smoothly glide from our current position to the target position
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothFactor * Time.deltaTime);
        }
    }
}
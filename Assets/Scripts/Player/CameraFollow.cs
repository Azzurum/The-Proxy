using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Targeting")]
    public Transform target; 

    // In 2D, the camera MUST stay pushed back on the Z axis
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Smoothing")]
    [Range(0.01f, 1f)]
    public float smoothTime = 0.15f; // 0.15 is the golden number for 2D! Lower is snappier, higher is floatier.

    // SmoothDamp needs a blank velocity variable to store its own math in the background
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.Find("Player_Kaelen");
            if (player != null) target = player.transform;
        }
    }

    void LateUpdate()
    {
        if (target != null)
        {
            Vector3 targetPosition = target.position + offset;

            // THE UPGRADE: SmoothDamp acts like a perfect spring, completely immune to framerate drops!
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }
    }
}
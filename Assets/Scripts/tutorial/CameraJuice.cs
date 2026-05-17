using UnityEngine;

public class CameraJuice : MonoBehaviour
{
    [Header("Floating Camera Settings")]
    [SerializeField] private float movementMagnitude = 0.05f; // How far it drifts
    [SerializeField] private float movementSpeed = 0.8f;     // How fast it swings

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        // Uses mathematical sin waves to smoothly oscillate the camera frame over time
        float newX = startPosition.x + Mathf.Sin(Time.time * movementSpeed) * movementMagnitude;
        float newY = startPosition.y + Mathf.Cos(Time.time * (movementSpeed * 0.5f)) * movementMagnitude;

        transform.position = new Vector3(newX, newY, startPosition.z);
    }
}
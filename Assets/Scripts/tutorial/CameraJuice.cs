using UnityEngine;

/// <summary>
/// Applies a continuous, smooth floating oscillation effect to the camera.
/// </summary>
public class CameraJuice : MonoBehaviour
{
    [Header("Floating Camera Settings")]
    [Tooltip("The maximum distance the camera drifts from its origin.")]
    [SerializeField] private float movementMagnitude = 0.05f; 
    [Tooltip("The speed multiplier of the oscillation sine wave.")]
    [SerializeField] private float movementSpeed = 0.8f;     

    private Vector3 _startPosition;

    private void Start()
    {
        _startPosition = transform.position;
    }

    private void Update()
    {
        // Phase offsets ensure a figure-eight or elliptical float pattern rather than a diagonal line.
        float newX = _startPosition.x + Mathf.Sin(Time.time * movementSpeed) * movementMagnitude;
        float newY = _startPosition.y + Mathf.Cos(Time.time * (movementSpeed * 0.5f)) * movementMagnitude;

        transform.position = new Vector3(newX, newY, _startPosition.z);
    }
}
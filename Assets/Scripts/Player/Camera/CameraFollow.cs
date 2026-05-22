using UnityEngine;

/// <summary>
/// Smoothly tracks a target or cutscene anchor, applying screen shake effects when triggered.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("The primary transform the camera should follow (usually the player).")]
    public Transform target;
    [Tooltip("An alternative target used to anchor the camera during starting cutscenes.")]
    public Transform cutsceneStartingTarget;
    [Tooltip("The offset from the target's position. Z must be negative to render 2D scenes correctly.")]
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Smoothing")]
    [Tooltip("The time it takes for the camera to catch up to the target.")]
    [Range(0.01f, 1f)]
    public float smoothTime = 0.15f;

    private Vector3 _velocity = Vector3.zero;
    private float _shakeTimeRemaining = 0f;
    private float _currentShakeMagnitude = 0f;
    private Vector3 _currentTrackedPosition;

    private void Start()
    {
        if (target == null && cutsceneStartingTarget != null)
        {
            transform.position = cutsceneStartingTarget.position + offset;
        }
        else if (target == null)
        {
            FindTarget();
        }

        if (target != null)
        {
            transform.position = target.position + offset;
        }

        _currentTrackedPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            FindTarget();
        }

        Vector3 targetPosition;

        if (target != null)
        {
            targetPosition = target.position + offset;
        }
        else if (cutsceneStartingTarget != null)
        {
            targetPosition = cutsceneStartingTarget.position + offset;
        }
        else
        {
            return; 
        }

        _currentTrackedPosition = Vector3.SmoothDamp(_currentTrackedPosition, targetPosition, ref _velocity, smoothTime);
        Vector3 finalPosition = _currentTrackedPosition;

        if (_shakeTimeRemaining > 0f)
        {
            Vector2 randomShake = Random.insideUnitCircle * _currentShakeMagnitude;
            finalPosition.x += randomShake.x;
            finalPosition.y += randomShake.y;

            _shakeTimeRemaining -= Time.deltaTime;
        }

        transform.position = finalPosition;
    }

    private void FindTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
        }
    }

    /// <summary>
    /// Initiates a screen shake effect for a specified duration and intensity.
    /// </summary>
    public void TriggerShake(float duration, float magnitude)
    {
        if (PlayerPrefs.GetInt("KineticTremor", 1) == 0) return;

        _shakeTimeRemaining = duration;
        _currentShakeMagnitude = magnitude;
    }
}
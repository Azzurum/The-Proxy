using UnityEngine;

/// <summary>
/// Handles the seamless looping translation of background elements on the X-axis.
/// </summary>
public class BackgroundMover : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("The direction and speed at which the background moves per second.")]
    [SerializeField] private Vector3 moveDirection = new Vector3(0.1f, 0f, 0f);

    private Vector3 _startPosition;
    private float _repeatWidth;

    private void Start()
    {
        _startPosition = transform.position;

        if (TryGetComponent<SpriteRenderer>(out var spriteRenderer))
        {
            _repeatWidth = spriteRenderer.bounds.size.x;
        }
        else
        {
            _repeatWidth = 19.2f; 
        }
    }

    private void Update()
    {
        transform.position += moveDirection * Time.deltaTime;

        if (moveDirection.x > 0)
        {
            if (transform.position.x >= _startPosition.x + _repeatWidth)
            {
                transform.position = _startPosition;
            }
        }
        else if (moveDirection.x < 0)
        {
            if (transform.position.x <= _startPosition.x - _repeatWidth)
            {
                transform.position = _startPosition;
            }
        }
    }
}
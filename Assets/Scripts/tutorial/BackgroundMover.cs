using UnityEngine;

public class BackgroundMover : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private Vector3 moveDirection = new Vector3(0.1f, 0f, 0f);

    private Vector3 startPosition;
    private float repeatWidth;

    void Start()
    {
        startPosition = transform.position;

        // Securely grab the exact width of the image sprite bounds
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            repeatWidth = spriteRenderer.bounds.size.x;
        }
        else
        {
            repeatWidth = 19.2f; // Secure fallback value
        }
    }

    void Update()
    {
        // Smooth frame-independent movement
        transform.position += moveDirection * Time.deltaTime;

        // Check if we are moving right (positive X)
        if (moveDirection.x > 0)
        {
            if (transform.position.x >= startPosition.x + repeatWidth)
            {
                transform.position = startPosition;
            }
        }
        // Check if we are moving left (negative X)
        else if (moveDirection.x < 0)
        {
            if (transform.position.x <= startPosition.x - repeatWidth)
            {
                transform.position = startPosition;
            }
        }
    }
}
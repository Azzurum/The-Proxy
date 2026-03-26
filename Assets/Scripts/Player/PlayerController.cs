using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("M.E.T. Rig State")]
    // This is the flag we will trigger later when the inventory opens
    public bool isRooted = false;

    private Rigidbody2D rb;
    private Vector2 movementInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // 1. Check if the boots are magnetically clamped
        if (isRooted)
        {
            movementInput = Vector2.zero;
            return; // Stop reading input entirely
        }

        // 2. Read WASD or Arrow Key inputs
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");

        // Normalize ensures diagonal movement isn't twice as fast
        movementInput = movementInput.normalized;
    }

    void FixedUpdate()
    {
        // 3. Apply the movement to the physics body
        rb.MovePosition(rb.position + movementInput * moveSpeed * Time.fixedDeltaTime);
    }
}
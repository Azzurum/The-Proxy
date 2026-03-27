using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("System State")]
    public bool isRooted = false; // Locks Kaelen in place when M.E.T. Rig is open

    private Rigidbody2D rb;
    private Vector2 movementInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // 1. THE LOCKDOWN: If boots are clamped, kill momentum and skip input!
        if (isRooted)
        {
            movementInput = Vector2.zero;
            rb.linearVelocity = Vector2.zero; // Force stop any physics sliding
            return;
        }

        // 2. Read standard WASD or Arrow Key inputs
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");

        // 3. Normalize prevents moving twice as fast diagonally
        movementInput = movementInput.normalized;
    }

    void FixedUpdate()
    {
        // Apply the calculated movement to the physics body
        rb.MovePosition(rb.position + movementInput * moveSpeed * Time.fixedDeltaTime);
    }
}
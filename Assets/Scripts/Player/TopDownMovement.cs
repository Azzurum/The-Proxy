using UnityEngine;

public class TopDownMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public Rigidbody2D rb;
    public Animator animator;

    Vector2 movement;

    void Update()
    {
        if (DialogueEngine.isDialogueActive) return;
        // 1. Get WASD or Arrow Key input (-1 to 1)
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // 2. Tell the Animator how fast we are moving to trigger the Walk transition
        animator.SetFloat("Speed", movement.sqrMagnitude);

        // 3. ONLY update the direction parameters if we are actually pressing a key!
        // This is the secret trick: if we stop pressing keys, movement becomes (0,0).
        // By putting this inside an IF statement, the Animator remembers the LAST direction
        // we pressed, which ensures the character faces the correct way when they Idle!
        if (movement != Vector2.zero)
        {
            animator.SetFloat("Horizontal", movement.x);
            animator.SetFloat("Vertical", movement.y);
        }
    }

    void FixedUpdate()
    {
        // Actually move the physical character
        rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
    }
}
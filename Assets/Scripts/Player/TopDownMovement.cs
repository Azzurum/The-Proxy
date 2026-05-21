using UnityEngine;

/// <summary>
/// Handles basic 2D top-down movement and animation state updates.
/// </summary>
public class TopDownMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("The base movement speed of the character.")]
    [SerializeField] private float moveSpeed = 5f;
    
    [Header("Component References")]
    [Tooltip("The Rigidbody2D component for physics-based movement.")]
    [SerializeField] private Rigidbody2D rb;
    [Tooltip("The Animator component for updating movement animations.")]
    [SerializeField] private Animator animator;

    private Vector2 _movement;

    private void Update()
    {
        if (DialogueEngine.isDialogueActive) return;

        _movement.x = Input.GetAxisRaw("Horizontal");
        _movement.y = Input.GetAxisRaw("Vertical");

        if (animator != null)
        {
            animator.SetFloat("Speed", _movement.sqrMagnitude);

            if (_movement != Vector2.zero)
            {
                animator.SetFloat("Horizontal", _movement.x);
                animator.SetFloat("Vertical", _movement.y);
            }
        }
    }

    private void FixedUpdate()
    {
        if (rb != null)
        {
            rb.MovePosition(rb.position + _movement.normalized * moveSpeed * Time.fixedDeltaTime);
        }
    }
}
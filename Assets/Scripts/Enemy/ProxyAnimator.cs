using UnityEngine;

/// <summary>
/// Coordinates visual feedback and sprite orientations for the Proxy AI movement and combat events.
/// </summary>
public class ProxyAnimator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The central Animator controlling the Proxy's states.")]
    public Animator animator;
    [Tooltip("The renderer for flipping the sprite horizontally.")]
    public SpriteRenderer spriteRenderer;

    /// <summary>
    /// Interprets an absolute movement vector into discrete 4-way blend parameters.
    /// </summary>
    /// <param name="moveDirection">The non-normalized velocity or direction vector.</param>
    public void UpdateAnimation(Vector2 moveDirection)
    {
        float speed = moveDirection.magnitude;
        animator.SetFloat("Speed", speed);

        if (speed > 0.1f)
        {
            if (Mathf.Abs(moveDirection.x) > Mathf.Abs(moveDirection.y))
            {
                animator.SetFloat("Direction", 1f); 
                spriteRenderer.flipX = (moveDirection.x < 0); 
            }
            else
            {
                if (moveDirection.y > 0)
                {
                    animator.SetFloat("Direction", 1f); 
                    if (moveDirection.x < -0.01f) spriteRenderer.flipX = true;
                    else if (moveDirection.x > 0.01f) spriteRenderer.flipX = false;
                }
                else
                {
                    animator.SetFloat("Direction", 0f); 
                    spriteRenderer.flipX = false; 
                }
            }
        }
    }

    /// <summary>
    /// Triggers the melee animation hash.
    /// </summary>
    public void TriggerAttack()
    {
        animator.SetTrigger("Attack");
    }
}
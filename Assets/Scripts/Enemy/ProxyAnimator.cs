using UnityEngine;

public class ProxyAnimator : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    public void UpdateAnimation(Vector2 moveDirection)
    {
        // 1. Calculate actual movement speed
        float speed = moveDirection.magnitude;
        animator.SetFloat("Speed", speed);

        // 2. Only change directions if we are actually moving
        if (speed > 0.1f)
        {
            // Are we moving mostly horizontally or mostly vertically?
            if (Mathf.Abs(moveDirection.x) > Mathf.Abs(moveDirection.y))
            {
                // === MOVING LEFT OR RIGHT ===
                animator.SetFloat("Direction", 1f); // 1 = Side Animation
                
                // Flip if moving left, un-flip if moving right
                spriteRenderer.flipX = (moveDirection.x < 0); 
            }
            else
            {
                // === MOVING UP OR DOWN ===
                if (moveDirection.y > 0)
                {
                    // MOVING UP: Force it to use the Side Animation
                    animator.SetFloat("Direction", 1f); // 1 = Side Animation
                    
                    // Even though we are moving up, we still want to face the correct horizontal side
                    if (moveDirection.x < -0.01f) spriteRenderer.flipX = true;
                    else if (moveDirection.x > 0.01f) spriteRenderer.flipX = false;
                    // If moving perfectly straight up (x == 0), it just keeps facing whatever side it was already facing.
                }
                else
                {
                    // MOVING DOWN: Force it to use the Down Animation
                    animator.SetFloat("Direction", 0f); // 0 = Down Animation
                    
                    // NEVER flip the down sprite!
                    spriteRenderer.flipX = false; 
                }
            }
        }
    }

    public void TriggerAttack()
    {
        animator.SetTrigger("Attack");
    }
}
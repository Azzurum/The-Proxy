using UnityEngine;
using UnityEngine.UI;

public class UITrayAnimator : MonoBehaviour
{
    public RectTransform trayRect;
    public Button latchButton; // Assign the Latch_ExtNode button in Inspector
    public Vector2 openPosition;
    public Vector2 closedPosition;
    public float animationDuration = 0.4f;
    public bool isOpen = false;

    private float animationTimer = 0f;
    private Vector2 currentStartPos;
    private Vector2 currentTargetPos;

    void Update()
    {
        if (animationTimer > 0)
        {
            animationTimer -= Time.deltaTime;
            
            // Clamp the timer between 0 and 1 so the math never overshoots
            float t = Mathf.Clamp01(1f - (animationTimer / animationDuration));
            t = Mathf.SmoothStep(0, 1, t); // Smooth easing
            
            // Move the tray smoothly from where it started to the target
            trayRect.anchoredPosition = Vector2.Lerp(currentStartPos, currentTargetPos, t);
        }
    }

    public void ToggleTray()
    {
        // Disable button during animation to prevent double-clicks
        if (latchButton != null)
        {
            latchButton.interactable = false;
        }

        isOpen = !isOpen;
        animationTimer = animationDuration;
        
        // 1. Dynamically capture EXACTLY where the tray is right now so it never teleports
        currentStartPos = trayRect.anchoredPosition; 
        
        // 2. Set the target based on whether we are opening or closing
        currentTargetPos = isOpen ? openPosition : closedPosition;
        
        // 3. FORCE the Y position to stay exactly the same. 
        // This guarantees the tray only slides left/right, even if your Inspector Y values are wrong!
        currentTargetPos.y = trayRect.anchoredPosition.y;

        // Re-enable button after animation completes
        Invoke(nameof(EnableButton), animationDuration);
    }

    private void EnableButton()
    {
        if (latchButton != null)
        {
            latchButton.interactable = true;
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Handles simple visual feedback by modifying an Image's color when the user hovers over a UI element.
/// </summary>
public class ButtonHoverGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("The Light Strip")]
    [Tooltip("The UI Image component that will glow upon hover.")]
    public Image lightImage;

    [Header("Colors")]
    [Tooltip("The default resting color of the light strip.")]
    public Color normalColor;
    [Tooltip("The highlighted color applied when the mouse hovers over the button.")]
    public Color hoverColor;

    private void Start()
    {
        if (lightImage != null)
        {
            lightImage.color = normalColor;
        }
    }

    /// <summary>
    /// Applies the hover color when the cursor enters the rect bounds.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (lightImage != null)
        {
            lightImage.color = hoverColor;
        }
    }

    /// <summary>
    /// Restores the normal resting color when the cursor exits the rect bounds.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (lightImage != null)
        {
            lightImage.color = normalColor;
        }
    }
}
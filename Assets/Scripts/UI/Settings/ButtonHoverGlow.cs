using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Required for hover detection!

public class ButtonHoverGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("The Light Strip")]
    public Image lightImage;

    [Header("Colors")]
    public Color normalColor;
    public Color hoverColor;

    void Start()
    {
        // Set to dark/off when the menu first loads
        if (lightImage != null)
        {
            lightImage.color = normalColor;
        }
    }

    // This acts like CSS :hover
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (lightImage != null)
        {
            lightImage.color = hoverColor;
        }
    }

    // This triggers when the mouse leaves
    public void OnPointerExit(PointerEventData eventData)
    {
        if (lightImage != null)
        {
            lightImage.color = normalColor;
        }
    }
}
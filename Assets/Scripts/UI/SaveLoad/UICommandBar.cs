using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class UICommandBar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public UISaveSlot parentSlot;
    public TextMeshProUGUI txtIndex;
    public TextMeshProUGUI txtCommand;
    public Image leftBorder;

    [Header("Hover Colors")]
    public Color parentHoverColor = Color.white;
    public Color elementHoverColor = Color.white;
    
    [Header("Normal Colors")]
    public Color normalTextColor = Color.white;
    public Color normalIndexColor = new Color(0.36f, 0.45f, 0.51f, 1f); // #5E7382
    public Color normalBorderColor = new Color(0.36f, 0.45f, 0.51f, 1f);

    private float _targetX = 0f;

    void Update()
    {
        // Only modify the X axis! Let the Vertical Layout Group control the Y axis.
        Vector3 pos = transform.localPosition;
        pos.x = Mathf.Lerp(pos.x, _targetX, 15f * Time.unscaledDeltaTime);
        transform.localPosition = pos;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _targetX = 20f; // Slide right 20 pixels

        if (txtIndex) txtIndex.color = elementHoverColor;
        if (txtCommand) txtCommand.color = elementHoverColor;
        if (leftBorder) leftBorder.color = elementHoverColor;

        if (parentSlot) parentSlot.SetHoverBackground(parentHoverColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _targetX = 0f; // Return to center

        if (txtIndex) txtIndex.color = normalIndexColor;
        if (txtCommand) txtCommand.color = normalTextColor;
        if (leftBorder) leftBorder.color = normalBorderColor;

        if (parentSlot) parentSlot.ResetHoverBackground();
    }
    
    void OnDisable() 
    {
        _targetX = 0f;
        
        // Reset X instantly if the menu is closed
        Vector3 pos = transform.localPosition;
        pos.x = 0f;
        transform.localPosition = pos;

        if (txtIndex) txtIndex.color = normalIndexColor;
        if (txtCommand) txtCommand.color = normalTextColor;
        if (leftBorder) leftBorder.color = normalBorderColor;
    }
}
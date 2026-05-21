using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles the hover interactions and sliding animations for individual command buttons inside a save slot.
/// </summary>
public class UICommandBar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    public UISaveSlot parentSlot;
    public TextMeshProUGUI txtIndex;
    public TextMeshProUGUI txtCommand;
    public Image leftBorder;

    [Header("Hover Colors")]
    public Color parentHoverColor = Color.white;
    public Color elementHoverColor = Color.white;
    
    [Header("Normal Colors")]
    public Color normalTextColor = Color.white;
    public Color normalIndexColor = new Color(0.36f, 0.45f, 0.51f, 1f); 
    public Color normalBorderColor = new Color(0.36f, 0.45f, 0.51f, 1f);

    private float _targetX = 0f;

    private void Update()
    {
        Vector3 pos = transform.localPosition;
        pos.x = Mathf.Lerp(pos.x, _targetX, 15f * Time.unscaledDeltaTime);
        transform.localPosition = pos;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _targetX = 20f; 

        if (txtIndex) txtIndex.color = elementHoverColor;
        if (txtCommand) txtCommand.color = elementHoverColor;
        if (leftBorder) leftBorder.color = elementHoverColor;

        if (parentSlot) parentSlot.SetHoverBackground(parentHoverColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _targetX = 0f; 

        if (txtIndex) txtIndex.color = normalIndexColor;
        if (txtCommand) txtCommand.color = normalTextColor;
        if (leftBorder) leftBorder.color = normalBorderColor;

        if (parentSlot) parentSlot.ResetHoverBackground();
    }
    
    private void OnDisable() 
    {
        _targetX = 0f;
        
        Vector3 pos = transform.localPosition;
        pos.x = 0f;
        transform.localPosition = pos;

        if (txtIndex) txtIndex.color = normalIndexColor;
        if (txtCommand) txtCommand.color = normalTextColor;
        if (leftBorder) leftBorder.color = normalBorderColor;
    }
}
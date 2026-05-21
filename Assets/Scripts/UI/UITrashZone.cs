using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Coordinates drag-and-drop feedback for item destruction/ejection zones.
/// </summary>
public class UITrashZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("The underlying background image component reacting to highlight events.")]
    public Image background;
    public Color normalColor = new Color(1f, 0.66f, 0f, 0.05f); 
    public Color highlightColor = new Color(1f, 0.66f, 0f, 0.3f); 

    private void Start() { if(background != null) background.color = normalColor; }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (DraggableItem.itemBeingDragged != null && background != null)
            background.color = highlightColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (background != null) background.color = normalColor;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (DraggableItem.itemBeingDragged != null)
        {
            DraggableItem.itemBeingDragged.ejectedFromRig = true;
            if (background != null) background.color = normalColor;
        }
    }
}
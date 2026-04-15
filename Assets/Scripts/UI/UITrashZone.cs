using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UITrashZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Image background;
    public Color normalColor = new Color(1f, 0.66f, 0f, 0.05f); // Faint Amber
    public Color highlightColor = new Color(1f, 0.66f, 0f, 0.3f); // Glowing Amber

    void Start() { if(background != null) background.color = normalColor; }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // ADD THIS LINE TO PROVE THE MOUSE IS TOUCHING IT:
        Debug.Log("<color=cyan>TRASH CAN: I feel the mouse!</color>"); 

        // Light up the trash can if we are holding an item over it
        if (DraggableItem.itemBeingDragged != null && background != null)
            background.color = highlightColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (background != null) background.color = normalColor;
    }

    public void OnDrop(PointerEventData eventData)
    {
        // Tell the item it has been thrown away!
        if (DraggableItem.itemBeingDragged != null)
        {
            DraggableItem.itemBeingDragged.ejectedFromRig = true;
            if (background != null) background.color = normalColor;
        }
    }
}
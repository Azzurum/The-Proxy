using UnityEngine;
using UnityEngine.EventSystems;

public class TrashSlot : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            GameObject droppedItem = eventData.pointerDrag;

            // Find the master manager and tell it to discard this item
            InventoryManager manager = FindAnyObjectByType<InventoryManager>();
            if (manager != null)
            {
                manager.DiscardItemToWorld(droppedItem);
            }
        }
    }
}
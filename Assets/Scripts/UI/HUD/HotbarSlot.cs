using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Represents a single slot in the player's quick-use hotbar. Handles item drops and highlighting.
/// </summary>
public class HotbarSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slot Setup")]
    [Tooltip("The numerical identifier for this slot (e.g., 1, 2, or 3).")]
    public int slotNumber;
    [Tooltip("The UI Image used to indicate that this slot is currently active.")]
    public Image highlightFrame;

    [Header("Physical Item")]
    [Tooltip("A reference to the DraggableItem currently occupying this slot.")]
    public DraggableItem containedItem;

    private Image backgroundImage;
    private Color normalColor;
    private Color hoverColor = new Color(0f, 1f, 0.8f, 0.3f);
    private QuestTracker _questTracker;

    private void Awake()
    {
        backgroundImage = GetComponent<Image>();
        if (backgroundImage != null) normalColor = backgroundImage.color;
        _questTracker = FindAnyObjectByType<QuestTracker>();
    }

    /// <summary>
    /// Called by the EventSystem when the pointer enters the slot's bounds.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (DraggableItem.itemBeingDragged != null && containedItem == null)
            if (backgroundImage != null) backgroundImage.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (backgroundImage != null) backgroundImage.color = normalColor;
    }

    /// <summary>
    /// Called by the EventSystem when a draggable item is released over this slot.
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        if (backgroundImage != null) backgroundImage.color = normalColor;

        DraggableItem draggedItem = DraggableItem.itemBeingDragged;

        if (draggedItem != null && containedItem == null)
        {
            draggedItem.dropAccepted = true;
            draggedItem.parentAfterDrag = this.transform;
            containedItem = draggedItem;

            if (containedItem.itemData != null && containedItem.itemData.itemID == "TOOL-WELD")
            {
                if (_questTracker != null && _questTracker.GetCurrentObjective() == 4)
                {
                    _questTracker.AdvanceObjective(5, "Weld the Airlock Door");
                }
            }

            if (slotNumber == 1 && HotbarManager.Instance != null)
            {
                HotbarManager.Instance.EquipSlot(1);
            }
        }
    }

    /// <summary>
    /// Empties the slot of its contained item and updates the hotbar state.
    /// </summary>
    public void ClearSlot()
    {
        containedItem = null;
        SetHighlight(false);

        if (HotbarManager.Instance != null)
        {
            HotbarManager.Instance.EquipSlot(slotNumber);
        }
    }

    /// <summary>
    /// Clears the item reference when a drag operation begins from this slot.
    /// </summary>
    public void DetachItem()
    {
        containedItem = null;
    }

    /// <summary>
    /// Toggles the visibility of the highlight frame.
    /// </summary>
    public void SetHighlight(bool isActive)
    {
        if (highlightFrame != null) highlightFrame.enabled = isActive;
    }
}
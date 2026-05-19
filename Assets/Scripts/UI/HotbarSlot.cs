using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HotbarSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slot Setup")]
    public int slotNumber;
    public Image highlightFrame;

    [Header("Physical Item")]
    public DraggableItem containedItem;

    private Image backgroundImage;
    private Color normalColor;
    private Color hoverColor = new Color(0f, 1f, 0.8f, 0.3f);

    void Awake()
    {
        backgroundImage = GetComponent<Image>();
        if (backgroundImage != null) normalColor = backgroundImage.color;
    }

    void Update()
    {
        // Automatically clear the slot if the item is dragged out of the hotbar!
        if (containedItem != null && containedItem.transform.parent != this.transform)
        {
            ClearSlot();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (DraggableItem.itemBeingDragged != null && containedItem == null)
            if (backgroundImage != null) backgroundImage.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (backgroundImage != null) backgroundImage.color = normalColor;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (backgroundImage != null) backgroundImage.color = normalColor;

        DraggableItem draggedItem = DraggableItem.itemBeingDragged;

        if (draggedItem != null && containedItem == null)
        {
            draggedItem.dropAccepted = true;
            draggedItem.parentAfterDrag = this.transform;
            containedItem = draggedItem;

            // --- ADD THIS TUTORIAL UPDATE CODE NOW ---
            if (containedItem.itemData != null && containedItem.itemData.itemID == "TOOL-WELD")
            {
                QuestTracker tracker = FindObjectOfType<QuestTracker>();
                if (tracker != null && tracker.GetCurrentObjective() == 4)
                {
                    // Advance objective to index 5: "Weld the Airlock Door"
                    tracker.AdvanceObjective(5, "Weld the Airlock Door");
                    Debug.Log("<color=green>TUTORIAL SUCCESS:</color> Welder placed in hotbar. Objective updated!");
                }
            }
            // ----------------------------------------

            if (slotNumber == 1 && HotbarManager.Instance != null)
            {
                HotbarManager.Instance.EquipSlot(1);
            }
        }
    }

    public void ClearSlot()
    {
        containedItem = null;
        SetHighlight(false);

        if (HotbarManager.Instance != null)
        {
            HotbarManager.Instance.EquipSlot(slotNumber);
        }
    }

    public void SetHighlight(bool isActive)
    {
        if (highlightFrame != null) highlightFrame.enabled = isActive;
    }
}
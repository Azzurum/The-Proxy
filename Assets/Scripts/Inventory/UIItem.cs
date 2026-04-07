using UnityEngine;
using UnityEngine.UI;

public class UIItem : MonoBehaviour
{
    public ItemData myData;
    public Image displayImage; // Sprite display instead of text
    public Image backgroundImage;

    public void Initialize(ItemData data, float cellSize)
    {
        myData = data;
        
        if (data != null)
        {
            // Display the sprite icon
            if (displayImage != null && data.icon != null)
            {
                displayImage.sprite = data.icon;
                displayImage.preserveAspect = true;
                displayImage.rectTransform.localEulerAngles = Vector3.zero;
                displayImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                displayImage.rectTransform.anchorMin = Vector2.zero;
                displayImage.rectTransform.anchorMax = Vector2.one;
                displayImage.rectTransform.anchoredPosition = Vector2.zero;
                displayImage.rectTransform.sizeDelta = Vector2.zero;
            }
        }

        // Pass the data to the Draggable script so it knows if it's corruption
        DraggableItem drag = GetComponent<DraggableItem>();
        if (drag != null)
        {
            drag.itemData = data;
            drag.itemName = data.itemName;
            drag.itemDescription = data.itemDescription;

            drag.SetCellSize(cellSize);

            // Apply the footprint to the draggable
            ItemFootprint fp = data.GetFootprint();
            drag.SetFootprint(fp);

            // Normalize the item rect and center it relative to its slot
            RectTransform itemRect = GetComponent<RectTransform>();
            itemRect.localScale = Vector3.one;
            itemRect.localRotation = Quaternion.identity;
            itemRect.anchorMin = new Vector2(0.5f, 0.5f);
            itemRect.anchorMax = new Vector2(0.5f, 0.5f);
            itemRect.pivot = new Vector2(0.5f, 0.5f);
            itemRect.anchoredPosition = Vector2.zero;
            itemRect.localPosition = new Vector3((fp.width - 1) * cellSize * 0.5f, -(fp.height - 1) * cellSize * 0.5f, 0);

            Debug.Log($"UIItem.Initialize: item={data.itemName} size={fp.width}x{fp.height} cellSize={cellSize} rectSize={itemRect.sizeDelta} anchoredPos={itemRect.anchoredPosition}");
        }
    }
}
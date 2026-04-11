using UnityEngine;
using UnityEngine.UI;

public class UIItem : MonoBehaviour
{
    public ItemData myData;
    
    [Header("Visual References")]
    [Tooltip("Drag the CHILD 'Icon' GameObject here")]
    public Image displayImage; 
    
    [Tooltip("Drag the ROOT Background Image here")]
    public Image backgroundImage;

    public void Initialize(ItemData data, float cellSize)
    {
        myData = data;

        // NOTE: I removed the "if (displayImage == null) GetComponent<Image>()" line.
        // That line was accidentally turning your background into the icon!

        if (data != null && displayImage != null)
        {
            if (data.icon != null)
            {
                displayImage.sprite = data.icon;
                displayImage.preserveAspect = true; // Prevents the picture from stretching
                
                // THE FIX: Mathematically force the icon to sit inside the background with padding!
                RectTransform iconRect = displayImage.GetComponent<RectTransform>();
                if (iconRect != null)
                {
                    // Lock anchors to the 4 corners of the parent footprint
                    iconRect.anchorMin = Vector2.zero; // Bottom-Left
                    iconRect.anchorMax = Vector2.one;  // Top-Right

                    // Apply 12 pixels of padding to all 4 sides so it shrinks inside the borders
                    float padding = 12f; 
                    iconRect.offsetMin = new Vector2(padding, padding);   // Push Right and Up
                    iconRect.offsetMax = new Vector2(-padding, -padding); // Push Left and Down
                }
            }
        }

        DraggableItem drag = GetComponent<DraggableItem>();
        if (drag != null)
        {
            drag.itemData = data;
            drag.itemName = data.itemName;
            drag.itemDescription = data.itemDescription;

            drag.SetCellSize(cellSize);
            
            // Read the saved rotation state from memory when spawning
            drag.isRotated = data.isRotated; 

            ItemFootprint fp = data.GetFootprint();
            drag.SetFootprint(drag.isRotated ? fp.GetRotated() : fp);
        }
    }
}
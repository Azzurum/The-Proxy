using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(GridLayoutGroup))]
public class DynamicGridScaler : MonoBehaviour
{
    public int columns = 5;
    public int rows = 10;
    public int spacing = 6;
    
    private GridLayoutGroup gridLayout;
    private RectTransform rectTransform;

    void Start()
    {
        gridLayout = GetComponent<GridLayoutGroup>();
        rectTransform = GetComponent<RectTransform>();
    }

    void OnRectTransformDimensionsChange()
    {
        UpdateGridSize();
    }

    public void UpdateGridSize()
    {
        if (gridLayout == null || rectTransform == null) return;

        // Calculate available width and height
        float availableWidth = rectTransform.rect.width - gridLayout.padding.left - gridLayout.padding.right - (spacing * (columns - 1));
        float availableHeight = rectTransform.rect.height - gridLayout.padding.top - gridLayout.padding.bottom - (spacing * (rows - 1));
        
        // Calculate what the perfect square size would be for both constraints
        float widthBasedSize = availableWidth / columns;
        float heightBasedSize = availableHeight / rows;

        // Pick the smaller size so it NEVER overflows, and subtract 0.1f to prevent Unity from wrapping!
        float finalCellSize = Mathf.Min(widthBasedSize, heightBasedSize) - 0.1f;

        gridLayout.cellSize = new Vector2(finalCellSize, finalCellSize);
        gridLayout.spacing = new Vector2(spacing, spacing);
    }
}
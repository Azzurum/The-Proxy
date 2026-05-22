using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Forces a UI GridLayoutGroup to strictly scale its cell sizes based on dynamic resolution constraints.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(GridLayoutGroup))]
public class DynamicGridScaler : MonoBehaviour
{
    [Header("Grid Layout Rules")]
    [Tooltip("The strictly enforced number of columns.")]
    public int columns = 5;
    [Tooltip("The strictly enforced number of rows.")]
    public int rows = 10;
    [Tooltip("The pixel padding gap between individual cells.")]
    public int spacing = 6;
    
    private GridLayoutGroup _gridLayout;
    private RectTransform _rectTransform;

    private void Start()
    {
        _gridLayout = GetComponent<GridLayoutGroup>();
        _rectTransform = GetComponent<RectTransform>();
    }

    private void OnRectTransformDimensionsChange()
    {
        UpdateGridSize();
    }

    /// <summary>
    /// Mathematically clamps the size of interior cells so they never overflow the bounding rect.
    /// </summary>
    public void UpdateGridSize()
    {
        if (_gridLayout == null || _rectTransform == null || columns <= 0 || rows <= 0) return;

        float availableWidth = _rectTransform.rect.width - _gridLayout.padding.left - _gridLayout.padding.right - (spacing * (columns - 1));
        float availableHeight = _rectTransform.rect.height - _gridLayout.padding.top - _gridLayout.padding.bottom - (spacing * (rows - 1));
        
        float widthBasedSize = availableWidth / columns;
        float heightBasedSize = availableHeight / rows;

        float finalCellSize = Mathf.Max(0.1f, Mathf.Min(widthBasedSize, heightBasedSize) - 0.1f);

        _gridLayout.cellSize = new Vector2(finalCellSize, finalCellSize);
        _gridLayout.spacing = new Vector2(spacing, spacing);
    }
}
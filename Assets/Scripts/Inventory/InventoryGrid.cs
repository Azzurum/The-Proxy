using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryGrid : MonoBehaviour
{
    [Header("Grid Dimensions")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    public int activeHeight = 10;

    [Header("Grid Visuals")]
    public float cellSize = 80f;
    public float slotSize = 75f;
    public float cellPadding = 40f;

    [Header("Active Memory")]
    public List<InventoryItem> activeItems = new List<InventoryItem>();

    public void InitializeGridVisuals(GameObject slotPrefab)
    {
        // 1. NUKE THE LAYOUT GROUP SO IT NEVER INTERFERES AGAIN!
        GridLayoutGroup glg = GetComponent<GridLayoutGroup>();
        if (glg != null) DestroyImmediate(glg);

        // 2. Force perfectly centered pivots
        RectTransform rect = GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(gridWidth * cellSize, gridHeight * cellSize);

        float startX = -(gridWidth * cellSize) / 2f + cellPadding;
        float startY = -(gridHeight * cellSize) / 2f + cellPadding;

        // 3. Manually place slots with pure math
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GameObject newSlot = Instantiate(slotPrefab, transform);
                RectTransform slotRect = newSlot.GetComponent<RectTransform>();

                slotRect.pivot = new Vector2(0.5f, 0.5f);
                slotRect.sizeDelta = new Vector2(slotSize, slotSize);
                slotRect.localPosition = new Vector2(startX + (x * cellSize), startY + (y * cellSize));

                InventorySlot slotLogic = newSlot.GetComponent<InventorySlot>();
                if (slotLogic != null) slotLogic.slotCoordinate = new Vector2Int(x, y);
            }
        }
    }

    public void SetMode(int height, bool showGrids)
    {
        activeHeight = height;

        // Hide the main background so it doesn't stretch 10 rows down
        Image bg = GetComponent<Image>();
        if (bg != null) bg.enabled = false;

        InventorySlot[] allSlots = GetComponentsInChildren<InventorySlot>(true);
        foreach (var slot in allSlots)
        {
            bool isActive = slot.slotCoordinate.y >= (gridHeight - height);
            slot.gameObject.SetActive(isActive);

            Image img = slot.GetComponent<Image>();
            if (img != null)
            {
                img.enabled = true;
                // Solid black for Buffer mode, Transparent grids for Locker mode
                if (!showGrids) img.color = isActive ? new Color(0.05f, 0.05f, 0.05f, 1f) : Color.clear;
                else img.color = isActive ? new Color(0, 0, 0, 0.4f) : Color.clear;
            }
        }
    }

    public Vector2 GetSnapPosition(Vector2Int anchorCoord, int sizeX, int sizeY)
    {
        float gridBottomLeftX = -(gridWidth * cellSize) / 2f;
        float gridBottomLeftY = -(gridHeight * cellSize) / 2f;

        float itemBottomLeftX = gridBottomLeftX + (anchorCoord.x * cellSize);
        float itemBottomLeftY = gridBottomLeftY + (anchorCoord.y * cellSize);

        float centerX = itemBottomLeftX + (sizeX * cellSize) / 2f;
        float centerY = itemBottomLeftY + (sizeY * cellSize) / 2f;

        return new Vector2(centerX, centerY);
    }

    public bool IsSpaceFree(Vector2Int anchor, int width, int height, GameObject itemBeingMoved)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int targetCell = new Vector2Int(anchor.x + x, anchor.y + y);

                if (targetCell.x < 0 || targetCell.x >= gridWidth ||
                    targetCell.y < 0 || targetCell.y >= gridHeight ||
                    targetCell.y < (gridHeight - activeHeight))
                {
                    return false;
                }

                foreach (var item in activeItems)
                {
                    if (item.uiObject == itemBeingMoved) continue;
                    bool overlapsX = targetCell.x >= item.position.x && targetCell.x < (item.position.x + item.size.x);
                    bool overlapsY = targetCell.y >= item.position.y && targetCell.y < (item.position.y + item.size.y);
                    if (overlapsX && overlapsY) return false;
                }
            }
        }
        return true;
    }

    public void RegisterItem(GameObject uiItem, Vector2Int anchorCoordinate, int cellsX, int cellsY, bool isRotated)
    {
        RemoveItem(uiItem);
        InventoryItem newData = new InventoryItem();
        newData.position = anchorCoordinate;
        newData.size = new Vector2Int(cellsX, cellsY);
        newData.isRotated = isRotated;
        newData.uiObject = uiItem;
        DraggableItem draggable = uiItem.GetComponent<DraggableItem>();
        if (draggable != null) newData.itemData = draggable.itemData;
        activeItems.Add(newData);
    }

    public void RemoveItem(GameObject uiItem)
    {
        activeItems.RemoveAll(item => item.uiObject == uiItem);
    }
}
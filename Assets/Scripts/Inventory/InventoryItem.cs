using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public Vector2Int position; // Bottom-left anchor (Row Index 0-9)
    public Vector2Int size;
    public bool isLocked;
    public bool isQuestItem;
    public bool isRotated;
    public bool isCorruption; // Flags this item as a dead block
    public GameObject uiObject; // Connects the math to the visual battery on screen
}
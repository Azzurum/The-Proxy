using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public Vector2Int position; // Bottom-left anchor (Row Index 0-9)
    public Vector2Int size;
    public bool isLocked;
    public bool isQuestItem;
    public bool isRotated;
}
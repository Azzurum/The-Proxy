using UnityEngine;

public class InventorySlot : MonoBehaviour
{
    public enum GridRegion
    {
        MainLeft,
        MainRight,
        External
    }

    [Header("Grid Data")]
    public Vector2Int slotCoordinate;
    public GridRegion gridRegion = GridRegion.MainLeft;
}
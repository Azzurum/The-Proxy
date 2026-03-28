using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName;
    [TextArea] public string itemDescription;
    public Vector2Int size;
    public Sprite icon;
}
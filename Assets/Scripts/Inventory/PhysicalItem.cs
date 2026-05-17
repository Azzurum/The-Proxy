using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PhysicalItem : MonoBehaviour
{
    [Header("Item Definition")]
    [Tooltip("Drag the matching ItemData ScriptableObject here")]
    public ItemData itemData;
}
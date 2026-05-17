using UnityEngine;
using UnityEngine.Tilemaps;

public class TileDoorTrigger : MonoBehaviour
{
    public Tilemap doorTilemap;
    public TileBase openAnimatedTile;
    public TileBase closedTile;

    private Vector3Int tilePosition;

    void Start()
    {
        tilePosition = doorTilemap.WorldToCell(transform.position);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            doorTilemap.SetTile(tilePosition, openAnimatedTile);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            doorTilemap.SetTile(tilePosition, closedTile);
        }
    }
}
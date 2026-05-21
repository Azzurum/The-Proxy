using UnityEngine;

/// <summary>
/// A trigger volume that updates the player's current location string for save game telemetry.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class Env_LocationZone : MonoBehaviour
{
    [Header("Ship Navigation")]
    [Tooltip("Exact text to appear on the Memory Sync UI when saving the game.")]
    public string zoneDisplayName = "DECK 02 - HABITATION RING";

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (SaveLoadManager.Instance != null)
            {
                SaveLoadManager.Instance.currentZoneName = zoneDisplayName;
            }
        }
    }
}
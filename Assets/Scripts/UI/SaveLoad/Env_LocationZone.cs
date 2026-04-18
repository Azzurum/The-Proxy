using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Env_LocationZone : MonoBehaviour
{
    [Header("Ship Navigation")]
    [Tooltip("Exact text to appear on the Memory Sync UI")]
    public string zoneDisplayName = "DECK 02 - HABITATION RING";

    private void Awake()
    {
        // Ensure this is strictly a trigger
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // When Kaelen walks into this invisible box, update the Save System's telemetry
        if (collision.CompareTag("Player"))
        {
            if (SaveLoadManager.Instance != null)
            {
                SaveLoadManager.Instance.currentZoneName = zoneDisplayName;
            }
        }
    }
}
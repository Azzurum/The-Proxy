using UnityEngine;

/// <summary>
/// A trigger volume that notifies the MetRigManager when the player enters or exits a signal-blocking "safe zone".
/// </summary>
public class FaradayZone : MonoBehaviour
{
    private MetRigManager _metRigManager;

    private void Start()
    {
        // Cache the manager reference on start to avoid expensive lookups during gameplay.
        _metRigManager = FindAnyObjectByType<MetRigManager>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (_metRigManager != null)
            {
                _metRigManager.inFaradayZone = true;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (_metRigManager != null)
            {
                _metRigManager.inFaradayZone = false;
            }
        }
    }
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A one-time trigger volume that activates a sequence of server racks with a ripple effect.
/// </summary>
public class ServerRowTrigger : MonoBehaviour
{
    [Tooltip("A list of all server racks that this trigger should activate.")]
    public List<DynamicServerRack> serversInRow;
    [Tooltip("The delay in seconds between each server's activation to create a ripple effect.")]
    public float rippleDelay = 0.08f;

    private bool hasTriggered = false;
    private WaitForSeconds _rippleDelayWait;

    private void Awake()
    {
        // Cache the WaitForSeconds object to prevent garbage collection in the coroutine loop.
        _rippleDelayWait = new WaitForSeconds(rippleDelay);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true; // Ensure this trigger only fires once.

            // Sort the servers by their distance to the player to create a natural-looking ripple effect originating from the player's position.
            serversInRow.Sort((a, b) => 
            {
                if (a == null) return 1; // Push null entries to the end.
                if (b == null) return -1; // Keep valid entries at the front.
                return Vector2.Distance(other.transform.position, a.transform.position)
                      .CompareTo(Vector2.Distance(other.transform.position, b.transform.position));
            });

            StartCoroutine(ActivateInSequence());
        }
    }

    private IEnumerator ActivateInSequence()
    {
        foreach (var server in serversInRow)
        {
            if (server != null) server.Activate();
            yield return _rippleDelayWait;
        }
    }
}
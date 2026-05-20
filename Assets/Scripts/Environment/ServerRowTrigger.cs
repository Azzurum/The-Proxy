using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ServerRowTrigger : MonoBehaviour
{
    [Tooltip("Drag all the server prefabs that should rise up when the player enters this zone.")]
    public List<DynamicServerRack> serversInRow;
    [Tooltip("The delay in seconds between each server activating for a ripple effect.")]
    public float rippleDelay = 0.08f;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true; // Lock it so it stays activated forever!

            // Sort the list of servers based on their distance to the player!
            serversInRow.Sort((a, b) => 
            {
                if (a == null) return 1;
                if (b == null) return -1;
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
            yield return new WaitForSeconds(rippleDelay);
        }
    }
}
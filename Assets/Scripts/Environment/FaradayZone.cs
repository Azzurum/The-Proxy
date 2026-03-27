using UnityEngine;

public class FaradayZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // When Kaelen steps INTO the safe room
        if (collision.CompareTag("Player"))
        {
            MetRigManager manager = FindFirstObjectByType<MetRigManager>();
            if (manager != null) manager.inFaradayZone = true;
            Debug.Log("<color=cyan>ENTERED FARADAY ZONE:</color> Magnetic dampeners active.");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // When Kaelen steps OUT of the safe room
        if (collision.CompareTag("Player"))
        {
            MetRigManager manager = FindFirstObjectByType<MetRigManager>();
            if (manager != null) manager.inFaradayZone = false;
            Debug.Log("<color=yellow>LEFT FARADAY ZONE:</color> Rig exposed to local network.");
        }
    }
}
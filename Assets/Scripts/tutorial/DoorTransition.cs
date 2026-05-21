using UnityEngine;

/// <summary>
/// Handles seamless, localized teleportation across doorways and toggles interior visual layers.
/// </summary>
public class DoorTransition : MonoBehaviour
{
    [Header("Teleport Settings")]
    [Tooltip("How far forward to slide the player when they step through.")]
    public Vector2 teleportOffset = new Vector2(0f, 2.5f);

    [Header("Visual Fade")]
    [Tooltip("If your room has a roof cover tilemap, drag it here to hide it when entering.")]
    public GameObject roomRoofCover;

    private bool _isTransitionActive = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !_isTransitionActive)
        {
            _isTransitionActive = true;

            Vector3 playerPos = other.transform.position;
            playerPos.x += teleportOffset.x;
            playerPos.y += teleportOffset.y;
            other.transform.position = playerPos;

            if (roomRoofCover != null)
            {
                roomRoofCover.SetActive(false);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _isTransitionActive = false;
        }
    }
}
using UnityEngine;

public class DoorTransition : MonoBehaviour
{
    [Header("Teleport Settings")]
    [Tooltip("How far forward to slide the player when they step through.")]
    public Vector2 teleportOffset = new Vector2(0f, 2.5f);

    [Header("Visual Fade (Optional)")]
    [Tooltip("If your room has a roof cover tilemap, drag it here to hide it when entering.")]
    public GameObject roomRoofCover;

    private bool isTransitionActive = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object stepping into the dark zone is our player
        if (other.CompareTag("Player") && !isTransitionActive)
        {
            isTransitionActive = true;

            // 1. Grab the player's current position
            Vector3 playerPos = other.transform.position;

            // 2. Add the offset to shift them smoothly up/past the wall collision mesh
            playerPos.x += teleportOffset.x;
            playerPos.y += teleportOffset.y;

            // 3. Apply the new position
            other.transform.position = playerPos;

            // 4. Cleanly reveal the new room interior if a roof cover object exists
            if (roomRoofCover != null)
            {
                roomRoofCover.SetActive(false);
            }

            Debug.Log("<color=lime>TRANSITION:</color> Player moved seamlessly into the next sector.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Reset the trigger flag so it's ready if they ever come back down
            isTransitionActive = false;
        }
    }
}
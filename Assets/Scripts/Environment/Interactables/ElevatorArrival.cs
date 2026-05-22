using System.Collections;
using UnityEngine;

/// <summary>
/// Manages the cinematic arrival sequence when a player enters a new level via an elevator.
/// </summary>
public class ElevatorArrival : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The speed at which the player character walks out of the elevator.")]
    public float walkOutSpeed = 2.5f;
    [Tooltip("Initial delay before the arrival sequence begins.")]
    public float initialDelay = 0.5f;
    [Tooltip("The duration of the elevator door opening animation.")]
    public float doorAnimationTime = 1.2f;

    [Header("References")]
    [Tooltip("The Animator component for the elevator doors.")]
    public Animator elevatorAnimator;
    [Tooltip("The position inside the elevator where the player is teleported to.")]
    public Transform walkInTarget; 
    [Tooltip("The final destination position for the player after walking out.")]
    public Transform walkOutTarget; 

    [Header("Linking")]
    [Tooltip("A unique ID that must match the ID of the ElevatorInteraction script from the previous scene.")]
    public string elevatorID; 

    private void Awake()
    {
        // Check if the last elevator used matches this elevator's ID to trigger the arrival sequence.
        string lastUsed = ElevatorManager.LastUsedElevatorID?.Trim();
        string currentID = elevatorID?.Trim();

        if (!string.IsNullOrEmpty(currentID) && currentID == lastUsed)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                // Temporarily disable all player sprites to prevent them from flashing on screen before the sequence starts.
                SpriteRenderer[] sprites = player.GetComponentsInChildren<SpriteRenderer>();
                foreach (SpriteRenderer s in sprites) 
                {
                    // Specifically deactivate the minimap blip object.
                    if (s.gameObject.name.Contains("Blip")) 
                    {
                        s.gameObject.SetActive(false); 
                        continue; 
                    }
                    s.enabled = false;
                    s.sortingOrder = -5; 
                }

                // Instantly move the player to the designated starting position inside the elevator.
                player.transform.position = walkInTarget.position;

                // Disable player controls and physics to give control to the cinematic.
                if (player.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    rb.linearVelocity = Vector2.zero; 
                    rb.bodyType = RigidbodyType2D.Kinematic; 
                    rb.interpolation = RigidbodyInterpolation2D.None; 
                }
                if (player.TryGetComponent<PlayerController>(out var movement))
                {
                    movement.enabled = false;
                }

                StartCoroutine(ArrivalSequence(player, sprites));
            }
        }
    }

    /// <summary>
    /// Controls the step-by-step cinematic of the player arriving and walking out of the elevator.
    /// </summary>
    private IEnumerator ArrivalSequence(GameObject player, SpriteRenderer[] sprites)
    {
        Animator playerAnim = player.GetComponentInChildren<Animator>();

        yield return new WaitForSeconds(initialDelay);

        // Open the elevator doors.
        if (elevatorAnimator != null) elevatorAnimator.Play("Elevator_Open"); 
        yield return new WaitForSeconds(doorAnimationTime);

        // Re-enable player sprites now that they are visibly walking out.
        foreach (SpriteRenderer s in sprites) 
        {
            if (s.gameObject.name.Contains("Blip")) 
            {
                s.gameObject.SetActive(true); 
                continue; 
            }
            s.enabled = true;
            s.sortingOrder = 20; 
        }

        // Animate the player walking to the exit position.
        if (playerAnim != null)
        {
            playerAnim.SetFloat("Speed", 1f); 
            playerAnim.SetFloat("Vertical", -1f); 
        }

        Vector3 targetPos = new Vector3(walkOutTarget.position.x, walkOutTarget.position.y, player.transform.position.z);
        while (Vector3.Distance(player.transform.position, targetPos) > 0.05f)
        {
            player.transform.position = Vector3.MoveTowards(player.transform.position, targetPos, walkOutSpeed * Time.deltaTime);
            yield return null;
        }

        // Restore player control and physics.
        if (playerAnim != null) playerAnim.SetFloat("Speed", 0f); 
        
        if (player.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.bodyType = RigidbodyType2D.Dynamic; 
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; 
        }
        
        if (player.TryGetComponent<PlayerController>(out var movement))
        {
            movement.enabled = true;
        }

        yield return new WaitForSeconds(0.5f);
        if (elevatorAnimator != null) elevatorAnimator.Play("Elevator_Close"); 
    }
}
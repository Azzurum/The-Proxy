using System.Collections;
using UnityEngine;

public class ElevatorArrival : MonoBehaviour
{
    [Header("Settings")]
    public float walkOutSpeed = 2.5f;
    public float initialDelay = 0.5f;

    [Header("References")]
    public Animator elevatorAnimator;
    public Transform walkInTarget; 
    public Transform walkOutTarget; // This is the destination outside the doors

    [Header("Linking")]
    public string elevatorID; // Match this to the Departure ID (e.g., "Elevator_1")

    private void Start()
    {
        // DEBUG: See what ID the manager is holding and what this elevator's ID is
        Debug.Log($"Checking Elevator: {elevatorID}. Last used was: {ElevatorManager.LastUsedElevatorID}");

        if (elevatorID == ElevatorManager.LastUsedElevatorID)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            
            if (player != null)
            {
                Debug.Log("Elevator Match Found! Teleporting Kaelen...");
                player.transform.position = walkInTarget.position;
                StartCoroutine(ArrivalSequence(player));
            }
            else
            {
                Debug.LogError("Elevator matched, but could not find an object tagged 'Player'!");
            }
        }
    }

    private IEnumerator ArrivalSequence(GameObject player)
    {
        // 1. PREPARE THE GHOST (Unity 6 Syntax)
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        PlayerController movement = player.GetComponent<PlayerController>();
        Animator playerAnim = player.GetComponentInChildren<Animator>();

        if (rb != null) 
        {
            rb.linearVelocity = Vector2.zero; //
            rb.bodyType = RigidbodyType2D.Kinematic; //
            rb.interpolation = RigidbodyInterpolation2D.None; //
        }
        
        if (movement != null) movement.enabled = false; //

        // Force Kaelen to the background layer so he is BEHIND the doors
        SpriteRenderer[] sprites = player.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer s in sprites) 
        {
            if (s.gameObject.name.Contains("Blip")) s.gameObject.SetActive(false); //
            s.sortingOrder = -5; //
        }

        yield return new WaitForSeconds(initialDelay);

        // 2. OPEN DOORS
        if (elevatorAnimator != null) elevatorAnimator.Play("Elevator_Open"); //
        yield return new WaitForSeconds(1f);

        // 3. WALK OUT
        if (playerAnim != null)
        {
            playerAnim.SetFloat("Speed", 1f); //
            playerAnim.SetFloat("Vertical", -1f); // Facing DOWN to walk out
        }

        while (Vector3.Distance(player.transform.position, walkOutTarget.position) > 0.05f)
        {
            // Z-Axis Lock to prevent disappearing
            Vector3 targetPos = new Vector3(walkOutTarget.position.x, walkOutTarget.position.y, player.transform.position.z);
            player.transform.position = Vector3.MoveTowards(player.transform.position, targetPos, walkOutSpeed * Time.deltaTime);
            yield return null;
        }

        // 4. RESTORE CONTROL & LAYERING
        foreach (SpriteRenderer s in sprites) 
        {
            s.sortingOrder = 6; // Back to normal layer
            if (s.gameObject.name.Contains("Blip")) s.gameObject.SetActive(true); //
        }

        if (playerAnim != null) playerAnim.SetFloat("Speed", 0f);
        
        if (rb != null) 
        {
            rb.bodyType = RigidbodyType2D.Dynamic; // Physics back on
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; 
        }
        
        if (movement != null) movement.enabled = true; // Kaelen can move again!

        // 5. CLOSE DOORS BEHIND HIM
        yield return new WaitForSeconds(0.5f);
        if (elevatorAnimator != null) elevatorAnimator.Play("Elevator_Close"); //
    }
}
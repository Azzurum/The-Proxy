using System.Collections;
using UnityEngine;

public class ElevatorArrival : MonoBehaviour
{
    [Header("Settings")]
    public float walkOutSpeed = 2.5f;
    public float initialDelay = 0.5f;
    public float doorAnimationTime = 1.2f;

    [Header("References")]
    public Animator elevatorAnimator;
    public Transform walkInTarget; 
    public Transform walkOutTarget; 

    [Header("Linking")]
    public string elevatorID; 

    private void Awake()
    {
        string lastUsed = ElevatorManager.LastUsedElevatorID?.Trim();
        string currentID = elevatorID?.Trim();

        if (!string.IsNullOrEmpty(currentID) && currentID == lastUsed)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            
            if (player != null)
            {
                // 1. COMPLETELY DISABLE RENDERING
                SpriteRenderer[] sprites = player.GetComponentsInChildren<SpriteRenderer>();
                foreach (SpriteRenderer s in sprites) 
                {
                    if (s.gameObject.name.Contains("Blip")) 
                    {
                        s.gameObject.SetActive(false); 
                        continue; 
                    }
                    s.enabled = false; // Turn off the sprite completely to prevent flash
                    s.sortingOrder = -5; 
                }

                // 2. TELEPORT HIM
                player.transform.position = walkInTarget.position;

                // 3. FREEZE MOVEMENT 
                Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
                PlayerController movement = player.GetComponent<PlayerController>();
                if (rb != null) 
                {
                    rb.linearVelocity = Vector2.zero; 
                    rb.bodyType = RigidbodyType2D.Kinematic; 
                    rb.interpolation = RigidbodyInterpolation2D.None; 
                }
                if (movement != null) movement.enabled = false;

                // 4. START THE REST OF THE CINEMATIC
                StartCoroutine(ArrivalSequence(player, sprites));
            }
        }
    }

    private IEnumerator ArrivalSequence(GameObject player, SpriteRenderer[] sprites)
    {
        Animator playerAnim = player.GetComponentInChildren<Animator>();

        // 1. WAIT IN THE DARK
        yield return new WaitForSeconds(initialDelay);

        // 2. OPEN DOORS
        if (elevatorAnimator != null) elevatorAnimator.Play("Elevator_Open"); 
        yield return new WaitForSeconds(doorAnimationTime);

        // 3. TURN SPRITES BACK ON BEFORE WALKING
        foreach (SpriteRenderer s in sprites) 
        {
            if (s.gameObject.name.Contains("Blip")) 
            {
                s.gameObject.SetActive(true); 
                continue; 
            }
            s.enabled = true; // Turn the sprite back on!
            s.sortingOrder = 20; 
        }

        // 4. WALK OUT
        if (playerAnim != null)
        {
            playerAnim.SetFloat("Speed", 1f); 
            playerAnim.SetFloat("Vertical", -1f); 
        }

        while (Vector3.Distance(player.transform.position, walkOutTarget.position) > 0.05f)
        {
            Vector3 targetPos = new Vector3(walkOutTarget.position.x, walkOutTarget.position.y, player.transform.position.z);
            player.transform.position = Vector3.MoveTowards(player.transform.position, targetPos, walkOutSpeed * Time.deltaTime);
            yield return null;
        }

        // 5. RESTORE CONTROL
        if (playerAnim != null) playerAnim.SetFloat("Speed", 0f); 
        
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) 
        {
            rb.bodyType = RigidbodyType2D.Dynamic; 
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; 
        }
        
        PlayerController movement = player.GetComponent<PlayerController>();
        if (movement != null) movement.enabled = true; 

        yield return new WaitForSeconds(0.5f);
        if (elevatorAnimator != null) elevatorAnimator.Play("Elevator_Close"); 
    }
}
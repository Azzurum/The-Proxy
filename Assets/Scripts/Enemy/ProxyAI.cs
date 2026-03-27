using UnityEngine;

public class ProxyAI : MonoBehaviour
{
    [Header("Targeting")]
    public Transform targetPlayer;
    private Vector2 lastKnownPosition; // Where did Kaelen vanish?
    private bool hasLastKnownPosition = false;

    // WANDER VARIABLES
    private bool isWandering = false;
    private Vector2 wanderTarget;
    private float wanderWaitTimer = 0f;
    public float searchRadius = 4f; // How wide of an area it searches

    [Header("Perception System")]
    public float hearingRadius = 6f; // Distance to hear footsteps
    private Vector3 previousPlayerPos; // Used to check if Kaelen is moving

    [Header("Movement Stats")]
    public float baseSpeed = 0.5f; // Very slow creeping speed
    public float sprintSpeed = 3.5f; // Terrifying sprint speed

    private float currentSpeed;
    private SpriteRenderer spriteRenderer;

    [Header("Stun Resistance")]
    private bool isStunned = false;
    private int stunCount = 0;
    private float stunMemoryTimer = 0f;
    private float memoryResetTime = 60f; // Proxy forgets stuns after 60 seconds

    [Header("Repulsor State")]
    private bool isKnockedBack = false;

    void Start()
    {
        currentSpeed = baseSpeed;
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Auto-find player if not assigned
        if (targetPlayer == null)
        {
            GameObject player = GameObject.Find("Player_Kaelen");
            if (player != null) targetPlayer = player.transform;
        }

        // Initialize position tracking
        if (targetPlayer != null) previousPlayerPos = targetPlayer.position;
    }

    void Update()
    {
        // 1. Manage Stun Memory (60s timer)
        if (stunCount > 0)
        {
            stunMemoryTimer -= Time.deltaTime;
            if (stunMemoryTimer <= 0)
            {
                stunCount = 0; // The Proxy forgot! It's vulnerable again.
                Debug.Log("PROXY AI: Stun resistance reset.");
            }
        }

        // 2. Continuous Hearing Detection (Footsteps)
        if (targetPlayer != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, targetPlayer.position);
            float playerMovement = Vector3.Distance(targetPlayer.position, previousPlayerPos);

            // If Kaelen is moving AND is within earshot, the Proxy hears him!
            if (playerMovement > 0.001f && distanceToPlayer <= hearingRadius)
            {
                lastKnownPosition = targetPlayer.position;
                hasLastKnownPosition = true;
                isWandering = false; // Snap to attention to investigate the sound!
            }

            // Save position to compare next frame
            previousPlayerPos = targetPlayer.position;
        }

        // 3. Only hunt if not stunned AND not flying backward!
        if (!isStunned && !isKnockedBack)
        {
            HuntPlayer();
        }
    }

    private void HuntPlayer()
    {
        MetRigManager manager = FindFirstObjectByType<MetRigManager>();

        // Kaelen is ONLY exposed if the Rig is screaming AND he is not shielded
        bool isSignalEmitting = (manager != null && manager.isRigOpen && !manager.inFaradayZone);

        // SCENARIO A: M.E.T. Rig is open! Perfect tracking.
        if (isSignalEmitting && targetPlayer != null)
        {
            lastKnownPosition = targetPlayer.position;
            hasLastKnownPosition = true;
            isWandering = false; // Snap out of wander mode!

            MoveTowardsTarget(targetPlayer.position, currentSpeed);
        }
        // SCENARIO B: Stealth Mode (Rig closed). Rely on hearing and memory.
        else if (hasLastKnownPosition)
        {
            // Step 1: Creep to the exact spot it last heard/saw a signal
            if (!isWandering)
            {
                if (Vector2.Distance(transform.position, lastKnownPosition) > 0.1f)
                {
                    MoveTowardsTarget(lastKnownPosition, baseSpeed);
                }
                else
                {
                    // It reached the spot and Kaelen isn't here! Start sweeping the area.
                    isWandering = true;
                    PickNewWanderTarget();
                }
            }
            // Step 2: Actively sweep the area
            else
            {
                if (Vector2.Distance(transform.position, wanderTarget) > 0.1f)
                {
                    // Walk to the random search point
                    MoveTowardsTarget(wanderTarget, baseSpeed);
                }
                else
                {
                    // It reached the search point. Pause, "listen", then pick a new spot.
                    wanderWaitTimer -= Time.deltaTime;
                    if (wanderWaitTimer <= 0f)
                    {
                        PickNewWanderTarget();
                    }
                }
            }
        }
    }

    // --- COMBAT AWARENESS ---
    // Firing a weapon instantly updates the AI's tracking
    public void OnCombatAction(Vector3 actionPosition)
    {
        Debug.Log("<color=red>PROXY AI: Combat sound detected! Exact location locked.</color>");
        lastKnownPosition = actionPosition;
        hasLastKnownPosition = true;
        isWandering = false; // Stop sweeping and go straight to the gunshot
    }

    // The Manager will call this to wake the monster up!
    public void OnSignalSpike(bool isListening)
    {
        if (isListening)
        {
            currentSpeed = sprintSpeed;
            if (spriteRenderer != null) spriteRenderer.color = Color.red; // Turn blood red
        }
        else
        {
            currentSpeed = baseSpeed;
            if (spriteRenderer != null) spriteRenderer.color = Color.magenta; // Return to normal color
        }
    }

    // This built-in Unity function fires the exact frame the Proxy touches another collider
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Did we hit the player?
        if (collision.CompareTag("Player"))
        {
            KillPlayer();
        }
    }

    private void KillPlayer()
    {
        Debug.LogError("CRITICAL FAILURE: Proxy has caught Kaelen! GAME OVER.");

        if (spriteRenderer != null) spriteRenderer.color = Color.black;

        // 1. Find the new Game Over Manager
        GameOverManager manager = FindFirstObjectByType<GameOverManager>();

        // 2. Tell it to trigger the screen and freeze time!
        if (manager != null)
        {
            manager.TriggerGameOver();
        }
        else
        {
            // Fallback just in case the manager is missing
            Time.timeScale = 0f;
        }
    }

    public void ApplyStun()
    {
        stunCount++;
        stunMemoryTimer = memoryResetTime; // Reset the 60-second memory clock

        float duration = 0f;

        // Calculate resistance based on the GDD rules!
        if (stunCount == 1) duration = 3f;
        else if (stunCount == 2) duration = 1.5f;
        else
        {
            Debug.LogWarning("PROXY AI: Immune to Stunner!");
            return; // Stay red/magenta, do not stop moving!
        }

        Debug.Log($"PROXY AI: Stunned for {duration} seconds!");
        StartCoroutine(StunRoutine(duration));
    }

    private System.Collections.IEnumerator StunRoutine(float time)
    {
        isStunned = true;

        // Visual feedback: turn blue/cyan to show it's electrocuted
        if (spriteRenderer != null) spriteRenderer.color = Color.cyan;

        yield return new WaitForSeconds(time);

        isStunned = false;

        // Return to normal color
        MetRigManager uiManager = FindFirstObjectByType<MetRigManager>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = (uiManager != null && uiManager.isRigOpen) ? Color.red : Color.magenta;
        }
    }

    public void ApplyRepulsor(Vector3 playerPosition, float knockbackDistance)
    {
        // Don't knock it back if it's already flying backward
        if (!isKnockedBack)
        {
            StartCoroutine(KnockbackRoutine(playerPosition, knockbackDistance));
        }
    }

    private System.Collections.IEnumerator KnockbackRoutine(Vector3 playerPosition, float distance)
    {
        isKnockedBack = true;

        // 1. Force math into 2D so it doesn't accidentally shoot into the Z-axis floor
        Vector2 myPos2D = new Vector2(transform.position.x, transform.position.y);
        Vector2 playerPos2D = new Vector2(playerPosition.x, playerPosition.y);

        // 2. Calculate the exact direction AWAY from Kaelen
        Vector2 pushDirection = (myPos2D - playerPos2D).normalized;
        Vector2 targetPosition = myPos2D + (pushDirection * distance);

        float knockbackSpeed = 25f; // Very fast, violent shove

        // 3. Slide the monster backward until it hits the target spot
        while (Vector2.Distance(myPos2D, targetPosition) > 0.1f)
        {
            myPos2D = Vector2.MoveTowards(myPos2D, targetPosition, knockbackSpeed * Time.deltaTime);

            // Re-apply to the actual transform, keeping original Z
            transform.position = new Vector3(myPos2D.x, myPos2D.y, transform.position.z);

            yield return null; // Wait for the next frame
        }

        isKnockedBack = false;
    }

    // Helper method to handle the physical sliding and rotating
    private void MoveTowardsTarget(Vector2 target, float speed)
    {
        transform.position = Vector2.MoveTowards(transform.position, target, speed * Time.deltaTime);

        Vector2 direction = target - (Vector2)transform.position;
        if (direction != Vector2.zero) // Prevent errors if it's exactly on the spot
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }
    }

    // Helper method to pick a random spot to search
    private void PickNewWanderTarget()
    {
        // Pick a random point in a circle around the last place it saw Kaelen
        Vector2 randomDirection = Random.insideUnitCircle * searchRadius;
        wanderTarget = lastKnownPosition + randomDirection;

        // Wait 1 to 3 seconds at the spot before moving again (makes it look like it's listening!)
        wanderWaitTimer = Random.Range(1f, 3f);
    }
}
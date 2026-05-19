using UnityEngine;

public class ProxyAI : MonoBehaviour
{
    [Header("Targeting")]
    public Transform targetPlayer;
    private Vector2 lastKnownPosition;
    private bool hasLastKnownPosition = false;

    public enum AIState
    {
        Idle,
        Investigating,
        Hunting,
        Wandering,
        Attacking,
        Stunned,
        KnockedBack,
        Distracted
    }

    [Header("AI State Machine")]
    public AIState currentState = AIState.Idle;
    public int maxWandersBeforeIdle = 3;
    private float stateTimer = 0f;
    private int wanderCount = 0;
    private Vector2 wanderTarget;
    private float wanderWaitTimer = 0f;
    public float searchRadius = 4f; 
    public Transform[] patrolWaypoints;

    [Header("Perception System")]
    public float hearingRadius = 6f;
    private Vector3 previousPlayerPos;

    [Header("Movement Stats")]
    public float baseSpeed = 2.5f; // Patrol speed (slower than Kaelen)
    public float sprintSpeed = 4.6f; // Hunt speed (Slightly faster than Kaelen, DbD Killer pace!)
    private float currentSpeed;

    [Header("Stun Resistance")]
    private int stunCount = 0;
    private float stunMemoryTimer = 0f;
    private float memoryResetTime = 60f;

    [Header("Signal Response")]
    [SerializeField] private float immediateSignalDistance = 3f;
    [SerializeField] private float delayedSignalDistance = 10f;
    [SerializeField] private float investigateSignalDistance = 20f;
    [SerializeField] private float delayedHuntSeconds = 2f;
    [SerializeField] private float signalSpeedMultiplier = 1.2f; // 20% buff when Rig is open
    private bool isSignalEmpowered = false; // Tracks if the Proxy is enraged by the open inventory

    [Header("Knockback")]
    [SerializeField] private float knockbackSpeed = 25f;

    [Header("Attack Behavior")]
    public float attackRecovery = 1.5f;
    public float attackRange = 1.5f;

    // Internal State Variables
    private bool isPlayerInMeleeRange = false;
    private bool canAttack = true;
    private Vector2 moveTarget;
    private bool hasMoveTarget = false;
    private Coroutine delayedHuntCoroutine;

    // Components & Managers
    [Header("Depth Sorting")]
    [Tooltip("Make sure this exactly matches Kaelen's sorting layer!")]
    public string sortingLayerName = "Player";
    public float depthOffset = -0.5f; // Where the Proxy's feet are relative to its center
    
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    private MetRigManager metRigManager;
    private GameOverManager gameOverManager;
    private InventoryManager inventoryManager;

    [Header("Audio SFX")]
    public AudioSource audioSource;
    public AudioClip sfxAttackSwing;
    public AudioClip sfxAttackHit;
    public AudioClip sfxStunned;

    void Start()
    {
        currentSpeed = baseSpeed;

        // Setup Components
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.sortingLayerName = sortingLayerName;
        
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        // PHYSICS FAILSAFE: The Proxy must be Dynamic to collide with the server maze!
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.mass = 1000f; // Make it a brick wall so Kaelen can't push it!
        rb.simulated = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;

        // Auto-find player if not assigned
        if (targetPlayer == null)
        {
            // Use the tag to safely find the player regardless of the GameObject's name
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) player = GameObject.Find("Player_Kaelen");
            
            if (player != null) targetPlayer = player.transform;
        }

        // Cache global managers safely
        metRigManager = FindAnyObjectByType<MetRigManager>();
        gameOverManager = FindAnyObjectByType<GameOverManager>();
        inventoryManager = FindAnyObjectByType<InventoryManager>();

        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (targetPlayer != null) previousPlayerPos = targetPlayer.position;
    }

    void Update()
    {
        if (targetPlayer == null) return; // Safety check

        ManageStunMemory();
        UpdatePerception();

        // Global Attack Trigger Check
        if (isPlayerInMeleeRange && canAttack && CanInterruptState(AIState.Attacking))
        {
            ChangeState(AIState.Attacking);
            return; // Halt other logic while attacking
        }

        // Execute State Behaviors
        switch (currentState)
        {
            case AIState.Hunting:
                if (isSignalEmpowered || isPlayerInMeleeRange)
                {
                    lastKnownPosition = targetPlayer.position;
                    SetMoveTarget(targetPlayer.position, sprintSpeed);
                }
                else
                {
                    // Signal lost! Keep running to the last known point, but downgrade to Investigate once reached
                    if (Vector2.Distance(transform.position, moveTarget) <= 0.1f)
                    {
                        ChangeState(AIState.Investigating);
                    }
                }
                break;

            case AIState.Investigating:
                if (Vector2.Distance(transform.position, moveTarget) <= 0.1f)
                {
                    stateTimer += Time.deltaTime;
                    if (stateTimer >= 1f && !isPlayerInMeleeRange) // Organic Pause: Stand at the location and "look around" for 1 second!
                    {
                        ChangeState(AIState.Wandering);
                    }
                }
                break;

            case AIState.Wandering:
                if (Vector2.Distance(transform.position, moveTarget) <= 0.1f)
                {
                    stateTimer += Time.deltaTime;
                    if (stateTimer >= wanderWaitTimer)
                    {
                        wanderCount++;
                        
                        // If the monster searched the area 3 times and found nothing, it gives up and idles.
                        if (wanderCount >= maxWandersBeforeIdle) ChangeState(AIState.Idle);
                        else PickNewWanderTarget();
                    }
                }
                break;
        }
        
        UpdateAnimationSpeed();

        // DYNAMIC DEPTH SORTING: Update sorting order based on Y position so the Proxy can walk behind things!
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = Mathf.RoundToInt((transform.position.y + depthOffset) * -10f);
        }
    }

    // --- STATE MACHINE LOGIC ---

    private bool CanInterruptState(AIState newState)
    {
        if (currentState == AIState.Stunned || currentState == AIState.KnockedBack) return false;
        if (currentState == AIState.Attacking && newState != AIState.Stunned && newState != AIState.KnockedBack) return false;
        if (currentState == AIState.Distracted && newState != AIState.Stunned && newState != AIState.KnockedBack) return false;
        return true;
    }

    private void ChangeState(AIState newState)
    {
        if (currentState == newState) return;
        
        currentState = newState;
        stateTimer = 0f;

        switch (newState)
        {
            case AIState.Idle:
                hasMoveTarget = false;
                currentSpeed = 0f;
                wanderCount = 0;
                break;
                
            case AIState.Hunting:
                currentSpeed = sprintSpeed;
                SetMoveTarget(targetPlayer.position, sprintSpeed);
                break;
                
            case AIState.Investigating:
                currentSpeed = baseSpeed;
                if (hasLastKnownPosition) SetMoveTarget(lastKnownPosition, baseSpeed);
                break;
                
            case AIState.Wandering:
                currentSpeed = baseSpeed;
                PickNewWanderTarget();
                break;
                
            case AIState.Attacking:
                StartCoroutine(AttackRoutine());
                break;
        }
    }

    private void ManageStunMemory()
    {
        if (stunCount > 0)
        {
            stunMemoryTimer -= Time.deltaTime;
            if (stunMemoryTimer <= 0)
            {
                stunCount = 0;
                Debug.Log("PROXY AI: Stun resistance reset.");
            }
        }
    }

    private void UpdatePerception()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, targetPlayer.position);
        float playerMovement = Vector3.Distance(targetPlayer.position, previousPlayerPos);

        // Continuous Hearing Detection (Footsteps)
        if (playerMovement > 0.001f && distanceToPlayer <= hearingRadius)
        {
            lastKnownPosition = targetPlayer.position;
            hasLastKnownPosition = true;
            
            if (CanInterruptState(AIState.Investigating) && currentState != AIState.Hunting) 
            {
                ChangeState(AIState.Investigating); 
            }
            else if (currentState == AIState.Hunting)
            {
                SetMoveTarget(targetPlayer.position, sprintSpeed);
            }
        }
        previousPlayerPos = targetPlayer.position;
    }

    // --- EXTERNAL COMBAT TRIGGERS ---

    public void OnCombatAction(Vector3 actionPosition)
    {
        Debug.Log("<color=red>PROXY AI: Combat sound detected! Exact location locked.</color>");
        lastKnownPosition = actionPosition;
        hasLastKnownPosition = true;
        
        if (CanInterruptState(AIState.Investigating))
        {
            ChangeState(AIState.Investigating);
        }
    }

    public void OnSignalSpike(bool isListening, float distance)
    {
        isSignalEmpowered = isListening; // Activates the 20% buff while the signal is active!

        if (isListening && distance >= 0)
        {
            lastKnownPosition = targetPlayer.position;
            hasLastKnownPosition = true;

            // LORE UPDATE: Always go into hunting mode when the inventory opens!
            if (distance < delayedSignalDistance) 
            {
                ChangeState(AIState.Hunting);
                Debug.Log("PROXY: Signal detected! Immediate hunt.");
            }
            else 
            {
                // If they are super far away, still hunt, but give the player a brief 2-second warning delay
                if (delayedHuntCoroutine != null) StopCoroutine(delayedHuntCoroutine);
                delayedHuntCoroutine = StartCoroutine(DelayedHunt(delayedHuntSeconds));
                Debug.Log("PROXY: Signal detected far! Delayed hunt.");
            }
        }
        else
        {
            if (delayedHuntCoroutine != null) 
            {
                StopCoroutine(delayedHuntCoroutine);
                Debug.Log("PROXY: Delayed hunt canceled - signal turned off!");
            }
            
            if (currentState == AIState.Hunting)
            {
                ChangeState(AIState.Investigating);
            }
        }
    }

    private System.Collections.IEnumerator DelayedHunt(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (CanInterruptState(AIState.Hunting))
        {
            ChangeState(AIState.Hunting);
            Debug.Log("PROXY: Delayed hunt activated!");
        }
    }

    // --- ATTACK LOGIC ---

    private System.Collections.IEnumerator AttackRoutine()
    {
        canAttack = false;
        hasMoveTarget = false; // Stop moving
        
        currentSpeed = 0f;

        // Snap direction to face the player right before triggering the attack animation
        if (targetPlayer != null && animator != null)
        {
            Vector2 dir = (targetPlayer.position - transform.position).normalized;
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            {
                animator.SetFloat("Direction", 1f); // Side Attack
                if (spriteRenderer != null) spriteRenderer.flipX = dir.x < 0;
            }
            else
            {
                if (dir.y > 0)
                {
                    animator.SetFloat("Direction", 1f); // Up uses Side Attack
                    if (spriteRenderer != null)
                    {
                        if (dir.x < -0.01f) spriteRenderer.flipX = true;
                        else if (dir.x > 0.01f) spriteRenderer.flipX = false;
                    }
                }
                else
                {
                    animator.SetFloat("Direction", 0f); // Down Attack
                    if (spriteRenderer != null) spriteRenderer.flipX = false;
                }
            }
            animator.SetTrigger("Attack");
        }

        Debug.Log("PROXY: Committing attack. Waiting for Animation Events...");
        yield break;
    }

    // --- ANIMATION EVENTS ---

    // 1. Call this from the exact frame the claw swings!
    public void AnimEvent_Strike()
    {
        if (audioSource != null) audioSource.PlayOneShot(sfxAttackSwing != null ? sfxAttackSwing : ProceduralAudioGen.GenerateWhoosh());

        if (currentState == AIState.Stunned || currentState == AIState.KnockedBack) return; // Prevent damage if interrupted!

        if (targetPlayer != null && Vector2.Distance(transform.position, targetPlayer.position) <= attackRange)
        {
            ExecuteAttack();
        }
        else
        {
            Debug.Log("PROXY: Attack missed! Kaelen escaped the strike zone.");
        }
    }

    // 2. Call this from the very last frame of the attack animation!
    public void AnimEvent_EndAttack()
    {
        if (currentState == AIState.Attacking)
        {
            if (isSignalEmpowered || isPlayerInMeleeRange)
            {
                ChangeState(AIState.Hunting); // Keep the pressure on!
            }
            else
            {
                ChangeState(AIState.Investigating); // Return to searching for the player
            }
        }
        StartCoroutine(RecoveryRoutine());
    }

    private System.Collections.IEnumerator RecoveryRoutine()
    {
        // Attack recovers 20% faster when empowered!
        float activeRecovery = isSignalEmpowered ? attackRecovery / signalSpeedMultiplier : attackRecovery;
        yield return new WaitForSeconds(activeRecovery);
        canAttack = true;
    }

    private void ExecuteAttack()
    {
        if (audioSource != null) audioSource.PlayOneShot(sfxAttackHit != null ? sfxAttackHit : ProceduralAudioGen.GenerateStaticGlitch(0.4f));
        Debug.Log("PROXY: Attack landed. Inventory corruption injected.");
        
        // Trigger a violent screen shake!
        CameraFollow cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null)
        {
            cam.TriggerShake(0.3f, 0.5f);
        }

        // Flash the screen red!
        if (ScreenEffectManager.Instance != null)
        {
            ScreenEffectManager.Instance.TriggerFlash(new Color(1f, 0f, 0f, 0.6f), 0.3f);
        }

        // Inject the corruption damage
        if (inventoryManager != null)
        {
            inventoryManager.AddCorruptionRow();
        }
        else
        {
            Debug.LogWarning("PROXY: Unable to apply corruption because InventoryManager is missing.");
        }
    }

    private void KillPlayer()
    {
        Debug.LogError("CRITICAL FAILURE: Mother has completely taken over! GAME OVER.");
        if (gameOverManager != null)
        {
            gameOverManager.TriggerGameOver();
        }
        else
        {
            Time.timeScale = 0f;
        }
    }

    // --- PHYSICS & COLLISION ---

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player")) isPlayerInMeleeRange = true;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player")) isPlayerInMeleeRange = false;
    }

    // --- DEFENSIVE REACTIONS ---

    public void ApplyStun()
    {
        if (currentState == AIState.Stunned) return;
        
        canAttack = true; // Reset attack readiness

        stunCount++;
        stunMemoryTimer = memoryResetTime; 

        float duration = 0f;
        if (stunCount == 1) duration = 3f;
        else if (stunCount == 2) duration = 1.5f;
        else
        {
            Debug.LogWarning("PROXY AI: Immune to Stunner!");
            return; 
        }

        Debug.Log($"PROXY AI: Stunned for {duration} seconds!");
        if (audioSource != null) audioSource.PlayOneShot(sfxStunned != null ? sfxStunned : ProceduralAudioGen.GenerateErrorBuzz(80f, 1.5f));
        
        ChangeState(AIState.Stunned);
        StartCoroutine(StunRoutine(duration));
    }

    private System.Collections.IEnumerator StunRoutine(float time)
    {
        hasMoveTarget = false; // Halt movement
        yield return new WaitForSeconds(time);
        if (currentState == AIState.Stunned)
        {
            ChangeState(AIState.Investigating); // Wake up and search!
        }
    }

    public void ApplyRepulsor(Vector3 playerPosition, float knockbackDistance)
    {
        if (currentState != AIState.KnockedBack)
        {
            canAttack = true;
            ChangeState(AIState.KnockedBack);
            StartCoroutine(KnockbackRoutine(playerPosition, knockbackDistance));
        }
    }

    private System.Collections.IEnumerator KnockbackRoutine(Vector3 playerPosition, float distance)
    {
        hasMoveTarget = false;

        Vector2 myPos2D = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 playerPos2D = new Vector2(playerPosition.x, playerPosition.y);
        Vector2 pushDirection = (myPos2D - playerPos2D).normalized;
        Vector2 targetPosition = myPos2D + (pushDirection * distance);

        while (Vector2.Distance(myPos2D, targetPosition) > 0.1f)
        {
            myPos2D = Vector2.MoveTowards(myPos2D, targetPosition, knockbackSpeed * Time.fixedDeltaTime);
            if (rb != null) rb.MovePosition(myPos2D);
            else transform.position = new Vector3(myPos2D.x, myPos2D.y, transform.position.z);
            
            yield return new WaitForFixedUpdate();
        }
        if (currentState == AIState.KnockedBack)
        {
            ChangeState(AIState.Investigating); // Wake up angry!
        }
    }

    public void DistractToLocation(Vector3 distractionPos, float duration)
    {
        if (CanInterruptState(AIState.Distracted))
        {
            ChangeState(AIState.Distracted);
            StartCoroutine(DistractionRoutine(distractionPos, duration));
        }
    }

    private System.Collections.IEnumerator DistractionRoutine(Vector3 distractionPos, float duration)
    {
        SetMoveTarget(distractionPos, sprintSpeed);
        yield return new WaitForSeconds(duration);
        if (currentState == AIState.Distracted)
        {
            ChangeState(AIState.Investigating);
        }
    }

    // --- MOVEMENT EXECUTION ---

    private void FixedUpdate()
    {
        if (rb != null) rb.linearVelocity = Vector2.zero; // Immediately kill any sliding momentum!

        if (!hasMoveTarget || currentState == AIState.Stunned || currentState == AIState.KnockedBack || currentState == AIState.Attacking || currentState == AIState.Idle) return;
        
        // Apply the 20% speed buff if the M.E.T. Rig is emitting a signal
        float activeSpeed = isSignalEmpowered ? currentSpeed * signalSpeedMultiplier : currentSpeed;
        Vector2 currentPosition = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 newPosition = Vector2.MoveTowards(currentPosition, moveTarget, activeSpeed * Time.fixedDeltaTime);
        
        if (rb != null) rb.MovePosition(newPosition);
        else transform.position = new Vector3(newPosition.x, newPosition.y, transform.position.z);
        
        if (Vector2.Distance(currentPosition, moveTarget) <= 0.05f)
        {
            hasMoveTarget = false;
        }

        UpdateAnimatorDirection(moveTarget);
    }

    private void SetMoveTarget(Vector2 target, float speed)
    {
        moveTarget = target;
        hasMoveTarget = true;
        currentSpeed = speed;
        UpdateAnimatorDirection(target);
    }

    private void PickNewWanderTarget()
    {
        // If you assigned specific patrol waypoints, pick the closest one!
        if (patrolWaypoints != null && patrolWaypoints.Length > 0)
        {
            Transform closestWaypoint = null;
            float closestDistance = float.MaxValue;
            Vector2 currentPosition = transform.position;

            foreach (Transform waypoint in patrolWaypoints)
            {
                if (waypoint == null) continue;

                float distance = Vector2.Distance(currentPosition, waypoint.position);

                // Ignore the waypoint we are currently standing on so it actually moves!
                if (distance < 1.0f) continue;

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestWaypoint = waypoint;
                }
            }

            if (closestWaypoint != null)
            {
                wanderTarget = closestWaypoint.position;
            }
            else
            {
                // Failsafe if it couldn't find a valid one
                wanderTarget = patrolWaypoints[Random.Range(0, patrolWaypoints.Length)].position;
            }
        }
        else // Fallback: Just wander blindly using math if no waypoints exist
        {
            Vector2 randomDirection = Random.insideUnitCircle * searchRadius;
            wanderTarget = lastKnownPosition + randomDirection;
        }

        wanderWaitTimer = Random.Range(1f, 3f);
        stateTimer = 0f;
        SetMoveTarget(wanderTarget, baseSpeed);
    }

    // --- ANIMATION CONTROLLER ---

    private void UpdateAnimatorDirection(Vector2 target)
    {
        if (animator == null) return;
        
        Vector2 currentPosition = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 direction = (target - currentPosition).normalized;
        
        if (direction.sqrMagnitude > 0.01f && hasMoveTarget && currentState != AIState.Stunned && currentState != AIState.Attacking)
        {
            if (spriteRenderer != null)
            {
                if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                {
                    animator.SetFloat("Direction", 1f); // Side
                    spriteRenderer.flipX = direction.x < 0;
                }
                else
                {
                    if (direction.y > 0)
                    {
                        animator.SetFloat("Direction", 1f); // Up uses Side animation
                        if (direction.x < -0.01f) spriteRenderer.flipX = true;
                        else if (direction.x > 0.01f) spriteRenderer.flipX = false;
                    }
                    else
                    {
                        animator.SetFloat("Direction", 0f); // Down
                        spriteRenderer.flipX = false;
                    }
                }
                
                spriteRenderer.flipY = false; // Never turn upside down anymore!
            }
        }
    }

    private void UpdateAnimationSpeed()
    {
        if (animator == null) return;
        
        float activeMultiplier = isSignalEmpowered ? signalSpeedMultiplier : 1f;

        if (currentState == AIState.Stunned || (!hasMoveTarget && currentState != AIState.Attacking))
        {
            animator.SetFloat("Speed", 0f);
            animator.speed = 1f; // CRITICAL: Unpause the component so Idle animations can play!
        }
        else if (currentState == AIState.Attacking)
        {
            animator.speed = activeMultiplier; // Faster attack animations!
        }
        else
        {
            animator.SetFloat("Speed", currentSpeed);
            // Dynamically scale animation speed based on movement speed.
            animator.speed = Mathf.Max(1f, currentSpeed / baseSpeed) * activeMultiplier;
        }
    }
}
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
    private float stateTimer = 0f;
    private Vector2 wanderTarget;
    private float wanderWaitTimer = 0f;
    public float searchRadius = 12f; // Increased significantly for aggressive searching

    [Header("Perception System")]
    public float hearingRadius = 14f; // Can hear you from further away
    private Vector3 previousPlayerPos;

    [Header("Movement Stats")]
    public float baseSpeed = 3.5f; // Buffed patrol speed
    public float sprintSpeed = 5.5f; // Buffed sprint speed
    private float currentSpeed;

    [Header("Stun Resistance")]
    private int stunCount = 0;
    private float stunMemoryTimer = 0f;
    private float memoryResetTime = 60f;

    [Header("Signal Response")]
    [SerializeField] private float delayedSignalDistance = 10f;
    [SerializeField] private float delayedHuntSeconds = 2f;
    [SerializeField] private float signalSpeedMultiplier = 1.2f; // 20% buff when Rig is open
    private bool isSignalEmpowered = false; // Tracks if the Proxy is enraged by the open inventory
    private bool isEnraged = false; // Permanent hunt mode for the Meltdown sequence

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

    [Header("Sixth Sense (Passive Tracking)")]
    public float minSixthSenseTime = 20f;
    public float maxSixthSenseTime = 60f;
    private float sixthSenseTimer = 0f;

    [Header("Dynamic Avoidance (Whiskers)")]
    public float whiskerLength = 1.5f;
    public float whiskerAngle = 25f;
    public int whiskerCount = 4; // 4 Pairs = 8 angled whiskers + 1 forward
    public float proxyWidth = 0.25f; // Reduced so the AI knows it can squeeze through tight gaps!
    public LayerMask obstacleMask;

    [Header("Stuck Detection & Teleport")]
    public float stuckDistanceThreshold = 0.5f;
    public float stuckTimeLimit = 1.0f;
    public float teleportFailsafeLimit = 4.0f;
    private Vector2 _stuckCheckPos;
    private float _stuckTimer = 0f;
    private float _totalStuckTime = 0f;
    private float _avoidanceBias = 1f; // Remembers which side it chose to avoid glitching

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

        // ADD FRICTIONLESS MATERIAL TO SLIDE OFF WALLS
        PhysicsMaterial2D slipMat = new PhysicsMaterial2D("ProxySlip");
        slipMat.friction = 0f;
        slipMat.bounciness = 0f;
        rb.sharedMaterial = slipMat;

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

        // PURE CODE SETUP: Automatically target all layers EXCEPT 'Ignore Raycast' so you don't have to configure anything!
        if (obstacleMask.value == 0)
        {
            obstacleMask = ~LayerMask.GetMask("Ignore Raycast");
        }

        if (targetPlayer != null)
        {
            lastKnownPosition = targetPlayer.position;
        }
        
        _stuckCheckPos = transform.position;

        // Initialize the first random ping
        sixthSenseTimer = Random.Range(minSixthSenseTime, maxSixthSenseTime);
        ChangeState(AIState.Wandering); // Force the Proxy to start prowling immediately!
    }

    void Update()
    {
        if (targetPlayer == null) return; // Safety check

        ManageStunMemory();
        UpdatePerception();
        UpdateSixthSense();

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
                if (isSignalEmpowered || isPlayerInMeleeRange || isEnraged)
                {
                    lastKnownPosition = targetPlayer.position;
                    
                    // Prevent the Proxy from violently pinning Kaelen against a wall while waiting for its attack cooldown!
                    if (isPlayerInMeleeRange && !canAttack)
                    {
                        hasMoveTarget = false;
                        UpdateAnimatorDirection(targetPlayer.position);
                    }
                    else
                    {
                        SetMoveTarget(targetPlayer.position, sprintSpeed);
                    }
                }
                else
                {
                    // Signal lost! Keep running to the last known point, but downgrade to Investigate once reached
                    if (!hasMoveTarget)
                    {
                        ChangeState(AIState.Investigating);
                    }
                }
                break;

            case AIState.Investigating:
                if (!hasMoveTarget)
                {
                    stateTimer += Time.deltaTime;
                    if (stateTimer >= 0.3f && !isPlayerInMeleeRange) // Organic Pause: Barely hesitate before sweeping!
                    {
                        ChangeState(AIState.Wandering);
                    }
                }
                break;

            case AIState.Wandering:
                if (!hasMoveTarget)
                {
                    stateTimer += Time.deltaTime;
                    if (stateTimer >= wanderWaitTimer)
                    {
                        PickNewWanderTarget(); // Never idles. It will tear the ship apart looking for you.
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
                if (!(isPlayerInMeleeRange && !canAttack))
                {
                    SetMoveTarget(targetPlayer.position, sprintSpeed);
                }
            }
        }
        previousPlayerPos = targetPlayer.position;
    }

    private void UpdateSixthSense()
    {
        if (isEnraged || currentState == AIState.Hunting) return; // Skip if it already knows where you are

        sixthSenseTimer -= Time.deltaTime;
        if (sixthSenseTimer <= 0f)
        {
            // Ping the player's exact location and reset the random timer!
            lastKnownPosition = targetPlayer.position;
            hasLastKnownPosition = true;
            sixthSenseTimer = Random.Range(minSixthSenseTime, maxSixthSenseTime);

            Debug.Log("<color=magenta>PROXY AI: Sixth sense ping! Sweeping Kaelen's current area.</color>");

            if (CanInterruptState(AIState.Investigating))
            {
                ChangeState(AIState.Investigating);
            }
        }
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
        if (isEnraged) return; // Cannot drop the signal during a meltdown!

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

    // Support for the enlarged Trigger Hitbox!
    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.CompareTag("Player")) isPlayerInMeleeRange = true;
    }

    private void OnTriggerExit2D(Collider2D collider)
    {
        if (collider.CompareTag("Player")) isPlayerInMeleeRange = false;
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
        Vector2 targetDirection = (moveTarget - currentPosition).normalized;
        
        // --- CUSTOM DYNAMIC AVOIDANCE (WHISKERS) ---
        Vector2 safeDirection = GetAvoidanceDirection(currentPosition, targetDirection);
        Vector2 newPosition = currentPosition + (safeDirection * activeSpeed * Time.fixedDeltaTime);
        
        if (rb != null) rb.MovePosition(newPosition);
        else transform.position = new Vector3(newPosition.x, newPosition.y, transform.position.z);
        
        // --- IMPROVED STUCK DETECTION & TELEPORT FAILSAFE ---
        _stuckTimer += Time.fixedDeltaTime;
        if (_stuckTimer >= stuckTimeLimit)
        {
            if (Vector2.Distance(currentPosition, _stuckCheckPos) < stuckDistanceThreshold)
            {
                _totalStuckTime += _stuckTimer;
                hasMoveTarget = false; // Give up and pick a new path

                if (_totalStuckTime >= teleportFailsafeLimit)
                {
                    Debug.Log("<color=orange>PROXY AI: Stuck for too long. Teleporting to open area!</color>");
                    TeleportToOpenArea();
                    _totalStuckTime = 0f;
                }
            }
            else _totalStuckTime = 0f; // Successfully moving!
            
            _stuckCheckPos = currentPosition;
            _stuckTimer = 0f;
        }

        // Forgiving arrival radius (0.5f) since avoidance might make us arrive slightly off-center
        if (Vector2.Distance(currentPosition, moveTarget) <= 0.5f)
        {
            hasMoveTarget = false;
        }

        UpdateAnimatorDirection(currentPosition + safeDirection);
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
        // We don't use waypoints anymore. The Proxy dynamically picks points around the last known location.
        Vector2 randomDirection = Random.insideUnitCircle * searchRadius;
        wanderTarget = lastKnownPosition + randomDirection;

        wanderWaitTimer = Random.Range(0.2f, 1f); // Very little hesitation between sweeps
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

    // Triggered by the Meltdown Manager in the escape scene
    public void TriggerEnragedHunt()
    {
        isEnraged = true;
        isSignalEmpowered = true; // Grants the 20% speed buff permanently
        ChangeState(AIState.Hunting);
    }

    // Triggered by cinematic directors to force an attack animation toward a specific point
    public void TriggerCinematicAttack(Vector3 targetPosition)
    {
        if (animator == null) return;
        
        Vector2 dir = (targetPosition - transform.position).normalized;
        
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            animator.SetFloat("Direction", 1f); // Side
            if (spriteRenderer != null) spriteRenderer.flipX = dir.x < 0;
        }
        else
        {
            if (dir.y > 0)
            {
                animator.SetFloat("Direction", 1f); // Up uses Side animation
                if (spriteRenderer != null)
                {
                    if (dir.x < -0.01f) spriteRenderer.flipX = true;
                    else if (dir.x > 0.01f) spriteRenderer.flipX = false;
                }
            }
            else
            {
                animator.SetFloat("Direction", 0f); // Down
                if (spriteRenderer != null) spriteRenderer.flipX = false;
            }
        }
        
        animator.speed = 1f; // Force normal playback speed just in case
        animator.SetTrigger("Attack");
    }

    // Forces the Proxy to look in a specific direction (useful for idling in cinematics)
    public void ForceLookDirection(Vector2 dir)
    {
        if (animator == null) return;
        
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            animator.SetFloat("Direction", 1f); // Side
            if (spriteRenderer != null) spriteRenderer.flipX = dir.x < 0;
        }
        else
        {
            if (dir.y > 0)
            {
                animator.SetFloat("Direction", 1f); // Up uses Side animation
                if (spriteRenderer != null)
                {
                    if (dir.x < -0.01f) spriteRenderer.flipX = true;
                    else if (dir.x > 0.01f) spriteRenderer.flipX = false;
                }
            }
            else
            {
                animator.SetFloat("Direction", 0f); // Down
                if (spriteRenderer != null) spriteRenderer.flipX = false;
            }
        }
    }

    // ==========================================
    // DYNAMIC AVOIDANCE LOGIC (WHISKERS)
    // ==========================================

    private Vector2 GetAvoidanceDirection(Vector2 currentPos, Vector2 targetDir)
    {
        if (IsPathClear(currentPos, proxyWidth, targetDir, whiskerLength)) 
        {
            _avoidanceBias = 1f; // Reset bias when path is completely clear
            return targetDir;
        }

        for (int i = 1; i <= whiskerCount; i++)
        {
            float currentAngle = whiskerAngle * i;

            // Try the biased side first to prevent rapid left/right oscillation
            Vector2 biasedDir = RotateVector(targetDir, currentAngle * _avoidanceBias);
            if (IsPathClear(currentPos, proxyWidth, biasedDir, whiskerLength)) return biasedDir;

            // Try the opposite side if the biased side is blocked
            Vector2 oppositeDir = RotateVector(targetDir, currentAngle * -_avoidanceBias);
            if (IsPathClear(currentPos, proxyWidth, oppositeDir, whiskerLength)) 
            {
                _avoidanceBias = -_avoidanceBias; // Swap bias!
                return oppositeDir;
            }
        }

        // Failsafe: Slide along the wall smoothly
        RaycastHit2D hit = DoRaycast(currentPos, proxyWidth, targetDir, whiskerLength);
        if (hit.collider != null)
        {
            Vector2 slideDir = Vector2.Perpendicular(hit.normal).normalized;
            if (Vector2.Dot(slideDir, targetDir) < 0) slideDir = -slideDir;
            return slideDir;
        }

        return targetDir;
    }

    private bool IsPathClear(Vector2 origin, float radius, Vector2 direction, float distance)
    {
        return DoRaycast(origin, radius, direction, distance).collider == null;
    }

    private RaycastHit2D DoRaycast(Vector2 origin, float radius, Vector2 direction, float distance)
    {
        RaycastHit2D[] hits = Physics2D.CircleCastAll(origin, radius, direction, distance, obstacleMask);
        foreach (var hit in hits)
        {
            // Ignore ourselves, triggers (combat hitboxes), and the player!
            if (hit.collider.gameObject != this.gameObject && !hit.collider.isTrigger && !hit.collider.CompareTag("Player"))
            {
                return hit; // We found a solid wall!
            }
        }
        return new RaycastHit2D(); // Returns empty
    }

    private Vector2 RotateVector(Vector2 v, float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        float s = Mathf.Sin(rad);
        float c = Mathf.Cos(rad);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }

    private void TeleportToOpenArea()
    {
        if (targetPlayer == null) return;

        Vector2 playerPos = targetPlayer.position;
        Vector2 bestSpot = transform.position; // Default to current position
        bool spotFound = false;

        // We want to teleport out of sight if possible, but MUST have line of sight TO THE PLAYER 
        // This guarantees the spot is inside the map and not behind an outer wall or in the void!
        for (float dist = 10f; dist >= 1.5f; dist -= 1.5f)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 testPos = playerPos + (dir * dist);

                // 1. Is there a clear line of sight from the Player to this spot?
                RaycastHit2D losHit = Physics2D.Raycast(playerPos, dir, dist, obstacleMask);
                
                if (losHit.collider == null) 
                {
                    // 2. Is the spot physically large enough to fit the Proxy?
                    Collider2D spaceHit = Physics2D.OverlapCircle(testPos, proxyWidth + 0.1f, obstacleMask);
                    
                    if (spaceHit == null)
                    {
                        bestSpot = testPos;
                        spotFound = true;
                        break;
                    }
                }
            }
            if (spotFound) break;
        }

        if (spotFound)
        {
            if (rb != null) rb.position = bestSpot;
            transform.position = new Vector3(bestSpot.x, bestSpot.y, transform.position.z);
            hasMoveTarget = false; // Reset movement memory
            UpdateAnimatorDirection(playerPos);
        }
    }

    // Draws the whiskers in the Unity Editor when you select the Proxy!
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, searchRadius);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);
        
        if (Application.isPlaying && hasMoveTarget)
        {
            Vector2 currentPos = transform.position;
            Vector2 targetDir = (moveTarget - currentPos).normalized;
            
            Gizmos.color = Color.green;
            Gizmos.DrawRay(currentPos, targetDir * whiskerLength);

            Gizmos.color = Color.cyan;
            for (int i = 1; i <= whiskerCount; i++)
            {
                float currentAngle = whiskerAngle * i;
                Gizmos.DrawRay(currentPos, RotateVector(targetDir, currentAngle) * whiskerLength);
                Gizmos.DrawRay(currentPos, RotateVector(targetDir, -currentAngle) * whiskerLength);
            }
        }
    }
}
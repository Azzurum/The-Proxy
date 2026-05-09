using UnityEngine;

public class ProxyAI : MonoBehaviour
{
    [Header("Targeting")]
    public Transform targetPlayer;
    private Vector2 lastKnownPosition;
    private bool hasLastKnownPosition = false;

    // WANDER VARIABLES
    private bool isWandering = false;
    private Vector2 wanderTarget;
    private float wanderWaitTimer = 0f;
    public float searchRadius = 4f; 

    [Header("Perception System")]
    public float hearingRadius = 6f;
    private Vector3 previousPlayerPos;

    [Header("Movement Stats")]
    public float baseSpeed = 2.0f;
    public float sprintSpeed = 6.0f;
    private float currentSpeed;

    [Header("Stun Resistance")]
    private bool isStunned = false;
    private int stunCount = 0;
    private float stunMemoryTimer = 0f;
    private float memoryResetTime = 60f;

    [Header("Signal Response")]
    [SerializeField] private float immediateSignalDistance = 3f;
    [SerializeField] private float delayedSignalDistance = 10f;
    [SerializeField] private float investigateSignalDistance = 20f;
    [SerializeField] private float delayedHuntSeconds = 2f;

    [Header("Knockback")]
    [SerializeField] private float knockbackSpeed = 25f;

    [Header("Attack Behavior")]
    public float attackWindup = 1.0f;
    public float attackRecovery = 1.5f;
    public float attackRange = 1.5f;

    // Internal State Variables
    private bool isKnockedBack = false;
    private bool isPlayerInMeleeRange = false;
    private bool canAttack = true;
    private bool isAttacking = false;
    private Vector2 moveTarget;
    private bool hasMoveTarget = false;
    private Coroutine delayedHuntCoroutine;

    // Components & Managers
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    private MetRigManager metRigManager;
    private GameOverManager gameOverManager;
    private InventoryManager inventoryManager;

    void Start()
    {
        currentSpeed = baseSpeed;

        // Setup Components
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;

        // Auto-find player if not assigned
        if (targetPlayer == null)
        {
            GameObject player = GameObject.Find("Player_Kaelen");
            if (player != null) targetPlayer = player.transform;
        }

        // Cache global managers safely
        metRigManager = FindAnyObjectByType<MetRigManager>();
        gameOverManager = FindAnyObjectByType<GameOverManager>();
        inventoryManager = FindAnyObjectByType<InventoryManager>();

        if (targetPlayer != null) previousPlayerPos = targetPlayer.position;
    }

    void Update()
    {
        if (targetPlayer == null) return; // Safety check

        ManageStunMemory();
        UpdatePerception();

        // State Machine execution hierarchy ensures no overlapping behaviors
        if (isStunned || isKnockedBack) return;

        if (isPlayerInMeleeRange && canAttack && !isAttacking)
        {
            StartCoroutine(AttackRoutine());
            return; // Halt other logic while attacking
        }

        if (!isAttacking)
        {
            HuntPlayer();
        }
        
        UpdateAnimationSpeed();
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
            isWandering = false; 
        }
        previousPlayerPos = targetPlayer.position;
    }

    private void HuntPlayer()
    {
        bool isSignalEmitting = (metRigManager != null && metRigManager.isRigOpen && !metRigManager.inFaradayZone);

        // SCENARIO A: Rig is open! Perfect tracking.
        if (isSignalEmitting)
        {
            lastKnownPosition = targetPlayer.position;
            hasLastKnownPosition = true;
            isWandering = false; 
            SetMoveTarget(targetPlayer.position, sprintSpeed);
        }
        // SCENARIO B: Stealth Mode. Rely on hearing and memory.
        else if (hasLastKnownPosition)
        {
            Vector2 currentPos = rb != null ? rb.position : (Vector2)transform.position;
            
            if (!isWandering)
            {
                if (Vector2.Distance(currentPos, lastKnownPosition) > 0.1f)
                {
                    SetMoveTarget(lastKnownPosition, baseSpeed);
                }
                else
                {
                    isWandering = true;
                    PickNewWanderTarget();
                }
            }
            else
            {
                if (Vector2.Distance(currentPos, wanderTarget) > 0.1f)
                {
                    SetMoveTarget(wanderTarget, baseSpeed);
                }
                else
                {
                    wanderWaitTimer -= Time.deltaTime;
                    if (wanderWaitTimer <= 0f)
                    {
                        PickNewWanderTarget();
                    }
                }
            }
        }
    }

    // --- EXTERNAL COMBAT TRIGGERS ---

    public void OnCombatAction(Vector3 actionPosition)
    {
        Debug.Log("<color=red>PROXY AI: Combat sound detected! Exact location locked.</color>");
        lastKnownPosition = actionPosition;
        hasLastKnownPosition = true;
        isWandering = false; 
    }

    public void OnSignalSpike(bool isListening, float distance)
    {
        if (isListening && distance >= 0)
        {
            lastKnownPosition = targetPlayer.position;
            hasLastKnownPosition = true;
            isWandering = false;

            if (distance < immediateSignalDistance) 
            {
                currentSpeed = sprintSpeed;
                Debug.Log("PROXY: Signal detected nearby! Immediate hunt.");
            }
            else if (distance < delayedSignalDistance) 
            {
                if (delayedHuntCoroutine != null) StopCoroutine(delayedHuntCoroutine);
                delayedHuntCoroutine = StartCoroutine(DelayedHunt(delayedHuntSeconds));
                Debug.Log("PROXY: Signal detected far! Delayed hunt.");
            }
            else if (distance < investigateSignalDistance) 
            {
                currentSpeed = baseSpeed;
                Debug.Log("PROXY: Signal detected very far! Investigating.");
            }
        }
        else
        {
            if (delayedHuntCoroutine != null) 
            {
                StopCoroutine(delayedHuntCoroutine);
                Debug.Log("PROXY: Delayed hunt canceled - signal turned off!");
            }
            currentSpeed = baseSpeed;
        }
    }

    private System.Collections.IEnumerator DelayedHunt(float delay)
    {
        yield return new WaitForSeconds(delay);
        currentSpeed = sprintSpeed;
        Debug.Log("PROXY: Delayed hunt activated!");
    }

    // --- ATTACK LOGIC ---

    private System.Collections.IEnumerator AttackRoutine()
    {
        canAttack = false;
        isAttacking = true;
        hasMoveTarget = false; // Stop moving
        
        float previousSpeed = currentSpeed;
        currentSpeed = 0f;

        Debug.Log("PROXY: Committing attack...");
        float elapsed = 0f;
        while (elapsed < attackWindup)
        {
            if (targetPlayer == null || Vector2.Distance(transform.position, targetPlayer.position) > attackRange)
            {
                Debug.Log("PROXY: Attack broken as Kaelen escaped!");
                ResetAttackState(previousSpeed);
                yield return new WaitForSeconds(attackRecovery);
                canAttack = true;
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (targetPlayer != null && Vector2.Distance(transform.position, targetPlayer.position) <= attackRange)
        {
            ExecuteAttack();
        }

        ResetAttackState(previousSpeed);
        yield return new WaitForSeconds(attackRecovery);
        canAttack = true;
    }

    private void ResetAttackState(float restoredSpeed)
    {
        currentSpeed = restoredSpeed;
        isAttacking = false;
    }

    private void ExecuteAttack()
    {
        Debug.Log("PROXY: Attack landed. Inventory corruption injected.");
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
        if (isStunned) return;
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
        StartCoroutine(StunRoutine(duration));
    }

    private System.Collections.IEnumerator StunRoutine(float time)
    {
        isStunned = true;
        hasMoveTarget = false; // Halt movement
        yield return new WaitForSeconds(time);
        isStunned = false;
    }

    public void ApplyRepulsor(Vector3 playerPosition, float knockbackDistance)
    {
        if (!isKnockedBack)
        {
            StartCoroutine(KnockbackRoutine(playerPosition, knockbackDistance));
        }
    }

    private System.Collections.IEnumerator KnockbackRoutine(Vector3 playerPosition, float distance)
    {
        isKnockedBack = true;
        hasMoveTarget = false;

        Vector2 myPos2D = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 playerPos2D = new Vector2(playerPosition.x, playerPosition.y);
        Vector2 pushDirection = (myPos2D - playerPos2D).normalized;
        Vector2 targetPosition = myPos2D + (pushDirection * distance);

        while (Vector2.Distance(myPos2D, targetPosition) > 0.1f)
        {
            myPos2D = Vector2.MoveTowards(myPos2D, targetPosition, knockbackSpeed * Time.deltaTime);
            if (rb != null) rb.MovePosition(myPos2D);
            else transform.position = new Vector3(myPos2D.x, myPos2D.y, transform.position.z);
            
            yield return null;
        }
        isKnockedBack = false;
    }

    // --- MOVEMENT EXECUTION ---

    private void FixedUpdate()
    {
        if (!hasMoveTarget || isStunned || isKnockedBack || isAttacking) return;
        
        Vector2 currentPosition = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 newPosition = Vector2.MoveTowards(currentPosition, moveTarget, currentSpeed * Time.fixedDeltaTime);
        
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
        Vector2 randomDirection = Random.insideUnitCircle * searchRadius;
        wanderTarget = lastKnownPosition + randomDirection;
        wanderWaitTimer = Random.Range(1f, 3f);
    }

    // --- ANIMATION CONTROLLER ---

    private void UpdateAnimatorDirection(Vector2 target)
    {
        if (animator == null) return;
        
        Vector2 currentPosition = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 direction = (target - currentPosition).normalized;
        
        if (direction.sqrMagnitude > 0.01f && hasMoveTarget && !isStunned && !isAttacking)
        {
            animator.SetFloat("MoveX", direction.x);
            animator.SetFloat("MoveY", direction.y);

            if (spriteRenderer != null)
            {
                // HORIZONTAL FLIP LOGIC
                if (direction.x < -0.1f) 
                {
                    spriteRenderer.flipX = true;  // Face Left
                }
                else if (direction.x > 0.1f) 
                {
                    spriteRenderer.flipX = false; // Face Right
                }

                // VERTICAL FLIP LOGIC (Upside-down ONLY when walking primarily up)
                // We use Mathf.Abs (Absolute value) to ignore negative signs when comparing
                if (direction.y > 0.1f && Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
                {
                    spriteRenderer.flipY = true;  // Turn upside down
                }
                else
                {
                    spriteRenderer.flipY = false; // Return to normal
                }
            }
        }
    }

    private void UpdateAnimationSpeed()
    {
        if (animator == null) return;
        
        if (isStunned || (!hasMoveTarget && !isAttacking))
        {
            animator.speed = 0f; // Pause animation when idle or stunned
        }
        else
        {
            // Dynamically scale animation speed based on movement speed.
            animator.speed = Mathf.Max(1f, currentSpeed / baseSpeed);
        }
    }
}
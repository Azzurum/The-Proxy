using UnityEngine;

/// <summary>
/// Core state machine and behavioral logic for the Proxy antagonist. Handles perception, pathfinding, and combat.
/// </summary>
public class ProxyAI : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("The player character to track and hunt.")]
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
    [Tooltip("The current behavioral state of the AI.")]
    public AIState currentState = AIState.Idle;
    
    [Tooltip("How far the AI searches around the last known position when wandering.")]
    public float searchRadius = 12f; 
    
    private float stateTimer = 0f;
    private Vector2 wanderTarget;
    private float wanderWaitTimer = 0f;

    [Header("Perception System")]
    [Tooltip("The radius within which the Proxy can hear player footsteps.")]
    public float hearingRadius = 25f; 
    private Vector3 previousPlayerPos;

    [Header("Movement Stats")]
    [Tooltip("Movement speed during idle and investigation phases.")]
    public float baseSpeed = 3.5f; 
    [Tooltip("Movement speed during an active hunt.")]
    public float sprintSpeed = 5.5f; 
    private float currentSpeed;

    [Header("Stun Resistance")]
    [Tooltip("Time in seconds before the Proxy forgets it was stunned, resetting its resistance.")]
    [SerializeField] private float memoryResetTime = 60f;
    private int stunCount = 0;
    private float stunMemoryTimer = 0f;

    [Header("Signal Response")]
    [Tooltip("The distance at which the Proxy hunts immediately vs a delayed response when detecting a signal.")]
    [SerializeField] private float delayedSignalDistance = 10f;
    [Tooltip("Time in seconds before a distant signal triggers a hunt.")]
    [SerializeField] private float delayedHuntSeconds = 2f;
    [Tooltip("Speed multiplier applied while the player's inventory signal is active.")]
    [SerializeField] private float signalSpeedMultiplier = 1.2f; 
    private bool isSignalEmpowered = false; 
    private bool isEnraged = false; 

    [Header("Knockback")]
    [Tooltip("The speed at which the Proxy is physically thrown back by the Repulsor.")]
    [SerializeField] private float knockbackSpeed = 25f;

    [Header("Attack Behavior")]
    [Tooltip("Cooldown in seconds after executing a strike.")]
    public float attackRecovery = 1.5f;
    [Tooltip("The maximum distance from the player to execute a melee strike.")]
    public float attackRange = 1.8f;

    private bool isPlayerInMeleeRange = false;
    private bool canAttack = true;
    private Vector2 moveTarget;
    private bool hasMoveTarget = false;
    private Coroutine delayedHuntCoroutine;
    private Coroutine _distractionCoroutine;
    private CameraFollow _cachedCamera;

    [Header("Sixth Sense (Passive Tracking)")]
    [Tooltip("Minimum time before the Proxy passively pings the player's location.")]
    public float minSixthSenseTime = 8f;
    [Tooltip("Maximum time before the Proxy passively pings the player's location.")]
    public float maxSixthSenseTime = 16f;
    private float sixthSenseTimer = 0f;

    [Header("Dynamic Avoidance (Whiskers)")]
    [Tooltip("Length of the dynamic collision-avoidance raycasts.")]
    public float whiskerLength = 2.0f;
    [Tooltip("The angle offset for each pair of whiskers.")]
    public float whiskerAngle = 15f;
    [Tooltip("Number of paired whiskers (e.g., 4 pairs = 8 side whiskers + 1 forward).")]
    public int whiskerCount = 6; 
    [Tooltip("The physical radius size considered for navigation clearance.")]
    public float proxyWidth = 0.25f; 
    [Tooltip("LayerMask containing solid environmental obstacles.")]
    public LayerMask obstacleMask;

    [Header("Stuck Detection & Teleport")]
    [Tooltip("The distance threshold to determine if the Proxy is physically stuck.")]
    public float stuckDistanceThreshold = 0.5f;
    [Tooltip("Time in seconds before the Proxy attempts to recalculate a path when stuck.")]
    public float stuckTimeLimit = 0.5f;
    [Tooltip("Time in seconds of being stuck before triggering the failsafe teleport.")]
    public float teleportFailsafeLimit = 2.0f;
    private Vector2 _stuckCheckPos;
    private float _stuckTimer = 0f;
    private float _totalStuckTime = 0f;
    private float _avoidanceBias = 1f;

    private ContactFilter2D _avoidanceFilter;
    private readonly RaycastHit2D[] _raycastHits = new RaycastHit2D[5];
    private readonly Collider2D[] _overlapHits = new Collider2D[1];

    [Header("Depth Sorting")]
    [Tooltip("Make sure this exactly matches Kaelen's sorting layer!")]
    public string sortingLayerName = "Player";
    [Tooltip("Vertical offset to determine dynamic sorting overlap (places the pivot at the feet).")]
    public float depthOffset = -0.5f;
    
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
    
    [Tooltip("Time between footstep sounds while wandering/investigating.")]
    [SerializeField] private float walkStepInterval = 0.5f;
    [Tooltip("Time between footstep sounds while hunting.")]
    [SerializeField] private float sprintStepInterval = 0.3f;
    private float _footstepTimer = 0f;

    void Start()
    {
        currentSpeed = baseSpeed;

        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.sortingLayerName = sortingLayerName;
        
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.mass = 1000f;
        rb.simulated = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;

        PhysicsMaterial2D slipMat = new PhysicsMaterial2D("ProxySlip");
        slipMat.friction = 0f;
        slipMat.bounciness = 0f;
        rb.sharedMaterial = slipMat;

        if (targetPlayer == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) targetPlayer = player.transform;
        }

        metRigManager = FindAnyObjectByType<MetRigManager>();
        gameOverManager = FindAnyObjectByType<GameOverManager>();
        inventoryManager = FindAnyObjectByType<InventoryManager>();
        _cachedCamera = FindAnyObjectByType<CameraFollow>();

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) 
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Force 3D sound settings even if the AudioSource was already attached in the Editor!
        audioSource.spatialBlend = 1f; 
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 2f;
        audioSource.maxDistance = 18f;

        if (targetPlayer != null) previousPlayerPos = targetPlayer.position;

        if (obstacleMask.value == 0)
        {
            obstacleMask = ~LayerMask.GetMask("Ignore Raycast");
        }

        _avoidanceFilter = new ContactFilter2D 
        { 
            layerMask = obstacleMask, 
            useLayerMask = true, 
            useTriggers = false 
        };

        if (targetPlayer != null)
        {
            lastKnownPosition = targetPlayer.position;
        }
        
        _stuckCheckPos = transform.position;

        sixthSenseTimer = Random.Range(minSixthSenseTime, maxSixthSenseTime);
        ChangeState(AIState.Wandering); 
    }

    void Update()
    {
        if (targetPlayer == null) return; 

        ManageStunMemory();
        UpdatePerception();
        UpdateSixthSense();

        if (isPlayerInMeleeRange && canAttack && CanInterruptState(AIState.Attacking))
        {
            ChangeState(AIState.Attacking);
            return;
        }

        switch (currentState)
        {
            case AIState.Hunting:
                if (isSignalEmpowered || isPlayerInMeleeRange || isEnraged)
                {
                    lastKnownPosition = targetPlayer.position;
                    
                    // Avoid pushing aggressively into the player if the attack is on cooldown.
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
                    if (stateTimer >= 0.3f && !isPlayerInMeleeRange) 
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
                        PickNewWanderTarget();
                    }
                }
                break;
        }
        
        UpdateAnimationSpeed();
        HandleFootsteps();

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = Mathf.RoundToInt((transform.position.y + depthOffset) * -10f);
        }
    }

    private void HandleFootsteps()
    {
        if (hasMoveTarget && currentState != AIState.Idle && currentState != AIState.Stunned && currentState != AIState.Attacking && currentState != AIState.KnockedBack)
        {
            _footstepTimer -= Time.deltaTime;
            if (_footstepTimer <= 0f)
            {
                if (audioSource != null) audioSource.PlayOneShot(ProceduralAudioGen.GenerateFootstep());
                
                float activeMultiplier = isSignalEmpowered ? signalSpeedMultiplier : 1f;
                _footstepTimer = (currentState == AIState.Hunting ? sprintStepInterval : walkStepInterval) / activeMultiplier;
            }
        }
        else
        {
            _footstepTimer = 0f;
        }
    }

    private bool CanInterruptState(AIState newState)
    {
        if (currentState == AIState.Stunned || currentState == AIState.KnockedBack) return false;
        if (currentState == AIState.Attacking && newState != AIState.Stunned && newState != AIState.KnockedBack) return false;
        if (currentState == AIState.Distracted && newState != AIState.Stunned && newState != AIState.KnockedBack) return false;
        return true;
    }

    /// <summary>
    /// Transitions the AI into a new behavior state, initializing necessary speed and target logic.
    /// </summary>
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
            }
        }
    }

    private void UpdatePerception()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, targetPlayer.position);
        float playerMovement = Vector3.Distance(targetPlayer.position, previousPlayerPos);

        bool playerDetected = false;

        // 1. Hearing Check
        if (playerMovement > 0.001f && distanceToPlayer <= hearingRadius)
        {
            playerDetected = true;
        }
        
        // 2. Line of Sight Check (Instantly spot the player down a dark hallway!)
        if (!playerDetected && distanceToPlayer <= 30f)
        {
            Vector2 dirToPlayer = (targetPlayer.position - transform.position).normalized;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dirToPlayer, distanceToPlayer, obstacleMask);
            if (hit.collider == null) playerDetected = true;
        }

        if (playerDetected)
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
        if (isEnraged || currentState == AIState.Hunting) return; 

        sixthSenseTimer -= Time.deltaTime;
        if (sixthSenseTimer <= 0f)
        {
            lastKnownPosition = targetPlayer.position;
            hasLastKnownPosition = true;
            sixthSenseTimer = Random.Range(minSixthSenseTime, maxSixthSenseTime);

            if (CanInterruptState(AIState.Investigating))
            {
                ChangeState(AIState.Investigating);
            }
        }
    }

    /// <summary>
    /// External hook for when a loud interaction occurs, alerting the Proxy to an exact location.
    /// </summary>
    public void OnCombatAction(Vector3 actionPosition)
    {
        lastKnownPosition = actionPosition;
        hasLastKnownPosition = true;
        
        if (CanInterruptState(AIState.Investigating))
        {
            ChangeState(AIState.Investigating);
        }
    }

    /// <summary>
    /// Hook triggered when the player's inventory is open, increasing the AI's aggressiveness based on distance.
    /// </summary>
    public void OnSignalSpike(bool isListening, float distance)
    {
        if (isEnraged) return; 

        isSignalEmpowered = isListening; 

        if (isListening && distance >= 0)
        {
            lastKnownPosition = targetPlayer.position;
            hasLastKnownPosition = true;

            if (distance < delayedSignalDistance) 
            {
                ChangeState(AIState.Hunting);
            }
            else 
            {
                if (delayedHuntCoroutine != null) StopCoroutine(delayedHuntCoroutine);
                delayedHuntCoroutine = StartCoroutine(DelayedHunt(delayedHuntSeconds));
            }
        }
        else
        {
            if (delayedHuntCoroutine != null) 
            {
                StopCoroutine(delayedHuntCoroutine);
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
        }
    }

    private System.Collections.IEnumerator AttackRoutine()
    {
        canAttack = false;
        hasMoveTarget = false; 
        
        currentSpeed = 0f;

        if (targetPlayer != null && animator != null)
        {
            Vector2 dir = (targetPlayer.position - transform.position).normalized;
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            {
                animator.SetFloat("Direction", 1f); 
                if (spriteRenderer != null) spriteRenderer.flipX = dir.x < 0;
            }
            else
            {
                if (dir.y > 0)
                {
                    animator.SetFloat("Direction", 1f); 
                    if (spriteRenderer != null)
                    {
                        if (dir.x < -0.01f) spriteRenderer.flipX = true;
                        else if (dir.x > 0.01f) spriteRenderer.flipX = false;
                    }
                }
                else
                {
                    animator.SetFloat("Direction", 0f);
                    if (spriteRenderer != null) spriteRenderer.flipX = false;
                }
            }
            animator.SetTrigger("Attack");
        }

        yield break;
    }

    /// <summary>
    /// Triggered by the Animation Event precisely when the attack frame connects. 
    /// Validates the player's distance and resolves damage.
    /// </summary>
    public void AnimEvent_Strike()
    {
        if (audioSource != null) audioSource.PlayOneShot(sfxAttackSwing != null ? sfxAttackSwing : ProceduralAudioGen.GenerateWhoosh());

        if (currentState == AIState.Stunned || currentState == AIState.KnockedBack) return; 

        if (targetPlayer != null && Vector2.Distance(transform.position, targetPlayer.position) <= attackRange)
        {
            ExecuteAttack();
        }
    }

    /// <summary>
    /// Triggered by the final frame of the attack animation to recover AI states.
    /// </summary>
    public void AnimEvent_EndAttack()
    {
        if (currentState == AIState.Attacking)
        {
            if (isSignalEmpowered || isPlayerInMeleeRange)
            {
                ChangeState(AIState.Hunting); 
            }
            else
            {
                ChangeState(AIState.Investigating); 
            }
        }
        StartCoroutine(RecoveryRoutine());
    }

    private System.Collections.IEnumerator RecoveryRoutine()
    {
        float activeRecovery = isSignalEmpowered ? attackRecovery / signalSpeedMultiplier : attackRecovery;
        yield return new WaitForSeconds(activeRecovery);
        canAttack = true;
    }

    /// <summary>
    /// Applies corruption damage and environmental feedback when an attack connects.
    /// </summary>
    private void ExecuteAttack()
    {
        if (audioSource != null) audioSource.PlayOneShot(sfxAttackHit != null ? sfxAttackHit : ProceduralAudioGen.GenerateStaticGlitch(0.4f));
        
        if (_cachedCamera != null)
        {
            _cachedCamera.TriggerShake(0.3f, 0.5f);
        }

        if (ScreenEffectManager.Instance != null)
        {
            ScreenEffectManager.Instance.TriggerFlash(new Color(1f, 0f, 0f, 0.6f), 0.3f);
        }

        if (inventoryManager != null)
        {
            inventoryManager.AddCorruptionRow();
        }
    }

    private void KillPlayer()
    {
        if (gameOverManager != null)
        {
            gameOverManager.TriggerGameOver();
        }
        else
        {
            Time.timeScale = 0f;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player")) isPlayerInMeleeRange = true;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player")) isPlayerInMeleeRange = false;
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.CompareTag("Player")) isPlayerInMeleeRange = true;
    }

    private void OnTriggerExit2D(Collider2D collider)
    {
        if (collider.CompareTag("Player")) isPlayerInMeleeRange = false;
    }

    /// <summary>
    /// Overrides the Proxy's state with a momentary stun, adapting duration based on built-up resistance.
    /// </summary>
    public void ApplyStun()
    {
        if (currentState == AIState.Stunned) return;
        
        canAttack = true; 

        stunCount++;
        stunMemoryTimer = memoryResetTime; 

        float duration = 0f;
        if (stunCount == 1) duration = 3f;
        else if (stunCount == 2) duration = 1.5f;
        else
        {
            return; 
        }

        if (audioSource != null) audioSource.PlayOneShot(sfxStunned != null ? sfxStunned : ProceduralAudioGen.GenerateErrorBuzz(80f, 1.5f));
        
        ChangeState(AIState.Stunned);
        StartCoroutine(StunRoutine(duration));
    }

    private System.Collections.IEnumerator StunRoutine(float time)
    {
        hasMoveTarget = false; 
        yield return new WaitForSeconds(time);
        if (currentState == AIState.Stunned)
        {
            ChangeState(AIState.Investigating);
        }
    }

    /// <summary>
    /// Physically displaces the AI away from the player via external forces (Repulsor).
    /// </summary>
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
            ChangeState(AIState.Investigating); 
        }
    }

    public void DistractToLocation(Vector3 distractionPos, float duration)
    {
        if (CanInterruptState(AIState.Distracted))
        {
            ChangeState(AIState.Distracted);
            if (_distractionCoroutine != null) StopCoroutine(_distractionCoroutine);
            _distractionCoroutine = StartCoroutine(DistractionRoutine(distractionPos, duration));
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

    private void FixedUpdate()
    {
        if (rb != null) rb.linearVelocity = Vector2.zero; 

        if (!hasMoveTarget || currentState == AIState.Stunned || currentState == AIState.KnockedBack || currentState == AIState.Attacking || currentState == AIState.Idle) return;
        
        float activeSpeed = isSignalEmpowered ? currentSpeed * signalSpeedMultiplier : currentSpeed;
        Vector2 currentPosition = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 targetDirection = (moveTarget - currentPosition).normalized;
        
        Vector2 safeDirection = GetAvoidanceDirection(currentPosition, targetDirection);
        Vector2 newPosition = currentPosition + (safeDirection * activeSpeed * Time.fixedDeltaTime);
        
        if (rb != null) rb.MovePosition(newPosition);
        else transform.position = new Vector3(newPosition.x, newPosition.y, transform.position.z);
        
        _stuckTimer += Time.fixedDeltaTime;
        if (_stuckTimer >= stuckTimeLimit)
        {
            if (Vector2.Distance(currentPosition, _stuckCheckPos) < stuckDistanceThreshold)
            {
                _totalStuckTime += _stuckTimer;
                hasMoveTarget = false; 

                if (_totalStuckTime >= teleportFailsafeLimit)
                {
                    TeleportToOpenArea();
                    _totalStuckTime = 0f;
                }
            }
            else _totalStuckTime = 0f; 
            
            _stuckCheckPos = currentPosition;
            _stuckTimer = 0f;
        }

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
        Vector2 randomDirection = Random.insideUnitCircle * searchRadius;
        wanderTarget = lastKnownPosition + randomDirection;

        wanderWaitTimer = Random.Range(0.2f, 1f);
        stateTimer = 0f;
        SetMoveTarget(wanderTarget, baseSpeed);
    }

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
                    animator.SetFloat("Direction", 1f); 
                    spriteRenderer.flipX = direction.x < 0;
                }
                else
                {
                    if (direction.y > 0)
                    {
                        animator.SetFloat("Direction", 1f); 
                        if (direction.x < -0.01f) spriteRenderer.flipX = true;
                        else if (direction.x > 0.01f) spriteRenderer.flipX = false;
                    }
                    else
                    {
                        animator.SetFloat("Direction", 0f); 
                        spriteRenderer.flipX = false;
                    }
                }
                
                spriteRenderer.flipY = false; 
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
            animator.speed = 1f; 
        }
        else if (currentState == AIState.Attacking)
        {
            animator.speed = activeMultiplier * 1.75f; 
        }
        else
        {
            animator.SetFloat("Speed", currentSpeed);
            animator.speed = Mathf.Max(1f, currentSpeed / baseSpeed) * activeMultiplier;
        }
    }

    /// <summary>
    /// Initiates a permanent, empowered hunting state. Used for climax/meltdown sequences.
    /// </summary>
    public void TriggerEnragedHunt()
    {
        isEnraged = true;
        isSignalEmpowered = true; 
        
        sprintSpeed *= 1.30f; // Boost sprint speed by 30% for the final chase sequence!
        ChangeState(AIState.Hunting);
    }

    /// <summary>
    /// Forces the AI to trigger a melee animation without modifying logic state, typically for cinematics.
    /// </summary>
    public void TriggerCinematicAttack(Vector3 targetPosition)
    {
        if (animator == null) return;
        
        Vector2 dir = (targetPosition - transform.position).normalized;
        
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            animator.SetFloat("Direction", 1f); 
            if (spriteRenderer != null) spriteRenderer.flipX = dir.x < 0;
        }
        else
        {
            if (dir.y > 0)
            {
                animator.SetFloat("Direction", 1f); 
                if (spriteRenderer != null)
                {
                    if (dir.x < -0.01f) spriteRenderer.flipX = true;
                    else if (dir.x > 0.01f) spriteRenderer.flipX = false;
                }
            }
            else
            {
                animator.SetFloat("Direction", 0f); 
                if (spriteRenderer != null) spriteRenderer.flipX = false;
            }
        }
        
        animator.speed = 1f; 
        animator.SetTrigger("Attack");
    }

    /// <summary>
    /// Overrides the visual orientation of the AI, useful for cinematic staging.
    /// </summary>
    public void ForceLookDirection(Vector2 dir)
    {
        if (animator == null) return;
        
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            animator.SetFloat("Direction", 1f); 
            if (spriteRenderer != null) spriteRenderer.flipX = dir.x < 0;
        }
        else
        {
            if (dir.y > 0)
            {
                animator.SetFloat("Direction", 1f); 
                if (spriteRenderer != null)
                {
                    if (dir.x < -0.01f) spriteRenderer.flipX = true;
                    else if (dir.x > 0.01f) spriteRenderer.flipX = false;
                }
            }
            else
            {
                animator.SetFloat("Direction", 0f); 
                if (spriteRenderer != null) spriteRenderer.flipX = false;
            }
        }
    }

    /// <summary>
    /// Calculates an alternate movement vector to bypass spatial obstacles.
    /// </summary>
    private Vector2 GetAvoidanceDirection(Vector2 currentPos, Vector2 targetDir)
    {
        if (IsPathClear(currentPos, proxyWidth, targetDir, whiskerLength)) 
        {
            _avoidanceBias = 1f; 
            return targetDir;
        }

        for (int i = 1; i <= whiskerCount; i++)
        {
            float currentAngle = whiskerAngle * i;

            Vector2 biasedDir = RotateVector(targetDir, currentAngle * _avoidanceBias);
            if (IsPathClear(currentPos, proxyWidth, biasedDir, whiskerLength)) return biasedDir;

            Vector2 oppositeDir = RotateVector(targetDir, currentAngle * -_avoidanceBias);
            if (IsPathClear(currentPos, proxyWidth, oppositeDir, whiskerLength)) 
            {
                _avoidanceBias = -_avoidanceBias; 
                return oppositeDir;
            }
        }

        RaycastHit2D hit = DoRaycast(currentPos, proxyWidth, targetDir, whiskerLength);
        if (hit.collider != null)
        {
            Vector2 slideDir = Vector2.Perpendicular(hit.normal).normalized;
            if (Vector2.Dot(slideDir, targetDir) < 0) slideDir = -slideDir;
            // Add a slight push AWAY from the wall to prevent snagging on tight corners
            return (slideDir + hit.normal * 0.4f).normalized;
        }

        return targetDir;
    }

    private bool IsPathClear(Vector2 origin, float radius, Vector2 direction, float distance)
    {
        return DoRaycast(origin, radius, direction, distance).collider == null;
    }

    /// <summary>
    /// Wrapper for a non-allocating circle cast that respects internal layer masks and ignores triggers.
    /// </summary>
    private RaycastHit2D DoRaycast(Vector2 origin, float radius, Vector2 direction, float distance)
    {
        int hitCount = Physics2D.CircleCast(origin, radius, direction, _avoidanceFilter, _raycastHits, distance);
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D currentHit = _raycastHits[i];
            if (currentHit.collider.gameObject != this.gameObject && !currentHit.collider.CompareTag("Player"))
            {
                return currentHit;
            }
        }
        return default;
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
        Vector2 bestSpot = transform.position; 
        bool spotFound = false;

        for (float dist = 10f; dist >= 1.5f; dist -= 1.5f)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 testPos = playerPos + (dir * dist);

                RaycastHit2D losHit = Physics2D.Raycast(playerPos, dir, dist, obstacleMask);
                
                if (losHit.collider == null) 
                {
                    int overlaps = Physics2D.OverlapCircle(testPos, proxyWidth + 0.1f, _avoidanceFilter, _overlapHits);
                    
                    if (overlaps == 0)
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
            hasMoveTarget = false;
            UpdateAnimatorDirection(playerPos);
        }
    }

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
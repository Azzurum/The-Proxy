using UnityEngine;

/// <summary>
/// Manages Kaelen's core movement, sprint stamina, and animation states.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Base walking speed for the character.")]
    [SerializeField] private float moveSpeed = 4.0f;
    [Tooltip("The character's movement speed while sprinting.")]
    [SerializeField] private float sprintSpeed = 6.5f;
    [Tooltip("The initial stamina capacity before fatigue sets in.")]
    [SerializeField] private float sprintMeterThreshold = 5f;

    [Header("Sprint & Fatigue System")]
    [Tooltip("The base rate at which the sprint meter recovers per second.")]
    [SerializeField] private float baseDecayRate = 0.08f;
    [Tooltip("Multiplier applied to the decay rate when the player is standing still.")]
    [SerializeField] private float idleDecayMultiplier = 2.0f;
    [Tooltip("How much the sprint meter threshold increases after being fully depleted.")]
    [SerializeField] private float thresholdIncrease = 2f;

    [Header("Status Penalties")]
    [Tooltip("Speed multiplier applied when the 'Crush' penalty is active.")]
    [SerializeField] private float crushSpeedMultiplier = 0.8f;
    [Tooltip("Speed multiplier applied when the sprint meter is fully depleted.")]
    [SerializeField] private float fatigueSpeedMultiplier = 0.6f;

    [Header("Audio")]
    [Tooltip("The heavy breathing sound played as a warning when stamina is low.")]
    [SerializeField] private AudioClip breathingClip;
    [Tooltip("Time between footstep sounds while walking.")]
    [SerializeField] private float walkStepInterval = 0.45f;
    [Tooltip("Time between footstep sounds while sprinting.")]
    [SerializeField] private float sprintStepInterval = 0.28f;

    [Header("System State")]
    [Tooltip("If true, all movement input is blocked. Used for cinematics or interactions.")]
    public bool isRooted = false;

    [Header("Animation & Visuals")]
    [Tooltip("The Animator component for the player character.")]
    public Animator animator;
    [Tooltip("The Unity Sorting Layer used to dynamically sort depth.")]
    [SerializeField] private string sortingLayerName = "Player";
    [Tooltip("Vertical offset from the pivot to determine the character's sorting order (should be at their feet).")]
    [SerializeField] private float depthOffset = -0.5f;

    // Private component references
    private Rigidbody2D _rb;
    private AudioSource _audioSource;
    private SpriteRenderer _spriteRenderer;
    private ScreenEffectManager _screenEffect;

    // Internal state
    private Vector2 _movementInput;
    private float _sprintMeter = 0f;
    private bool _isSprinting = false;
    private bool _corruptionAddedThisCycle = false;
    private float _currentThreshold;
    private bool _isMovementLocked = false;
    private float _baseScaleX;
    private float _footstepTimer = 0f;
    
    /// <summary>A read-only property for the current sprint meter value.</summary>
    public float SprintMeter => _sprintMeter;
    /// <summary>A read-only property for the current sprint meter capacity.</summary>
    public float SprintMeterThreshold => _currentThreshold;

    void Start()
    {
        // --- Component Caching ---
        _rb = GetComponent<Rigidbody2D>();
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _screenEffect = ScreenEffectManager.Instance;
        
        // --- Physics Setup ---
        _rb.bodyType = RigidbodyType2D.Dynamic;
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;

        // Create a frictionless material at runtime to ensure the player slides along walls smoothly.
        PhysicsMaterial2D slipMat = new PhysicsMaterial2D("PlayerSlip");
        slipMat.friction = 0f;
        slipMat.bounciness = 0f;
        _rb.sharedMaterial = slipMat;

        // --- State Initialization ---
        _currentThreshold = sprintMeterThreshold;
        _baseScaleX = Mathf.Abs(transform.localScale.x);
        if (_spriteRenderer != null) _spriteRenderer.sortingLayerName = sortingLayerName;

        // --- BULLETPROOF CAMERA FIX ---
        // Guarantees the camera always finds and locks onto Kaelen instantly when any new scene starts!
        // (We skip this in level_1 so we don't accidentally interrupt the opening space cinematic).
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "level_1")
        {
            CameraFollow camFollow = FindAnyObjectByType<CameraFollow>(FindObjectsInactive.Include);
            if (camFollow != null)
            {
                camFollow.target = this.transform;
                camFollow.enabled = true; // Force it on in case it was saved as disabled in the prefab!
                
                Camera cam = camFollow.GetComponent<Camera>();
                if (cam != null)
                {
                    cam.transform.position = this.transform.position + camFollow.offset;
                }
            }
        }
    }

    void Update()
    {
        HandleInput();
        HandleStamina();
        UpdateAnimationAndVisuals();
        HandleFootsteps();
    }

    void FixedUpdate()
    {
        // Immediately kill any external physical forces to prevent sliding.
        _rb.linearVelocity = Vector2.zero;

        if (isRooted || _isMovementLocked)
        {
            return;
        }

        // Determine the final effective speed based on current state and penalties.
        float effectiveSpeed = _isSprinting ? sprintSpeed : moveSpeed;
        if (InventoryManager.Instance != null && InventoryManager.Instance.CrushTier >= 1)
        {
            effectiveSpeed *= crushSpeedMultiplier;
        }
        if (_sprintMeter >= _currentThreshold)
        {
            effectiveSpeed *= fatigueSpeedMultiplier;
        }
        
        // Apply movement to the Rigidbody for smooth, collision-aware motion.
        _rb.MovePosition(_rb.position + _movementInput * effectiveSpeed * Time.fixedDeltaTime);
    }

    /// <summary>
    /// Reads raw player input and handles gameplay locks like 'isRooted'.
    /// </summary>
    private void HandleInput()
    {
        if (isRooted || _isMovementLocked)
        {
            _movementInput = Vector2.zero;
            return;
        }

        _movementInput.x = Input.GetAxisRaw("Horizontal");
        _movementInput.y = Input.GetAxisRaw("Vertical");
        _movementInput = _movementInput.normalized;
    }

    /// <summary>
    /// Manages the sprint meter, fatigue penalties, and corruption gain from over-exertion.
    /// </summary>
    private void HandleStamina()
    {
        _isSprinting = Input.GetKey(KeyCode.LeftShift) && _movementInput != Vector2.zero && _sprintMeter < _currentThreshold;
        bool isIdle = _movementInput == Vector2.zero;
        float decayRate = baseDecayRate * (isIdle ? idleDecayMultiplier : 1f);

        if (_isSprinting)
        {
            // Per the GDD, the Crush penalty makes sprinting more taxing.
            float strainRate = (InventoryManager.Instance != null && InventoryManager.Instance.CrushTier >= 3) ? 2.0f : 1.0f;
            
            _sprintMeter += Time.deltaTime * strainRate;
            if (_sprintMeter >= _currentThreshold && !_corruptionAddedThisCycle)
            {
                if (InventoryManager.Instance != null) InventoryManager.Instance.AddCorruptionRow();
                _currentThreshold += thresholdIncrease;
                _sprintMeter = _currentThreshold;
                _corruptionAddedThisCycle = true;
                _isMovementLocked = true; // Lock movement briefly to signify exhaustion.
            }
        }
        else
        {
            _sprintMeter = Mathf.Max(0f, _sprintMeter - Time.deltaTime * decayRate);
            if (_sprintMeter < _currentThreshold)
            {
                _corruptionAddedThisCycle = false;
            }
            // Allow movement again once stamina has recovered slightly.
            if (_sprintMeter <= _currentThreshold * 0.95f)
            {
                _isMovementLocked = false;
            }
        }

        // Trigger audio/visual warnings when stamina is critically low.
        bool nearThreshold = _sprintMeter >= _currentThreshold - 1f && _isSprinting;
        if (_screenEffect != null) _screenEffect.SetWarning(nearThreshold);
        if (nearThreshold && !_audioSource.isPlaying && breathingClip != null)
        {
            _audioSource.PlayOneShot(breathingClip);
        }
    }

    /// <summary>
    /// Triggers procedural soft footstep sounds based on movement and sprint state.
    /// </summary>
    private void HandleFootsteps()
    {
        if (_movementInput != Vector2.zero && !isRooted && !_isMovementLocked)
        {
            _footstepTimer -= Time.deltaTime;
            if (_footstepTimer <= 0f)
            {
                _audioSource.PlayOneShot(ProceduralAudioGen.GenerateSoftFootstep());
                _footstepTimer = _isSprinting ? sprintStepInterval : walkStepInterval;
            }
        }
        else
        {
            _footstepTimer = 0f; // Instantly step when starting to move
        }
    }

    /// <summary>
    /// Updates the Animator, sprite flipping, and depth sorting based on the current movement state.
    /// </summary>
    private void UpdateAnimationAndVisuals()
    {
        if (animator != null)
        {
            // The animator uses the last non-zero input to determine which way to face when idle.
            if (_movementInput != Vector2.zero)
            {
                animator.SetFloat("Horizontal", _movementInput.x);
                animator.SetFloat("Vertical", _movementInput.y);
            }
            animator.SetFloat("Speed", _movementInput.sqrMagnitude);

            // Double the animation speed if we are actively sprinting!
            animator.speed = (_isSprinting && _movementInput != Vector2.zero && !isRooted && !_isMovementLocked) ? 2.0f : 1.0f;
        }

        // Flip the entire character transform based on horizontal movement direction.
        if (_movementInput.x > 0.01f)
        {
            transform.localScale = new Vector3(_baseScaleX, transform.localScale.y, transform.localScale.z);
        }
        else if (_movementInput.x < -0.01f)
        {
            transform.localScale = new Vector3(-_baseScaleX, transform.localScale.y, transform.localScale.z);
        }

        // Dynamically update sorting order to allow walking in front of/behind objects.
        if (_spriteRenderer != null)
        {
            _spriteRenderer.sortingOrder = Mathf.RoundToInt((transform.position.y + depthOffset) * -10f);
        }
    }

    /// <summary>
    /// Loads the character's stamina state from a save file.
    /// </summary>
    public void LoadStaminaState(float savedMeter, float savedThreshold)
    {
        _sprintMeter = savedMeter;
        _currentThreshold = savedThreshold;
        
        // Ensure the player is not stuck in a locked state after loading.
        _isMovementLocked = false;
        _corruptionAddedThisCycle = false;
    }
}
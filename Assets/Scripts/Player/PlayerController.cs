using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float sprintMeterThreshold = 5f; 

    [Header("Sprint Decay")]
    public float baseDecayRate = 2f;
    public float idleDecayMultiplier = 1.5f;
    public float thresholdIncrease = 2f; 

    [Header("Crush Penalty")]
    public float crushSpeedMultiplier = 0.8f;

    [Header("Fatigue Penalty")]
    public float fatigueSpeedMultiplier = 0.6f; 

    [Header("Audio Warning")]
    public AudioClip breathingClip;

    [Header("System State")]
    public bool isRooted = false; 

    [Header("Animations")]
    public Animator animator; 

    private Rigidbody2D rb;
    private Vector2 movementInput;
    private InventoryManager inventoryManager;
    private float sprintMeter = 0f;
    private bool isSprinting = false;
    private ScreenEffectManager screenEffect;
    private AudioSource audioSource;
    private bool corruptionAddedThisCycle = false;
    private float currentThreshold;
    private bool isMovementLocked = false;
    private float baseScale;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        inventoryManager = FindAnyObjectByType<InventoryManager>();
        screenEffect = FindAnyObjectByType<ScreenEffectManager>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        currentThreshold = sprintMeterThreshold;
        baseScale = Mathf.Abs(transform.localScale.x);
    }

    void Update()
    {
        // 1. ALWAYS read input first
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");
        movementInput = movementInput.normalized;

        // 2. IMMEDIATELY send this to the animator
        if (animator != null)
        {
            if (movementInput != Vector2.zero)
            {
                animator.SetFloat("Horizontal", movementInput.x);
                animator.SetFloat("Vertical", movementInput.y);
            }
            animator.SetFloat("Speed", movementInput.sqrMagnitude);
        }

        // 3. FLIP THE VISUALS DYNAMICALLY
        if (movementInput.x > 0)
        {
            // Face Right
            transform.localScale = new Vector3(baseScale, transform.localScale.y, transform.localScale.z);
        }
        else if (movementInput.x < 0)
        {
            // Face Left
            transform.localScale = new Vector3(-baseScale, transform.localScale.y, transform.localScale.z);
        }

        // 4. NOW apply your gameplay locks
        if (isRooted)
        {
            movementInput = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isMovementLocked)
        {
            movementInput = Vector2.zero;
        }

        // 5. Sprint handling
        isSprinting = Input.GetKey(KeyCode.LeftShift) && movementInput != Vector2.zero && sprintMeter < currentThreshold;
        bool isIdle = movementInput == Vector2.zero;
        float decayRate = baseDecayRate * (isIdle ? idleDecayMultiplier : 1f);

        if (isSprinting)
        {
            sprintMeter += Time.deltaTime;
            if (sprintMeter >= currentThreshold && !corruptionAddedThisCycle)
            {
                inventoryManager.AddCorruptionRow();
                currentThreshold += thresholdIncrease; 
                sprintMeter = currentThreshold; 
                corruptionAddedThisCycle = true;
                isMovementLocked = true; 
                Debug.Log("SPRINT: Extended sprinting added 1 row of corruption!");
            }
        }
        else
        {
            sprintMeter = Mathf.Max(0f, sprintMeter - Time.deltaTime * decayRate);
            if (sprintMeter < currentThreshold)
            {
                corruptionAddedThisCycle = false;
            }
            if (sprintMeter <= currentThreshold * 0.5f)
            {
                isMovementLocked = false;
            }
        }

        // Warning when close to threshold
        bool nearThreshold = sprintMeter >= currentThreshold - 1f && isSprinting;
        if (screenEffect != null) screenEffect.SetWarning(nearThreshold);
        if (nearThreshold && !audioSource.isPlaying && breathingClip != null)
        {
            audioSource.PlayOneShot(breathingClip);
        }
    }

    void FixedUpdate()
    {
        // Force stop if movement is locked
        if (isMovementLocked)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float effectiveSpeed = moveSpeed;
        if (inventoryManager != null && inventoryManager.CrushTier >= 1)
        {
            effectiveSpeed *= crushSpeedMultiplier;
        }
        if (isSprinting)
        {
            effectiveSpeed = sprintSpeed;
        }
        if (sprintMeter >= currentThreshold)
        {
            effectiveSpeed *= fatigueSpeedMultiplier;
        }
        
        // Apply the calculated movement to the physics body
        rb.MovePosition(rb.position + movementInput * effectiveSpeed * Time.fixedDeltaTime);
    }

    public float SprintMeter => sprintMeter;
    public float SprintMeterThreshold => currentThreshold;

    // ==========================================
    // SAVE SYSTEM INTEGRATION
    // ==========================================
    public void LoadStaminaState(float savedMeter, float savedThreshold)
    {
        sprintMeter = savedMeter;
        currentThreshold = savedThreshold;
        
        // Reset any physical locks so the player doesn't spawn frozen!
        isMovementLocked = false;
        corruptionAddedThisCycle = false;
    }
}
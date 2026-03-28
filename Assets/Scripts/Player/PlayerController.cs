using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float sprintMeterThreshold = 5f; // Initial time to build before adding corruption

    [Header("Sprint Decay")]
    public float baseDecayRate = 2f;
    public float idleDecayMultiplier = 1.5f;
    public float thresholdIncrease = 2f; // How much harder it gets each time

    [Header("Crush Penalty")]
    public float crushSpeedMultiplier = 0.8f;

    [Header("Fatigue Penalty")]
    public float fatigueSpeedMultiplier = 0.6f; // Speed when stamina bar is full

    [Header("Audio Warning")]
    public AudioClip breathingClip;

    [Header("System State")]
    public bool isRooted = false; // Locks Kaelen in place when M.E.T. Rig is open

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

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        inventoryManager = FindFirstObjectByType<InventoryManager>();
        screenEffect = FindFirstObjectByType<ScreenEffectManager>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        currentThreshold = sprintMeterThreshold;
    }

    void Update()
    {
        // 1. THE LOCKDOWN: If boots are clamped, kill momentum and skip input!
        if (isRooted)
        {
            movementInput = Vector2.zero;
            rb.linearVelocity = Vector2.zero; // Force stop any physics sliding
            return;
        }

        // 2. Read standard WASD or Arrow Key inputs
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");

        // 3. Normalize prevents moving twice as fast diagonally
        movementInput = movementInput.normalized;

        // Lock movement if stamina penalty is active
        if (isMovementLocked)
        {
            movementInput = Vector2.zero;
        }

        // 4. Sprint handling
        isSprinting = Input.GetKey(KeyCode.LeftShift) && movementInput != Vector2.zero && sprintMeter < currentThreshold;
        bool isIdle = movementInput == Vector2.zero;
        float decayRate = baseDecayRate * (isIdle ? idleDecayMultiplier : 1f);

        if (isSprinting)
        {
            sprintMeter += Time.deltaTime;
            if (sprintMeter >= currentThreshold && !corruptionAddedThisCycle)
            {
                // Add corruption row
                inventoryManager.AddCorruptionRow();
                currentThreshold += thresholdIncrease; // Make it harder next time
                sprintMeter = currentThreshold; // Stay at new max
                corruptionAddedThisCycle = true;
                isMovementLocked = true; // Lock movement until bar drops to 50%
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
}
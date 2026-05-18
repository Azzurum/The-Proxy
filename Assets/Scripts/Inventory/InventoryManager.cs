using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using UnityEngine.SceneManagement;

public class InventoryManager : MonoBehaviour
{
    [Header("Data & Layout Architecture")]
    public InventoryState inventoryState;
    public Transform gridLeft;
    public Transform gridRight;
    public Transform gridExt;
    public GameObject emptySlotPrefab;
    public GameObject filledItemPrefab;

    [Tooltip("Drag all your ItemData ScriptableObjects here so the game can load them by ID")]
    public List<ItemData> itemDatabase = new List<ItemData>();

    [Header("Grid Sizing")]
    public float cellSizeOverride = 0f; 
    private bool gridRefreshPending = false;

    [Header("World Spawning")]
    public Transform playerTransform;
    public GameObject physicalBatteryPrefab;

    [Header("Corruption Setup")]
    public ItemData corruptionData;

    [Header("Audio SFX")]
    public AudioSource audioSource;
    public AudioClip sfxCorruptionAdded;

    [Header("MOTHER-v4 System Shock")]
    public float shockInterval = 60f; // UPDATED TO 60 SECONDS
    public float shockTimer;
    private MetRigManager metRigManager;

    [Header("Signal Broadcasting")]
    public UnityEvent onItemDropped; 

    [Header("Inspection UI")]
    public UnityEngine.UI.Image uiInspectIcon; // The visual square on the left

    // --- Health Event System ---
    public event Action<float> OnHealthStateChanged; // The broadcast megaphone for UI

    [Header("System Constants")]
    public bool isSystemActive = true;
    public Slider systemShockProgressBar;

    [Header("Crush Penalties")]
    private int crushTier = 0;
    private float crushTimer = 0f;
    private float[] crushDurations = { 5f, 10f }; 

    [Header("Hallucination System")]
    private float hallucinationCooldown = 15f;
    private float jumpscareCooldown = 45f; // Initial cooldown before first jumpscare

    [Header("Game Over State")]
    private bool isGameOverSequenceStarted = false;

    [Header("Locker State")]
    public bool isInteractingWithLocker = false;
    private LockerStorage activeLocker = null; // Track the locker currently being viewed

    void Start()
    {
        while (inventoryState.mainGridSlots.Count < 100) inventoryState.mainGridSlots.Add(null);
        while (inventoryState.matterBufferSlots.Count < 25) inventoryState.matterBufferSlots.Add(null);
        
        // Ensure the external UI always defaults to the Buffer on boot
        inventoryState.extGridSlots = inventoryState.matterBufferSlots;

        while (inventoryState.hotbarSlots.Count < 3) inventoryState.hotbarSlots.Add(null);

        metRigManager = FindAnyObjectByType<MetRigManager>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        shockInterval = 60f; // UPDATED TO 60 SECONDS
        shockTimer = shockInterval;
        
        RefreshAllGrids();
        BroadcastHealthState(); // Ensure UI gets the initial health on boot
        ClearInspectionScreen(); // NEW: Clear the screen on boot
        LoadWorldItems(); // Load any items dropped on the floor!
    }

    void Update()
    {
        // FIXED: Removed 'metRigManager.isRigOpen' so this timer runs everywhere, constantly!
        if (isSystemActive)
        {
            // LORE UPDATE: Leaving the M.E.T. Rig open heavily accelerates corruption buildup!
            float tickMultiplier = (metRigManager != null && metRigManager.isRigOpen) ? 2.5f : 1.0f;
            shockTimer -= Time.deltaTime * tickMultiplier;

            if (systemShockProgressBar != null) 
            {
                systemShockProgressBar.value = 1f - (shockTimer / shockInterval);
            }

            if (HasHallucinations)
            {
                if (jumpscareCooldown > 0f) jumpscareCooldown -= Time.deltaTime;
                hallucinationCooldown -= Time.deltaTime;

                if (hallucinationCooldown <= 0f)
                {
                    TriggerRandomHallucination();
                    
                    // LORE UPDATE: Hallucinations appear when corruption reaches 5 stacks, and triple in frequency at 8 stacks!
                    float baseCooldown = GetCorruptionPercentage() >= 0.8f ? 8f : 22f;
                    hallucinationCooldown = baseCooldown + UnityEngine.Random.Range(-4f, 4f);
                }
            }

            if (shockTimer <= 0f)
            {
                ResolveCorruptionTick();
                shockTimer = shockInterval;
            }
        }

        if (crushTimer > 0)
        {
            crushTimer -= Time.deltaTime;

            if (crushTimer <= 0)
            {
                crushTier = Mathf.Max(0, crushTier - 1);

                if (crushTier > 0) crushTimer = crushDurations[Mathf.Min(crushTier - 1, crushDurations.Length - 1)];
            }
        }

        if (Input.GetKeyDown(KeyCode.C)) ExecuteCleanProtocol();

        // DEBUG: Press H to instantly test the Hallucination Jumpscare!
        if (Input.GetKeyDown(KeyCode.H)) StartCoroutine(FakeProxyJumpscareRoutine());
    }

    // ==========================================
    // HALLUCINATION SYSTEM
    // ==========================================

    private void TriggerRandomHallucination()
    {
        float rand = UnityEngine.Random.value;
        
        // Jumpscare chance: only allow if cooldown is ready (at least 90s between scares)
        if (rand < 0.25f && jumpscareCooldown <= 0f) 
        {
            jumpscareCooldown = UnityEngine.Random.Range(80f, 110f); // ~1.5 minutes cooldown!
            StartCoroutine(FakeProxyJumpscareRoutine());
        }
        else if (audioSource != null) // 85% chance for creepy audio
        {
            float audioRand = UnityEngine.Random.value;
            if (audioRand < 0.30f) StartCoroutine(ApproachingFootstepsRoutine()); // 30% Approaching footsteps
            else if (audioRand < 0.60f) PlaySpatialAudio(ProceduralAudioGen.GenerateWhisper()); // 30% Whisper
            else if (audioRand < 0.80f) audioSource.PlayOneShot(ProceduralAudioGen.GenerateFootstep(0.4f)); // 20% Fake footstep
            else if (audioRand < 0.90f) audioSource.PlayOneShot(ProceduralAudioGen.GenerateStaticGlitch(0.3f)); // 10% Fake proxy static
            else audioSource.PlayOneShot(ProceduralAudioGen.GenerateHiss(0.5f)); // 10% Distant hiss
        }
    }

    private System.Collections.IEnumerator ApproachingFootstepsRoutine()
    {
        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null) yield break;

        // Spawn a temporary audio source off-screen
        GameObject tempAudioObj = new GameObject("Hallucination_Footsteps");
        Vector2 startDir = UnityEngine.Random.insideUnitCircle.normalized;
        float distance = 12f;
        tempAudioObj.transform.position = player.position + (Vector3)(startDir * distance);

        AudioSource tempSource = tempAudioObj.AddComponent<AudioSource>();
        tempSource.spatialBlend = 0.8f; 
        tempSource.rolloffMode = AudioRolloffMode.Linear;
        tempSource.minDistance = 2f;
        tempSource.maxDistance = 15f;

        int steps = UnityEngine.Random.Range(5, 9);
        float timeBetweenSteps = 0.6f;

        for (int i = 0; i < steps; i++)
        {
            if (player == null || tempAudioObj == null) break;

            // Move the sound progressively closer to the player
            float progress = (float)i / (steps - 1);
            distance = Mathf.Lerp(12f, 2f, progress);
            
            // Keep the direction, but update position relative to the player's current position
            tempAudioObj.transform.position = player.position + (Vector3)(startDir * distance);

            // Increase volume/intensity
            tempSource.volume = Mathf.Lerp(0.2f, 1.0f, progress) * ProceduralAudioGen.globalVolume;
            tempSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
            
            tempSource.PlayOneShot(ProceduralAudioGen.GenerateFootstep(0.4f));

            yield return new WaitForSeconds(timeBetweenSteps + UnityEngine.Random.Range(-0.05f, 0.05f));
        }

        Destroy(tempAudioObj, 1.0f);
    }

    private void PlaySpatialAudio(AudioClip clip)
    {
        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null) return;

        // Spawn very close to the player's ear (left or right)
        GameObject tempAudioObj = new GameObject("Hallucination_Whisper");
        Vector2 sideDir = UnityEngine.Random.value > 0.5f ? Vector2.right : Vector2.left;
        tempAudioObj.transform.position = player.position + (Vector3)(sideDir * 1.5f);

        AudioSource tempSource = tempAudioObj.AddComponent<AudioSource>();
        tempSource.spatialBlend = 0.8f; 
        tempSource.rolloffMode = AudioRolloffMode.Linear;
        tempSource.minDistance = 1f;
        tempSource.maxDistance = 5f;
        tempSource.volume = ProceduralAudioGen.globalVolume;
        tempSource.clip = clip;
        tempSource.Play();

        Destroy(tempAudioObj, clip.length + 0.1f);
    }

    private System.Collections.IEnumerator FakeProxyJumpscareRoutine()
    {
        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        ProxyAI realProxy = FindAnyObjectByType<ProxyAI>();

        if (player == null || realProxy == null) yield break;

        // 1. Find a valid off-screen spawn location with NO walls in the way
        Vector3 bestSpawnPos = Vector3.zero;
        float maxClearDist = 0f;
        float targetSpawnDist = 15f; // Ensures it starts outside the camera view

        for (int i = 0; i < 16; i++) // Scan in 16 random directions
        {
            Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
            float currentClearDist = targetSpawnDist;

            RaycastHit2D[] hits = Physics2D.RaycastAll(player.position, dir, targetSpawnDist);
            foreach (var hit in hits)
            {
                // Stop if we hit a wall (ignores triggers, items, and the player)
                if (!hit.collider.isTrigger && !hit.collider.CompareTag("Player") && !hit.collider.CompareTag("Interactable") && !hit.collider.CompareTag("MasterKey"))
                {
                    if (hit.distance < currentClearDist) currentClearDist = hit.distance;
                }
            }

            // Keep track of the longest clear path
            if (currentClearDist > maxClearDist)
            {
                maxClearDist = currentClearDist;
                bestSpawnPos = player.position + (Vector3)(dir * (currentClearDist - 1f)); // Spawn slightly in front of the wall
            }
        }

        // If Kaelen is in a tiny room and the furthest wall is too close (on-screen), abort the visual and just play a creepy sound
        if (maxClearDist < 8f)
        {
            if (audioSource != null) audioSource.PlayOneShot(ProceduralAudioGen.GenerateStaticGlitch(0.5f));
            yield break;
        }

        // 1.5 Instantiate an exact copy of the real Proxy
        GameObject fakeProxy = Instantiate(realProxy.gameObject, bestSpawnPos, Quaternion.identity);
        fakeProxy.name = "Hallucination_Proxy";

        // Strip all AI and Physics so it is purely a visual ghost
        ProxyAI fakeAI = fakeProxy.GetComponent<ProxyAI>();
        if (fakeAI != null) { fakeAI.enabled = false; Destroy(fakeAI); }
        
        Rigidbody2D rb = fakeProxy.GetComponent<Rigidbody2D>();
        if (rb != null) Destroy(rb);
        
        foreach (var col in fakeProxy.GetComponentsInChildren<Collider2D>()) Destroy(col);

        Animator fakeAnim = fakeProxy.GetComponent<Animator>();
        SpriteRenderer fakeRenderer = fakeProxy.GetComponent<SpriteRenderer>();
        if (fakeRenderer != null) fakeRenderer.sortingOrder = 10;

        // 2. Play a terrifying sound as it spawns
        if (audioSource != null) audioSource.PlayOneShot(ProceduralAudioGen.GenerateStaticGlitch(1.0f));

        float speed = 25f; // Insanely fast
        
        if (fakeAnim != null)
        {
            fakeAnim.SetFloat("Speed", speed);
            fakeAnim.speed = Mathf.Max(1f, speed / realProxy.baseSpeed); // Turbo animation speed!
        }

        // 3. Sprint at the player
        while (fakeProxy != null && player != null && Vector3.Distance(fakeProxy.transform.position, player.position) > 1.0f)
        {
            fakeProxy.transform.position = Vector3.MoveTowards(fakeProxy.transform.position, player.position, speed * Time.deltaTime);
            
            // Update the animation direction to accurately match its movement!
            Vector2 dir = (player.position - fakeProxy.transform.position).normalized;
            if (fakeAnim != null && fakeRenderer != null)
            {
                if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
                {
                    fakeAnim.SetFloat("Direction", 1f); // Side
                    fakeRenderer.flipX = dir.x < 0;
                }
                else
                {
                    if (dir.y > 0)
                    {
                        fakeAnim.SetFloat("Direction", 1f); // Up
                        if (dir.x < -0.01f) fakeRenderer.flipX = true;
                        else if (dir.x > 0.01f) fakeRenderer.flipX = false;
                    }
                    else
                    {
                        fakeAnim.SetFloat("Direction", 0f); // Down
                        fakeRenderer.flipX = false;
                    }
                }
            }
            yield return null;
        }

        // 4. Hit the player and vanish with a violent pop
        if (fakeProxy != null)
        {
            if (audioSource != null) audioSource.PlayOneShot(ProceduralAudioGen.GenerateErrorBuzz(100f, 0.2f));
            
            CameraFollow cam = FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.TriggerShake(0.3f, 0.5f);
            
            if (ScreenEffectManager.Instance != null) ScreenEffectManager.Instance.TriggerFlash(Color.black, 0.15f);
            
            Destroy(fakeProxy);
        }
    }

    public void ResolveCorruptionTick()
    {   
        // Forcefully cancel any active drag before restructuring the grid!
        if (DraggableItem.itemBeingDragged != null) DraggableItem.itemBeingDragged.AbortDrag();

        // Play the corruption warning sound
        if (audioSource != null)
        {
            audioSource.PlayOneShot(sfxCorruptionAdded != null ? sfxCorruptionAdded : ProceduralAudioGen.GenerateErrorBuzz(90f, 0.8f));
        }

        SyncDataFromUI();

        int columns = 5;

        for (int i = 0; i < columns; i++)
        {
            EjectItemIfValid(inventoryState.mainGridSlots[i]);
            EjectItemIfValid(inventoryState.mainGridSlots[i + 50]);     
        }

        for (int i = 0; i < 45; i++)
        {
            inventoryState.mainGridSlots[i] = inventoryState.mainGridSlots[i + columns];
            inventoryState.mainGridSlots[i + 50] = inventoryState.mainGridSlots[i + 50 + columns]; 
        }

        for (int i = 45; i < 50; i++)
        {
            inventoryState.mainGridSlots[i] = corruptionData;
            inventoryState.mainGridSlots[i + 50] = corruptionData;
        }

        CheckForGameOver();
        RefreshAllGrids(); 
        BroadcastHealthState(); // Update health after taking damage
        SaveWorldItems(); // Save any items that were just ejected to the floor!
    }

    public void ExecuteCleanProtocol()
    {
        // Forcefully cancel any active drag before restructuring the grid!
        if (DraggableItem.itemBeingDragged != null) DraggableItem.itemBeingDragged.AbortDrag();

        bool didCleanAnything = false;
        int columns = 5;

        for (int i = 45; i < 50; i++)
        {
            if (inventoryState.mainGridSlots[i] == corruptionData) { inventoryState.mainGridSlots[i] = null; didCleanAnything = true; }
            if (inventoryState.mainGridSlots[i + 50] == corruptionData) { inventoryState.mainGridSlots[i + 50] = null; didCleanAnything = true; }
        }

        if (didCleanAnything)
        {
            for (int i = 44; i >= 0; i--)
            {
                inventoryState.mainGridSlots[i + columns] = inventoryState.mainGridSlots[i];
                inventoryState.mainGridSlots[i + 50 + columns] = inventoryState.mainGridSlots[i + 50];
            }
            for (int i = 0; i < columns; i++)
            {
                inventoryState.mainGridSlots[i] = null;
                inventoryState.mainGridSlots[i + 50] = null;
            }
            RefreshAllGrids();
            BroadcastHealthState(); // Update health after healing/purging
        }
    }

    // Mathematically calculate health and broadcast it to the Face and EKG
    public void BroadcastHealthState()
    {
        if (inventoryState == null || corruptionData == null) return;

        int corruptionCount = 0;
        int maxSlots = 100;

        foreach (ItemData item in inventoryState.mainGridSlots)
        {
            if (item == corruptionData) corruptionCount++;
        }

        float corruptionPercentage = (float)corruptionCount / maxSlots;
        float currentHealthPercentage = Mathf.Clamp01(1f - corruptionPercentage);

        OnHealthStateChanged?.Invoke(currentHealthPercentage);

        if (UI_ParasiteOverride.Instance != null)
        {
            UI_ParasiteOverride.Instance.SetExactStacks(corruptionCount);
        }
    }

    private void EjectItemIfValid(ItemData itemData)
    {
        if (itemData != null && itemData != corruptionData)
        {
            // THE FIX: Use the specific item's worldPrefab, NOT the hardcoded Battery!
            if (itemData.worldPrefab != null && playerTransform != null)
            {
                Vector3 startPos = playerTransform.position;
                Vector3 spawnPos = GetScatterPosition(startPos);
                
                GameObject ejected = Instantiate(itemData.worldPrefab, startPos, Quaternion.identity);
                PhysicalItem pi = ejected.GetComponent<PhysicalItem>();
                if (pi == null) pi = ejected.GetComponentInChildren<PhysicalItem>();
                if (pi != null) 
                {
                    pi.itemData = itemData;
                    pi.TriggerDropAnimation(startPos, spawnPos);
                }

                if (UIPickupLog.Instance != null) UIPickupLog.Instance.AddLog(itemData.itemName ?? itemData.itemID, new Color(1f, 0.4f, 0f), "Ejected");
            }
        }
    }

    public void RefreshAllGrids()
    {
        if (!IsGridVisible())
        {
            gridRefreshPending = true;
            return;
        }

        gridRefreshPending = false;

        RefreshGrid(gridLeft, inventoryState.mainGridSlots, 5, 10, 0);
        RefreshGrid(gridRight, inventoryState.mainGridSlots, 5, 10, 50); 
        if (gridExt != null) RefreshGrid(gridExt, inventoryState.extGridSlots, 5, 5, 0);
    }

    private bool IsGridVisible()
    {
        return (gridLeft != null && gridLeft.gameObject.activeInHierarchy)
            || (gridRight != null && gridRight.gameObject.activeInHierarchy)
            || (gridExt != null && gridExt.gameObject.activeInHierarchy);
    }

    public void RefreshAllGridsIfPending()
    {
        if (gridRefreshPending) RefreshAllGrids();
    }

    void RefreshGrid(Transform gridTransform, List<ItemData> dataList, int columns, int rows, int dataOffset)
    {
        float currentCellSize = 75f;

        GridLayoutGroup layout = gridTransform.GetComponent<GridLayoutGroup>();
        if (layout != null) currentCellSize = layout.cellSize.x;
        else if (cellSizeOverride > 0f) currentCellSize = cellSizeOverride;

        bool[] spawnedMask = new bool[columns * rows];

        for (int i = 0; i < dataList.Count - dataOffset && i < columns * rows; i++)
        {
            int x = i % columns;
            int y = i / columns;

            Transform slotTransform;
            if (i < gridTransform.childCount)
            {
                slotTransform = gridTransform.GetChild(i);
                // We reuse the slot to save performance! Just clear any items inside it.
                for (int c = slotTransform.childCount - 1; c >= 0; c--) Destroy(slotTransform.GetChild(c).gameObject);
            }
            else
            {
                GameObject slotObj = Instantiate(emptySlotPrefab, gridTransform);
                slotTransform = slotObj.transform;
                
                RectTransform slotRect = slotObj.GetComponent<RectTransform>();
                if (slotRect != null) { slotRect.localScale = Vector3.one; slotRect.localRotation = Quaternion.identity; slotRect.pivot = new Vector2(0f, 1f); }
                
                InventorySlot slotLogic = slotObj.GetComponent<InventorySlot>();
                if (slotLogic != null)
                {
                    slotLogic.slotCoordinate = new Vector2Int(x, y);
                    if (gridTransform == gridLeft) slotLogic.gridRegion = InventorySlot.GridRegion.MainLeft;
                    else if (gridTransform == gridRight) slotLogic.gridRegion = InventorySlot.GridRegion.MainRight;
                    else if (gridTransform == gridExt) slotLogic.gridRegion = InventorySlot.GridRegion.External;
                }
            }

            ItemData data = dataList[i + dataOffset];

            if (data != null)
            {
                if (data != corruptionData)
                {
                    if (spawnedMask[y * columns + x]) continue;

                    bool isRotated = false;
                    ItemFootprint fp = data.GetFootprint();
                    if (fp != null)
                    {
                        if (fp.width == 1 && fp.height > 1) 
                        {
                            if (x + 1 < columns && i + dataOffset + 1 < dataList.Count && dataList[i + dataOffset + 1] == data) isRotated = true;
                        }
                        else if (fp.width > 1 && fp.height == 1)
                        {
                            if (y + 1 < rows && i + dataOffset + columns < dataList.Count && dataList[i + dataOffset + columns] == data) isRotated = true;
                        }
                    }

                    int w = fp != null ? fp.width : 1;
                    int h = fp != null ? fp.height : 1;
                    if (isRotated) { int temp = w; w = h; h = temp; }

                    for (int fy = 0; fy < h; fy++)
                    {
                        for (int fx = 0; fx < w; fx++)
                        {
                            int cy = y + fy;
                            int cx = x + fx;
                            if (cx < columns && cy < rows)
                            {
                                spawnedMask[cy * columns + cx] = true;
                            }
                        }
                    }
                }

                GameObject itemObj = Instantiate(filledItemPrefab, slotTransform);
                RectTransform itemRect = itemObj.GetComponent<RectTransform>();

                if (itemRect != null)
                {
                    itemRect.localScale = Vector3.one;
                    itemRect.localRotation = Quaternion.identity;
                }

                Canvas itemCanvas = itemObj.GetComponent<Canvas>();

                if (itemCanvas != null)
                {
                    itemCanvas.overrideSorting = true;
                    itemCanvas.sortingOrder = (gridTransform == gridExt) ? 1 : 5;
                }

                UIItem uiItem = itemObj.GetComponent<UIItem>();

                if (uiItem != null) uiItem.Initialize(data, currentCellSize);
                
                DraggableItem dragItem = itemObj.GetComponent<DraggableItem>();

                if (dragItem != null) {
                    dragItem.cellSize = currentCellSize;
                    dragItem.UpdateVisualSize();
                }
            }
        }
    }

    private bool IsInventoryFullyCorrupted()
    {
        int corruptionCount = 0;
        foreach (ItemData item in inventoryState.mainGridSlots)
            if (item == corruptionData) corruptionCount++;
        return corruptionCount >= 100;
    }

    private void CheckForGameOver()
    {
        // If it's not fully corrupted, OR we are already dying, do nothing.
        if (!IsInventoryFullyCorrupted() || isGameOverSequenceStarted) return;
        
        // Lock the sequence so it doesn't trigger again!
        isGameOverSequenceStarted = true;

        // Start the dramatic timed sequence
        StartCoroutine(GameOverSequenceRoutine());
    }

    private System.Collections.IEnumerator GameOverSequenceRoutine()
    {
        // 1. Is the player currently looking at the M.E.T. Rig?
        if (metRigManager != null && metRigManager.isRigOpen)
        {
            // Force them to stare at the fully corrupted inventory and flatline for 1.5 seconds
            yield return new WaitForSeconds(1.5f);
            
            // Forcefully close the inventory using the new method we just added!
            metRigManager.CloseRig();
        }

        // 2. Now trigger the actual Game Over animation/screen
        GameOverManager gameOver = FindAnyObjectByType<GameOverManager>();

        if (gameOver != null) 
        {
            gameOver.TriggerGameOver();
        }
        else 
        {
            Time.timeScale = 0f;
        }
    }

    private void EscalateCrushPenaltyTimer()
    {
        crushTier = Mathf.Min(crushTier + 1, 3);
        if (crushTier <= 2) crushTimer = crushDurations[crushTier - 1];
    }

    public void OnItemDroppedSignal() { onItemDropped?.Invoke(); }

    public bool CanDropToSlot(InventorySlot slot, DraggableItem draggedItem)
    {
        if (slot == null || draggedItem == null || draggedItem.itemData == null) return false;
        
        // RESTRICT PORTABLE STASHING: You can only put items INTO the external storage if you are physically at a locker!
        if (slot.gridRegion == InventorySlot.GridRegion.External && !isInteractingWithLocker)
        {
            return false; // Read-Only Mode!
        }

        ItemFootprint footprint = draggedItem.footprint;
        if (footprint == null) footprint = new ItemFootprint(1, 1);
        return IsSpaceFreeForFootprint(slot, footprint);
    }

    private bool IsSpaceFreeForFootprint(InventorySlot anchorSlot, ItemFootprint footprint)
    {
        int offsetX = -Mathf.FloorToInt(footprint.width / 2f);
        int offsetY = -Mathf.FloorToInt(footprint.height / 2f);

        int startX = anchorSlot.slotCoordinate.x + offsetX;
        int startY = anchorSlot.slotCoordinate.y + offsetY;
        
        int w = footprint != null ? footprint.width : 1;
        int h = footprint != null ? footprint.height : 1;
        
        int maxCols = 5;
        int maxRows = anchorSlot.gridRegion == InventorySlot.GridRegion.External ? 5 : 10;
        int offset = anchorSlot.gridRegion == InventorySlot.GridRegion.MainRight ? 50 : 0;
        List<ItemData> targetGrid = anchorSlot.gridRegion == InventorySlot.GridRegion.External ? inventoryState.extGridSlots : inventoryState.mainGridSlots;
        
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // THE FIX: Ignore empty footprint cells when checking for collision
                if (!footprint.GetCell(x, y)) continue;

                int cx = startX + x;
                int cy = startY + y;

                if (cx < 0 || cx >= maxCols || cy < 0 || cy >= maxRows) return false;
                if (targetGrid[offset + (cy * maxCols) + cx] != null) return false;
            }
        }
        return true;
    }

    public int CrushTier => crushTier;
    public bool HasHallucinations => GetCorruptionPercentage() >= 0.5f; // 50% = 5 Stacks
    public void AddCorruptionRow() { ResolveCorruptionTick(); }

    public void DiscardItemToWorld(GameObject itemUI)
    {
        DraggableItem dragItem = itemUI.GetComponent<DraggableItem>();
        
        if (dragItem != null && dragItem.itemData != null && dragItem.itemData.worldPrefab != null)
        {
            Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
            
            // Start the drop exactly at Kaelen's feet
            Vector3 basePos = player != null ? player.position : Vector3.zero;
            
            // Ask the scatter math to find a clean, empty patch of floor
            Vector3 spawnPos = GetScatterPosition(basePos);
            
            GameObject dropped = Instantiate(dragItem.itemData.worldPrefab, basePos, Quaternion.identity);
            PhysicalItem pi = dropped.GetComponent<PhysicalItem>();
            if (pi == null) pi = dropped.GetComponentInChildren<PhysicalItem>();
            if (pi != null) 
            {
                pi.itemData = dragItem.itemData;
                pi.TriggerDropAnimation(basePos, spawnPos);
            }

            if (UIPickupLog.Instance != null) UIPickupLog.Instance.AddLog(dragItem.itemData.itemName ?? dragItem.itemData.itemID, Color.gray, "Dropped");
        }

        Destroy(itemUI);
        SyncDataFromUI();
        SaveWorldItems(); // Save the room after dropping an item!
    }

    private Vector3 GetScatterPosition(Vector3 center)
    {
        float dropRadius = 1.2f; // How far away from Kaelen's feet it can bounce
        float itemSize = 0.3f;   // The physical space the item needs to not overlap

        // Try 15 different random spots around Kaelen's feet
        for (int i = 0; i < 15; i++)
        {
            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * dropRadius;
            Vector3 testPos = center + (Vector3)randomOffset;
            
            // Scan the area for colliders
            Collider2D[] hits = Physics2D.OverlapCircleAll(testPos, itemSize);
            bool isSpaceFree = true;

            // Check if any of the things we hit are other items
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Interactable") || hit.CompareTag("MasterKey"))
                {
                    isSpaceFree = false; // Spot taken!
                    break;
                }
                
                // Check if we hit a solid obstacle/wall (ignores triggers and Kaelen himself)
                if (!hit.isTrigger && !hit.CompareTag("Player"))
                {
                    isSpaceFree = false; // Inside a wall!
                    break;
                }
            }

            // If the coast is clear, drop it here!
            if (isSpaceFree)
            {
                return testPos;
            }
        }
        
        // Failsafe: If Kaelen drops 50 items and the floor is 100% covered, 
        // just drop it at his feet with a tiny jitter so they don't perfectly overlap.
        return center + (Vector3)(UnityEngine.Random.insideUnitCircle * 0.2f);
    }

    public Transform externalStorageGrid => gridExt;
    
    public bool TryPickupItem(ItemData itemToPickup)
    {
        if (itemToPickup == null) return false;

        ItemFootprint fp = itemToPickup.GetFootprint();
        int w = fp != null ? fp.width : 1;
        int h = fp != null ? fp.height : 1;

        // Scan ONLY the External Storage (which is a 5x5 grid, offset 0)
        if (AttemptPlacement(itemToPickup, w, h, inventoryState.extGridSlots, 5, 5, 0)) 
        {
            return true;
        }

        // If the 5x5 external tray is full, reject the pickup!
        Debug.Log("<color=red>External Tray Full! No space for " + itemToPickup.itemName + "</color>");
        return false;
    }

    // The mathematical scanner to find empty space
    private bool AttemptPlacement(ItemData item, int w, int h, List<ItemData> targetGrid, int maxCols, int maxRows, int gridOffset)
    {
        // Scan every possible starting cell in this specific grid
        for (int y = 0; y <= maxRows - h; y++)
        {
            for (int x = 0; x <= maxCols - w; x++)
            {
                bool spaceFree = true;

                // Check if the specific footprint shape is clear
                for (int cy = 0; cy < h; cy++)
                {
                    for (int cx = 0; cx < w; cx++)
                    {
                        if (item.GetFootprint() != null && !item.GetFootprint().GetCell(cx, cy)) continue;
                        int index = gridOffset + ((y + cy) * maxCols) + (x + cx);
                        if (targetGrid[index] != null)
                        {
                            spaceFree = false;
                            break;
                        }
                    }
                    if (!spaceFree) break;
                }

                // If we found a perfect fit, lock it in!
                if (spaceFree)
                {
                    for (int cy = 0; cy < h; cy++)
                    {
                        for (int cx = 0; cx < w; cx++)
                        {
                            if (item.GetFootprint() != null && !item.GetFootprint().GetCell(cx, cy)) continue;
                            int index = gridOffset + ((y + cy) * maxCols) + (x + cx);
                            targetGrid[index] = item;
                        }
                    }
                    RefreshAllGrids();
                    return true;
                }
            }
        }
        return false;
    }

    // Mathematically predicts if an item CAN be picked up without actually moving it
    public bool CanFitItemToExternalTray(ItemData itemToPickup)
    {
        if (itemToPickup == null) return false;

        ItemFootprint fp = itemToPickup.GetFootprint();
        int w = fp != null ? fp.width : 1;
        int h = fp != null ? fp.height : 1;

        return CheckPlacementSpace(itemToPickup, w, h, inventoryState.extGridSlots, 5, 5, 0);
    }

    // A non-destructive version of AttemptPlacement
    private bool CheckPlacementSpace(ItemData item, int w, int h, List<ItemData> targetGrid, int maxCols, int maxRows, int gridOffset)
    {
        for (int y = 0; y <= maxRows - h; y++)
        {
            for (int x = 0; x <= maxCols - w; x++)
            {
                bool spaceFree = true;
                for (int cy = 0; cy < h; cy++)
                {
                    for (int cx = 0; cx < w; cx++)
                    {
                        if (item.GetFootprint() != null && !item.GetFootprint().GetCell(cx, cy)) continue;
                        int index = gridOffset + ((y + cy) * maxCols) + (x + cx);
                        if (targetGrid[index] != null) { spaceFree = false; break; }
                    }
                    if (!spaceFree) break;
                }
                if (spaceFree) return true; // Found a spot!
            }
        }
        return false;
    }

    public void OpenLocker(LockerStorage locker)
    {
        SyncDataFromUI();
        
        activeLocker = locker;
        // Swap the external grid's memory to point to this specific locker
        inventoryState.extGridSlots = locker.gridSlots;
        isInteractingWithLocker = true;

        if (gridExt != null && gridExt.parent != null)
        {
            gridExt.parent.gameObject.SetActive(true);
            RefreshAllGrids();
        }
    }

    public void DisconnectFromLocker()
    {
        SyncDataFromUI();
        inventoryState.extGridSlots = inventoryState.matterBufferSlots;
        isInteractingWithLocker = false;
        
        // Save the locker's new contents to the hard drive now that we are done!
        if (activeLocker != null)
        {
            activeLocker.SaveLockerState();
            activeLocker = null;
        }
    }

    // Checks if Kaelen is currently holding any items in his digitized buffer
    public bool HasItemsInExternalStorage()
    {
        if (inventoryState == null || inventoryState.extGridSlots == null) return false;
        foreach (var item in inventoryState.extGridSlots)
        {
            if (item != null) return true;
        }
        return false;
    }

    public bool TryConsumeBatteries(int amountRequired)
    {
        int count = 0;
        foreach (var item in inventoryState.mainGridSlots) if (item != null && item.itemID == "BATT") count++;
        
        if (count >= amountRequired)
        {
            int removed = 0;
            for (int i = 0; i < inventoryState.mainGridSlots.Count; i++)
            {
                if (inventoryState.mainGridSlots[i] != null && inventoryState.mainGridSlots[i].itemID == "BATT")
                {
                    inventoryState.mainGridSlots[i] = null;
                    removed++;
                    if (removed >= amountRequired) break;
                }
            }
            RefreshAllGrids();
            return true;
        }
        return false;
    }

    public void SyncDataFromUI()
    {
        // CRITICAL FIX: If the UI is closed, do not wipe the memory! The 1D array is already safe.
        if (!IsGridVisible()) return;
        // THE FIX: If the grid graphics are hidden and out of date, do NOT scrape them!
        // This protects your true background data from being overwritten.
        if (gridRefreshPending) return;

        for (int i = 0; i < 100; i++) inventoryState.mainGridSlots[i] = null;
        if (gridExt != null) for (int i = 0; i < 25; i++) inventoryState.extGridSlots[i] = null;

        ScrapeGrid(gridLeft, inventoryState.mainGridSlots, 0, 5, 10);
        ScrapeGrid(gridRight, inventoryState.mainGridSlots, 50, 5, 10);
        if (gridExt != null) ScrapeGrid(gridExt, inventoryState.extGridSlots, 0, 5, 5);
        
        if (HotbarManager.Instance != null && inventoryState.hotbarSlots != null && inventoryState.hotbarSlots.Count >= 3)
        {
            for (int i = 0; i < 3; i++)
            {
                if (HotbarManager.Instance.quickSlots.Length > i && HotbarManager.Instance.quickSlots[i] != null && HotbarManager.Instance.quickSlots[i].containedItem != null)
                {
                    inventoryState.hotbarSlots[i] = HotbarManager.Instance.quickSlots[i].containedItem.itemData;
                }
                else
                {
                    inventoryState.hotbarSlots[i] = null;
                }
            }
        }

        // Calculate and broadcast health one final time just in case manual dragging caused corruption changes
        BroadcastHealthState();
    }

    private void ScrapeGrid(Transform gridTransform, List<ItemData> targetList, int dataOffset, int cols, int rows)
    {
        if (gridTransform == null) return;
        
        for (int i = 0; i < gridTransform.childCount; i++)
        {
            Transform slot = gridTransform.GetChild(i);
            
            // THE FIX: Loop through ALL children in the slot! 
            // This allows the Gun and the Battery to safely share an anchor slot without deleting each other.
            for (int c = 0; c < slot.childCount; c++)
            {
                Transform itemObj = slot.GetChild(c);
                DraggableItem dragItem = itemObj.GetComponent<DraggableItem>();
                UIItem uiItem = itemObj.GetComponent<UIItem>();

                if (uiItem != null && uiItem.myData != null)
                {
                    int startX = i % cols;
                    int startY = i / cols;
                    
                    int w = 1;
                    int h = 1;
                    ItemFootprint fp = null;

                    if (dragItem != null)
                    {
                        fp = dragItem.footprint;
                        w = fp != null ? fp.width : dragItem.sizeX;
                        h = fp != null ? fp.height : dragItem.sizeY;
                    }
                    else
                    {
                        fp = uiItem.myData.GetFootprint();
                        w = fp != null ? fp.width : 1;
                        h = fp != null ? fp.height : 1;
                    }
                    
                    w = Mathf.Max(1, w);
                    h = Mathf.Max(1, h);
                    
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            // Skip empty negative space so it doesn't overwrite smaller items
                            if (fp != null && !fp.GetCell(x, y)) continue;

                            int cx = startX + x;
                            int cy = startY + y;
                            if (cx < cols && cy < rows)
                            {
                                int index = dataOffset + (cy * cols) + cx;
                                targetList[index] = uiItem.myData;
                            }
                        }
                    }
                }
            }
        }
    }

    // ==========================================
    // NEW INSPECTION METHODS 
    // ==========================================

    // Call this from DraggableItem.cs or UIItem.cs when an item is clicked
    public void SetInspectionIcon(Sprite itemSprite)
    {
        if (uiInspectIcon != null && itemSprite != null)
        {
            uiInspectIcon.sprite = itemSprite;
            uiInspectIcon.color = Color.white; 
        }
    }

    // This is called automatically in Start() so it boots up blank
    public void ClearInspectionScreen()
    {
        if (uiInspectIcon != null)
        {
            uiInspectIcon.sprite = null;
            uiInspectIcon.color = new Color(0, 0, 0, 0); 
        }
    }

    // ==========================================
    // SAVE SYSTEM EXPORT & IMPORT LOGIC
    // ==========================================

    public float GetCorruptionPercentage()
    {
        int count = 0;
        foreach (ItemData item in inventoryState.mainGridSlots)
        {
            if (item == corruptionData) count++;
        }
        return Mathf.Clamp01(count / 100f);
    }

    public List<SavedGridItem> ExportInventoryForSave()
    {
        List<SavedGridItem> savedItems = new List<SavedGridItem>();
        
        // Create a mathematical mask of the entire inventory (Left, Right, and Ext)
        bool[,] memoryMask = new bool[18, 10];

        // Helper function to safely read any grid coordinate mathematically
        ItemData GetItemAtGlobal(int x, int y)
        {
            if (x < 0 || y < 0 || y >= 10 || x >= 18) return null;
            if (x < 5) return inventoryState.mainGridSlots[(y * 5) + x];
            if (x < 10) return inventoryState.mainGridSlots[50 + (y * 5) + (x - 5)];
            if (x < 15 && y < 5 && inventoryState.extGridSlots != null) return inventoryState.extGridSlots[(y * 5) + (x - 10)];
            if (x >= 15 && x < 18 && y == 0 && inventoryState.hotbarSlots != null) return inventoryState.hotbarSlots[x - 15];
            return null;
        }

        // Loop through every single possible coordinate in the 1D Arrays
        for (int gy = 0; gy < 10; gy++)
        {
            for (int gx = 0; gx < 18; gx++)
            {
                // Skip if we already mapped this coordinate as part of a larger item's footprint
                if (memoryMask[gx, gy]) continue;

                ItemData item = GetItemAtGlobal(gx, gy);
                if (item == null || item == corruptionData) continue;
                if (string.IsNullOrEmpty(item.itemID)) continue;

                // WE FOUND A NEW ITEM!
                ItemFootprint fp = item.GetFootprint();
                bool isRotated = false;

                // Intelligent Rotation Inference: Since the 1D array loses rotation, we check the neighboring cells to see which way the item is facing!
                if (fp != null)
                {
                    if (fp.width == 1 && fp.height > 1) 
                    {
                        ItemData rightItem = GetItemAtGlobal(gx + 1, gy);
                        if (rightItem == item) isRotated = true; // It's lying on its side!
                    }
                    else if (fp.width > 1 && fp.height == 1)
                    {
                        ItemData bottomItem = GetItemAtGlobal(gx, gy + 1);
                        if (bottomItem == item) isRotated = true; // It's standing up!
                    }
                }

                // Secure the item
                savedItems.Add(new SavedGridItem(item.itemID, gx, gy, isRotated));

                // Mask out the item's physical footprint so we don't accidentally save duplicates of it
                int w = fp != null ? fp.width : 1;
                int h = fp != null ? fp.height : 1;
                
                if (isRotated) { int temp = w; w = h; h = temp; } // Swap dimensions if rotated

                for (int fy = 0; fy < h; fy++)
                {
                    for (int fx = 0; fx < w; fx++)
                    {
                        if (gx + fx < 18 && gy + fy < 10) memoryMask[gx + fx, gy + fy] = true;
                    }
                }
            }
        }

        return savedItems;
    }

    public void LoadInventoryFromSave(List<SavedGridItem> savedItems, float savedCorruptionPct)
    {
        float currentCellSize = 75f;
        if (cellSizeOverride > 0f) currentCellSize = cellSizeOverride;

        // 1. FORCE BUILD PRISTINE GRIDS (Bypassing visibility constraints)
        void BuildGrid(Transform grid, int cols, int rows, InventorySlot.GridRegion region)
        {
            if (grid == null) return;
            
            // Force generate or reuse perfect empty slots
            for (int i = 0; i < cols * rows; i++)
            {
                Transform slotTransform;
                if (i < grid.childCount)
                {
                    slotTransform = grid.GetChild(i);
                    // Clear any existing items in this slot
                    for (int c = slotTransform.childCount - 1; c >= 0; c--) Destroy(slotTransform.GetChild(c).gameObject);
                }
                else
                {
                    GameObject slotObj = Instantiate(emptySlotPrefab, grid);
                    slotTransform = slotObj.transform;
                    
                    RectTransform slotRect = slotObj.GetComponent<RectTransform>();
                    if (slotRect != null) { slotRect.localScale = Vector3.one; slotRect.localRotation = Quaternion.identity; slotRect.pivot = new Vector2(0f, 1f); }
                    
                    InventorySlot slotLogic = slotObj.GetComponent<InventorySlot>();
                    if (slotLogic != null)
                    {
                        slotLogic.slotCoordinate = new Vector2Int(i % cols, i / cols);
                        slotLogic.gridRegion = region;
                    }
                }
            }
        }

        BuildGrid(gridLeft, 5, 10, InventorySlot.GridRegion.MainLeft);
        BuildGrid(gridRight, 5, 10, InventorySlot.GridRegion.MainRight);
        if (gridExt != null) BuildGrid(gridExt, 5, 5, InventorySlot.GridRegion.External);

        // 2. SPAWN SAVED ITEMS DIRECTLY INTO THEIR EXACT SLOTS
        foreach (SavedGridItem savedItem in savedItems)
        {
            ItemData foundData = itemDatabase.Find(x => x.itemID == savedItem.itemID);
            if (foundData != null)
            {
                Transform targetSlot = null;
                if (savedItem.gridPosX < 5) targetSlot = gridLeft.GetChild((savedItem.gridPosY * 5) + savedItem.gridPosX);
                else if (savedItem.gridPosX < 10) targetSlot = gridRight.GetChild((savedItem.gridPosY * 5) + (savedItem.gridPosX - 5));
                else if (gridExt != null && savedItem.gridPosX < 15) targetSlot = gridExt.GetChild((savedItem.gridPosY * 5) + (savedItem.gridPosX - 10));
                else if (savedItem.gridPosX >= 15 && savedItem.gridPosX < 18)
                {
                    int hotbarIndex = savedItem.gridPosX - 15;
                    if (HotbarManager.Instance != null && HotbarManager.Instance.quickSlots.Length > hotbarIndex)
                    {
                        targetSlot = HotbarManager.Instance.quickSlots[hotbarIndex].transform;
                    }
                    if (inventoryState.hotbarSlots != null && inventoryState.hotbarSlots.Count > hotbarIndex)
                    {
                        inventoryState.hotbarSlots[hotbarIndex] = foundData;
                    }
                    
                    if (targetSlot == null) continue;
                }

                if (targetSlot != null)
                {
                    GameObject newObj = Instantiate(filledItemPrefab, targetSlot);
                    
                    RectTransform itemRect = newObj.GetComponent<RectTransform>();
                    if (itemRect != null) { itemRect.localScale = Vector3.one; itemRect.localRotation = Quaternion.identity; }
                    
                    Canvas itemCanvas = newObj.GetComponent<Canvas>();
                    if (itemCanvas != null) { itemCanvas.overrideSorting = true; itemCanvas.sortingOrder = (targetSlot.parent == gridExt) ? 1 : 5; }

                    UIItem uiItem = newObj.GetComponent<UIItem>();
                    if (uiItem != null) 
                    {
                        foundData.isRotated = savedItem.isRotated; 
                        uiItem.Initialize(foundData, currentCellSize); 
                    }

                    DraggableItem dragItem = newObj.GetComponent<DraggableItem>();
                    if (dragItem != null) {
                        dragItem.cellSize = currentCellSize;
                        dragItem.UpdateVisualSize();
                    }

                    if (targetSlot.GetComponent<HotbarSlot>() != null && dragItem != null)
                    {
                        // Dynamically resolve the missing script connection
                        targetSlot.GetComponent<HotbarSlot>().containedItem = dragItem;
                    }
                }
            }
            else Debug.LogWarning($"<color=red>LOAD ERROR:</color> Item ID {savedItem.itemID} missing from Database!");
        }

        // 3. RESTORE CORRUPTION BLOCKS PHYSICALLY (From the Bottom-Up)
        int corruptionBlocksToSpawn = Mathf.FloorToInt(savedCorruptionPct * 100f);
        int spawned = 0;
        
        for (int row = 9; row >= 0 && spawned < corruptionBlocksToSpawn; row--)
        {
            // Fill Left Side
            for (int col = 0; col < 5 && spawned < corruptionBlocksToSpawn; col++)
            {
                Transform slot = gridLeft.GetChild((row * 5) + col);
                if (slot.childCount == 0) // Only spawn if an item isn't already here!
                {
                    GameObject crptObj = Instantiate(filledItemPrefab, slot);
                    UIItem uiItem = crptObj.GetComponent<UIItem>();
                    if (uiItem != null) uiItem.Initialize(corruptionData, currentCellSize);
                    spawned++;
                }
            }
            // Fill Right Side
            for (int col = 0; col < 5 && spawned < corruptionBlocksToSpawn; col++)
            {
                Transform slot = gridRight.GetChild((row * 5) + col);
                if (slot.childCount == 0)
                {
                    GameObject crptObj = Instantiate(filledItemPrefab, slot);
                    UIItem uiItem = crptObj.GetComponent<UIItem>();
                    if (uiItem != null) uiItem.Initialize(corruptionData, currentCellSize);
                    spawned++;
                }
            }
        }

        // 4. SYNCHRONIZE BACKEND AND LOCK UI
        gridRefreshPending = false;  // CRITICAL: Tells the system NOT to destroy our work when the Rig opens!
        SyncDataFromUI();            // Mathematically updates the 1D arrays based on the UI we just built
    }

    // ==========================================
    // WORLD ITEM SAVE SYSTEM
    // ==========================================

    public void SaveWorldItems(GameObject ignoreObj = null)
    {
        PhysicalItem[] itemsOnFloor = FindObjectsByType<PhysicalItem>(FindObjectsInactive.Exclude);
        string saveString = "";

        foreach (PhysicalItem pi in itemsOnFloor)
        {
            // Skip items that are currently being destroyed or picked up
            if (pi == null || pi.itemData == null || pi.gameObject == ignoreObj) continue;

            // Use the target position if it's currently mid-bounce, otherwise use its actual resting position
            Vector3 pos = pi.IsBouncing ? pi.TargetPosition : pi.transform.position;
            float rotZ = pi.transform.eulerAngles.z;
            saveString += $"{pi.itemData.itemID},{pos.x:F3},{pos.y:F3},{pos.z:F3},{rotZ:F3};";
        }

        string sceneName = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString("WorldItems_" + sceneName, saveString);
        PlayerPrefs.SetInt("WorldItemsSaved_" + sceneName, 1);
        PlayerPrefs.Save();
    }

    public void LoadWorldItems()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        
        // If we have never saved this room before, leave the default items alone!
        if (PlayerPrefs.GetInt("WorldItemsSaved_" + sceneName, 0) == 0) return;

        // Otherwise, clear the room's default items so we don't duplicate them
        PhysicalItem[] defaultItems = FindObjectsByType<PhysicalItem>(FindObjectsInactive.Exclude);
        foreach (PhysicalItem pi in defaultItems) Destroy(pi.gameObject);

        // And load our saved items back onto the floor
        string saveString = PlayerPrefs.GetString("WorldItems_" + sceneName, "");
        if (string.IsNullOrEmpty(saveString)) return; 

        string[] items = saveString.Split(';');
        foreach (string itemDataString in items)
        {
            if (string.IsNullOrEmpty(itemDataString)) continue;

            string[] data = itemDataString.Split(',');
            if (data.Length >= 4)
            {
                string id = data[0];
                if (float.TryParse(data[1], out float x) && float.TryParse(data[2], out float y) && float.TryParse(data[3], out float z))
                {
                    ItemData foundItem = itemDatabase.Find(i => i.itemID == id);
                    if (foundItem != null && foundItem.worldPrefab != null)
                    {
                        GameObject spawned = Instantiate(foundItem.worldPrefab, new Vector3(x, y, z), Quaternion.identity);
                        
                        // Restore the messy rotation!
                        if (data.Length >= 5 && float.TryParse(data[4], out float savedRotZ))
                            spawned.transform.rotation = Quaternion.Euler(0, 0, savedRotZ);
                            
                        PhysicalItem pi = spawned.GetComponent<PhysicalItem>();
                        if (pi == null) pi = spawned.GetComponentInChildren<PhysicalItem>();
                        if (pi != null) pi.itemData = foundItem;
                    }
                }
            }
        }
    }
}
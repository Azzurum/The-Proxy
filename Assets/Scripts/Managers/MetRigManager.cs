using UnityEngine;
using UnityEngine.UI;

public class MetRigManager : MonoBehaviour
{
    [Header("System References")]
    public GameObject terminalOverlayUI; // Drag the UI_TerminalOverlay here
    public PlayerController playerController; // Drag Player_Kaelen here
    [Header("Faraday Shielding")]
    public bool inFaradayZone = false; // Is Kaelen standing in a safe room?

    [Header("Rig State")]
    public bool isRigOpen = false;

    [Header("MOTHER Abilities")]
    public float sonarDuration = 5f;
    public float signalMaskDuration = 8f;

    private bool isSonarActive = false;
    private float sonarTimer = 0f;
    private bool isSignalMasked = false;
    private float signalMaskTimer = 0f;

    private ProxyAI proxyAI;
    private AudioSource rigAudioSource;
    private Coroutine fanNoiseRoutine;

    void Start()
    {
        // AUTO-WIRING: Find the UI and Player if they aren't assigned!
        if (terminalOverlayUI == null)
        {
            // The easiest way to find the Terminal is to look for its Animator!
            var rigAnim = FindAnyObjectByType<MetRigAnimator>(FindObjectsInactive.Include);
            if (rigAnim != null) terminalOverlayUI = rigAnim.gameObject;
            else Debug.LogWarning("MetRigManager: Could not auto-find UI_TerminalOverlay!");
        }

        if (playerController == null)
        {
            playerController = FindAnyObjectByType<PlayerController>();
        }

        // Ensure the heavy UI is hidden when the game first starts
        if (terminalOverlayUI != null)
        {
            terminalOverlayUI.SetActive(false);
        }

        proxyAI = FindAnyObjectByType<ProxyAI>();

        rigAudioSource = gameObject.AddComponent<AudioSource>();
        rigAudioSource.volume = 0.6f;
    }

    void Update()
    {
        // Listen for the Tab key to open/close the inventory
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleRig();
        }

        // Update MOTHER ability timers
        if (isSonarActive)
        {
            sonarTimer -= Time.deltaTime;
            if (sonarTimer <= 0)
            {
                isSonarActive = false;
                Debug.Log("SONAR: Deactivated - Proxy location hidden.");
            }
        }

        if (isSignalMasked)
        {
            signalMaskTimer -= Time.deltaTime;
            if (signalMaskTimer <= 0)
            {
                isSignalMasked = false;
                Debug.Log("SIGNAL MASK: Deactivated - Signal leaking again.");
                
                // Immediately alert the Proxy if the inventory is still open!
                if (isRigOpen && proxyAI != null && !inFaradayZone)
                {
                    float distance = Vector2.Distance(transform.position, proxyAI.transform.position);
                    proxyAI.OnSignalSpike(true, distance);
                }
            }
        }
    }

    public void ToggleRig()
    {
        // Fix: Disconnect from the locker BEFORE hiding the UI so the inventory can save your changes!
        InventoryManager inventoryManager = FindAnyObjectByType<InventoryManager>();
        if (isRigOpen && inventoryManager != null)
        {
            inventoryManager.DisconnectFromLocker();
        }

        isRigOpen = !isRigOpen;

        if (isRigOpen)
        {
            bool wasInactive = !terminalOverlayUI.activeSelf;
            terminalOverlayUI.SetActive(true);
            
            // If it was mid-closing animation, force it to reverse and open!
            if (!wasInactive)
            {
                MetRigAnimator animator = terminalOverlayUI.GetComponent<MetRigAnimator>();
                if (animator != null) animator.PlayOpenAnimation();
                
                // LORE UPDATE: The heavy cooling fans scream, masking ambient noise!
                if (fanNoiseRoutine != null) StopCoroutine(fanNoiseRoutine);
                fanNoiseRoutine = StartCoroutine(FanNoiseLoop());
            }
        }
        else
        {
            MetRigAnimator animator = terminalOverlayUI.GetComponent<MetRigAnimator>();
            if (animator != null) animator.CloseInventoryWithAnimation();
            else terminalOverlayUI.SetActive(false);
            
            if (fanNoiseRoutine != null) StopCoroutine(fanNoiseRoutine);
        }

        if (playerController != null)
        {
            playerController.isRooted = isRigOpen;
        }

        if (proxyAI != null)
        {
            // LORE LOGIC: The Faraday Zone actively blocks the signal!
            bool signalLeaked = isRigOpen && !inFaradayZone;
            float distance = signalLeaked ? Vector2.Distance(transform.position, proxyAI.transform.position) : -1f;
            proxyAI.OnSignalSpike(signalLeaked && !isSignalMasked, distance);
        }

        if (isRigOpen)
        {
            if (inventoryManager != null)
            {
                Canvas.ForceUpdateCanvases();
                inventoryManager.RefreshAllGrids();

                // ONLY show the external tray if we are at a locker, OR if we have items to retrieve!
                bool shouldShowExt = inventoryManager.isInteractingWithLocker || inventoryManager.HasItemsInExternalStorage();
                if (inventoryManager.gridExt != null && inventoryManager.gridExt.parent != null)
                {
                    inventoryManager.gridExt.parent.gameObject.SetActive(shouldShowExt);
                }
            }
        }

        // Console Warnings based on where you are standing
        if (isRigOpen && !inFaradayZone && !isSignalMasked)
        {
            Debug.Log("<color=red>SIGNAL SPIKE:</color> Massive electromagnetic flare emitted! The Proxy is listening...");
        }
        else if (isRigOpen && inFaradayZone)
        {
            Debug.Log("<color=cyan>FARADAY SHIELD ACTIVE:</color> M.E.T. Rig opened safely. Signal masked.");
        }
        else if (isRigOpen && isSignalMasked)
        {
            Debug.Log("<color=yellow>SIGNAL MASK ACTIVE:</color> M.E.T. Rig opened safely. Signal jammed.");
        }
    }

    private System.Collections.IEnumerator FanNoiseLoop()
    {
        // Continuously pump loud pneumatic hiss while the rig is open
        while (isRigOpen)
        {
            if (rigAudioSource != null) rigAudioSource.PlayOneShot(ProceduralAudioGen.GenerateHiss(1.5f));
            yield return new WaitForSecondsRealtime(1.0f);
        }
    }

    // MOTHER Abilities
    public void UseOverride()
    {
        InventoryManager mgr = FindAnyObjectByType<InventoryManager>();
        if (mgr != null)
        {
            mgr.AddCorruptionRow();
            Debug.Log("MOTHER: Override used - High-tier door unlocked. +1 Corruption row.");
            // TODO: Unlock door logic
        }
    }

    public void UseSonar()
    {
        InventoryManager mgr = FindAnyObjectByType<InventoryManager>();
        if (mgr != null)
        {
            mgr.AddCorruptionRow();
            isSonarActive = true;
            sonarTimer = sonarDuration;
            Debug.Log("MOTHER: Sonar activated - Proxy location revealed for 5 seconds. +1 Corruption row.");
            // TODO: Show proxy on mini-map
        }
    }

    public void UseSignalMask()
    {
        InventoryManager mgr = FindAnyObjectByType<InventoryManager>();
        if (mgr != null)
        {
            mgr.AddCorruptionRow();
            mgr.AddCorruptionRow(); // +2 rows
            isSignalMasked = true;
            signalMaskTimer = signalMaskDuration;
            Debug.Log("MOTHER: Signal Mask activated - Inventory safe for 8 seconds. +2 Corruption rows.");
        }
    }

    private void EmitSignalSpike()
    {
        Debug.Log("SIGNAL SPIKE: Massive electromagnetic flare emitted! The Proxy is listening...");
    }

    public void CloseRig()
    {
        // If the rig is currently open, toggle it closed.
        if (isRigOpen)
        {
            ToggleRig();
        }

        QuestTracker tracker = FindObjectOfType<QuestTracker>();
        if (tracker != null && tracker.GetCurrentObjective() == 4)
        {
            // Advance tracker to Phase 4: Application & Progression
            tracker.AdvanceObjective(5, "Weld the Airlock Door");
        }

    }

    public void OpenRig()
    {
        // If the rig is currently closed, toggle it open.
        if (!isRigOpen)
        {
            ToggleRig();
        }
    }
}
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

    void Start()
    {
        // Ensure the heavy UI is hidden when the game first starts
        if (terminalOverlayUI != null)
        {
            terminalOverlayUI.SetActive(false);
        }

        proxyAI = FindFirstObjectByType<ProxyAI>();
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
            }
        }
    }

    public void ToggleRig()
    {
        isRigOpen = !isRigOpen;
        terminalOverlayUI.SetActive(isRigOpen);

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
            InventoryManager inventoryManager = FindFirstObjectByType<InventoryManager>();
            if (inventoryManager != null)
            {
                Canvas.ForceUpdateCanvases();
                inventoryManager.RefreshAllGrids();
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

    // MOTHER Abilities
    public void UseOverride()
    {
        InventoryManager mgr = FindFirstObjectByType<InventoryManager>();
        if (mgr != null)
        {
            mgr.AddCorruptionRow();
            Debug.Log("MOTHER: Override used - High-tier door unlocked. +1 Corruption row.");
            // TODO: Unlock door logic
        }
    }

    public void UseSonar()
    {
        InventoryManager mgr = FindFirstObjectByType<InventoryManager>();
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
        InventoryManager mgr = FindFirstObjectByType<InventoryManager>();
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
}
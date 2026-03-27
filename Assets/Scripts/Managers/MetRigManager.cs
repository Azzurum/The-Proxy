using UnityEngine;

public class MetRigManager : MonoBehaviour
{
    [Header("System References")]
    public GameObject terminalOverlayUI; // Drag the UI_TerminalOverlay here
    public PlayerController playerController; // Drag Player_Kaelen here
    [Header("Faraday Shielding")]
    public bool inFaradayZone = false; // Is Kaelen standing in a safe room?

    [Header("Rig State")]
    public bool isRigOpen = false;

    void Start()
    {
        // Ensure the heavy UI is hidden when the game first starts
        if (terminalOverlayUI != null)
        {
            terminalOverlayUI.SetActive(false);
        }
    }

    void Update()
    {
        // Listen for the Tab key to open/close the inventory
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleRig();
        }
    }

    public void ToggleRig()
    {
        isRigOpen = !terminalOverlayUI.activeSelf;
        terminalOverlayUI.SetActive(isRigOpen);

        if (playerController != null)
        {
            playerController.isRooted = isRigOpen;
        }

        ProxyAI proxy = FindFirstObjectByType<ProxyAI>();
        if (proxy != null)
        {
            // LORE LOGIC: The Faraday Zone actively blocks the signal!
            bool signalLeaked = isRigOpen && !inFaradayZone;
            proxy.OnSignalSpike(signalLeaked);
        }

        // Console Warnings based on where you are standing
        if (isRigOpen && !inFaradayZone)
        {
            Debug.Log("<color=red>SIGNAL SPIKE:</color> Massive electromagnetic flare emitted! The Proxy is listening...");
        }
        else if (isRigOpen && inFaradayZone)
        {
            Debug.Log("<color=cyan>FARADAY SHIELD ACTIVE:</color> M.E.T. Rig opened safely. Signal masked.");
        }
    }

    private void EmitSignalSpike()
    {
        Debug.Log("SIGNAL SPIKE: Massive electromagnetic flare emitted! The Proxy is listening...");
    }
}
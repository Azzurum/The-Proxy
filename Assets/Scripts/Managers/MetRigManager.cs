using Unity.VisualScripting;
using UnityEngine;

public class MetRigManager : MonoBehaviour
{
    [Header("System References")]
    public GameObject terminalOverlayUI; // Drag the UI_TerminalOverlay here
    public PlayerController playerController; // Drag Player_Kaelen here

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

    // Look for where you are setting your UI Canvas active/inactive
    public void ToggleRig()
    {
        // 1. FIXED: We use your actual variable name (terminalOverlayUI)
        // We also removed 'bool' so it updates your public variable at the top of the script!
        isRigOpen = !terminalOverlayUI.activeSelf;
        terminalOverlayUI.SetActive(isRigOpen);

        // 2. Find the monster in the scene
        ProxyAI proxy = FindFirstObjectByType<ProxyAI>();

        // 3. Tell the monster if the UI is open (sprint) or closed (creep)
        if (proxy != null)
        {
            proxy.OnSignalSpike(isRigOpen);
        }

        if (isRigOpen)
        {
            EmitSignalSpike();
        }
    }

    private void EmitSignalSpike()
    {
        // We will connect this to the Proxy AI's detection system later
        Debug.Log("SIGNAL SPIKE: Massive electromagnetic flare emitted! The Proxy is listening...");
    }
}
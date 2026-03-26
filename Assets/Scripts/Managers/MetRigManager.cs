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

    private void ToggleRig()
    {
        isRigOpen = !isRigOpen;

        // 1. Show or hide the 10x10 Grid UI
        terminalOverlayUI.SetActive(isRigOpen);

        // 2. Magnetically clamp or unclamp the boots
        if (playerController != null)
        {
            playerController.isRooted = isRigOpen;
        }

        // 3. Trigger the Signal Spike if the rig was just opened
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
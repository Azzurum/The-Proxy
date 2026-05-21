using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the user interface for selecting a destination deck from an elevator terminal.
/// </summary>
public class DeckTerminalUI : MonoBehaviour
{
    [Header("System Links")]
    [Tooltip("The elevator interaction script that this terminal controls.")]
    public ElevatorInteraction connectedElevator;
    
    [Header("Keyhole Module")]
    [Tooltip("The UI Image that visually represents the lock status (e.g., a colored light).")]
    public Image statusLight; 
    [Tooltip("The color to display when the required Master Key is NOT present.")]
    public Color lockedColor = new Color(0.85f, 0.12f, 0.15f); 
    [Tooltip("The color to display when the required Master Key IS present.")]
    public Color unlockedColor = new Color(0.15f, 0.85f, 0.25f); 
    [Tooltip("The Item ID of the Master Key required to unlock the final deck button.")]
    public string requiredMasterKeyID = "MasterKey2";

    [Header("Deck Buttons")]
    [Tooltip("Button to travel to Deck 1.")]
    public Button buttonDeck1;
    [Tooltip("Button to travel to Deck 2.")]
    public Button buttonDeck2;
    [Tooltip("Button to travel to Deck 3.")]
    public Button buttonDeck3;
    [Tooltip("Button to close the terminal UI without traveling.")]
    public Button buttonExit;

    private void OnEnable()
    {
        // When the UI appears, ensure the cursor is unlocked and visible for interaction.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (buttonDeck1 != null) buttonDeck1.interactable = true;
        if (buttonDeck2 != null) buttonDeck2.interactable = true;
        if (buttonDeck3 != null) buttonDeck3.interactable = true;

        DisableCurrentFloorButton();
        CheckMasterKeyAccess();
    }

    /// <summary>
    /// Prevents the player from traveling to the floor they are already on.
    /// </summary>
    private void DisableCurrentFloorButton()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == "level_1")
        {
            if (buttonDeck1 != null) buttonDeck1.interactable = false;
        }
        else if (currentScene == "level_2")
        {
            if (buttonDeck2 != null) buttonDeck2.interactable = false;
        }
        else if (currentScene == "level_3")
        {
            if (buttonDeck3 != null) buttonDeck3.interactable = false;
        }
    }

    /// <summary>
    /// Checks the player's inventory for the required key and updates the UI accordingly.
    /// </summary>
    private void CheckMasterKeyAccess()
    {
        bool hasKey = InventoryManager.Instance != null && InventoryManager.Instance.HasItem(requiredMasterKeyID); 

        if (hasKey)
        {
            if (statusLight != null) statusLight.color = unlockedColor;
            
            // Keep the button interactable if the player has the key, unless they are already on that floor.
            if (buttonDeck3 != null && SceneManager.GetActiveScene().name != "level_3") 
            {
                buttonDeck3.interactable = true;
            }
        }
        else
        {
            // If the key is missing, lock the button and show the corresponding status color.
            if (statusLight != null) statusLight.color = lockedColor;
            if (buttonDeck3 != null) buttonDeck3.interactable = false; 
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelInteraction();
        }
    }

    /// <summary>
    /// Called by deck selection buttons to initiate travel to the specified scene.
    /// </summary>
    /// <param name="sceneName">The name of the destination scene to load.</param>
    public void SelectDeck(string sceneName)
    {
        LockCursorAndClose();
        if (connectedElevator != null)
        {
            connectedElevator.ConfirmDeparture(sceneName);
        }
    }

    /// <summary>
    /// Called by the 'Exit' button to close the terminal and cancel the elevator sequence.
    /// </summary>
    public void CancelInteraction()
    {
        LockCursorAndClose();
        if (connectedElevator != null)
        {
            connectedElevator.CancelDeparture();
        }
    }

    /// <summary>
    /// Restores the game's default cursor state and hides the terminal UI.
    /// </summary>
    private void LockCursorAndClose()
    {
        Cursor.lockState = CursorLockMode.None; 
        Cursor.visible = true; 
        
        gameObject.SetActive(false);
    }
}
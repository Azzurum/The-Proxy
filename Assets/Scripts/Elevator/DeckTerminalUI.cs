using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Required for checking the scene name

public class DeckTerminalUI : MonoBehaviour
{
    [Header("System Links")]
    public ElevatorInteraction connectedElevator;
    
    [Header("Keyhole Module")]
    public Image statusLight; 
    public Color lockedColor = new Color(0.85f, 0.12f, 0.15f); 
    public Color unlockedColor = new Color(0.15f, 0.85f, 0.25f); 

    [Header("Deck Buttons")]
    public Button buttonDeck1;
    public Button buttonDeck2;
    public Button buttonDeck3;
    public Button buttonExit;

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Reset all buttons to be interactable first
        if (buttonDeck1 != null) buttonDeck1.interactable = true;
        if (buttonDeck2 != null) buttonDeck2.interactable = true;
        if (buttonDeck3 != null) buttonDeck3.interactable = true;

        DisableCurrentFloorButton();
        CheckMasterKeyAccess();
    }

    private void DisableCurrentFloorButton()
    {
        // Get the name of the floor Kaelen is currently on
        string currentScene = SceneManager.GetActiveScene().name;

        // Compare the scene name to your floor names and disable the matching button
        // Make sure these strings "level_1", "level_2" match your actual scene names!
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

    private void CheckMasterKeyAccess()
    {
        // Replace this with your actual inventory check logic later
        bool hasKey = false; 

        if (hasKey)
        {
            if (statusLight != null) statusLight.color = unlockedColor;
            // Only unlock if we aren't already on Level 3
            if (buttonDeck3 != null && SceneManager.GetActiveScene().name != "level_3") 
            {
                buttonDeck3.interactable = true;
            }
        }
        else
        {
            if (statusLight != null) statusLight.color = lockedColor;
            if (buttonDeck3 != null) buttonDeck3.interactable = false; 
        }
    }

    private void Update()
    {
        // Force the mouse to stay visible while this UI is open!
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelInteraction();
        }
    }

    // Connect Deck 1, 2, and 3 buttons to this
    public void SelectDeck(string sceneName)
    {
        LockCursorAndClose();
        if (connectedElevator != null)
        {
            connectedElevator.ConfirmDeparture(sceneName);
        }
    }

    // Connect your [ X ] button to this
    public void CancelInteraction()
    {
        LockCursorAndClose();
        if (connectedElevator != null)
        {
            connectedElevator.CancelDeparture();
        }
    }

    private void LockCursorAndClose()
    {
        // Keep the mouse free and visible for your 2D gameplay!
        Cursor.lockState = CursorLockMode.None; 
        Cursor.visible = true; 
        
        gameObject.SetActive(false);
    }
}
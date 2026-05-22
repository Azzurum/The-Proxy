using UnityEngine;

/// <summary>
/// Safely hides the dialogue wrapper without stopping the typing coroutines, while opening a target panel.
/// </summary>
public class TerminalPanelToggle : MonoBehaviour
{
    [Tooltip("The CanvasGroup attached to the Dialogue_Wrapper.")]
    public CanvasGroup dialogueWrapperGroup;

    /// <summary>
    /// Hides the dialogue and opens the assigned panel.
    /// </summary>
    public void OpenPanel(GameObject targetPanel)
    {
        if (dialogueWrapperGroup != null)
        {
            dialogueWrapperGroup.alpha = 0f;
            dialogueWrapperGroup.interactable = false;
            dialogueWrapperGroup.blocksRaycasts = false;
        }
        
        if (targetPanel != null) 
        {
            // FAILSAFE: Ensure the parent container is awake so this panel can actually be seen!
            if (targetPanel.transform.parent != null && !targetPanel.transform.parent.gameObject.activeSelf)
            {
                targetPanel.transform.parent.gameObject.SetActive(true);
            }
            targetPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Closes the assigned panel and restores the dialogue.
    /// </summary>
    public void ClosePanel(GameObject targetPanel)
    {
        if (dialogueWrapperGroup != null)
        {
            dialogueWrapperGroup.alpha = 1f;
            dialogueWrapperGroup.interactable = true;
            dialogueWrapperGroup.blocksRaycasts = true;
        }
        
        if (targetPanel != null) 
        {
            targetPanel.SetActive(false);
            
            // FAILSAFE: If the parent container (like Modal_Config) is still on, it will block the screen. Turn it off!
            if (targetPanel.transform.parent != null && targetPanel.transform.parent.name.Contains("Modal"))
            {
                targetPanel.transform.parent.gameObject.SetActive(false);
            }
        }
    }
}
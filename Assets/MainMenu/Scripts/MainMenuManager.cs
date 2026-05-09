using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // Required for Coroutines

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public RectTransform panelSaveLoad;
    public GameObject darkBlocker;

    public void StartNewRun()
    {
        Debug.Log("SYSTEM BOOT: Loading First Level...");
        SceneManager.LoadScene("MainGame"); 
    }

    public void OpenLoadGame()
    {
        Debug.Log("ACCESSING MEMORY: Opening Load Menu...");
        // Turning these on automatically triggers their OnEnable() entrance animations
        panelSaveLoad.gameObject.SetActive(true);
        darkBlocker.SetActive(true);
    }

    public void CloseLoadGame()
    {
        Debug.Log("CLOSING MEMORY...");
        // Start the delayed shutdown process
        StartCoroutine(CloseMenuRoutine());
    }

    private IEnumerator CloseMenuRoutine()
    {
        // 1. Trigger the custom exit animations
        panelSaveLoad.GetComponent<UIPanelAnimator>().SlideOut();
        darkBlocker.GetComponent<UIBlockerAnimator>().FadeOut();

        // 2. Wait 0.25 seconds for the animations to finish (ignoring time scale)
        yield return new WaitForSecondsRealtime(0.25f);

        // 3. Fully disable the objects once they are off-screen/invisible
        panelSaveLoad.gameObject.SetActive(false); 
        darkBlocker.SetActive(false); 
    }

    public void OpenSettings()
    {
        Debug.Log("CALIBRATING: Opening Settings...");
    }

    public void ExitGame()
    {
        Debug.Log("SYSTEM SHUTDOWN: Quitting Game...");
        Application.Quit();
    }
}
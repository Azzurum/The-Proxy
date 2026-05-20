using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreenManager : MonoBehaviour
{
    [Header("Scene Configuration")]
    [Tooltip("The exact name of the scene to load when clicking New Game.")]
    public string newGameSceneName = "Intro_EarthOffice";

    // This method will be linked to your "New Game" button
    public void StartNewGame()
    {
        SceneManager.LoadScene(newGameSceneName);
    }
}
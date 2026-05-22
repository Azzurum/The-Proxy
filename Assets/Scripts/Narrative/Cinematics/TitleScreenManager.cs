using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Coordinates transitions from the Main Menu into the active gameplay scenes.
/// </summary>
public class TitleScreenManager : MonoBehaviour
{
    [Header("Scene Configuration")]
    [Tooltip("The exact name of the scene to load when clicking New Game.")]
    public string newGameSceneName = "Intro_EarthOffice";

    /// <summary>
    /// Triggers the transition to the specified introductory scene.
    /// </summary>
    public void StartNewGame()
    {
        SceneManager.LoadScene(newGameSceneName);
    }
}
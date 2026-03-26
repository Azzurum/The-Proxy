using UnityEngine;
using UnityEngine.SceneManagement; // Required to reload the level!

public class GameOverManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject gameOverPanel; // Drag your Panel here

    void Start()
    {
        // Ensure the Game Over screen is invisible when the game starts
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    // The Proxy will call this method when it touches Kaelen
    public void TriggerGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true); // Show the red screen
        }

        Time.timeScale = 0f; // Freeze the game world
    }

    // The Restart Button will call this method when clicked
    public void RestartGame()
    {
        // CRITICAL: You MUST unfreeze time before reloading, or the new level will be frozen too!
        Time.timeScale = 1f;

        // Reloads the exact scene you are currently playing
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
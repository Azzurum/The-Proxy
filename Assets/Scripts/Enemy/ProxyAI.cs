using UnityEngine;

public class ProxyAI : MonoBehaviour
{
    [Header("Targeting")]
    public Transform targetPlayer;

    [Header("Movement Stats")]
    public float baseSpeed = 0.5f; // Very slow creeping speed
    public float sprintSpeed = 3.5f; // Terrifying sprint speed

    private float currentSpeed;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        currentSpeed = baseSpeed;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (targetPlayer == null)
        {
            GameObject player = GameObject.Find("Player_Kaelen");
            if (player != null) targetPlayer = player.transform;
        }
    }

    void Update()
    {
        HuntPlayer();
    }

    private void HuntPlayer()
    {
        if (targetPlayer != null)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                targetPlayer.position,
                currentSpeed * Time.deltaTime
            );

            Vector2 direction = targetPlayer.position - transform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }
    }

    // The Manager will call this to wake the monster up!
    public void OnSignalSpike(bool isListening)
    {
        if (isListening)
        {
            currentSpeed = sprintSpeed;
            if (spriteRenderer != null) spriteRenderer.color = Color.red; // Turn blood red
            Debug.Log("PROXY AI: Signal acquired! Sprinting towards player!");
        }
        else
        {
            currentSpeed = baseSpeed;
            if (spriteRenderer != null) spriteRenderer.color = Color.magenta; // Return to normal color
            Debug.Log("PROXY AI: Signal lost. Returning to slow creep.");
        }
    }

    // This built-in Unity function fires the exact frame the Proxy touches another collider
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Did we hit the player?
        if (collision.CompareTag("Player"))
        {
            KillPlayer();
        }
    }

    private void KillPlayer()
    {
        Debug.LogError("CRITICAL FAILURE: Proxy has caught Kaelen! GAME OVER.");

        if (spriteRenderer != null) spriteRenderer.color = Color.black;

        // 1. Find the new Game Over Manager
        GameOverManager manager = FindFirstObjectByType<GameOverManager>();

        // 2. Tell it to trigger the screen and freeze time!
        if (manager != null)
        {
            manager.TriggerGameOver();
        }
        else
        {
            // Fallback just in case the manager is missing
            Time.timeScale = 0f;
        }
    }
}
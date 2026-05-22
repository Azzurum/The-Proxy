using UnityEngine;
using System.Collections;

/// <summary>
/// Manages the animation, collision, and effects for a single server rack in the dynamic maze.
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(AudioSource))]
public class DynamicServerRack : MonoBehaviour
{
    [Header("Animation Frames")]
    [Tooltip("The animation frames for the server, where index 0 is fully lowered and the last index is fully raised.")]
    public Sprite[] serverSprites;
    [Tooltip("The default sprite to show when the server is underground and walkable.")]
    public Sprite floorSprite;

    [Header("Cycle Settings")]
    [Tooltip("The duration in seconds for the server to complete its rise animation.")]
    public float animationDuration = 0.5f;

    [Header("Effects")]
    [Tooltip("The particle system prefab to instantiate when the server rises.")]
    public ParticleSystem sparkEffect;
    [Tooltip("The rectangular area around the server's pivot where spark effects can randomly spawn.")]
    public Vector2 sparkScatterArea = new Vector2(0.4f, 0.8f);
    [Tooltip("The probability (from 0.0 to 1.0) that sparks will be emitted when the server rises.")]
    [Range(0f, 1f)]
    public float sparkChance = 0.3f;

    [Header("Pathfinding Updates")]
    [Tooltip("The sprite frame index at which the server's collider should become solid.")]
    public int solidThresholdFrame = 6; 

    [Header("Depth Sorting")]
    [Tooltip("The name of the Sorting Layer to use for this server, which should match the player and other characters.")]
    public string sortingLayerName = "Player";
    [Tooltip("A vertical offset to adjust the server's perceived depth, allowing characters to walk behind it correctly.")]
    public float depthOffset = -1.5f;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D col;
    private AudioSource audioSource;
    private bool isSolid = false;
    private Coroutine activeRoutine;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<BoxCollider2D>();
        
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1f; // Ensure audio is 3D and positional.
        audioSource.minDistance = 2f;
        audioSource.maxDistance = 12f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;

        spriteRenderer.sprite = floorSprite;
        col.enabled = true;
        col.isTrigger = true; // Start as a trigger to allow the player to walk over it.
        isSolid = false;

        // Apply depth sorting based on Y-position to create a proper 2.5D effect.
        spriteRenderer.sortingLayerName = sortingLayerName;
        spriteRenderer.sortingOrder = Mathf.RoundToInt((transform.position.y + depthOffset) * -10f);
    }

    /// <summary>
    /// Begins the animation sequence to raise the server from the floor.
    /// </summary>
    public void Activate()
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(RiseRoutine());
    }

    /// <summary>
    /// Begins the animation sequence to lower the server back into the floor.
    /// </summary>
    public void Deactivate()
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(LowerRoutine());
    }

    /// <summary>
    /// The coroutine that handles the frame-by-frame animation and state changes for the server rising.
    /// </summary>
    private IEnumerator RiseRoutine()
    {
        if (sparkEffect != null && Random.value <= sparkChance)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-sparkScatterArea.x, sparkScatterArea.x),
                Random.Range(-sparkScatterArea.y, sparkScatterArea.y),
                0f
            );

            // PERFORMANCE NOTE: Instantiating particle effects can cause GC spikes. For a high-density maze, consider using an object pool.
            ParticleSystem sparkInstance = Instantiate(sparkEffect, transform.position + randomOffset, Quaternion.identity);
            
            if (sparkInstance.TryGetComponent<Renderer>(out var pRenderer))
            {
                // Ensure sparks render in front of the server.
                pRenderer.sortingOrder = spriteRenderer.sortingOrder + 10;
            }
            
            // PERFORMANCE WARNING: Generating procedural audio clips on the fly is extremely expensive and causes major garbage collection.
            // These should be pre-generated and cached if possible.
            audioSource.pitch = Random.Range(0.9f, 1.3f);
            audioSource.PlayOneShot(ProceduralAudioGen.GenerateSparkCrackle(0.2f), 0.3f);
            
            Destroy(sparkInstance.gameObject, 2f); // Clean up the particle effect after it has finished playing.
        }

        float timer = 0f;
        int lastFrameIndex = -1;

        while (timer < animationDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / animationDuration);
            int frameIndex = Mathf.Clamp(Mathf.FloorToInt(progress * serverSprites.Length), 0, serverSprites.Length - 1);

            // Only update the sprite if the calculated frame index has changed to avoid unnecessary rendering work.
            if (frameIndex != lastFrameIndex)
            {
                spriteRenderer.sprite = serverSprites[frameIndex];
                lastFrameIndex = frameIndex;

                if (frameIndex >= solidThresholdFrame && !isSolid)
                {
                    isSolid = true;
                    col.isTrigger = false; // The server is now a solid wall.
                    audioSource.pitch = Random.Range(0.85f, 1.15f);
                    audioSource.PlayOneShot(ProceduralAudioGen.GenerateServerRise(0.4f), 0.6f);
                    UpdatePathfinding();
                }
            }
            
            yield return null;
        }
        
        // Failsafe to guarantee the server is in its fully raised and solid state at the end of the animation.
        if (serverSprites.Length > 0) spriteRenderer.sprite = serverSprites[serverSprites.Length - 1];
        if (!isSolid)
        {
            isSolid = true;
            col.isTrigger = false;
            UpdatePathfinding();
        }
    }

    /// <summary>
    /// The coroutine for lowering the server. Currently unused as per the GDD.
    /// </summary>
    private IEnumerator LowerRoutine()
    {
        yield break;
    }

    /// <summary>
    /// A placeholder function to notify a pathfinding system (like A* Pathfinding Project) that this area's walkability has changed.
    /// </summary>
    private void UpdatePathfinding()
    {
    }
}
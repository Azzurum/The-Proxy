using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(AudioSource))]
public class DynamicServerRack : MonoBehaviour
{
    [Header("Animation Frames")]
    [Tooltip("Drag your 12 Server sprites here. 0 = Down/Hidden, 11 = Fully Up")]
    public Sprite[] serverSprites;
    [Tooltip("The default sprite to show when the server is underground and walkable.")]
    public Sprite floorSprite;

    [Header("Cycle Settings")]
    [Tooltip("How fast the server rises and lowers (in seconds).")]
    public float animationDuration = 0.5f;

    [Header("Effects")]
    [Tooltip("Assign a Particle System prefab to play when the server rises.")]
    public ParticleSystem sparkEffect;
    [Tooltip("How far from the center the sparks can randomly spawn (X width, Y height).")]
    public Vector2 sparkScatterArea = new Vector2(0.4f, 0.8f); // Defaults to a tall rectangle shape!
    [Tooltip("Chance (0 to 1) for this server to emit sparks when rising.")]
    [Range(0f, 1f)]
    public float sparkChance = 0.3f;

    [Header("Pathfinding Updates")]
    [Tooltip("At what sprite frame should it become a solid wall? (0-11)")]
    public int solidThresholdFrame = 6; 

    [Header("Depth Sorting")]
    [Tooltip("Make sure this exactly matches Kaelen's sorting layer!")]
    public string sortingLayerName = "Player";
    [Tooltip("Adjust this to move the 'depth center' lower so Kaelen can walk behind the top half.")]
    public float depthOffset = -1.5f;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D col;
    private AudioSource audioSource;
    private bool isSolid = false;
    private Coroutine activeRoutine;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<BoxCollider2D>();
        
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1f; // Make it 3D sound so it's not deafening everywhere
        audioSource.minDistance = 2f;
        audioSource.maxDistance = 12f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;

        // Start in the default "floor" state
        spriteRenderer.sprite = floorSprite;
        col.enabled = true; // Keep the collider awake in the physics engine...
        col.isTrigger = true; // ...but make it a ghost trigger so Kaelen can walk over it!
        isSolid = false;

        // Calculate depth sorting so characters can walk behind the server!
        spriteRenderer.sortingLayerName = sortingLayerName;
        spriteRenderer.sortingOrder = Mathf.RoundToInt((transform.position.y + depthOffset) * -10f);
    }

    public void Activate()
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(RiseRoutine());
    }

    public void Deactivate()
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(LowerRoutine());
    }

    private IEnumerator RiseRoutine()
    {
        // Play the spark particle effect if it's assigned, based on the random chance!
        if (sparkEffect != null && Random.value <= sparkChance)
        {
            // Calculate a random spawn position around the server's body
            Vector3 randomOffset = new Vector3(
                Random.Range(-sparkScatterArea.x, sparkScatterArea.x),
                Random.Range(-sparkScatterArea.y, sparkScatterArea.y),
                0f
            );

            ParticleSystem sparkInstance = Instantiate(sparkEffect, transform.position + randomOffset, Quaternion.identity);
            
            // Force the sparks to render in front of the 2D servers and floor
            Renderer pRenderer = sparkInstance.GetComponent<Renderer>();
            if (pRenderer != null) pRenderer.sortingOrder = spriteRenderer.sortingOrder + 10;
            
            // Play our brand new spark sound quietly!
            audioSource.pitch = Random.Range(0.9f, 1.3f);
            audioSource.PlayOneShot(ProceduralAudioGen.GenerateSparkCrackle(0.2f), 0.3f);
            
            Destroy(sparkInstance.gameObject, 2f); // Clean up the sparks after 2 seconds
        }

        float timer = 0f;
        int lastFrameIndex = -1;

        // Smooth, frame-rate independent animation loop
        while (timer < animationDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / animationDuration);
            int frameIndex = Mathf.Clamp(Mathf.FloorToInt(progress * serverSprites.Length), 0, serverSprites.Length - 1);

            if (frameIndex != lastFrameIndex)
            {
                spriteRenderer.sprite = serverSprites[frameIndex];
                lastFrameIndex = frameIndex;

                // Enable physical collision the exact moment it visually pops up
                if (frameIndex >= solidThresholdFrame && !isSolid)
                {
                    isSolid = true;
                    col.isTrigger = false; // Instantly becomes a solid brick wall!
                    audioSource.pitch = Random.Range(0.85f, 1.15f); // Randomize pitch so ripples sound organic!
                    audioSource.PlayOneShot(ProceduralAudioGen.GenerateServerRise(0.4f), 0.6f); // Smooth, heavy slide
                    UpdatePathfinding();
                }
            }
            
            yield return null; // Wait exactly 1 frame
        }
        
        // Failsafe to ensure the final frame and collision are locked in
        if (serverSprites.Length > 0) spriteRenderer.sprite = serverSprites[serverSprites.Length - 1];
        if (!isSolid)
        {
            isSolid = true;
            col.isTrigger = false;
            UpdatePathfinding();
        }
    }

    private IEnumerator LowerRoutine()
    {
        // We don't need this anymore since they stay activated permanently, 
        // but we will leave it here empty in case you ever want to reset them via script!
        yield break;
    }

    private void UpdatePathfinding()
    {
        // NOTE: If you are using the popular A* Pathfinding Project, uncomment this line (and add 'using Pathfinding;' at the top):
        // if (AstarPath.active != null) AstarPath.active.UpdateGraphs(col.bounds);
    }
}
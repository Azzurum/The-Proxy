using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PhysicalItem : MonoBehaviour
{
    [Header("Item Definition")]
    [Tooltip("Drag the matching ItemData ScriptableObject here")]
    public ItemData itemData;

    public bool IsBouncing { get; private set; } = false;
    public Vector3 TargetPosition { get; private set; }

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        // Fixes the "Cage" and "Proxy Wall" exploits! 
        // Triggers can be interacted with, but you can physically walk right through them.
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true; 

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        // Force it onto the Player sorting layer so it doesn't spawn behind the floor!
        if (spriteRenderer != null) spriteRenderer.sortingLayerName = "Player";
    }

    void Update()
    {
        // DYNAMIC DEPTH SORTING: Ensure it correctly renders in front of/behind Kaelen!
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = Mathf.RoundToInt((transform.position.y - 0.2f) * -10f);
        }
    }

    public void TriggerDropAnimation(Vector3 startPos, Vector3 targetPos)
    {
        TargetPosition = targetPos;
        StartCoroutine(DropRoutine(startPos, targetPos));
    }

    private System.Collections.IEnumerator DropRoutine(Vector3 startPos, Vector3 targetPos)
    {
        IsBouncing = true;
        float duration = 0.4f; // Time in seconds it takes to hit the floor
        float elapsed = 0f;
        
        // Give it a random rotation so it lands messily!
        Quaternion startRot = transform.rotation;
        Quaternion targetRot = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Slide linearly toward the target X/Y coordinates
            Vector2 currentPos = Vector2.Lerp(startPos, targetPos, t);
            
            // Use a Sine wave to create a perfect arc (simulating an upward jump and gravity fall)
            float bounceHeight = Mathf.Sin(t * Mathf.PI) * 1.0f; 
            transform.position = new Vector3(currentPos.x, currentPos.y + bounceHeight, transform.position.z);
            
            // Spin the item as it flies through the air
            transform.rotation = Quaternion.Lerp(startRot, targetRot, t);
            
            yield return null;
        }
        
        transform.position = targetPos;
        transform.rotation = targetRot;
        IsBouncing = false;

        AudioSource audio = GetComponent<AudioSource>();
        if (audio == null) 
        {
            audio = gameObject.AddComponent<AudioSource>();
            audio.spatialBlend = 0.6f; // Give it a bit of 3D spatial presence in the world
        }
        // Play a low, dull thud when the item hits the ground
        audio.PlayOneShot(ProceduralAudioGen.GenerateClick(150f, 0.08f));
    }
}
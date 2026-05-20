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
    private AudioSource audioSource;
    private Coroutine decoyRoutine;

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

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) 
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0.6f; // Give it a bit of 3D spatial presence in the world
        }
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

        // Play a low, dull thud when the item hits the ground
        if (audioSource != null) audioSource.PlayOneShot(ProceduralAudioGen.GenerateClick(150f, 0.08f));

        // --- DECOY LOGIC ---
        // If this item is a Decoy, start the blinking and sound effect!
        if (itemData != null && (itemData.itemID.Contains("DECOY") || itemData.itemName.ToLower().Contains("decoy")))
        {
            if (decoyRoutine != null) StopCoroutine(decoyRoutine);
            decoyRoutine = StartCoroutine(DecoyActiveRoutine());

            // Alert the Proxy to come investigate this exact spot!
            ProxyAI proxy = FindAnyObjectByType<ProxyAI>();
            if (proxy != null)
            {
                proxy.DistractToLocation(transform.position, 15f); // Distract for 15 seconds
            }
        }
    }

    private System.Collections.IEnumerator DecoyActiveRoutine()
    {
        // Generate a soft, non-annoying radar beep
        AudioClip softBeep = GenerateSoftBeep();
        
        Color baseColor = Color.white;
        Color glowColor = new Color(0f, 1f, 1f, 1f); // Cyan glow

        while (true)
        {
            // 1. Play the soft beep
            if (audioSource != null)
            {
                audioSource.pitch = 1f;
                audioSource.PlayOneShot(softBeep, 0.2f); // 20% volume so it's not annoying!
            }

            // 2. Flash the sprite cyan quickly
            if (spriteRenderer != null) spriteRenderer.color = glowColor;
            yield return new WaitForSeconds(0.1f);
            if (spriteRenderer != null) spriteRenderer.color = baseColor;
            
            // 3. Wait for the next radar ping (1.5 seconds)
            yield return new WaitForSeconds(1.4f);
        }
    }

    private AudioClip GenerateSoftBeep()
    {
        int sampleRate = 44100;
        float duration = 0.15f;
        AudioClip clip = AudioClip.Create("DecoyBeep", (int)(sampleRate * duration), 1, sampleRate, false);
        float[] samples = new float[clip.samples];
        for (int i = 0; i < samples.Length; i++)
        {
            float t = (float)i / sampleRate;
            float wave = Mathf.Sin(t * Mathf.PI * 2f * 600f); // 600Hz is a smooth, gentle tone
            float envelope = Mathf.Sin(t * Mathf.PI / duration); // Smooth fade in/out so it doesn't "click" or hurt the ears
            samples[i] = wave * envelope * 0.5f; 
        }
        clip.SetData(samples, 0);
        return clip;
    }
}
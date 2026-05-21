using UnityEngine;

/// <summary>
/// Controls the physical properties, physics animations, and special behaviors (like decoys) of items in the physical world.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PhysicalItem : MonoBehaviour
{
    [Header("Item Definition")]
    [Tooltip("The ScriptableObject defining this item's base properties and inventory data.")]
    public ItemData itemData;

    public bool IsBouncing { get; private set; } = false;
    public Vector3 TargetPosition { get; private set; }

    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private Coroutine decoyRoutine;
    private ProxyAI _cachedProxy;

    void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true; 

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        if (spriteRenderer != null) spriteRenderer.sortingLayerName = "Player";

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) 
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0.6f; 
        }
        
        _cachedProxy = FindAnyObjectByType<ProxyAI>();
    }

    void Update()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = Mathf.RoundToInt((transform.position.y - 0.2f) * -10f);
        }
    }

    /// <summary>
    /// Starts the physical drop animation, arcing the item to its target position.
    /// </summary>
    public void TriggerDropAnimation(Vector3 startPos, Vector3 targetPos)
    {
        TargetPosition = targetPos;
        StartCoroutine(DropRoutine(startPos, targetPos));
    }

    private System.Collections.IEnumerator DropRoutine(Vector3 startPos, Vector3 targetPos)
    {
        IsBouncing = true;
        float duration = 0.4f; 
        float elapsed = 0f;
        
        Quaternion startRot = transform.rotation;
        Quaternion targetRot = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            Vector2 currentPos = Vector2.Lerp(startPos, targetPos, t);
            
            float bounceHeight = Mathf.Sin(t * Mathf.PI) * 1.0f; 
            transform.position = new Vector3(currentPos.x, currentPos.y + bounceHeight, transform.position.z);
            
            transform.rotation = Quaternion.Lerp(startRot, targetRot, t);
            
            yield return null;
        }
        
        transform.position = targetPos;
        transform.rotation = targetRot;
        IsBouncing = false;

        if (audioSource != null) audioSource.PlayOneShot(ProceduralAudioGen.GenerateClick(150f, 0.08f));

        if (itemData != null && (itemData.itemID.Contains("DECOY") || itemData.itemName.ToLower().Contains("decoy")))
        {
            if (decoyRoutine != null) StopCoroutine(decoyRoutine);
            decoyRoutine = StartCoroutine(DecoyActiveRoutine());

            if (_cachedProxy != null)
            {
                _cachedProxy.DistractToLocation(transform.position, 15f);
            }
        }
    }

    private System.Collections.IEnumerator DecoyActiveRoutine()
    {
        AudioClip softBeep = GenerateSoftBeep();
        
        Color baseColor = Color.white;
        Color glowColor = new Color(0f, 1f, 1f, 1f); 

        while (true)
        {
            if (audioSource != null)
            {
                audioSource.pitch = 1f;
                audioSource.PlayOneShot(softBeep, 0.2f); 
            }

            if (spriteRenderer != null) spriteRenderer.color = glowColor;
            yield return new WaitForSeconds(0.1f);
            if (spriteRenderer != null) spriteRenderer.color = baseColor;
            
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
            float wave = Mathf.Sin(t * Mathf.PI * 2f * 600f); 
            float envelope = Mathf.Sin(t * Mathf.PI / duration); 
            samples[i] = wave * envelope * 0.5f; 
        }
        clip.SetData(samples, 0);
        return clip;
    }
}
using UnityEngine;

public class BackgroundScroller : MonoBehaviour
{
    [Header("Scroll Settings")]
    [SerializeField] private Vector2 scrollSpeed = new Vector2(-0.5f, 0f);

    private SpriteRenderer spriteRenderer;
    private Material material;
    private Vector2 currentOffset = Vector2.zero;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // This instantiates a unique runtime material instance
            material = spriteRenderer.material;
        }
    }

    void Update()
    {
        if (material != null)
        {
            // Calculate frame-rate independent texture drift
            currentOffset += scrollSpeed * Time.deltaTime;

            // This forces the offset update across all standard 2D texture maps
            material.mainTextureOffset = currentOffset;

            // Safety backup for specific URP 2D Unlit graphic cards
            if (material.HasProperty("_MainTex")) material.SetTextureOffset("_MainTex", currentOffset);
            if (material.HasProperty("_BaseMap")) material.SetTextureOffset("_BaseMap", currentOffset);
        }
    }
}
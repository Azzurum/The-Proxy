using UnityEngine;

/// <summary>
/// Scrolls the texture offset of a material continuously to create a parallax or endless moving effect.
/// </summary>
public class BackgroundScroller : MonoBehaviour
{
    [Header("Scroll Settings")]
    [Tooltip("The speed and direction applied to the material's texture offset.")]
    [SerializeField] private Vector2 scrollSpeed = new Vector2(-0.5f, 0f);

    private Material _material;
    private Vector2 _currentOffset = Vector2.zero;

    private void Start()
    {
        if (TryGetComponent<SpriteRenderer>(out var spriteRenderer))
        {
            // Instantiates a unique material clone to prevent modifying project assets.
            _material = spriteRenderer.material;
        }
    }

    private void Update()
    {
        if (_material != null)
        {
            _currentOffset += scrollSpeed * Time.deltaTime;

            _material.mainTextureOffset = _currentOffset;

            if (_material.HasProperty("_MainTex")) _material.SetTextureOffset("_MainTex", _currentOffset);
            if (_material.HasProperty("_BaseMap")) _material.SetTextureOffset("_BaseMap", _currentOffset);
        }
    }

    private void OnDestroy()
    {
        if (_material != null)
        {
            // Critical cleanup to prevent memory leaks from instantiated materials.
            Destroy(_material);
        }
    }
}
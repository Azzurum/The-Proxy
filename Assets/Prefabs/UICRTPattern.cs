using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
[ExecuteInEditMode]
public class UICRTPattern : MonoBehaviour
{
    [Header("Pure Scanline Settings")]
    [Range(0.01f, 1f)]
    [Tooltip("Darkness of the black horizontal lines.")]
    public float lineDarkness = 0.25f; 

    [Range(1, 5)]
    [Tooltip("1 = Thinnest possible lines. 2 or 3 = Thicker lines. Adjust until it looks right!")]
    public int lineThickness = 2; 

    private RawImage _rawImage;
    private Texture2D _tex;

    void Awake() { InitializeScript(); }
    void OnEnable() { InitializeScript(); }
    void OnValidate() { GenerateAndApplyPattern(); }

    void InitializeScript()
    {
        if (_rawImage == null) _rawImage = GetComponent<RawImage>();
        _rawImage.raycastTarget = false;
        _rawImage.color = Color.white; 
        GenerateAndApplyPattern();
    }

    void GenerateAndApplyPattern()
    {
        if (_rawImage == null) return;

        // A tiny 1x2 texture: Just one clear pixel and one dark pixel stacked.
        _tex = new Texture2D(1, 2, TextureFormat.RGBA32, false);
        _tex.filterMode = FilterMode.Point; // Keeps the line razor sharp
        _tex.wrapMode = TextureWrapMode.Repeat; // Tiles it infinitely

        Color dark = new Color(0, 0, 0, lineDarkness);
        Color clear = new Color(0, 0, 0, 0);

        _tex.SetPixel(0, 0, dark);  // Bottom half = The Black Line
        _tex.SetPixel(0, 1, clear); // Top half = The Empty Space

        _tex.Apply();
        _rawImage.texture = _tex;

        UpdateUVScale();
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            UpdateUVScale();
            ApplyFlickerAnimation();
        }
    }

    void UpdateUVScale()
    {
        if (_rawImage == null || _rawImage.rectTransform == null) return;
        
        // FOOLPROOF MATH: Instead of using the physical screen, we use the exact UI Rect.
        // This guarantees the lines scale perfectly with your Canvas without artifacting.
        float tilesY = _rawImage.rectTransform.rect.height / (float)lineThickness;
        
        // X tiles is 1 (it just stretches across), Y tiles is how many horizontal lines we can fit.
        _rawImage.uvRect = new Rect(0, 0, 1f, tilesY);
    }

    void ApplyFlickerAnimation()
    {
        if (_rawImage == null) return;
        // Subtle HTML Flicker Animation
        float flicker = Mathf.Lerp(0.85f, 1f, Mathf.PerlinNoise(Time.unscaledTime * 15f, 0f));
        _rawImage.color = new Color(1, 1, 1, flicker);
    }
}
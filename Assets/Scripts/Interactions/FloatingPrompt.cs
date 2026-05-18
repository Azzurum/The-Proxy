using UnityEngine;
using TMPro; // Required for TextMeshPro!

public class FloatingPrompt : MonoBehaviour
{
    [Header("Visuals")]
    public TextMeshPro textMesh;
    
    [Header("Animation Settings")]
    public float floatSpeed = 3f;     // How fast it bobs
    public float floatHeight = 0.1f;  // How high it bobs
    public float fadeSpeed = 8f;      // How fast it fades in/out

    private Vector3 _startPos;
    private float _targetAlpha = 0f;

    private Transform _dynamicTarget;
    private Vector3 _dynamicOffset;
    private bool _isDynamic = false;

    void Start()
    {
        // 1. Remember exactly where we placed it in the scene
        _startPos = transform.localPosition;

        // 2. Automatically grab the TextMeshPro component if you forgot to drag it in
        if (textMesh == null) textMesh = GetComponent<TextMeshPro>();

        // 3. Force it to be completely invisible when the game starts
        if (textMesh != null)
        {
            Color c = textMesh.color;
            c.a = 0f;
            textMesh.color = c;
        }
    }

    void Update()
    {
        // 1. The Bobbing Math 
        Vector3 bobbingOffset = new Vector3(0, Mathf.Sin(Time.time * floatSpeed) * floatHeight, 0);
        
        if (_dynamicTarget != null)
        {
            // Snap to a target (like items on the floor)
            transform.position = _dynamicTarget.position + _dynamicOffset + bobbingOffset;
        }
        else if (!_isDynamic)
        {
            // Original logic: bob relative to starting position (for Terminals)
            transform.localPosition = _startPos + bobbingOffset;
        }

        // 2. The Fading Math (Smoothly blends the alpha transparency)
        if (textMesh != null)
        {
            Color c = textMesh.color;
            c.a = Mathf.Lerp(c.a, _targetAlpha, Time.deltaTime * fadeSpeed);
            textMesh.color = c;
        }
    }

    // These two commands act as the light switches for the prompt!
    public void ShowPrompt() => _targetAlpha = 1f;
    public void HidePrompt() => _targetAlpha = 0f;

    // Allows the player script to dynamically snap this prompt to items
    public void SetDynamicTarget(Transform target, Vector3 offset)
    {
        _isDynamic = true;
        _dynamicTarget = target;
        _dynamicOffset = offset;
    }

    // Changes the color of the text while preserving its current transparency fade
    public void SetPromptColor(Color newColor)
    {
        if (textMesh != null)
        {
            newColor.a = textMesh.color.a;
            textMesh.color = newColor;
        }
    }
}
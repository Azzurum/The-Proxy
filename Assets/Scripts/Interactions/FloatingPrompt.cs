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
    private TMP_Text _textComponent;

    void Start()
    {
        // 1. Remember exactly where we placed it in the scene
        _startPos = transform.localPosition;

        // 2. Automatically grab the Text component (supports both 3D text and Canvas UI text)
        if (textMesh != null) _textComponent = textMesh;
        else _textComponent = GetComponent<TMP_Text>();

        // 3. Force it to be completely invisible when the game starts
        if (_textComponent != null)
        {
            Color c = _textComponent.color;
            c.a = 0f;
            _textComponent.color = c;

            // FIX: Force the floating text to render OVER the tilemaps!
            MeshRenderer mr = _textComponent.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                // FIX: Force it to the absolute max front so it never gets lost behind a tilemap!
                mr.sortingOrder = 32000;           
            }
            else
            {
                // If it's Canvas UI instead of 3D text, we force the Canvas to the front!
                Canvas parentCanvas = _textComponent.canvas;
                if (parentCanvas != null) { parentCanvas.overrideSorting = true; parentCanvas.sortingOrder = 50; }
            }
        }
        else
        {
            Debug.LogError($"<color=red>[ERROR]</color> FloatingPrompt on '{gameObject.name}' cannot find a TextMeshPro component!");
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
        if (_textComponent != null)
        {
            Color c = _textComponent.color;
            c.a = Mathf.Lerp(c.a, _targetAlpha, Time.deltaTime * fadeSpeed);
            _textComponent.color = c;
        }
    }

    // These two commands act as the light switches for the prompt!
    public void ShowPrompt() 
    { 
        _targetAlpha = 1f; 
        // Force it to wake up if the object was accidentally unchecked in the inspector!
        if (!gameObject.activeSelf) gameObject.SetActive(true);
    }

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
        if (_textComponent != null)
        {
            newColor.a = _textComponent.color.a;
            _textComponent.color = newColor;
        }
    }
}
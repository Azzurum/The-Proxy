using UnityEngine;
using TMPro;

/// <summary>
/// Handles the bobbing animation, fading, and dynamic positioning of contextual UI text prompts in the world.
/// </summary>
public class FloatingPrompt : MonoBehaviour
{
    [Header("Visuals")]
    [Tooltip("The TextMeshPro component used to display the prompt.")]
    public TextMeshPro textMesh;
    
    [Header("Animation Settings")]
    [Tooltip("How fast the prompt bobs up and down.")]
    public float floatSpeed = 3f;     
    [Tooltip("The vertical distance the prompt travels during its bob animation.")]
    public float floatHeight = 0.1f;  
    [Tooltip("How quickly the text fades in and out when shown or hidden.")]
    public float fadeSpeed = 8f;      

    private Vector3 _startPos;
    private float _targetAlpha = 0f;

    private Transform _dynamicTarget;
    private Vector3 _dynamicOffset;
    private bool _isDynamic = false;
    private TMP_Text _textComponent;

    void Start()
    {
        _startPos = transform.localPosition;

        if (textMesh != null) _textComponent = textMesh;
        else _textComponent = GetComponent<TMP_Text>();

        if (_textComponent != null)
        {
            Color c = _textComponent.color;
            c.a = 0f;
            _textComponent.color = c;

            MeshRenderer mr = _textComponent.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingOrder = 32000;           
            }
            else
            {
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
        Vector3 bobbingOffset = new Vector3(0, Mathf.Sin(Time.time * floatSpeed) * floatHeight, 0);
        
        if (_dynamicTarget != null)
        {
            transform.position = _dynamicTarget.position + _dynamicOffset + bobbingOffset;
        }
        else if (!_isDynamic)
        {
            transform.localPosition = _startPos + bobbingOffset;
        }

        if (_textComponent != null)
        {
            Color c = _textComponent.color;
            c.a = Mathf.Lerp(c.a, _targetAlpha, Time.deltaTime * fadeSpeed);
            _textComponent.color = c;
        }
    }

    /// <summary>
    /// Fades the prompt in and ensures its GameObject is active.
    /// </summary>
    public void ShowPrompt() 
    { 
        _targetAlpha = 1f; 
        if (!gameObject.activeSelf) gameObject.SetActive(true);
    }

    /// <summary>
    /// Smoothly fades the prompt out to full transparency.
    /// </summary>
    public void HidePrompt() => _targetAlpha = 0f;

    /// <summary>
    /// Binds the prompt to track a specific transform dynamically (e.g., following a physical item on the floor).
    /// </summary>
    public void SetDynamicTarget(Transform target, Vector3 offset)
    {
        _isDynamic = true;
        _dynamicTarget = target;
        _dynamicOffset = offset;
    }

    /// <summary>
    /// Updates the color of the text while preserving its current alpha transparency.
    /// </summary>
    public void SetPromptColor(Color newColor)
    {
        if (_textComponent != null)
        {
            newColor.a = _textComponent.color.a;
            _textComponent.color = newColor;
        }
    }
}
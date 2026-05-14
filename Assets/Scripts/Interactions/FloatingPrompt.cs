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
        // 1. The Bobbing Math (Uses Sine waves for a perfect, organic hover)
        transform.localPosition = _startPos + new Vector3(0, Mathf.Sin(Time.time * floatSpeed) * floatHeight, 0);

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
}
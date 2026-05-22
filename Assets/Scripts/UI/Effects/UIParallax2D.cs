using UnityEngine;

/// <summary>
/// Applies a subtle parallax tracking effect to UI elements based on mouse position.
/// </summary>
public class UIParallax2D : MonoBehaviour
{
    [Header("2D Parallax Settings")]
    [Tooltip("How far it moves. Use small numbers (e.g., 0.1 to 1.0) since we are in World Space.")]
    public float moveStrength = 0.5f;
    
    [Tooltip("How smooth the sliding feels.")]
    public float smoothSpeed = 5f;
    
    [Tooltip("Check this to make it move opposite to the mouse (good for foregrounds).")]
    public bool invertDirection = false;

    private Vector3 _originalLocalPos;
    private Vector2 _screenCenter;

    void OnEnable()
    {
        _originalLocalPos = transform.localPosition;
    }

    void Update()
    {
        _screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 mousePos = Input.mousePosition;

        float offsetX = (mousePos.x - _screenCenter.x) / _screenCenter.x;
        float offsetY = (mousePos.y - _screenCenter.y) / _screenCenter.y;

        offsetX = Mathf.Clamp(offsetX, -1f, 1f);
        offsetY = Mathf.Clamp(offsetY, -1f, 1f);

        float dir = invertDirection ? -1f : 1f;

        // Calculate the slide
        Vector3 targetPos = _originalLocalPos + new Vector3(
            offsetX * moveStrength * dir,
            offsetY * moveStrength * dir,
            0f
        );

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, smoothSpeed * Time.unscaledDeltaTime);
    }

    void OnDisable()
    {
        transform.localPosition = _originalLocalPos;
    }
}
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Modulates coordinate rect transformations for sliding external container trays (like buffers and lockers).
/// </summary>
public class UITrayAnimator : MonoBehaviour
{
    [Header("Core Config")]
    [Tooltip("The actual sliding UI layer subject to animation shifts.")]
    public RectTransform trayRect;
    [Tooltip("The physical push button element designated for locking overrides (e.g. Latch_ExtNode).")]
    public Button latchButton; 
    
    [Header("Animation States")]
    public Vector2 openPosition;
    public Vector2 closedPosition;
    [Tooltip("Duration required in seconds for a complete slide transition.")]
    public float animationDuration = 0.4f;
    public bool isOpen = false;

    [Header("Audio SFX")]
    public AudioSource audioSource;

    private float _animationTimer = 0f;
    private Vector2 _currentStartPos;
    private Vector2 _currentTargetPos;

    private void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Update()
    {
        if (_animationTimer > 0)
        {
            _animationTimer -= Time.deltaTime;
            
            float t = Mathf.Clamp01(1f - (_animationTimer / animationDuration));
            t = Mathf.SmoothStep(0, 1, t); 
            
            trayRect.anchoredPosition = Vector2.Lerp(_currentStartPos, _currentTargetPos, t);
        }
    }

    /// <summary>
    /// Bounces the open/close state logic and establishes new origin vectors.
    /// </summary>
    public void ToggleTray()
    {
        if (latchButton != null)
        {
            latchButton.interactable = false;
        }

        isOpen = !isOpen;
        _animationTimer = animationDuration;
        
        _currentStartPos = trayRect.anchoredPosition; 
        _currentTargetPos = isOpen ? openPosition : closedPosition;
        _currentTargetPos.y = trayRect.anchoredPosition.y;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(ProceduralAudioGen.GenerateTrayLatch(isOpen));
        }

        Invoke(nameof(EnableButton), animationDuration);
    }

    private void EnableButton()
    {
        if (latchButton != null)
        {
            latchButton.interactable = true;
        }
    }
}
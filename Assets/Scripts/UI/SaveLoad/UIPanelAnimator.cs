using UnityEngine;
using System.Collections;

public class UIPanelAnimator : MonoBehaviour
{
    private RectTransform _rectTransform;
    private Vector2 _restingPosition;
    private Coroutine _currentRoutine;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        // Save where the panel is supposed to sit on the screen
        _restingPosition = _rectTransform.anchoredPosition;
    }

    void OnEnable()
    {
        if (_currentRoutine != null) StopCoroutine(_currentRoutine);
        
        // Instantly hide off-screen to the right, then animate in
        _rectTransform.anchoredPosition = new Vector2(Screen.width, _restingPosition.y);
        _currentRoutine = StartCoroutine(SlideIn());
    }

    private IEnumerator SlideIn()
    {
        float timer = 0;
        float duration = 0.25f; 
        float startX = _rectTransform.anchoredPosition.x;
        
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; // Unscaled to ignore game pauses
            
            float t = timer / duration;
            t = 1f - Mathf.Pow(1f - t, 4f); // Ease-out: fast start, slow stop
            
            _rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(startX, _restingPosition.x, t), _restingPosition.y);
            yield return null;
        }
        
        // Snap to exact position to prevent floating-point rounding errors
        _rectTransform.anchoredPosition = _restingPosition;
    }

    public void SlideOut()
    {
        // Only trigger the exit if the panel is currently turned on
        if (gameObject.activeInHierarchy)
        {
            if (_currentRoutine != null) StopCoroutine(_currentRoutine);
            _currentRoutine = StartCoroutine(SlideOutRoutine());
        }
    }

    private IEnumerator SlideOutRoutine()
    {
        float timer = 0;
        float duration = 0.25f; 
        float startX = _rectTransform.anchoredPosition.x;
        float targetX = Screen.width; // Move back off-screen to the right

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            
            float t = timer / duration;
            t = Mathf.Pow(t, 3f); // Ease-in: slow start, whip off screen

            _rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(startX, targetX, t), _restingPosition.y);
            yield return null;
        }
    }
}
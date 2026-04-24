using UnityEngine;
using System.Collections;

public class UIPanelAnimator : MonoBehaviour
{
    private RectTransform _rectTransform;
    private Vector2 _restingPosition;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _restingPosition = _rectTransform.anchoredPosition;
    }

    void OnEnable()
    {
        // Instantly push the panel off-screen to the right the moment it turns on
        _rectTransform.anchoredPosition = new Vector2(Screen.width, _restingPosition.y);
        
        // Whip it into its resting place
        StartCoroutine(SlideIn());
    }

    private IEnumerator SlideIn()
    {
        float timer = 0;
        float duration = 0.25f; // Extremely fast entrance
        float startX = _rectTransform.anchoredPosition.x;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; // Bypasses Time.timeScale = 0
            float t = timer / duration;
            t = 1f - Mathf.Pow(1f - t, 4f); // Aggressive Cubic Ease-Out
            
            _rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(startX, _restingPosition.x, t), _restingPosition.y);
            yield return null;
        }
        _rectTransform.anchoredPosition = _restingPosition;
    }
}
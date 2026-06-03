using UnityEngine;
using System.Collections;

/// <summary>
/// Coordinates the smooth slide-in and slide-out animations for major UI panels.
/// </summary>
public class UIPanelAnimator : MonoBehaviour
{
    private RectTransform _rectTransform;
    private Vector2 _restingPosition;
    private Coroutine _currentRoutine;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _restingPosition = _rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        if (_currentRoutine != null) StopCoroutine(_currentRoutine);
        
        _rectTransform.anchoredPosition = new Vector2(Screen.width, _restingPosition.y);
        _currentRoutine = StartCoroutine(SlideIn());

        Vector3 camPos = Camera.main != null ? Camera.main.transform.position : transform.position;
        AudioSource.PlayClipAtPoint(ProceduralAudioGen.GenerateWhoosh(0.2f), camPos, ProceduralAudioGen.globalVolume * 0.5f);
    }

    private IEnumerator SlideIn()
    {
        float timer = 0;
        float duration = 0.25f; 
        float startX = _rectTransform.anchoredPosition.x;
        
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; 
            
            float t = timer / duration;
            t = 1f - Mathf.Pow(1f - t, 4f); 
            
            _rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(startX, _restingPosition.x, t), _restingPosition.y);
            yield return null;
        }
        
        _rectTransform.anchoredPosition = _restingPosition;
    }

    /// <summary>
    /// Triggers the panel to gracefully slide off-screen to the right.
    /// </summary>
    public void SlideOut()
    {
        if (gameObject.activeInHierarchy)
        {
            if (_currentRoutine != null) StopCoroutine(_currentRoutine);
            _currentRoutine = StartCoroutine(SlideOutRoutine());

            Vector3 camPos = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(ProceduralAudioGen.GenerateWhoosh(0.2f), camPos, ProceduralAudioGen.globalVolume * 0.5f);
        }
    }

    private IEnumerator SlideOutRoutine()
    {
        float timer = 0;
        float duration = 0.25f; 
        float startX = _rectTransform.anchoredPosition.x;
        float targetX = Screen.width; 

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            
            float t = timer / duration;
            t = Mathf.Pow(t, 3f); 

            _rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(startX, targetX, t), _restingPosition.y);
            yield return null;
        }
    }
}
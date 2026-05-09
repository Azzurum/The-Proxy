using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Image))]
public class UIBlockerAnimator : MonoBehaviour
{
    private Image _blockerImage;
    private Coroutine _currentRoutine;
    
    [Header("Fade Settings")]
    public float targetAlpha = 1f; 
    public float duration = 0.25f;   

    void Awake()
    {
        _blockerImage = GetComponent<Image>();
    }

    void OnEnable()
    {
        if (_currentRoutine != null) StopCoroutine(_currentRoutine);
        _currentRoutine = StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        float timer = 0f;
        Color c = Color.black; 
        c.a = 0f; 
        _blockerImage.color = c;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; 
            float t = timer / duration;
            t = 1f - Mathf.Pow(1f - t, 3f); 

            c.a = Mathf.Lerp(0f, targetAlpha, t);
            _blockerImage.color = c;
            yield return null;
        }
        c.a = targetAlpha;
        _blockerImage.color = c;
    }

    // --- NEW: THE EXIT ANIMATION ---
    public void FadeOut()
    {
        if (gameObject.activeInHierarchy)
        {
            if (_currentRoutine != null) StopCoroutine(_currentRoutine);
            _currentRoutine = StartCoroutine(FadeOutRoutine());
        }
    }

    private IEnumerator FadeOutRoutine()
    {
        float timer = 0f;
        Color c = _blockerImage.color;
        float startAlpha = c.a;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; 
            float t = timer / duration;
            t = Mathf.Pow(t, 3f); // Ease-In

            c.a = Mathf.Lerp(startAlpha, 0f, t);
            _blockerImage.color = c;
            yield return null;
        }
        c.a = 0f;
        _blockerImage.color = c;
    }
}
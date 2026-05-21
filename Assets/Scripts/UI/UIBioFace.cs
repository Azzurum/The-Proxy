using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Coordinates the Doom-style biological health face portrait and its associated damage feedback juices.
/// </summary>
public class UIBioFace : MonoBehaviour
{
    [Header("Face UI References")]
    public Image faceImage;
    private RectTransform _faceRect;
    private Vector2 _originalPosition;
    private Vector3 _originalScale;

    [Header("Kaelen's Face States")]
    public Sprite healthyFace;
    public Sprite hurtFace;
    public Sprite criticalFace;
    public Sprite deadFace;

    [Header("Juice Effects")]
    public Color damageColor = Color.red;
    private float _previousHealth = 1f;
    private bool _isCritical = false;
    private Coroutine _shakeCoroutine;

    private InventoryManager _invManager;

    private void Start()
    {
        if (faceImage != null)
        {
            _faceRect = faceImage.GetComponent<RectTransform>();
            _originalPosition = _faceRect.anchoredPosition;
            _originalScale = _faceRect.localScale;
        }

        _invManager = FindAnyObjectByType<InventoryManager>();
        if (_invManager != null)
        {
            _invManager.OnHealthStateChanged += UpdateFace;
            _invManager.BroadcastHealthState(); 
        }
    }

    private void OnDestroy()
    {
        if (_invManager != null) _invManager.OnHealthStateChanged -= UpdateFace;
    }

    private void Update()
    {
        if (_isCritical && _faceRect != null)
        {
            float pulse = 1f + (Mathf.Sin(Time.time * 5f) * 0.05f);
            _faceRect.localScale = _originalScale * pulse;
        }
        else if (_faceRect != null && _faceRect.localScale != _originalScale)
        {
            _faceRect.localScale = _originalScale;
        }
    }

    /// <summary>
    /// Mutates the sprite and triggers screen shake parameters if the parsed health has degenerated.
    /// </summary>
    public void UpdateFace(float healthPercentage)
    {
        if (faceImage == null) return;

        if (healthPercentage < _previousHealth)
        {
            if (gameObject.activeInHierarchy) 
            {
                if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
                _shakeCoroutine = StartCoroutine(DamageJuiceRoutine());
            }
        }
        _previousHealth = healthPercentage;

        if (healthPercentage <= 0f)
        {
            faceImage.sprite = deadFace;
            _isCritical = false; 
        }
        else if (healthPercentage <= 0.4f) 
        {
            faceImage.sprite = criticalFace;
            _isCritical = true; 
        }
        else if (healthPercentage <= 0.85f) 
        {
            faceImage.sprite = hurtFace;
            _isCritical = false;
        }
        else
        {
            faceImage.sprite = healthyFace; 
            _isCritical = false;
        }
    }

    private IEnumerator DamageJuiceRoutine()
    {
        float elapsed = 0f;
        float duration = 0.2f; 
        float magnitude = 10f; 

        faceImage.color = damageColor; 

        while (elapsed < duration)
        {
            if (_faceRect == null || faceImage == null) yield break;

            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude;
            
            _faceRect.anchoredPosition = _originalPosition + new Vector2(offsetX, offsetY);

            elapsed += Time.deltaTime;
            
            faceImage.color = Color.Lerp(damageColor, Color.white, elapsed / duration);
            
            yield return null; 
        }

        _faceRect.anchoredPosition = _originalPosition;
        faceImage.color = Color.white;
    }
}
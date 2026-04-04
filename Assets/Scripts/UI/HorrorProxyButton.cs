using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

// Added IPointerClickHandler to detect and play the click sound
public class HorrorProxyButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Text Components")]
    public TMP_Text mainText;
    public TMP_Text ghostCyan;
    public TMP_Text ghostRed;
    public TMP_Text errorCodeFlash;

    [Header("Audio SFX")]
    [Tooltip("The AudioSource on this button or a central one")]
    public AudioSource audioSource;
    public AudioClip SND_UI_Button_Hover;
    public AudioClip SND_UI_Button_Click;

    [Header("The EKG Flatline")]
    public RectTransform ekgContainer;
    public Color normalLineColor = new Color(0, 1, 1, 1);
    public Color spikeLineColor = new Color(1, 0, 0, 1);
    public int segmentResolution = 40;
    public float lineWidth = 2f;
    private Image[] _lineSegments;

    [Header("White Border Glitch")]
    [Tooltip("Drag the MAIN BUTTON'S Image component here")]
    public Image borderImage; 
    public float maxBorderAlpha = 1.0f;
    public float idleBorderAlpha = 0.2f;
    public float borderBlinkSpeed = 8f; 

    [Header("Subtle Disquiet (Jitter/Chromatic)")]
    public float vibrationSpeed = 0.05f;
    public float vibrationIntensity = 1.0f;
    public float chromaticOffset = 2.0f;

    [Header("Error Code Flashing")]
    public float minErrorInterval = 0.5f;
    public float maxErrorInterval = 2.0f;
    public float errorFlashDuration = 0.1f;
    public Vector2 flashPositionRange = new Vector2(100f, 50f);
    public Vector2 flashScaleRange = new Vector2(0.4f, 0.7f);

    [TextArea(2, 5)]
    public string[] errorCodesPool = { 
        "ERR: ACCESS_DENIED", "SYS: UNSTABLE", "PROXY_OVERRIDE", 
        "0x00F77_CRITICAL", "TERMINATION_PENDING", "NULL_REF"
    };

    private Coroutine _glitchRoutine;
    private Coroutine _errorRoutine;
    private Vector3 _originalMainPos;
    private Vector3 _originalCyanPos;
    private Vector3 _originalRedPos;
    private Vector3 _originalFlashPos;
    private bool _isHovering = false;

    private void Start()
    {
        // Auto-assign AudioSource if not set
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource != null) audioSource.playOnAwake = false;

        if (mainText) _originalMainPos = mainText.transform.localPosition;
        if (ghostCyan) _originalCyanPos = ghostCyan.transform.localPosition;
        if (ghostRed) _originalRedPos = ghostRed.transform.localPosition;
        if (errorCodeFlash) _originalFlashPos = errorCodeFlash.transform.localPosition;

        InitializeDynamicLine();
        ResetGlitchState();
    }

    private void InitializeDynamicLine()
    {
        if (ekgContainer == null) return;
        _lineSegments = new Image[segmentResolution];
        for (int i = 0; i < segmentResolution; i++)
        {
            GameObject segObj = new GameObject("Segment_" + i);
            segObj.transform.SetParent(ekgContainer, false);
            Image img = segObj.AddComponent<Image>();
            img.color = normalLineColor;
            img.raycastTarget = false;
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            _lineSegments[i] = img;
        }
    }

    private void Update()
    {
        DrawFlowingLine();
    }

    private void DrawFlowingLine()
    {
        if (ekgContainer == null || _lineSegments == null) return;
        float width = ekgContainer.rect.width;
        float startX = -width / 2f;
        Vector2[] points = new Vector2[segmentResolution + 1];

        for (int i = 0; i <= segmentResolution; i++)
        {
            float t = (float)i / segmentResolution;
            float edgeTaper = Mathf.Sin(t * Mathf.PI); 
            float currentY = 0f;

            if (!_isHovering) currentY = Mathf.Sin((t * 12f) - (Time.unscaledTime * 4f)) * 6f * edgeTaper;
            else currentY = (Mathf.PerlinNoise(t * 25f, Time.unscaledTime * 30f) - 0.5f) * 60f * edgeTaper;

            points[i] = new Vector2(startX + (t * width), currentY);
        }

        for (int i = 0; i < segmentResolution; i++)
        {
            Vector2 p1 = points[i];
            Vector2 p2 = points[i + 1];
            Vector2 dir = p2 - p1;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float distance = dir.magnitude;

            RectTransform rt = _lineSegments[i].rectTransform;
            rt.anchoredPosition = p1 + dir / 2f;
            rt.sizeDelta = new Vector2(distance, lineWidth);
            rt.localRotation = Quaternion.Euler(0, 0, angle);
            _lineSegments[i].color = _isHovering ? spikeLineColor : normalLineColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData) 
    {
        if (_isHovering) return;
        _isHovering = true;

        // Play Hover SFX
        if (audioSource && SND_UI_Button_Hover) audioSource.PlayOneShot(SND_UI_Button_Hover);

        _glitchRoutine = StartCoroutine(GlitchLoopRealtime());
        _errorRoutine = StartCoroutine(ErrorFlashLoopRealtime());
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Play Click SFX
        if (audioSource && SND_UI_Button_Click) audioSource.PlayOneShot(SND_UI_Button_Click);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_isHovering) return;
        _isHovering = false;
        if (_glitchRoutine != null) StopCoroutine(_glitchRoutine);
        if (_errorRoutine != null) StopCoroutine(_errorRoutine);
        ResetGlitchState();
    }

    private void ResetGlitchState()
    {
        _isHovering = false;
        
        if (mainText) mainText.transform.localPosition = _originalMainPos;
        if (ghostCyan) { ghostCyan.transform.localPosition = _originalCyanPos; ghostCyan.gameObject.SetActive(false); }
        if (ghostRed) { ghostRed.transform.localPosition = _originalRedPos; ghostRed.gameObject.SetActive(false); }
        if (errorCodeFlash) errorCodeFlash.gameObject.SetActive(false);
        
        if (borderImage) borderImage.color = new Color(1f, 1f, 1f, idleBorderAlpha);
    }

    private IEnumerator GlitchLoopRealtime()
    {
        if (ghostCyan) ghostCyan.gameObject.SetActive(true);
        if (ghostRed) ghostRed.gameObject.SetActive(true);

        while (_isHovering)
        {
            Vector3 randomVibrate = new Vector3(
                Random.Range(-vibrationIntensity, vibrationIntensity),
                Random.Range(-vibrationIntensity, vibrationIntensity),
                0f
            );
            mainText.transform.localPosition = _originalMainPos + randomVibrate;

            if (ghostCyan) ghostCyan.transform.localPosition = _originalCyanPos + randomVibrate + (Vector3.left * chromaticOffset);
            if (ghostRed) ghostRed.transform.localPosition = _originalRedPos + randomVibrate + (Vector3.right * chromaticOffset);

            if (Random.value < 0.1f)
            {
                mainText.gameObject.SetActive(!mainText.gameObject.activeSelf);
                if (ghostCyan) ghostCyan.gameObject.SetActive(!ghostCyan.gameObject.activeSelf);
            }
            else
            {
                mainText.gameObject.SetActive(true);
                if (ghostCyan) ghostCyan.gameObject.SetActive(true);
            }

            // --- BORDER BLINK LOGIC ---
            if (borderImage)
            {
                float pulse = Mathf.Sin(Time.unscaledTime * borderBlinkSpeed);
                float currentAlpha = pulse > 0 ? maxBorderAlpha : idleBorderAlpha;
                borderImage.color = new Color(1f, 1f, 1f, currentAlpha);
            }

            yield return new WaitForSecondsRealtime(vibrationSpeed);
        }
    }

    private IEnumerator ErrorFlashLoopRealtime()
    {
        if (errorCodeFlash == null || errorCodesPool.Length == 0) yield break;
        while (_isHovering)
        {
            yield return new WaitForSecondsRealtime(Random.Range(minErrorInterval, maxErrorInterval));
            if (_isHovering)
            {
                errorCodeFlash.text = errorCodesPool[Random.Range(0, errorCodesPool.Length)];
                errorCodeFlash.transform.localPosition = _originalFlashPos + new Vector3(Random.Range(-flashPositionRange.x, flashPositionRange.x), Random.Range(-flashPositionRange.y, flashPositionRange.y), 0);
                errorCodeFlash.transform.localScale = Vector3.one * Random.Range(flashScaleRange.x, flashScaleRange.y);
                errorCodeFlash.gameObject.SetActive(true);
                yield return new WaitForSecondsRealtime(errorFlashDuration);
                errorCodeFlash.gameObject.SetActive(false);
            }
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

// Added ISelectHandler and IDeselectHandler for Gamepad/Keyboard support
public class HorrorProxyButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, ISelectHandler, IDeselectHandler
{
    [Header("Text Components")]
    public TMP_Text mainText;
    public TMP_Text ghostCyan;
    public TMP_Text ghostRed;
    public TMP_Text errorCodeFlash;

    [Header("Audio SFX")]
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

    [Header("Phase 4: Biometric Link")]
    [Range(0, 1)] public float corruptionPercent = 0f; // 0 = Clean, 1 = Full 10x10 Corruption

    private Coroutine _glitchRoutine;
    private Coroutine _errorRoutine;
    private Vector3 _originalMainPos;
    private Vector3 _originalCyanPos;
    private Vector3 _originalRedPos;
    private Vector3 _originalFlashPos;
    private bool _isHovering = false;

    private void Start()
    {
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

            if (!_isHovering) 
            {
                // --- BIOMETRIC UPDATE: Idle line gets "heart palpitations" (Noise) as corruption grows ---
                float idleSine = Mathf.Sin((t * 12f) - (Time.unscaledTime * 4f)) * 6f;
                float corruptionNoise = (Mathf.PerlinNoise(t * 10f, Time.unscaledTime) - 0.5f) * (40f * corruptionPercent);
                currentY = (idleSine + corruptionNoise) * edgeTaper;
            }
            else 
            {
                // Hover spike gets more violent with corruption
                float hoverIntensity = 60f + (40f * corruptionPercent);
                currentY = (Mathf.PerlinNoise(t * 25f, Time.unscaledTime * 30f) - 0.5f) * hoverIntensity * edgeTaper;
            }

            points[i] = new Vector2(startX + (t * width), currentY);
        }

        for (int i = 0; i < segmentResolution; i++)
        {
            // ... (keep the drawing math the same as Phase 3) ...
            Vector2 p1 = points[i];
            Vector2 p2 = points[i + 1];
            Vector2 dir = p2 - p1;
            RectTransform rt = _lineSegments[i].rectTransform;
            rt.anchoredPosition = p1 + dir / 2f;
            rt.sizeDelta = new Vector2(dir.magnitude, lineWidth);
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            
            // --- BIOMETRIC UPDATE: Line color bleeds to red even when idle if corruption is high ---
            _lineSegments[i].color = Color.Lerp(normalLineColor, spikeLineColor, _isHovering ? 1.0f : corruptionPercent);
        }
    }

    // --- INTERACTION HANDLERS ---

    public void OnPointerEnter(PointerEventData eventData) 
    {
        if (_isHovering) return;
        _isHovering = true;
        if (audioSource && SND_UI_Button_Hover) audioSource.PlayOneShot(SND_UI_Button_Hover);
        _glitchRoutine = StartCoroutine(GlitchLoopRealtime());
        _errorRoutine = StartCoroutine(ErrorFlashLoopRealtime());
    }

    public void OnPointerExit(PointerEventData eventData) => ResetGlitchState();
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (audioSource && SND_UI_Button_Click) audioSource.PlayOneShot(SND_UI_Button_Click);
    }

    // Gamepad/Keyboard Support
    public void OnSelect(BaseEventData eventData) => OnPointerEnter(null);
    public void OnDeselect(BaseEventData eventData) => ResetGlitchState();

    private void ResetGlitchState()
    {
        _isHovering = false;
        if (_glitchRoutine != null) StopCoroutine(_glitchRoutine);
        if (_errorRoutine != null) StopCoroutine(_errorRoutine);

        // BUG FIX: Always ensure text is visible when resetting
        if (mainText) 
        {
            mainText.gameObject.SetActive(true);
            mainText.transform.localPosition = _originalMainPos;
        }

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
            // --- BIOMETRIC UPDATE: Vibration and Chromatic Offset increase with corruption ---
            float currentVibration = vibrationIntensity + (vibrationIntensity * corruptionPercent * 2f);
            float currentChromatic = chromaticOffset + (chromaticOffset * corruptionPercent * 3f);

            Vector3 randomVibrate = new Vector3(Random.Range(-currentVibration, currentVibration), Random.Range(-currentVibration, currentVibration), 0f);
            mainText.transform.localPosition = _originalMainPos + randomVibrate;

            if (ghostCyan) ghostCyan.transform.localPosition = _originalCyanPos + randomVibrate + (Vector3.left * currentChromatic);
            if (ghostRed) ghostRed.transform.localPosition = _originalRedPos + randomVibrate + (Vector3.right * currentChromatic);

            // Flicker more often if corrupted
            if (Random.value < (0.1f + (corruptionPercent * 0.2f))) mainText.gameObject.SetActive(!mainText.gameObject.activeSelf);
            else mainText.gameObject.SetActive(true);

            // Speed up the pulse based on corruption
            if (borderImage)
            {
                float pulse = Mathf.Sin(Time.unscaledTime * (borderBlinkSpeed + (corruptionPercent * 10f)));
                borderImage.color = new Color(1f, 1f, 1f, pulse > 0 ? maxBorderAlpha : idleBorderAlpha);
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
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class HorrorProxyButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Text Components")]
    public TMP_Text mainText;
    public TMP_Text ghostCyan;
    public TMP_Text ghostRed;
    public TMP_Text errorCodeFlash;

    [Header("Subtle Disquiet (Jitter/Chromatic)")]
    public float vibrationSpeed = 0.05f;
    public float vibrationIntensity = 1.0f;
    public float chromaticOffset = 2.0f;

    [Header("Error Code Flashing")]
    public float minErrorInterval = 0.5f;
    public float maxErrorInterval = 2.0f;
    public float errorFlashDuration = 0.1f;
    
    [Tooltip("How far from the center the error text can spawn")]
    public Vector2 flashPositionRange = new Vector2(100f, 50f);
    
    [Tooltip("Random scale range for the error text (e.g. 0.5 to 0.8)")]
    public Vector2 flashScaleRange = new Vector2(0.4f, 0.7f);

    [TextArea(2, 5)]
    public string[] errorCodesPool = { 
        "ERR: ACCESS_DENIED", 
        "SYS: UNSTABLE", 
        "PROXY_OVERRIDE", 
        "0x00F77_CRITICAL", 
        "TERMINATION_PENDING",
        "NULL_REF",
        "DATA_LEAK"
    };

    private Coroutine _glitchRoutine;
    private Coroutine _errorRoutine;
    private Vector3 _originalMainPos;
    private Vector3 _originalCyanPos;
    private Vector3 _originalRedPos;
    private Vector3 _originalFlashPos; // Added to track flash center
    private bool _isHovering = false;

    private void Start()
    {
        if (mainText) _originalMainPos = mainText.transform.localPosition;
        if (ghostCyan) _originalCyanPos = ghostCyan.transform.localPosition;
        if (ghostRed) _originalRedPos = ghostRed.transform.localPosition;
        if (errorCodeFlash) _originalFlashPos = errorCodeFlash.transform.localPosition;

        ResetGlitchState();
    }

    public void OnPointerEnter(PointerEventData eventData) 
    {
        if (_isHovering) return;
        _isHovering = true;

        _glitchRoutine = StartCoroutine(GlitchLoopRealtime());
        _errorRoutine = StartCoroutine(ErrorFlashLoopRealtime());
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
        if (ghostCyan)
        {
            ghostCyan.transform.localPosition = _originalCyanPos;
            ghostCyan.gameObject.SetActive(false);
        }
        if (ghostRed)
        {
            ghostRed.transform.localPosition = _originalRedPos;
            ghostRed.gameObject.SetActive(false);
        }
        
        if (errorCodeFlash) errorCodeFlash.gameObject.SetActive(false);
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

            yield return new WaitForSecondsRealtime(vibrationSpeed);
        }
    }

    private IEnumerator ErrorFlashLoopRealtime()
    {
        if (errorCodeFlash == null || errorCodesPool.Length == 0) yield break;

        while (_isHovering)
        {
            float waitTime = Random.Range(minErrorInterval, maxErrorInterval);
            yield return new WaitForSecondsRealtime(waitTime);

            if (_isHovering)
            {
                // 1. Pick Random Text
                string chosenCode = errorCodesPool[Random.Range(0, errorCodesPool.Length)];
                errorCodeFlash.text = chosenCode;

                // 2. Set Random Small Position (Relative to button center)
                float randX = Random.Range(-flashPositionRange.x, flashPositionRange.x);
                float randY = Random.Range(-flashPositionRange.y, flashPositionRange.y);
                errorCodeFlash.transform.localPosition = _originalFlashPos + new Vector3(randX, randY, 0);

                // 3. Set Random Small Scale
                float randScale = Random.Range(flashScaleRange.x, flashScaleRange.y);
                errorCodeFlash.transform.localScale = new Vector3(randScale, randScale, 1f);

                // 4. Flash and Hide
                errorCodeFlash.gameObject.SetActive(true);
                yield return new WaitForSecondsRealtime(errorFlashDuration);
                errorCodeFlash.gameObject.SetActive(false);
            }
        }
    }
}
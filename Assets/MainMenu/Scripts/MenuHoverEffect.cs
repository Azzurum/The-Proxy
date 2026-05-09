using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class MenuHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Text Objects")]
    public TextMeshProUGUI cyanText;
    public TextMeshProUGUI redText;

    [Header("Text Content")]
    public string normalRightText = "NEW RUN";
    public string glitchRightText = "NEW MEAT";

    [Header("Animation Settings")]
    public float hoverNudge = 30f; 
    public float lerpSpeed = 15f;  
    public float buttonStressLevel = 0.1f;

    [Header("Colors")]
    public Color calmCyan = new Color(0f, 0.94f, 1f, 1f); // #00f0ff
    public Color calmRed = new Color(1f, 0f, 0.23f, 1f);  // #ff003c
    public Color hoverColor = Color.white;

    private Vector2 cyanStartPos;
    private Vector2 redStartPos;
    private bool isHovering = false;
    private Coroutine scrambleCoroutine;

    // The characters used in the glitch effect
    private readonly string glitchChars = "01#%&<>-_\\/[]{}—=+*^?█";

    void Start()
    {
        if (cyanText != null) cyanStartPos = cyanText.rectTransform.anchoredPosition;
        if (redText != null) redStartPos = redText.rectTransform.anchoredPosition;

        if (cyanText != null) cyanText.text = normalRightText; // Changed to use your variable
        if (redText != null) redText.text = normalRightText;
    }

    void Update()
    {
        if (cyanText == null || redText == null) return;

        // Determine targets based on hover state
        Vector2 cyanTargetPos = isHovering ? cyanStartPos + new Vector2(-hoverNudge, 0) : cyanStartPos;
        Vector2 redTargetPos = isHovering ? redStartPos + new Vector2(hoverNudge, 0) : redStartPos;
        
        Color cyanTargetColor = isHovering ? hoverColor : calmCyan;
        Color redTargetColor = isHovering ? hoverColor : calmRed;

        // Smoothly animate using unscaled time (works even if game pauses)
        cyanText.rectTransform.anchoredPosition = Vector2.Lerp(cyanText.rectTransform.anchoredPosition, cyanTargetPos, Time.unscaledDeltaTime * lerpSpeed);
        redText.rectTransform.anchoredPosition = Vector2.Lerp(redText.rectTransform.anchoredPosition, redTargetPos, Time.unscaledDeltaTime * lerpSpeed);

        cyanText.color = Color.Lerp(cyanText.color, cyanTargetColor, Time.unscaledDeltaTime * lerpSpeed);
        redText.color = Color.Lerp(redText.color, redTargetColor, Time.unscaledDeltaTime * lerpSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Prevent hovering if the Execution sequence has started!
        if (ExecutionSequence.Instance != null && ExecutionSequence.Instance.menuMatrixGroup.alpha < 1f) return;

        isHovering = true;

        if (StressSystem.Instance != null) StressSystem.Instance.SetTargetStress(buttonStressLevel);

        // Start the matrix text scramble
        if (scrambleCoroutine != null) StopCoroutine(scrambleCoroutine);
        scrambleCoroutine = StartCoroutine(ScrambleText(glitchRightText));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;

        if (StressSystem.Instance != null) StressSystem.Instance.SetTargetStress(0.1f);

        // Stop scrambling and immediately reset the text
        if (scrambleCoroutine != null) StopCoroutine(scrambleCoroutine);
        redText.text = normalRightText;
    }

    private IEnumerator ScrambleText(string targetWord)
    {
        int length = targetWord.Length;
        float iter = 0;

        while (iter < length)
        {
            string currentResult = "";
            for (int i = 0; i < length; i++)
            {
                if (i < Mathf.FloorToInt(iter))
                {
                    // Reveal the correct character
                    currentResult += targetWord[i];
                }
                else
                {
                    // Insert a random glitch character
                    currentResult += glitchChars[Random.Range(0, glitchChars.Length)];
                }
            }

            redText.text = currentResult;
            iter += 0.5f; // Controls how fast it reveals the real word
            yield return new WaitForSecondsRealtime(0.02f);
        }

        // Ensure the final word locks in perfectly
        redText.text = targetWord;
    }
}
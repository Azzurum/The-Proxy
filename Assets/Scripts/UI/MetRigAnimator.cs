using UnityEngine;
using System.Collections;

public class MetRigAnimator : MonoBehaviour
{
    [Header("Main UI Panels")]
    public RectTransform visorLeft;
    public RectTransform visorRight;
    public RectTransform bottomGroup;

    [Header("Left Extensions")]
    public RectTransform latchExtNode;
    public RectTransform extNodeTray;

    [Header("Right Extensions")]
    public RectTransform latchMap;
    public RectTransform mapTray;

    [Header("Animation Settings")]
    public float slideDuration = 0.25f;
    public float slideDistanceX = 800f;
    public float slideDistanceY = 500f;
    
    [Header("Audio")]
    public AudioSource audioSource;

    private Vector2 leftVisiblePos;
    private Vector2 rightVisiblePos;
    private Vector2 bottomVisiblePos;

    private Vector2 latchExtVisiblePos;
    private Vector2 extTrayVisiblePos;
    private Vector2 latchMapVisiblePos;
    private Vector2 mapTrayVisiblePos;

    private Coroutine animCoroutine;

    void Awake()
    {
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // Store their exact layout positions as the "Open" state
        if (visorLeft != null) leftVisiblePos = visorLeft.anchoredPosition;
        if (visorRight != null) rightVisiblePos = visorRight.anchoredPosition;
        if (bottomGroup != null) bottomVisiblePos = bottomGroup.anchoredPosition;

        if (latchExtNode != null) latchExtVisiblePos = latchExtNode.anchoredPosition;
        if (extNodeTray != null) extTrayVisiblePos = extNodeTray.anchoredPosition;
        if (latchMap != null) latchMapVisiblePos = latchMap.anchoredPosition;
        if (mapTray != null) mapTrayVisiblePos = mapTray.anchoredPosition;
    }

    void OnEnable()
    {
        // Prevent the animation and audio from playing when the game first boots up!
        if (Time.timeSinceLevelLoad > 0.5f)
        {
            PlayOpenAnimation();
        }
        else
        {
            // Instantly snap to the open positions so it's ready, without playing sound
            if (visorLeft != null) visorLeft.anchoredPosition = leftVisiblePos;
            if (visorRight != null) visorRight.anchoredPosition = rightVisiblePos;
            if (bottomGroup != null) bottomGroup.anchoredPosition = bottomVisiblePos;
            if (latchExtNode != null) latchExtNode.anchoredPosition = latchExtVisiblePos;
            if (extNodeTray != null) extNodeTray.anchoredPosition = extTrayVisiblePos;
            if (latchMap != null) latchMap.anchoredPosition = latchMapVisiblePos;
            if (mapTray != null) mapTray.anchoredPosition = mapTrayVisiblePos;
        }
    }

    public void PlayOpenAnimation()
    {
        if (audioSource != null)
        {
            // Procedural Audio: Heavy pneumatic hiss + Boot-up system chime
            audioSource.PlayOneShot(ProceduralAudioGen.GeneratePneumaticBlast(0.4f));
            audioSource.PlayOneShot(ProceduralAudioGen.GenerateAscendingChime(0.25f));
        }

        // Instantly snap to hidden positions so it doesn't flicker before sliding
        if (visorLeft != null) visorLeft.anchoredPosition = leftVisiblePos + new Vector2(-slideDistanceX, 0);
        if (visorRight != null) visorRight.anchoredPosition = rightVisiblePos + new Vector2(slideDistanceX, 0);
        if (bottomGroup != null) bottomGroup.anchoredPosition = bottomVisiblePos + new Vector2(0, -slideDistanceY);

        if (latchExtNode != null) latchExtNode.anchoredPosition = latchExtVisiblePos + new Vector2(-slideDistanceX, 0);
        if (extNodeTray != null) extNodeTray.anchoredPosition = extTrayVisiblePos + new Vector2(-slideDistanceX, 0);
        if (latchMap != null) latchMap.anchoredPosition = latchMapVisiblePos + new Vector2(slideDistanceX, 0);
        if (mapTray != null) mapTray.anchoredPosition = mapTrayVisiblePos + new Vector2(slideDistanceX, 0);

        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(SlidePanels(true));
    }

    public void PlayCloseAnimation()
    {
        // Force any open trays to close so they don't break sync when re-opened!
        UITrayAnimator[] trays = GetComponentsInChildren<UITrayAnimator>(true);
        foreach (var tray in trays)
        {
            if (tray.isOpen)
            {
                tray.isOpen = false;
                if (tray.trayRect != null) tray.trayRect.anchoredPosition = tray.closedPosition;
            }
        }

        // Procedural Audio: Fast shut-down whoosh + heavy UI click
        // PLAYED IN WORLD SPACE: This guarantees the sound plays even if the Tab key instantly turns off the Canvas!
        Vector3 camPos = Camera.main != null ? Camera.main.transform.position : transform.position;
        AudioSource.PlayClipAtPoint(ProceduralAudioGen.GenerateWhoosh(0.2f), camPos, ProceduralAudioGen.globalVolume);
        AudioSource.PlayClipAtPoint(ProceduralAudioGen.GenerateClick(300f, 0.1f), camPos, ProceduralAudioGen.globalVolume);

        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(SlidePanels(false));
    }

    // Helper method to let the animation finish BEFORE turning off the UI
    public void CloseInventoryWithAnimation()
    {
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(CloseRoutine());
        }
    }

    private IEnumerator CloseRoutine()
    {
        PlayCloseAnimation();
        // Wait for the slide to finish (ignoring Time.timeScale in case the game is paused)
        yield return new WaitForSecondsRealtime(slideDuration);

        gameObject.SetActive(false); // Turn off the Canvas!
    }

    private IEnumerator SlidePanels(bool isOpening)
    {
        float elapsed = 0f;

        // Define the hidden "off-screen" positions
        Vector2 leftHidden = leftVisiblePos + new Vector2(-slideDistanceX, 0);
        Vector2 rightHidden = rightVisiblePos + new Vector2(slideDistanceX, 0);
        Vector2 bottomHidden = bottomVisiblePos + new Vector2(0, -slideDistanceY);

        Vector2 latchExtHidden = latchExtVisiblePos + new Vector2(-slideDistanceX, 0);
        Vector2 extTrayHidden = extTrayVisiblePos + new Vector2(-slideDistanceX, 0);
        Vector2 latchMapHidden = latchMapVisiblePos + new Vector2(slideDistanceX, 0);
        Vector2 mapTrayHidden = mapTrayVisiblePos + new Vector2(slideDistanceX, 0);

        // Determine start and end points
        Vector2 leftStart = isOpening ? leftHidden : leftVisiblePos;
        Vector2 rightStart = isOpening ? rightHidden : rightVisiblePos;
        Vector2 bottomStart = isOpening ? bottomHidden : bottomVisiblePos;

        Vector2 latchExtStart = isOpening ? latchExtHidden : latchExtVisiblePos;
        Vector2 extTrayStart = isOpening ? extTrayHidden : extTrayVisiblePos;
        Vector2 latchMapStart = isOpening ? latchMapHidden : latchMapVisiblePos;
        Vector2 mapTrayStart = isOpening ? mapTrayHidden : mapTrayVisiblePos;

        Vector2 leftEnd = isOpening ? leftVisiblePos : leftHidden;
        Vector2 rightEnd = isOpening ? rightVisiblePos : rightHidden;
        Vector2 bottomEnd = isOpening ? bottomVisiblePos : bottomHidden;

        Vector2 latchExtEnd = isOpening ? latchExtVisiblePos : latchExtHidden;
        Vector2 extTrayEnd = isOpening ? extTrayVisiblePos : extTrayHidden;
        Vector2 latchMapEnd = isOpening ? latchMapVisiblePos : latchMapHidden;
        Vector2 mapTrayEnd = isOpening ? mapTrayVisiblePos : mapTrayHidden;

        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime; 
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration); // Smooth ease in/out

            if (visorLeft != null) visorLeft.anchoredPosition = Vector2.LerpUnclamped(leftStart, leftEnd, t);
            if (visorRight != null) visorRight.anchoredPosition = Vector2.LerpUnclamped(rightStart, rightEnd, t);
            if (bottomGroup != null) bottomGroup.anchoredPosition = Vector2.LerpUnclamped(bottomStart, bottomEnd, t);

            if (latchExtNode != null) latchExtNode.anchoredPosition = Vector2.LerpUnclamped(latchExtStart, latchExtEnd, t);
            if (extNodeTray != null) extNodeTray.anchoredPosition = Vector2.LerpUnclamped(extTrayStart, extTrayEnd, t);
            if (latchMap != null) latchMap.anchoredPosition = Vector2.LerpUnclamped(latchMapStart, latchMapEnd, t);
            if (mapTray != null) mapTray.anchoredPosition = Vector2.LerpUnclamped(mapTrayStart, mapTrayEnd, t);

            yield return null;
        }

        // Snap perfectly to the target at the end
        if (visorLeft != null) visorLeft.anchoredPosition = leftEnd;
        if (visorRight != null) visorRight.anchoredPosition = rightEnd;
        if (bottomGroup != null) bottomGroup.anchoredPosition = bottomEnd;

        if (latchExtNode != null) latchExtNode.anchoredPosition = latchExtEnd;
        if (extNodeTray != null) extNodeTray.anchoredPosition = extTrayEnd;
        if (latchMap != null) latchMap.anchoredPosition = latchMapEnd;
        if (mapTray != null) mapTray.anchoredPosition = mapTrayEnd;
    }
}
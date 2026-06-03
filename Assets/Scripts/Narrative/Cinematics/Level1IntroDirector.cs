using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Orchestrates the opening cinematic of Level 1: fading from space into the hangar, and triggering the first dialogue.
/// </summary>
public class Level1IntroDirector : MonoBehaviour
{
    [Header("Cinematic Elements")]
    public CanvasGroup screenFader;
    public CinematicTypewriter typewriter;
    public GameObject spaceCinematicGroup;
    [Tooltip("The exact object the camera should look at during the space scene.")]
    public Transform spaceCameraAnchor;
    [Tooltip("The spaceship object inside the hangar that the camera should follow while it lands.")]
    public Transform landingShipAnchor;
    [Tooltip("How long the landing animation takes before Kaelen steps out.")]
    public float landingAnimationDuration = 4.0f;
    [Tooltip("How long the text stays on screen after the ship lands before it fades out.")]
    public float textHoldDuration = 4.0f; 
    
    [Header("Skip Settings")]
    [Tooltip("Canvas group containing the skip prompt (e.g. 'Hold SPACE to Skip'). Optional.")]
    public CanvasGroup skipPromptGroup;
    [Tooltip("UI Image that fills up as the player holds Space. Optional.")]
    public Image skipFillRing;
    [Tooltip("How long Space must be held to skip the cinematic.")]
    public float requiredSkipTime = 1.5f;
    private float _skipHoldTimer = 0f;
    private bool _isSkipped = false;

    [Header("Next Sequence")]
    public TutorialDialogueTrigger tutorialDialogue;
    public PlayerController playerController;

    private bool _bypassCinematic = false;

    void Awake()
    {
        // --- AGGRESSIVE SCENE-PROOFING & AUTO-WIRING ---
        // This director is ONLY meant to run in level_1. If it's in any other scene, it must be neutralized.
        if (SceneManager.GetActiveScene().name != "level_1")
        {
            // Before destroying, attempt to fix the camera which this script's presence might have broken.
            CameraFollow cam = FindAnyObjectByType<CameraFollow>();
            PlayerController pc = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (cam != null && pc != null)
            {
                cam.enabled = true;
                cam.target = pc.transform;
            }
            // Destroy this component so its Start() and Update() methods never run and cause problems.
            Destroy(this);
            return;
        }

        // Auto-wire critical components in case prefab links were broken during scene setup.
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (tutorialDialogue == null) tutorialDialogue = FindAnyObjectByType<TutorialDialogueTrigger>(FindObjectsInactive.Include);

        if (screenFader != null)
        {
            // Hide the old Unity Editor fader in case it was left in World Space,
            // but DO NOT destroy it in case your Text objects are safely nested inside it!
            screenFader.alpha = 0f; 
            screenFader.blocksRaycasts = false;
        }

        // If the player arrived via an elevator or is loading a saved game, bypass the cinematic!
        if (!string.IsNullOrEmpty(ElevatorManager.LastUsedElevatorID) || SaveLoadManager.pendingLoadSlot != -1)
        {
            _bypassCinematic = true;
        }

        // Dynamically generate a foolproof, mathematically perfect Screen Fader via code!
        // This guarantees it is ScreenSpaceOverlay, covers the whole monitor, and ignores the Camera.
        GameObject faderObj = new GameObject("Dynamic_Screen_Fader");
        Canvas faderCanvas = faderObj.AddComponent<Canvas>();
        faderCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        faderCanvas.sortingOrder = 32767;

        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(faderObj.transform, false);
        Image img = imageObj.AddComponent<Image>();
        img.color = Color.black;

        RectTransform rect = img.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        screenFader = faderObj.AddComponent<CanvasGroup>();
        
        if (_bypassCinematic)
        {
            screenFader.alpha = 0f;
            screenFader.blocksRaycasts = false;
        }
        else
        {
            screenFader.alpha = 1f;
            screenFader.blocksRaycasts = true;
        }
    }

    void Start()
    {
        if (_bypassCinematic)
        {
            SilentBypassCinematic();
        }
        else
        {
            StartCoroutine(IntroSequence());
        }
    }

    void Update()
    {
        if (_isSkipped) return;

        // Hide the skip prompt and disable skipping if the visual novel dialogue has already started
        if (DialogueEngine.isDialogueActive) 
        {
            if (skipPromptGroup != null) skipPromptGroup.alpha = 0f;
            return;
        }

        // Detect if the player is holding Space, Escape, or Enter
        if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Escape) || Input.GetKey(KeyCode.Return))
        {
            if (skipPromptGroup != null) skipPromptGroup.alpha = 1f;

            _skipHoldTimer += Time.deltaTime;
            if (skipFillRing != null) skipFillRing.fillAmount = _skipHoldTimer / requiredSkipTime;

            if (_skipHoldTimer >= requiredSkipTime)
            {
                SkipCinematic();
            }
        }
        else
        {
            _skipHoldTimer = 0f;
            if (skipFillRing != null) skipFillRing.fillAmount = 0f;
            
            // Make the prompt blink/pulse smoothly every 2 seconds so the player knows they can skip
            if (skipPromptGroup != null) skipPromptGroup.alpha = Mathf.Lerp(0.1f, 0.8f, Mathf.PingPong(Time.time, 1f));
        }
    }

    private void SkipCinematic()
    {
        _isSkipped = true;
        StopAllCoroutines(); // Instantly kill the cinematic sequence and all fading timers

        if (skipPromptGroup != null) skipPromptGroup.alpha = 0f;

        // Force Screen Fader entirely off
        if (screenFader != null)
        {
            screenFader.alpha = 0f;
            screenFader.blocksRaycasts = false;
        }

        // Clean up the Space Scene visuals
        if (spaceCinematicGroup != null) spaceCinematicGroup.SetActive(false);
        GameObject spaceBg = GameObject.Find("space_background");
        if (spaceBg != null) spaceBg.SetActive(false);

        // Hide the Typewriter Texts
        if (typewriter != null)
        {
            if (typewriter.spaceText != null) typewriter.spaceText.gameObject.SetActive(false);
            if (typewriter.hangarText != null) typewriter.hangarText.gameObject.SetActive(false);
        }

        // Fast-forward Landing Ship animation to the end so it safely sits docked!
        if (landingShipAnchor != null)
        {
            Animator shipAnim = landingShipAnchor.GetComponent<Animator>();
            if (shipAnim == null) shipAnim = landingShipAnchor.GetComponentInChildren<Animator>();
            if (shipAnim != null)
            {
                shipAnim.Update(10f); // Magically fast-forwards the animation by 10 seconds instantly
                shipAnim.enabled = false;
            }
            foreach (var ps in landingShipAnchor.GetComponentsInChildren<ParticleSystem>()) ps.Stop();
        }

        // Snap Kaelen into the game
        if (playerController != null)
        {
            playerController.gameObject.SetActive(true);
            SpriteRenderer[] playerSprites = playerController.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in playerSprites) sr.enabled = true;
        }

        // Snap the Camera directly to Kaelen
        Camera mainCam = Camera.main;
        if (mainCam == null) mainCam = FindAnyObjectByType<Camera>();
        if (mainCam != null)
        {
            CameraFollow cam = mainCam.GetComponent<CameraFollow>();
            if (cam != null && playerController != null)
            {
                cam.target = playerController.transform;
                cam.enabled = true;
                mainCam.transform.position = playerController.transform.position + cam.offset;
            }
        }

        // Immediately trigger Kaelen's opening dialogue!
        if (tutorialDialogue != null) tutorialDialogue.TriggerKaelenDialogue();
        
        // Wait for dialogue to close to give controls back
        StartCoroutine(WaitForDialogueToFinish());
    }

    private void SilentBypassCinematic()
    {
        _isSkipped = true;

        // Clean up the Space Scene visuals immediately
        if (spaceCinematicGroup != null) spaceCinematicGroup.SetActive(false);
        GameObject spaceBg = GameObject.Find("space_background");
        if (spaceBg != null) spaceBg.SetActive(false);

        // Fast-forward Landing Ship animation to the end so it safely sits docked
        if (landingShipAnchor != null)
        {
            Animator shipAnim = landingShipAnchor.GetComponent<Animator>();
            if (shipAnim == null) shipAnim = landingShipAnchor.GetComponentInChildren<Animator>();
            if (shipAnim != null) shipAnim.enabled = false;
            foreach (var ps in landingShipAnchor.GetComponentsInChildren<ParticleSystem>()) ps.Stop();
        }

        // Hide the Typewriter Texts
        if (typewriter != null)
        {
            if (typewriter.spaceText != null) typewriter.spaceText.gameObject.SetActive(false);
            if (typewriter.hangarText != null) typewriter.hangarText.gameObject.SetActive(false);
        }

        if (skipPromptGroup != null) skipPromptGroup.alpha = 0f;
        
        // Snap the Camera directly to Kaelen so it doesn't get stranded looking at the empty hangar!
        Camera mainCam = Camera.main;
        if (mainCam == null) mainCam = FindAnyObjectByType<Camera>();
        if (mainCam != null)
        {
            CameraFollow cam = mainCam.GetComponent<CameraFollow>();
            if (cam != null && playerController != null)
            {
                cam.target = playerController.transform;
                cam.enabled = true;
                mainCam.transform.position = playerController.transform.position + cam.offset;
            }
        }

        // Note: We do NOT root the player here. 
        // The ElevatorArrival.cs script will take over and perfectly animate Kaelen walking out of the elevator!
    }

    private IEnumerator IntroSequence()
    {
        // 1. Lock the player but keep the GameObject active!
        // Since you fixed the Grid Z-position, we can safely just hide the sprites instead of deactivating the whole object.
        SpriteRenderer[] playerSprites = null;
        if (playerController != null) 
        {
            playerController.gameObject.SetActive(true); // Failsafe to guarantee Kaelen's object is on
            playerController.isRooted = true;
            playerSprites = playerController.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in playerSprites) sr.enabled = false;
        }

        // 2. Lock the camera to the Space scene and disable smoothing to prevent crazy teleport panning
        Camera mainCam = Camera.main;
        if (mainCam == null) mainCam = FindAnyObjectByType<Camera>();
        
        CameraFollow cam = mainCam != null ? mainCam.GetComponent<CameraFollow>() : null;
        if (cam != null) cam.enabled = false; 

        Transform targetAnchor = spaceCameraAnchor != null ? spaceCameraAnchor : (spaceCinematicGroup != null ? spaceCinematicGroup.transform : null);
        if (targetAnchor != null && mainCam != null)
        {
            mainCam.transform.position = targetAnchor.position + (cam != null ? cam.offset : new Vector3(0,0,-10f));
            if (cam != null)
            {
                cam.target = targetAnchor;
                cam.enabled = true; // Let it smoothly track the space ship if it moves
            }
        }

        // 3. Start with a pitch-black screen and the space visuals turned ON
        if (screenFader != null) screenFader.alpha = 1f;
        if (spaceCinematicGroup != null) spaceCinematicGroup.SetActive(true);

        yield return new WaitForSeconds(1f);

        // 4. Fade in to reveal the purple ship flying in space
        yield return StartCoroutine(Fade(1f, 0f, 2f));

        // 5. Type the "Space Text" (e.g., "AETHER-CORE TRANSIT...")
        if (typewriter != null) typewriter.StartSpaceTyping();
        
        yield return new WaitForSeconds(4f); // Wait for the player to read

        // 6. Fade back to black
        yield return StartCoroutine(Fade(0f, 1f, 1.5f));

        // Hold the black screen for a moment to create a clean, professional scene cut
        yield return new WaitForSeconds(1.0f);

        // 7. Snap the camera directly to the LANDING SHIP while the screen is pitch black
        if (landingShipAnchor != null && mainCam != null)
        {
            if (cam != null) cam.enabled = false; // Prevent wild panning during the fade
            mainCam.transform.position = landingShipAnchor.position + (cam != null ? cam.offset : new Vector3(0,0,-10f));
            if (cam != null)
            {
                cam.target = landingShipAnchor;
                cam.enabled = true; // Re-enable so it smoothly tracks the landing ship!
            }
        }
        else if (playerController != null) // Fallback just in case
        {
            if (cam != null) cam.enabled = false;
            if (cam != null) cam.target = playerController.transform;
            if (mainCam != null) mainCam.transform.position = playerController.transform.position + (cam != null ? cam.offset : new Vector3(0, 0, -10f));
            if (cam != null) cam.enabled = true; // Re-enable camera smoothing now that it's snapped!
        }

        // 8. Turn off the space visuals and the space text
        if (spaceCinematicGroup != null) spaceCinematicGroup.SetActive(false);
        if (typewriter != null && typewriter.spaceText != null) typewriter.spaceText.gameObject.SetActive(false);
        
        // Explicitly disable the space backgrounds in case they are outside the group
        GameObject spaceBg = GameObject.Find("space_background");
        if (spaceBg != null) spaceBg.SetActive(false);

        yield return new WaitForSeconds(0.5f);

        // 9. Setup and Type the "Hangar Text" so it starts EXACTLY as the landing scene begins
        if (typewriter != null && typewriter.hangarText != null) 
        {
            Canvas hangarCanvas = typewriter.hangarText.GetComponentInParent<Canvas>();
            if (hangarCanvas != null) 
            { 
                hangarCanvas.renderMode = RenderMode.ScreenSpaceOverlay; 
                hangarCanvas.sortingOrder = 100; 
                Image bg = hangarCanvas.GetComponent<Image>();
                if (bg != null) bg.enabled = false;
            }

            RectTransform txtRect = typewriter.hangarText.GetComponent<RectTransform>();
            if (txtRect != null)
            {
                txtRect.anchorMin = new Vector2(0f, 0f);
                txtRect.anchorMax = new Vector2(0f, 0f);
                txtRect.pivot = new Vector2(0f, 0f);
                txtRect.anchoredPosition = new Vector2(60f, 60f); // Nice padding from the corner
                txtRect.sizeDelta = new Vector2(2000f, 150f); // Expand the box so it has plenty of room
            }
            
            typewriter.hangarText.textWrappingMode = TMPro.TextWrappingModes.NoWrap; // Force it to type out perfectly horizontal
            typewriter.hangarText.fontSize = 54;
            typewriter.hangarText.alignment = TMPro.TextAlignmentOptions.BottomLeft;
            
            typewriter.StartHangarTyping();
        }

        // 10. Fade into the hangar WHILE the text is typing (Do not yield, let it run in the background!)
        StartCoroutine(Fade(1f, 0f, 2f));

        // 11. Wait for the landing animation to completely finish
        yield return new WaitForSeconds(landingAnimationDuration);

        // 12. STOP the landing ship's animator so it doesn't loop back to the sky, but KEEP visuals ON
        if (landingShipAnchor != null)
        {
            Animator shipAnim = landingShipAnchor.GetComponent<Animator>();
            if (shipAnim == null) shipAnim = landingShipAnchor.GetComponentInChildren<Animator>();
            if (shipAnim != null) shipAnim.enabled = false;
            
            // Stop engine thrust particles since the ship has successfully landed
            foreach (var ps in landingShipAnchor.GetComponentsInChildren<ParticleSystem>()) ps.Stop();
        }

        // 13. Kaelen appears IMMEDIATELY when the ship finishes landing!
        if (playerController != null)
        {
            playerController.gameObject.SetActive(true);
        }
        if (playerSprites != null)
        {
            foreach (var sr in playerSprites) sr.enabled = true;
        }

        // The main cinematic is over! Disable the skip logic and hide the prompt permanently.
        _isSkipped = true;
        if (skipPromptGroup != null) skipPromptGroup.alpha = 0f;

        // 14. Camera smoothly PANS to Kaelen and locks onto him
        if (cam != null && playerController != null)
        {
            cam.target = playerController.transform;
        }

        // 15. Handle the text holding and fading concurrently so it doesn't block the dialogue!
        if (typewriter != null && typewriter.hangarText != null)
        {
            StartCoroutine(HoldAndFadeTextRoutine(typewriter.hangarText, textHoldDuration, 2.5f));
        }

        // 16. Wait just half a second for the camera to start panning
        yield return new WaitForSeconds(0.5f);

        // 17. Trigger Kaelen's opening monologue instantly!
        if (tutorialDialogue != null) tutorialDialogue.TriggerKaelenDialogue();

        // Give the dialogue engine a split-second to actually start before we check if it's active!
        yield return new WaitForSeconds(0.5f);

        yield return new WaitUntil(() => !DialogueEngine.isDialogueActive);

        // 19. Aggressively give the player control
        if (playerController != null) 
        {
            playerController.enabled = true;
            playerController.isRooted = false;
        }
    }

    private IEnumerator WaitForDialogueToFinish()
    {
        yield return new WaitForSeconds(0.5f);
        yield return new WaitUntil(() => !DialogueEngine.isDialogueActive);

        if (playerController != null) 
        {
            playerController.enabled = true;
            playerController.isRooted = false;
        }
    }

    private IEnumerator Fade(float start, float end, float duration)
    {
        if (screenFader == null) yield break;
        screenFader.blocksRaycasts = true; // Block clicks during fade
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            screenFader.alpha = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }
        screenFader.alpha = end;
        
        if (end <= 0.01f) screenFader.blocksRaycasts = false; // Allow clicks when clear
    }

    private IEnumerator HoldAndFadeTextRoutine(TMPro.TMP_Text text, float holdTime, float fadeTime)
    {
        yield return new WaitForSeconds(holdTime);
        yield return StartCoroutine(FadeTextAlpha(text, fadeTime));
    }

    private IEnumerator FadeTextAlpha(TMPro.TMP_Text text, float duration)
    {
        if (text == null) yield break;
        
        // Using a CanvasGroup guarantees shadows, outlines, and underlays fade cleanly without "poofing"!
        CanvasGroup cg = text.gameObject.GetComponent<CanvasGroup>();
        if (cg == null) cg = text.gameObject.AddComponent<CanvasGroup>();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }
        cg.alpha = 0f;

        // Safely turn off the text once the fade is fully complete
        text.gameObject.SetActive(false);
        cg.alpha = 1f; // Reset for future uses
    }
}
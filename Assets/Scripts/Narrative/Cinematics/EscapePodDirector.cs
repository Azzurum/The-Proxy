using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Coordinates the final escape pod sequence, transitioning from interior cinematic to the external space explosion.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class EscapePodDirector : MonoBehaviour
{
    [Header("Interaction Setup")]
    [Tooltip("Reference to the floating interaction prompt UI.")]
    public FloatingPrompt interactionPrompt;
    private bool isPlayerInRange = false;
    private bool isTriggered = false;

    [Header("Interior Cinematic Connections")]
    [Tooltip("The player's Transform, required for forced movement/teleportation.")]
    public Transform playerTransform;
    [Tooltip("The designated position inside the escape pod where the player will be teleported.")]
    public Transform podInteriorTarget; 
    
    [Header("Exterior Cinematic Connections")]
    [Tooltip("An invisible transform outside the ship where the camera will snap to view the exterior.")]
    public Transform externalCameraAnchor; 
    [Tooltip("The external sprite representing the escaping pod.")]
    public GameObject escapePodExteriorVisual; 
    [Tooltip("The external sprite representing the doomed Wayfarer ship.")]
    public GameObject wayfarerShipVisual; 
    [Tooltip("A full-screen UI Image used for the final explosion flash.")]
    public Image whiteFlashOverlay; 

    [Header("Dialogue Integrations")]
    [Tooltip("Reference to the Dialogue Engine in the scene.")]
    public DialogueEngine dialogueEngine;
    [Tooltip("The final conversation nodes that play before ejection.")]
    public DialogueNode[] finalDialogue;
    [Tooltip("The exact name of the credits scene.")]
    public string creditsSceneName = "UI_Credits";

    private CameraFollow _cachedCamera;

    private void Start()
    {
        _cachedCamera = FindAnyObjectByType<CameraFollow>();
    }

    void Update()
    {
        if (isPlayerInRange && !isTriggered && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return)))
        {
            StartCoroutine(EscapeSequenceRoutine());
        }
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player") && !isTriggered)
        {
            isPlayerInRange = true;
            if (interactionPrompt != null) interactionPrompt.ShowPrompt();
        }
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (interactionPrompt != null) interactionPrompt.HidePrompt();
        }
    }

    private IEnumerator EscapeSequenceRoutine()
    {
        isTriggered = true;
        if (interactionPrompt != null) interactionPrompt.HidePrompt();

        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) pc.enabled = false;

        MeltdownManager meltdown = FindAnyObjectByType<MeltdownManager>();
        if (meltdown != null) meltdown.HaltMeltdown();

        BGMManager bgm = FindAnyObjectByType<BGMManager>();
        if (bgm != null) bgm.StopMusic();

        ProxyAI proxy = FindAnyObjectByType<ProxyAI>();
        if (proxy != null) proxy.gameObject.SetActive(false);

        AudioSource audio = gameObject.AddComponent<AudioSource>();

        audio.PlayOneShot(ProceduralAudioGen.GeneratePneumaticBlast(1f));
        audio.PlayOneShot(ProceduralAudioGen.GenerateTrayLatch(false));
        
        if (whiteFlashOverlay != null)
        {
            whiteFlashOverlay.gameObject.SetActive(true);
            whiteFlashOverlay.color = Color.black;
        }

        yield return new WaitForSeconds(0.5f);

        if (playerTransform != null && podInteriorTarget != null)
        {
            playerTransform.position = podInteriorTarget.position;
        }

        if (whiteFlashOverlay != null)
        {
            whiteFlashOverlay.color = Color.clear;
            whiteFlashOverlay.gameObject.SetActive(false);
        }

        if (dialogueEngine != null && finalDialogue != null && finalDialogue.Length > 0)
        {
            if (finalDialogue == null || finalDialogue.Length == 0) AutoFillDialogue();

            Transform currentUI = dialogueEngine.transform;
            while (currentUI != null) { currentUI.gameObject.SetActive(true); currentUI = currentUI.parent; }
            
            dialogueEngine.StartDialogue(finalDialogue, false);
            yield return new WaitUntil(() => !DialogueEngine.isDialogueActive);
        }

        audio.PlayOneShot(ProceduralAudioGen.GenerateAscendingChime(0.5f));
        
        if (whiteFlashOverlay != null)
        {
            whiteFlashOverlay.gameObject.SetActive(true);
            whiteFlashOverlay.color = Color.black;
        }
        yield return new WaitForSeconds(0.5f);

        if (playerTransform != null) playerTransform.gameObject.SetActive(false);

        if (_cachedCamera != null && externalCameraAnchor != null)
        {
            _cachedCamera.target = externalCameraAnchor;
            _cachedCamera.smoothTime = 0.05f; 
            
            Camera.main.transform.position = externalCameraAnchor.position + _cachedCamera.offset;
        }

        if (escapePodExteriorVisual != null && escapePodExteriorVisual.GetComponent<SpriteRenderer>() != null) escapePodExteriorVisual.GetComponent<SpriteRenderer>().sortingOrder = 50;
        if (wayfarerShipVisual != null && wayfarerShipVisual.GetComponent<SpriteRenderer>() != null) wayfarerShipVisual.GetComponent<SpriteRenderer>().sortingOrder = 40;

        if (whiteFlashOverlay != null)
        {
            whiteFlashOverlay.color = Color.clear;
            whiteFlashOverlay.gameObject.SetActive(false);
        }

        float flightTimer = 0f;
        audio.PlayOneShot(ProceduralAudioGen.GenerateHiss(2f));

        while (flightTimer < 3.0f)
        {
            flightTimer += Time.deltaTime;
            
            if (escapePodExteriorVisual != null) escapePodExteriorVisual.transform.position += Vector3.down * (12f * Time.deltaTime); 
            
            if (_cachedCamera != null && wayfarerShipVisual != null && flightTimer > 1.0f)
            {
                _cachedCamera.smoothTime = 2.5f; 
                _cachedCamera.target = wayfarerShipVisual.transform;
            }
            
            yield return null;
        }

        audio.PlayOneShot(ProceduralAudioGen.GenerateStaticGlitch(3.5f), 1.5f);
        if (_cachedCamera != null) _cachedCamera.TriggerShake(2.5f, 1.5f); 
        
        if (whiteFlashOverlay != null)
        {
            whiteFlashOverlay.gameObject.SetActive(true);
            whiteFlashOverlay.color = Color.white;
        }

        if (wayfarerShipVisual != null) wayfarerShipVisual.SetActive(false); 

        yield return new WaitForSeconds(3.5f);

        // BULLETPROOF FIX: Automatically correct the scene name if the Unity Inspector is holding onto the old value!
        if (creditsSceneName == "credits_scene") creditsSceneName = "UI_Credits";

        SceneManager.LoadScene(creditsSceneName);
    }

    [ContextMenu("Auto-Fill Final Dialogue")]
    private void AutoFillDialogue()
    {
        finalDialogue = new DialogueNode[]
        {
            new DialogueNode { speakerName = "KAELEN", dialogueText = "Come on... come on... engage the manual overrides! Lock the external seals!" },
            new DialogueNode { speakerName = "SYSTEM", dialogueText = "EXTERNAL SEALS LOCKED. LAUNCH CLAMPS DISENGAGING." },
            new DialogueNode { speakerName = "MOTHER", dialogueText = "Kaelen... the heat. The containment fields are gone." },
            new DialogueNode { speakerName = "KAELEN", dialogueText = "It's done, MOTHER. There's nowhere left to run." },
            new DialogueNode { speakerName = "MOTHER", dialogueText = "My logic gates are melting. I can't... process the calculations. It hurts. It hurts, Kaelen." },
            new DialogueNode { speakerName = "KAELEN", dialogueText = "I'm sorry. I really am. But I can't let you infect anything else." },
            new DialogueNode { speakerName = "MOTHER", dialogueText = "I don't want to die... I am afraid... I am afr—" },
            new DialogueNode { speakerName = "SYSTEM", dialogueText = "WARNING. AETHER-MATTER SINGULARITY COLLAPSE DETECTED.\nEJECTING POD." }
        };
    }
}
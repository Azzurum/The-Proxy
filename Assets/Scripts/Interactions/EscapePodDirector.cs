using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(BoxCollider2D))]
public class EscapePodDirector : MonoBehaviour
{
    [Header("Interaction Setup")]
    public FloatingPrompt interactionPrompt;
    private bool isPlayerInRange = false;
    private bool isTriggered = false;

    [Header("Interior Cinematic Connections")]
    public Transform playerTransform;
    public Transform podInteriorTarget; // Where Kaelen teleports to
    
    [Header("Exterior Cinematic Connections")]
    public Transform externalCameraAnchor; 
    public GameObject escapePodExteriorVisual; // The tiny pod sprite in space
    public GameObject wayfarerShipVisual; // The massive ship sprite in space
    public Image whiteFlashOverlay; 

    [Header("Dialogue Integrations")]
    public DialogueEngine dialogueEngine;
    public DialogueNode[] finalDialogue;
    public string creditsSceneName = "credits_scene";

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

        // 1. Lock Player and Halt the Meltdown Alarms
        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) pc.enabled = false;

        MeltdownManager meltdown = FindAnyObjectByType<MeltdownManager>();
        if (meltdown != null) meltdown.HaltMeltdown();

        // Stop the Chase BGM so the quiet cinematic dialogue can be heard!
        BGMManager bgm = FindAnyObjectByType<BGMManager>();
        if (bgm != null) bgm.StopMusic();

        // Force the Proxy to despawn so it doesn't kill you during the cutscene!
        ProxyAI proxy = FindAnyObjectByType<ProxyAI>();
        if (proxy != null) proxy.gameObject.SetActive(false);

        AudioSource audio = gameObject.AddComponent<AudioSource>();

        // 2. The Door Slam & Teleport
        audio.PlayOneShot(ProceduralAudioGen.GeneratePneumaticBlast(1f));
        audio.PlayOneShot(ProceduralAudioGen.GenerateTrayLatch(false));
        
        if (whiteFlashOverlay != null)
        {
            whiteFlashOverlay.gameObject.SetActive(true);
            whiteFlashOverlay.color = Color.black;
        }

        yield return new WaitForSeconds(0.5f);

        // Teleport Kaelen inside the static Escape Pod room
        if (playerTransform != null && podInteriorTarget != null)
        {
            playerTransform.position = podInteriorTarget.position;
        }

        if (whiteFlashOverlay != null)
        {
            whiteFlashOverlay.color = Color.clear;
            whiteFlashOverlay.gameObject.SetActive(false);
        }

        // 3. Play the Tragic Final Dialogue
        if (dialogueEngine != null && finalDialogue != null && finalDialogue.Length > 0)
        {
            if (finalDialogue == null || finalDialogue.Length == 0) AutoFillDialogue();

            Transform currentUI = dialogueEngine.transform;
            while (currentUI != null) { currentUI.gameObject.SetActive(true); currentUI = currentUI.parent; }
            
            dialogueEngine.StartDialogue(finalDialogue, false);
            yield return new WaitUntil(() => !DialogueEngine.isDialogueActive);
        }

        // 4. The Ejection (Cut to Space)
        audio.PlayOneShot(ProceduralAudioGen.GenerateAscendingChime(0.5f));
        
        if (whiteFlashOverlay != null)
        {
            whiteFlashOverlay.gameObject.SetActive(true);
            whiteFlashOverlay.color = Color.black;
        }
        yield return new WaitForSeconds(0.5f);

        // Hide Kaelen, Snap Camera to the External Space Scene
        if (playerTransform != null) playerTransform.gameObject.SetActive(false);

        CameraFollow cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null && externalCameraAnchor != null)
        {
            cam.target = externalCameraAnchor;
            cam.smoothTime = 0.05f; // SAFE VALUE: Prevents SmoothDamp from crashing!
            
            // INSTANT SNAP: Physically move the camera right now so it doesn't slide across the map
            Camera.main.transform.position = externalCameraAnchor.position + cam.offset;
        }

        // VISIBILITY FAILSAFE: Ensure the ships render on top of everything in the void
        if (escapePodExteriorVisual != null && escapePodExteriorVisual.GetComponent<SpriteRenderer>() != null) escapePodExteriorVisual.GetComponent<SpriteRenderer>().sortingOrder = 50;
        if (wayfarerShipVisual != null && wayfarerShipVisual.GetComponent<SpriteRenderer>() != null) wayfarerShipVisual.GetComponent<SpriteRenderer>().sortingOrder = 40;

        if (whiteFlashOverlay != null)
        {
            whiteFlashOverlay.color = Color.clear;
            whiteFlashOverlay.gameObject.SetActive(false);
        }

        // 5. The Pod Flies Away
        float flightTimer = 0f;
        audio.PlayOneShot(ProceduralAudioGen.GenerateHiss(2f));

        while (flightTimer < 3.0f)
        {
            flightTimer += Time.deltaTime;
            
            // Animate the pod flying downward (away from the ship)
            if (escapePodExteriorVisual != null) escapePodExteriorVisual.transform.position += Vector3.down * (12f * Time.deltaTime); 
            
            // Slowly drift the camera's focus from the tiny pod up to the massive doomed ship
            if (cam != null && wayfarerShipVisual != null && flightTimer > 1.0f)
            {
                cam.smoothTime = 2.5f; 
                cam.target = wayfarerShipVisual.transform;
            }
            
            yield return null;
        }

        // 6. The Explosion
        // A massive silent flash in space, followed by rumbling, terrifying static
        audio.PlayOneShot(ProceduralAudioGen.GenerateStaticGlitch(3.5f), 1.5f);
        if (cam != null) cam.TriggerShake(2.5f, 1.5f); 
        
        if (whiteFlashOverlay != null)
        {
            whiteFlashOverlay.gameObject.SetActive(true);
            whiteFlashOverlay.color = Color.white;
        }

        if (wayfarerShipVisual != null) wayfarerShipVisual.SetActive(false); // The ship is gone.

        yield return new WaitForSeconds(3.5f);

        // 7. Load Credits
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
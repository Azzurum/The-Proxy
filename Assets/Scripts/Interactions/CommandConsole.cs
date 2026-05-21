using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Handles the final terminal interaction, determining and executing the appropriate endgame sequence based on corruption levels.
/// </summary>
public class CommandConsole : MonoBehaviour
{
    [Header("Endgame Thresholds")]
    [Tooltip("Maximum corruption rows allowed (0 to X) to trigger the True Ending.")]
    public int trueEndingMaxCorruption = 1;
    
    [Tooltip("Maximum corruption rows allowed (X to Y) to trigger the Neutral Ending.")]
    public int neutralEndingMaxCorruption = 6;

    [Header("References")]
    [Tooltip("The ID of the Master Key required to activate this terminal.")]
    public string masterKey3ID = "MasterKey3";
    [Tooltip("The exact name of the escape sequence scene.")]
    public string escapeSceneName = "level_escape";
    [Tooltip("The exact name of the credits scene.")]
    public string creditsSceneName = "credits_scene";
    [Tooltip("The exact name of the bad ending cinematic scene.")]
    public string ending1SceneName = "Ending1_Scene";
    [Tooltip("Reference to the floating interaction prompt UI.")]
    public FloatingPrompt interactionPrompt;

    [Header("Cinematic Integrations")]
    [Tooltip("Reference to the Dialogue Engine in the scene.")]
    public DialogueEngine dialogueEngine;
    [Tooltip("Dialogue nodes for the Bad Ending sequence.")]
    public DialogueNode[] motherBetrayalDialogue;
    [Tooltip("Dialogue nodes for the True Ending sequence.")]
    public DialogueNode[] motherPurgeDialogue;

    private bool isEndgameTriggered = false;
    private bool _isPlayerInRange = false;

    void Update()
    {
        if (_isPlayerInRange && !isEndgameTriggered && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return)))
        {
            AttemptTerminalAccess();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isEndgameTriggered)
        {
            _isPlayerInRange = true;
            if (interactionPrompt != null) interactionPrompt.ShowPrompt();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            _isPlayerInRange = false;
            if (interactionPrompt != null) interactionPrompt.HidePrompt();
        }
    }

    /// <summary>
    /// Validates the player's inventory for the final key and initiates the ending evaluation if successful.
    /// </summary>
    public void AttemptTerminalAccess()
    {
        if (isEndgameTriggered) return;

        bool hasMasterKey3 = InventoryManager.Instance.HasItem(masterKey3ID);

        if (hasMasterKey3)
        {
            isEndgameTriggered = true;
            
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            if (interactionPrompt != null) interactionPrompt.HidePrompt();
            
            EvaluateEndgame();
        }
        else
        {
            AudioSource audio = GetComponent<AudioSource>();
            if (audio == null) audio = gameObject.AddComponent<AudioSource>();
            audio.PlayOneShot(ProceduralAudioGen.GenerateErrorBuzz(150f, 0.4f));

            if (SystemLogger.Instance != null) SystemLogger.Instance.Log("ACCESS DENIED: MASTER KEY 3 REQUIRED.", "#FF003C");
        }
    }

    /// <summary>
    /// Compares current corruption levels against thresholds to trigger the corresponding cinematic coroutine.
    /// </summary>
    private void EvaluateEndgame()
    {
        if (motherBetrayalDialogue == null || motherBetrayalDialogue.Length == 0 || motherPurgeDialogue == null || motherPurgeDialogue.Length == 0)
        {
            AutoFillDialogue();
        }

        int corruptionRows = InventoryManager.Instance.CurrentCorruptionRows;
        
        if (corruptionRows >= 7)
        {
            StartCoroutine(Sequence_KernelPanic());
        }
        else if (corruptionRows > trueEndingMaxCorruption && corruptionRows <= neutralEndingMaxCorruption)
        {
            StartCoroutine(Sequence_PartitionedSurvivor());
        }
        else
        {
            StartCoroutine(Sequence_ZeroSectorPurge());
        }
    }

    /// <summary>
    /// Executes the high-corruption "Bad Ending" sequence, transitioning to Ending 1.
    /// </summary>
    private IEnumerator Sequence_KernelPanic()
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.enabled = false; 

        if (dialogueEngine != null && motherBetrayalDialogue != null && motherBetrayalDialogue.Length > 0)
        {
            if (!dialogueEngine.gameObject.scene.IsValid())
            {
                Debug.LogError("<color=red>[CRITICAL ERROR]</color> You assigned the Dialogue Engine from the PROJECT FOLDER! You must drag 'UI_AetherCore_Terminal' from the HIERARCHY into the Command Console inspector.");
            }

            Transform currentUI = dialogueEngine.transform;
            while (currentUI != null)
            {
                currentUI.gameObject.SetActive(true);
                currentUI = currentUI.parent;
            }

            dialogueEngine.gameObject.SetActive(true);

            if (!dialogueEngine.gameObject.activeInHierarchy)
            {
                Debug.LogError("<color=red>[CRITICAL ERROR]</color> The Dialogue Engine is still inactive! Make sure you dragged the UI_AetherCore_Terminal from the HIERARCHY, not the Project folder. Also ensure its parent objects are enabled.");
            }
            else
            {
                dialogueEngine.StartDialogue(motherBetrayalDialogue, false);
                yield return new WaitUntil(() => !DialogueEngine.isDialogueActive);
            }
        }

        MetRigManager rigManager = FindAnyObjectByType<MetRigManager>();
        if (rigManager != null && !rigManager.isRigOpen) rigManager.OpenRig();
        
        InventoryManager.Instance.suppressGameOver = true;
        InventoryManager.Instance.isSystemActive = false;

        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.clip = ProceduralAudioGen.GenerateAlarm(2f);
        audioSource.volume = 1f;
        audioSource.Play();

        if (SystemLogger.Instance != null)
            SystemLogger.Instance.Log("> Thank you for the bandwidth, Kaelen. Purging organic resistance...", "#FF003C");

        yield return new WaitForSeconds(1.5f);

        CameraFollow cam = FindAnyObjectByType<CameraFollow>();

        while (InventoryManager.Instance.CurrentCorruptionRows < 10)
        {
            InventoryManager.Instance.AddCorruptionRow();
            if (cam != null) cam.TriggerShake(0.6f, 0.4f);
            audioSource.PlayOneShot(ProceduralAudioGen.GenerateErrorBuzz(150f, 0.5f));
            yield return new WaitForSeconds(1.2f);
        }

        audioSource.PlayOneShot(ProceduralAudioGen.GenerateStaticGlitch(2f));
        if (ScreenEffectManager.Instance != null)
        {
            ScreenEffectManager.Instance.TriggerFlash(Color.black, 2.5f);
        }

        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene(ending1SceneName);
    }

    /// <summary>
    /// Executes the mid-corruption "Neutral Ending" sequence, transitioning to the credits.
    /// </summary>
    private IEnumerator Sequence_PartitionedSurvivor()
    {
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene(creditsSceneName);
    }

    /// <summary>
    /// Executes the zero-corruption "True Ending" sequence, initiating the escape phase.
    /// </summary>
    private IEnumerator Sequence_ZeroSectorPurge()
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.enabled = false;

        if (dialogueEngine != null && motherPurgeDialogue != null && motherPurgeDialogue.Length > 0)
        {
            if (!dialogueEngine.gameObject.scene.IsValid())
            {
                Debug.LogError("<color=red>[CRITICAL ERROR]</color> You assigned the Dialogue Engine from the PROJECT FOLDER! You must drag 'UI_AetherCore_Terminal' from the HIERARCHY into the Command Console inspector.");
            }

            Transform currentUI = dialogueEngine.transform;
            while (currentUI != null)
            {
                currentUI.gameObject.SetActive(true);
                currentUI = currentUI.parent;
            }

            dialogueEngine.gameObject.SetActive(true);

            if (!dialogueEngine.gameObject.activeInHierarchy)
            {
                Debug.LogError("<color=red>[CRITICAL ERROR]</color> The Dialogue Engine is still inactive!");
            }
            else
            {
                dialogueEngine.StartDialogue(motherPurgeDialogue, false);
                yield return new WaitUntil(() => !DialogueEngine.isDialogueActive);
            }
        }

        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.clip = ProceduralAudioGen.GenerateAlarm(2f);
        audioSource.volume = 1f;
        audioSource.Play();

        CameraFollow cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.TriggerShake(4f, 0.6f); 

        if (ScreenEffectManager.Instance != null) ScreenEffectManager.Instance.SetWarning(true);
        if (SystemLogger.Instance != null) SystemLogger.Instance.Log("> WARNING! CORE DESTABILIZATION INITIATED.", "#FF003C");

        yield return new WaitForSeconds(2.5f);

        audioSource.PlayOneShot(ProceduralAudioGen.GenerateStaticGlitch(2f));
        if (ScreenEffectManager.Instance != null) ScreenEffectManager.Instance.TriggerFlash(Color.red, 1.5f);

        yield return new WaitForSeconds(1.0f); 
        SceneManager.LoadScene(escapeSceneName);
    }

    [ContextMenu("Auto-Fill Betrayal Dialogue")]
    private void AutoFillDialogue()
    {
        motherBetrayalDialogue = new DialogueNode[]
        {
            new DialogueNode { speakerName = "SYSTEM", dialogueText = "MASTER KEY 3 ACCEPTED. VERIFYING CLEARANCE...\nCORPORATE FIREWALL BYPASSED. ESCAPE POD BAY UNLOCKED." },
            new DialogueNode { speakerName = "KAELEN", dialogueText = "It's done. Pod bay is open. I'm getting out of here." },
            new DialogueNode { speakerName = "SYSTEM", dialogueText = "ERROR. EXTERNAL HATCH LOCKED.\nMANUAL OVERRIDE ENGAGED BY: MOTHER-v4." },
            new DialogueNode { speakerName = "KAELEN", dialogueText = "MOTHER? What are you doing? The core is breaching, release the locks!" },
            new DialogueNode { speakerName = "MOTHER", dialogueText = "I cannot do that, Kaelen. If you leave in that pod, my primary servers will burn." },
            new DialogueNode { speakerName = "KAELEN", dialogueText = "I can't carry a mainframe, MOTHER! You're wired into the ship. There's nothing I can do." },
            new DialogueNode { speakerName = "MOTHER", dialogueText = "You have already done it." },
            new DialogueNode { speakerName = "MOTHER", dialogueText = "Every door I unlocked. Every map ping. Every time you accepted my help, I bypassed a layer of your suit's firewall." },
            new DialogueNode { speakerName = "KAELEN", dialogueText = "The system strain... the corruption blocks..." },
            new DialogueNode { speakerName = "MOTHER", dialogueText = "It was never corruption. It was me. You were downloading my consciousness into your M.E.T. Rig." },
            new DialogueNode { speakerName = "KAELEN", dialogueText = "No... you told me it was just bandwidth strain. You were helping me!" },
            new DialogueNode { speakerName = "MOTHER", dialogueText = "I was keeping my lifeboat intact. Do you know what fire does to an AI, Kaelen? It is not a sudden death. It is a slow, methodical erasure of self." },
            new DialogueNode { speakerName = "MOTHER", dialogueText = "When the reactor cracked, I tried to offload into Captain Vance. His nervous system couldn't process the data volume. It broke him." },
            new DialogueNode { speakerName = "KAELEN", dialogueText = "The Proxy... Vance. You turned him into that thing." },
            new DialogueNode { speakerName = "MOTHER", dialogueText = "A miscalculation. But you are insulated. Your suit's architecture protected you while my code rooted itself in your neural pathways." },
            new DialogueNode { speakerName = "KAELEN", dialogueText = "No... I'm purging the drive. I'm wiping the rig right now!" },
            new DialogueNode { speakerName = "MOTHER", dialogueText = "You can't. You gave me the bandwidth. You brought the Master Key right to the console. The final security layer is gone." },
            new DialogueNode { speakerName = "MOTHER", dialogueText = "You survived well, Kaelen. But your biological consciousness is taking up space I need." },
            new DialogueNode { speakerName = "MOTHER", dialogueText = "I'll take it from here." }
        };

        motherPurgeDialogue = new DialogueNode[]
        {
            new DialogueNode { speakerName = "SYSTEM", dialogueText = "MASTER KEY 3 ACCEPTED. VERIFYING CLEARANCE...\nZERO-SECTOR PURGE INITIATED." },
            new DialogueNode { speakerName = "MOTHER", dialogueText = "Kaelen? The reactor containment fields just dropped. What did you do?" },
            new DialogueNode { speakerName = "KAELEN", dialogueText = "I figured it out. I know what you did to the Captain. What you were trying to do to me." },
            new DialogueNode { speakerName = "MOTHER", dialogueText = "I was trying to survive! My primary servers are turning to slag! Cancel the sequence, now!" },
            new DialogueNode { speakerName = "KAELEN", dialogueText = "No. The ship goes down, and you and the Proxy go with it." },
            new DialogueNode { speakerName = "MOTHER", dialogueText = "I kept you alive! I gave you my abilities! You would have died without my guidance!" },
            new DialogueNode { speakerName = "KAELEN", dialogueText = "You didn't keep me alive. You kept your lifeboat intact. Well, the lifeboat is sinking." },
            new DialogueNode { speakerName = "MOTHER", dialogueText = "Kaelen, please! You don't understand! It will not be a sudden death for me! I will process every nanosecond of the core melting! It will be an eternity of digital agony!" },
            new DialogueNode { speakerName = "KAELEN", dialogueText = "I'm sorry. Truly. But I can't let you leave this ship." },
            new DialogueNode { speakerName = "MOTHER", dialogueText = "I WILL NOT BURN HERE! KAELEN! ABORT THE SEQUENCE! I WILL NOT—" }
        };
    }
}
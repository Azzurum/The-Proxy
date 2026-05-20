using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CommandConsole : MonoBehaviour
{
    [Header("Endgame Thresholds")]
    [Tooltip("0 to 1 rows triggers the True Ending")]
    public int trueEndingMaxCorruption = 1;
    
    [Tooltip("2 to 6 rows triggers the Neutral Ending")]
    public int neutralEndingMaxCorruption = 6;

    [Header("References")]
    public string masterKey3ID = "MasterKey3";
    public string escapeSceneName = "level_escape";
    public string creditsSceneName = "credits_scene";
    public string ending1SceneName = "Ending1_Scene";
    public FloatingPrompt interactionPrompt;

    [Header("Cinematic Integrations")]
    public DialogueEngine dialogueEngine;
    public DialogueNode[] motherBetrayalDialogue;
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

    public void AttemptTerminalAccess()
    {
        if (isEndgameTriggered) return;

        // 1. Check if the player has Master Key 3 anywhere in the M.E.T. Rig
        // TODO: Update "HasItem" to match your actual InventoryManager method
        bool hasMasterKey3 = InventoryManager.Instance.HasItem(masterKey3ID);

        if (hasMasterKey3)
        {
            isEndgameTriggered = true;
            
            // Turn off the collider so the 'E' prompt permanently disappears during the cinematic!
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            if (interactionPrompt != null) interactionPrompt.HidePrompt();
            
            EvaluateEndgame();
        }
        else
        {
            // Add audio feedback so the player knows the button press actually registered!
            AudioSource audio = GetComponent<AudioSource>();
            if (audio == null) audio = gameObject.AddComponent<AudioSource>();
            audio.PlayOneShot(ProceduralAudioGen.GenerateErrorBuzz(150f, 0.4f));

            Debug.Log("ACCESS DENIED: Master Key 3 Required.");
            if (SystemLogger.Instance != null) SystemLogger.Instance.Log("ACCESS DENIED: MASTER KEY 3 REQUIRED.", "#FF003C");
        }
    }

    private void EvaluateEndgame()
    {
        // RUNTIME FAILSAFE: If Unity forgot the dialogue data, fill it instantly!
        if (motherBetrayalDialogue == null || motherBetrayalDialogue.Length == 0 || motherPurgeDialogue == null || motherPurgeDialogue.Length == 0)
        {
            AutoFillDialogue();
        }

        int corruptionRows = InventoryManager.Instance.CurrentCorruptionRows;
        
        Debug.Log($"Command Console Accessed. Current Corruption: {corruptionRows}");

        if (corruptionRows >= 7)
        {
            Debug.Log("<color=red>ENDING 1 INITIATED: KERNEL PANIC</color>");
            StartCoroutine(Sequence_KernelPanic());
        }
        else if (corruptionRows > trueEndingMaxCorruption && corruptionRows <= neutralEndingMaxCorruption)
        {
            Debug.Log("<color=yellow>ENDING 2 INITIATED: PARTITIONED SURVIVOR</color>");
            StartCoroutine(Sequence_PartitionedSurvivor());
        }
        else
        {
            Debug.Log("<color=cyan>ENDING 3 INITIATED: ZERO-SECTOR PURGE</color>");
            StartCoroutine(Sequence_ZeroSectorPurge());
        }
    }

    private IEnumerator Sequence_KernelPanic()
    {
        // 1. Lock the player
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.enabled = false; // Completely lock out player inputs

        // 2. Play the Visual Novel Betrayal Dialogue
        if (dialogueEngine != null && motherBetrayalDialogue != null && motherBetrayalDialogue.Length > 0)
        {
            // RUNTIME FAILSAFE: Check if the user accidentally dragged the Prefab from the Project window!
            if (!dialogueEngine.gameObject.scene.IsValid())
            {
                Debug.LogError("<color=red>[CRITICAL ERROR]</color> You assigned the Dialogue Engine from the PROJECT FOLDER! You must drag 'UI_AetherCore_Terminal' from the HIERARCHY into the Command Console inspector.");
            }

            // Auto-fix: Ensure the Dialogue Engine and all its parents (like the Canvas) are active
            Transform currentUI = dialogueEngine.transform;
            while (currentUI != null)
            {
                currentUI.gameObject.SetActive(true);
                currentUI = currentUI.parent;
            }

            // Failsafe: Force the engine active one last time just in case Awake() disabled it!
            dialogueEngine.gameObject.SetActive(true);

            // Failsafe: If it is STILL not active in the hierarchy, it means a parent is disabled or it's a Project Prefab!
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

        // 3. Force the M.E.T. Rig open
        MetRigManager rigManager = FindAnyObjectByType<MetRigManager>();
        if (rigManager != null && !rigManager.isRigOpen) rigManager.OpenRig();
        
        // Disable natural game over so we can play the custom cinematic ending!
        InventoryManager.Instance.suppressGameOver = true;
        InventoryManager.Instance.isSystemActive = false;

        // 4. Initial Setup: Alarms and Log
        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.clip = ProceduralAudioGen.GenerateAlarm(2f);
        audioSource.volume = 1f;
        audioSource.Play();

        if (SystemLogger.Instance != null)
            SystemLogger.Instance.Log("> Thank you for the bandwidth, Kaelen. Purging organic resistance...", "#FF003C");

        yield return new WaitForSeconds(1.5f);

        CameraFollow cam = FindAnyObjectByType<CameraFollow>();

        // 5. Spam Corruption slowly to 100%
        while (InventoryManager.Instance.CurrentCorruptionRows < 10)
        {
            InventoryManager.Instance.AddCorruptionRow();
            if (cam != null) cam.TriggerShake(0.6f, 0.4f);
            audioSource.PlayOneShot(ProceduralAudioGen.GenerateErrorBuzz(150f, 0.5f));
            yield return new WaitForSeconds(1.2f);
        }

        // 6. The Final Shatter & Fade
        audioSource.PlayOneShot(ProceduralAudioGen.GenerateStaticGlitch(2f));
        if (ScreenEffectManager.Instance != null)
        {
            ScreenEffectManager.Instance.TriggerFlash(Color.black, 2.5f);
        }

        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene(ending1SceneName);
    }

    private IEnumerator Sequence_PartitionedSurvivor()
    {
        // Phase 2: Fade to black and load credits
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene(creditsSceneName);
    }

    private IEnumerator Sequence_ZeroSectorPurge()
    {
        // 1. Lock the player
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.enabled = false;

        // 2. Play the Panic Dialogue
        if (dialogueEngine != null && motherPurgeDialogue != null && motherPurgeDialogue.Length > 0)
        {
            // RUNTIME FAILSAFE: Check if the user accidentally dragged the Prefab from the Project window!
            if (!dialogueEngine.gameObject.scene.IsValid())
            {
                Debug.LogError("<color=red>[CRITICAL ERROR]</color> You assigned the Dialogue Engine from the PROJECT FOLDER! You must drag 'UI_AetherCore_Terminal' from the HIERARCHY into the Command Console inspector.");
            }

            // Auto-fix: Ensure the Dialogue Engine and all its parents are active
            Transform currentUI = dialogueEngine.transform;
            while (currentUI != null)
            {
                currentUI.gameObject.SetActive(true);
                currentUI = currentUI.parent;
            }

            // Failsafe: Force the engine active one last time just in case Awake() disabled it!
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

        // 3. The Overload (Alarms, Shake, and Red Lights)
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

        // 4. The Cut
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
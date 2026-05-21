using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the state, animations, and shielding logic for the player's M.E.T. Rig interface, as well as MOTHER's symbiote abilities.
/// </summary>
public class MetRigManager : MonoBehaviour
{
    [Header("System References")]
    [Tooltip("The main UI overlay for the terminal interface.")]
    public GameObject terminalOverlayUI;
    [Tooltip("Reference to the player controller for rooting movement while the rig is open.")]
    public PlayerController playerController;
    
    [Header("Faraday Shielding")]
    [Tooltip("Indicates whether the player is currently inside a signal-blocking Faraday zone.")]
    public bool inFaradayZone = false; 

    [Header("Rig State")]
    [Tooltip("Indicates whether the M.E.T. Rig inventory interface is actively open.")]
    public bool isRigOpen = false;

    [Header("MOTHER Abilities")]
    [Tooltip("Duration in seconds that the Sonar ability reveals the Proxy's location.")]
    public float sonarDuration = 5f;
    [Tooltip("Duration in seconds that the Signal Mask ability hides the player's UI signature.")]
    public float signalMaskDuration = 8f;

    private bool _isSonarActive = false;
    private float _sonarTimer = 0f;
    private bool _isSignalMasked = false;
    private float _signalMaskTimer = 0f;

    private ProxyAI _proxyAI;
    private AudioSource _rigAudioSource;
    private Coroutine _fanNoiseRoutine;
    private MetRigAnimator _rigAnimator;
    private QuestTracker _questTracker;

    private void Start()
    {
        if (terminalOverlayUI == null)
        {
            _rigAnimator = FindAnyObjectByType<MetRigAnimator>(FindObjectsInactive.Include);
            if (_rigAnimator != null) terminalOverlayUI = _rigAnimator.gameObject;
        }
        else
        {
            _rigAnimator = terminalOverlayUI.GetComponent<MetRigAnimator>();
        }

        if (playerController == null)
        {
            playerController = FindAnyObjectByType<PlayerController>();
        }

        if (terminalOverlayUI != null)
        {
            terminalOverlayUI.SetActive(false);
        }

        _proxyAI = FindAnyObjectByType<ProxyAI>();
        _questTracker = FindAnyObjectByType<QuestTracker>();

        _rigAudioSource = gameObject.AddComponent<AudioSource>();
        _rigAudioSource.volume = 0.6f;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleRig();
        }

        if (_isSonarActive)
        {
            _sonarTimer -= Time.deltaTime;
            if (_sonarTimer <= 0)
            {
                _isSonarActive = false;
            }
        }

        if (_isSignalMasked)
        {
            _signalMaskTimer -= Time.deltaTime;
            if (_signalMaskTimer <= 0)
            {
                _isSignalMasked = false;
                
                if (isRigOpen && _proxyAI != null && !inFaradayZone)
                {
                    float distance = Vector2.Distance(transform.position, _proxyAI.transform.position);
                    _proxyAI.OnSignalSpike(true, distance);
                }
            }
        }
    }

    /// <summary>
    /// Toggles the M.E.T. Rig interface on or off, handling animations, audio, and enemy perception triggers.
    /// </summary>
    public void ToggleRig()
    {
        if (isRigOpen && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.DisconnectFromLocker();
        }

        isRigOpen = !isRigOpen;

        if (isRigOpen)
        {
            bool wasInactive = !terminalOverlayUI.activeSelf;
            terminalOverlayUI.SetActive(true);
            
            if (!wasInactive && _rigAnimator != null)
            {
                _rigAnimator.PlayOpenAnimation();
                
                if (_fanNoiseRoutine != null) StopCoroutine(_fanNoiseRoutine);
                _fanNoiseRoutine = StartCoroutine(FanNoiseLoop());
            }
        }
        else
        {
            if (_rigAnimator != null) _rigAnimator.CloseInventoryWithAnimation();
            else terminalOverlayUI.SetActive(false);
            
            if (_fanNoiseRoutine != null) StopCoroutine(_fanNoiseRoutine);
        }

        if (playerController != null)
        {
            playerController.isRooted = isRigOpen;
        }

        if (_proxyAI != null)
        {
            bool signalLeaked = isRigOpen && !inFaradayZone;
            float distance = signalLeaked ? Vector2.Distance(transform.position, _proxyAI.transform.position) : -1f;
            _proxyAI.OnSignalSpike(signalLeaked && !_isSignalMasked, distance);
        }

        if (isRigOpen && InventoryManager.Instance != null)
        {
            Canvas.ForceUpdateCanvases();
            InventoryManager.Instance.RefreshAllGrids();

            bool shouldShowExt = InventoryManager.Instance.isInteractingWithLocker || InventoryManager.Instance.HasItemsInExternalStorage();
            if (InventoryManager.Instance.gridExt != null && InventoryManager.Instance.gridExt.parent != null)
            {
                InventoryManager.Instance.gridExt.parent.gameObject.SetActive(shouldShowExt);
            }
        }
    }

    private System.Collections.IEnumerator FanNoiseLoop()
    {
        while (isRigOpen)
        {
            if (_rigAudioSource != null) _rigAudioSource.PlayOneShot(ProceduralAudioGen.GenerateHiss(1.5f));
            yield return new WaitForSecondsRealtime(1.0f);
        }
    }

    /// <summary>
    /// Activates the MOTHER Override ability, trading corruption for high-tier door access.
    /// </summary>
    public void UseOverride()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddCorruptionRow();
        }
    }

    /// <summary>
    /// Activates the MOTHER Sonar ability, trading corruption to reveal the Proxy's location.
    /// </summary>
    public void UseSonar()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddCorruptionRow();
            _isSonarActive = true;
            _sonarTimer = sonarDuration;
        }
    }

    /// <summary>
    /// Activates the MOTHER Signal Mask ability, heavily trading corruption for temporary stealth while using the inventory.
    /// </summary>
    public void UseSignalMask()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddCorruptionRow();
            InventoryManager.Instance.AddCorruptionRow(); 
            _isSignalMasked = true;
            _signalMaskTimer = signalMaskDuration;
        }
    }

    /// <summary>
    /// Manually triggers the Rig to close and updates active tutorials if applicable.
    /// </summary>
    public void CloseRig()
    {
        if (isRigOpen)
        {
            ToggleRig();
        }

        if (_questTracker == null) _questTracker = FindAnyObjectByType<QuestTracker>();
        if (_questTracker != null && _questTracker.GetCurrentObjective() == 4)
        {
            _questTracker.AdvanceObjective(5, "Weld the Airlock Door");
        }
    }

    /// <summary>
    /// Manually triggers the Rig to open.
    /// </summary>
    public void OpenRig()
    {
        if (!isRigOpen)
        {
            ToggleRig();
        }
    }
}
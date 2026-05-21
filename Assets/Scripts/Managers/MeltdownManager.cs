using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Coordinates the final escape sequence, managing the countdown timer, the Proxy's enraged state, and audio/visual alarms.
/// </summary>
public class MeltdownManager : MonoBehaviour
{
    [Header("Meltdown Settings")]
    [Tooltip("Total time in seconds before the ship explodes, failing the mission.")]
    public float timeRemaining = 60f;
    [Tooltip("Is the countdown currently running?")]
    public bool isMeltdownActive = false; 

    [Header("UI References")]
    [Tooltip("The text component displaying the active countdown.")]
    public TextMeshProUGUI timerText;
    [Tooltip("The UI image used for the initial cinematic screen flash.")]
    public Image redFlashOverlay;
    [Tooltip("The UI text object prompting the player to run.")]
    public TextMeshProUGUI objectiveText;

    [Header("Cinematic References")]
    [Tooltip("The player's Transform, used to snap the camera back after the breach sequence.")]
    public Transform playerTransform;
    [Tooltip("The transform representing the door that the Proxy breaches during the cinematic.")]
    public Transform proxySpawnDoor;

    private AudioSource _alarmAudioSource;
    private CameraFollow _camFollow;
    private GameOverManager _gameOverManager;
    private ProxyAI _proxyAI;
    private PlayerController _playerController;
    private int _lastSecondBeep = 11;

    private void Start()
    {
        _camFollow = FindAnyObjectByType<CameraFollow>();
        _gameOverManager = FindAnyObjectByType<GameOverManager>();
        _proxyAI = FindAnyObjectByType<ProxyAI>();
        _playerController = FindAnyObjectByType<PlayerController>();

        _alarmAudioSource = gameObject.AddComponent<AudioSource>();
        _alarmAudioSource.clip = ProceduralAudioGen.GenerateAlarm(3f);
        _alarmAudioSource.loop = true;
        _alarmAudioSource.volume = 1f;
        _alarmAudioSource.Play();

        if (ScreenEffectManager.Instance != null)
        {
            ScreenEffectManager.Instance.SetWarning(true);
        }

        StartCoroutine(EscapeCinematicRoutine());
    }

    /// <summary>
    /// Plays the initial cutscene showing the Proxy breaching the door before the final timer starts.
    /// </summary>
    private IEnumerator EscapeCinematicRoutine()
    {
        if (_playerController != null) _playerController.enabled = false;

        if (redFlashOverlay != null)
        {
            redFlashOverlay.gameObject.SetActive(true);
            redFlashOverlay.color = Color.red;
            float fade = 1f;
            while (fade > 0f)
            {
                fade -= Time.deltaTime * 1.5f; 
                redFlashOverlay.color = new Color(1f, 0f, 0f, fade);
                yield return null;
            }
            redFlashOverlay.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(0.5f);

        if (_camFollow != null && (_proxyAI != null || proxySpawnDoor != null))
        {
            float originalSmooth = _camFollow.smoothTime;
            _camFollow.smoothTime = 0.4f; 
            
            _camFollow.target = _proxyAI != null ? _proxyAI.transform : proxySpawnDoor;
            
            if (_proxyAI != null) _proxyAI.ForceLookDirection(Vector2.right);
            
            yield return new WaitForSeconds(1.5f); 

            if (_proxyAI != null && proxySpawnDoor != null)
            {
                _proxyAI.TriggerCinematicAttack(_proxyAI.transform.position + Vector3.right);
                yield return new WaitForSeconds(0.4f); 
            }

            Time.timeScale = 0.1f; 
            if (ScreenEffectManager.Instance != null) ScreenEffectManager.Instance.TriggerFlash(Color.white, 0.2f);
            _alarmAudioSource.PlayOneShot(ProceduralAudioGen.GeneratePneumaticBlast(1f));
            _alarmAudioSource.PlayOneShot(ProceduralAudioGen.GenerateErrorBuzz(150f, 0.5f));
            _camFollow.TriggerShake(0.8f, 0.6f); 

            Animator doorAnim = proxySpawnDoor != null ? proxySpawnDoor.GetComponent<Animator>() : null;
            if (doorAnim != null) doorAnim.SetTrigger("OpenDoor"); 
            else if (proxySpawnDoor != null) proxySpawnDoor.gameObject.SetActive(false); 

            yield return new WaitForSecondsRealtime(0.7f); 
            Time.timeScale = 1f; 

            _camFollow.target = playerTransform != null ? playerTransform : (_playerController != null ? _playerController.transform : null);
            yield return new WaitForSeconds(1.0f); 
            
            _camFollow.smoothTime = originalSmooth; 
        }

        if (objectiveText != null) StartCoroutine(PulseRunTextRoutine());

        if (_proxyAI != null)
        {
            _proxyAI.TriggerEnragedHunt();
        }

        if (_playerController != null) _playerController.enabled = true;
        isMeltdownActive = true;

        yield return new WaitForSeconds(3.5f);
        if (objectiveText != null) objectiveText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Disables the meltdown sequence, usually invoked when the player reaches the escape pod.
    /// </summary>
    public void HaltMeltdown()
    {
        isMeltdownActive = false;
        if (_alarmAudioSource != null) _alarmAudioSource.Stop();
        if (timerText != null) timerText.gameObject.SetActive(false);
        
        if (ScreenEffectManager.Instance != null) ScreenEffectManager.Instance.SetWarning(false);
        if (_camFollow != null) _camFollow.TriggerShake(0f, 0f); 
    }

    /// <summary>
    /// Pulses the color and scale of the objective text to incite panic.
    /// </summary>
    private IEnumerator PulseRunTextRoutine()
    {
        objectiveText.text = "RUN!!!!!";
        objectiveText.gameObject.SetActive(true);
        
        while (objectiveText.gameObject.activeSelf)
        {
            objectiveText.color = (Mathf.FloorToInt(Time.time * 12) % 2 == 0) ? Color.red : Color.white;
            
            float scale = 1f + (Mathf.Sin(Time.time * 25f) * 0.15f);
            objectiveText.transform.localScale = new Vector3(scale, scale, 1f);
            
            yield return null;
        }
    }

    private void Update()
    {
        if (!isMeltdownActive) return;

        timeRemaining -= Time.deltaTime;

        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60F);
            int seconds = Mathf.FloorToInt(timeRemaining - minutes * 60);
            int milliseconds = Mathf.FloorToInt((timeRemaining - minutes * 60 - seconds) * 100);
            
            timerText.SetText("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
            
            if (timeRemaining <= 10f) 
            {
                timerText.color = (Mathf.FloorToInt(Time.time * 10) % 2 == 0) ? Color.red : Color.white;

                int currentSecond = Mathf.CeilToInt(timeRemaining);
                if (currentSecond != _lastSecondBeep && currentSecond > 0)
                {
                    _lastSecondBeep = currentSecond;
                    
                    _alarmAudioSource.PlayOneShot(ProceduralAudioGen.GenerateClick(1500f, 0.15f));
                    _alarmAudioSource.PlayOneShot(ProceduralAudioGen.GenerateErrorBuzz(250f, 0.2f));

                    if (ScreenEffectManager.Instance != null) ScreenEffectManager.Instance.TriggerFlash(new Color(1f, 0f, 0f, 0.4f), 0.2f);
                    
                    if (_camFollow != null) _camFollow.TriggerShake(0.4f, 0.6f);
                }
            }
        }

        float continuousShake = timeRemaining <= 10f ? 0.35f : 0.2f;
        if (_camFollow != null) _camFollow.TriggerShake(0.2f, continuousShake); 

        if (timeRemaining <= 0) TriggerFailure();
    }

    /// <summary>
    /// Triggers the game over sequence when the timer runs out.
    /// </summary>
    private void TriggerFailure()
    {
        isMeltdownActive = false;
        if (timerText != null) timerText.text = "00:00:00";
        
        if (_alarmAudioSource != null) _alarmAudioSource.PlayOneShot(ProceduralAudioGen.GenerateStaticGlitch(3f), 1.5f);
        if (_gameOverManager != null) _gameOverManager.TriggerGameOver();
    }
}
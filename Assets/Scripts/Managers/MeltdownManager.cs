using UnityEngine;
using TMPro;
using System.Collections;

public class MeltdownManager : MonoBehaviour
{
    [Header("Meltdown Settings")]
    public float timeRemaining = 60f;
    public bool isMeltdownActive = true;

    [Header("UI References")]
    public TextMeshProUGUI timerText;

    private AudioSource alarmAudioSource;
    private CameraFollow camFollow;
    private GameOverManager gameOverManager;

    void Start()
    {
        camFollow = FindAnyObjectByType<CameraFollow>();
        gameOverManager = FindAnyObjectByType<GameOverManager>();

        // 1. Start the Siren
        alarmAudioSource = gameObject.AddComponent<AudioSource>();
        alarmAudioSource.clip = ProceduralAudioGen.GenerateAlarm(3f);
        alarmAudioSource.loop = true;
        alarmAudioSource.volume = 1f;
        alarmAudioSource.Play();

        // 2. Pulse the Red Warning Screen
        if (ScreenEffectManager.Instance != null)
        {
            ScreenEffectManager.Instance.SetWarning(true);
        }

        // 3. Enrage the Proxy!
        ProxyAI proxy = FindAnyObjectByType<ProxyAI>();
        if (proxy != null)
        {
            proxy.TriggerEnragedHunt();
        }
    }

    void Update()
    {
        if (!isMeltdownActive) return;

        timeRemaining -= Time.deltaTime;

        if (timerText != null)
        {
            // Format the timer as [MM:SS:ms] (e.g. 00:59:84)
            int minutes = Mathf.FloorToInt(timeRemaining / 60F);
            int seconds = Mathf.FloorToInt(timeRemaining - minutes * 60);
            int milliseconds = Mathf.FloorToInt((timeRemaining - minutes * 60 - seconds) * 100);
            
            timerText.text = string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
            
            // Flash the text violently red when under 10 seconds!
            if (timeRemaining <= 10f) 
            {
                timerText.color = (Mathf.FloorToInt(Time.time * 10) % 2 == 0) ? Color.red : Color.white;
            }
        }

        // Continuous Camera Shake
        if (camFollow != null) camFollow.TriggerShake(0.2f, 0.2f); 

        if (timeRemaining <= 0) TriggerFailure();
    }

    private void TriggerFailure()
    {
        isMeltdownActive = false;
        if (timerText != null) timerText.text = "00:00:00";
        
        alarmAudioSource.PlayOneShot(ProceduralAudioGen.GenerateStaticGlitch(3f), 1.5f);
        if (gameOverManager != null) gameOverManager.TriggerGameOver();
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MeltdownManager : MonoBehaviour
{
    [Header("Meltdown Settings")]
    public float timeRemaining = 60f;
    public bool isMeltdownActive = false; // Start false, activated by cinematic

    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public Image redFlashOverlay; 
    public TextMeshProUGUI objectiveText; 

    [Header("Cinematic References")]
    public Transform playerTransform;
    public Transform proxySpawnDoor; 

    private AudioSource alarmAudioSource;
    private CameraFollow camFollow;
    private GameOverManager gameOverManager;
    private int lastSecondBeep = 11;

    void Start()
    {
        camFollow = FindAnyObjectByType<CameraFollow>();
        gameOverManager = FindAnyObjectByType<GameOverManager>();

        // 1. Setup the Siren (but don't start timer yet)
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

        // 3. Start the Cinematic!
        StartCoroutine(EscapeCinematicRoutine());
    }

    private IEnumerator EscapeCinematicRoutine()
    {
        // 1. Lock the player
        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) pc.enabled = false;

        // 2. Fade in from the Red Flash (from level 3's transition)
        if (redFlashOverlay != null)
        {
            redFlashOverlay.gameObject.SetActive(true);
            redFlashOverlay.color = Color.red;
            float fade = 1f;
            while (fade > 0f)
            {
                fade -= Time.deltaTime * 1.5f; // Fades out over ~0.6 seconds
                redFlashOverlay.color = new Color(1f, 0f, 0f, fade);
                yield return null;
            }
            redFlashOverlay.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(0.5f);

        // 3. Pan the camera to the Proxy's breach point
        ProxyAI proxy = FindAnyObjectByType<ProxyAI>();

        if (camFollow != null && (proxy != null || proxySpawnDoor != null))
        {
            float originalSmooth = camFollow.smoothTime;
            camFollow.smoothTime = 0.4f; // Slower, dramatic pan
            
            // Target the Proxy directly so we don't accidentally pan to an off-center door pivot!
            camFollow.target = proxy != null ? proxy.transform : proxySpawnDoor;
            
            // Force the Proxy to use its Right Side Idle while the camera pans!
            if (proxy != null) proxy.ForceLookDirection(Vector2.right);
            
            yield return new WaitForSeconds(1.5f); // Wait for the camera to arrive

            // 4. The Proxy attacks the door!
            if (proxy != null && proxySpawnDoor != null)
            {
                // Force the attack to also aim right, ignoring the door's exact mathematical center!
                proxy.TriggerCinematicAttack(proxy.transform.position + Vector3.right);
                yield return new WaitForSeconds(0.4f); // Wait for the visual 'swing' to land
            }

            // 5. The door explodes!
            Time.timeScale = 0.1f; // Cinematic slow-mo impact!
            if (ScreenEffectManager.Instance != null) ScreenEffectManager.Instance.TriggerFlash(Color.white, 0.2f);
            alarmAudioSource.PlayOneShot(ProceduralAudioGen.GeneratePneumaticBlast(1f));
            alarmAudioSource.PlayOneShot(ProceduralAudioGen.GenerateErrorBuzz(150f, 0.5f));
            camFollow.TriggerShake(0.8f, 0.6f); // Violent shake

            // Destroy the door (or play its animation)
            Animator doorAnim = proxySpawnDoor != null ? proxySpawnDoor.GetComponent<Animator>() : null;
            if (doorAnim != null) doorAnim.SetTrigger("OpenDoor"); 
            else if (proxySpawnDoor != null) proxySpawnDoor.gameObject.SetActive(false); // Fallback: just delete it

            yield return new WaitForSecondsRealtime(0.7f); // Stare at the Proxy for a split second in real-world time
            Time.timeScale = 1f; // Snap back to full speed!

            // 6. Pan back to Kaelen
            camFollow.target = playerTransform != null ? playerTransform : (pc != null ? pc.transform : null);
            yield return new WaitForSeconds(1.0f); // Wait for camera to return
            
            camFollow.smoothTime = originalSmooth; // Restore normal camera speed
        }

        // 7. Show the Pulsing "RUN!!!!!" Objective
        if (objectiveText != null) StartCoroutine(PulseRunTextRoutine());

        // 8. Enrage the Proxy!
        if (proxy != null)
        {
            proxy.TriggerEnragedHunt();
        }

        // 9. Unlock Kaelen and Start the 60-Second Meltdown Timer!
        if (pc != null) pc.enabled = true;
        isMeltdownActive = true;

        // Hide the "RUN!!!!!" text after 3.5 seconds
        yield return new WaitForSeconds(3.5f);
        if (objectiveText != null) objectiveText.gameObject.SetActive(false);
    }

    public void HaltMeltdown()
    {
        isMeltdownActive = false;
        if (alarmAudioSource != null) alarmAudioSource.Stop();
        if (timerText != null) timerText.gameObject.SetActive(false);
        
        if (ScreenEffectManager.Instance != null) ScreenEffectManager.Instance.SetWarning(false);
        if (camFollow != null) camFollow.TriggerShake(0f, 0f); // Kill continuous shake
    }

    private IEnumerator PulseRunTextRoutine()
    {
        objectiveText.text = "RUN!!!!!";
        objectiveText.gameObject.SetActive(true);
        
        while (objectiveText.gameObject.activeSelf)
        {
            // Flash between Red and White rapidly
            objectiveText.color = (Mathf.FloorToInt(Time.time * 12) % 2 == 0) ? Color.red : Color.white;
            
            // Violent scale pulsing (heartbeat effect)
            float scale = 1f + (Mathf.Sin(Time.time * 25f) * 0.15f);
            objectiveText.transform.localScale = new Vector3(scale, scale, 1f);
            
            yield return null;
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

                int currentSecond = Mathf.CeilToInt(timeRemaining);
                if (currentSecond != lastSecondBeep && currentSecond > 0)
                {
                    lastSecondBeep = currentSecond;
                    
                    // Play a harsh mechanical beep sound
                    alarmAudioSource.PlayOneShot(ProceduralAudioGen.GenerateClick(1500f, 0.15f));
                    alarmAudioSource.PlayOneShot(ProceduralAudioGen.GenerateErrorBuzz(250f, 0.2f));

                    // Flash the screen red on the exact beat
                    if (ScreenEffectManager.Instance != null) ScreenEffectManager.Instance.TriggerFlash(new Color(1f, 0f, 0f, 0.4f), 0.2f);
                    
                    // Violent camera shake on the beat
                    if (camFollow != null) camFollow.TriggerShake(0.4f, 0.6f);
                }
            }
        }

        // Continuous Camera Shake (increases in intensity when under 10 seconds)
        float continuousShake = timeRemaining <= 10f ? 0.35f : 0.2f;
        if (camFollow != null) camFollow.TriggerShake(0.2f, continuousShake); 

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
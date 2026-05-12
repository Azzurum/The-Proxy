using UnityEngine;

public class AudioEngine : MonoBehaviour
{
    public static AudioEngine Instance;

    [Header("Audio Players")]
    public AudioSource heartbeatSource;
    public AudioSource staticSource;

    [Header("Heartbeat Settings")]
    public float calmBPM = 50f;   // Beats per minute when idle
    public float panicBPM = 160f; // Beats per minute at max stress
    
    private float nextBeatTime;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Update()
    {
        HandleDynamicHeartbeat();
    }

    void HandleDynamicHeartbeat()
    {
        if (heartbeatSource == null || heartbeatSource.clip == null) return;

        // Ask the StressSystem how stressed we currently are
        float currentStress = StressSystem.Instance.currentStress;

        // Calculate how fast the heart should be beating right now
        float currentBPM = Mathf.Lerp(calmBPM, panicBPM, currentStress);
        float timeBetweenBeats = 60f / currentBPM; // Convert BPM to seconds

        // Is it time for the next beat?
        if (Time.time >= nextBeatTime)
        {
            // Make the heartbeat louder as stress goes up
            heartbeatSource.volume = Mathf.Lerp(0.2f, 1.0f, currentStress);
            
            // Play the *thump*
            heartbeatSource.PlayOneShot(heartbeatSource.clip);
            
            // Set the timer for the next *thump*
            nextBeatTime = Time.time + timeBetweenBeats;
        }
    }

    // Other scripts can call this to trigger a scare
    public void PlayGlitchStatic()
    {
        if (staticSource != null && staticSource.clip != null)
        {
            // Randomize the pitch slightly so the static sounds different every time
            staticSource.pitch = Random.Range(0.8f, 1.2f);
            staticSource.PlayOneShot(staticSource.clip);
        }
    }
}
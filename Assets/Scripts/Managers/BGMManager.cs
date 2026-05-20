using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance;

    [Header("Audio Component")]
    public AudioSource bgmSource;

    [Header("Level Soundtracks")]
    public AudioClip introBGM;
    public AudioClip level1BGM;
    public AudioClip level2BGM;
    public AudioClip level3BGM;
    public AudioClip chaseBGM;

    [Header("Settings")]
    [Range(0f, 1f)] public float musicVolume = 0.5f;

    void Awake()
    {
        // Singleton Pattern: Ensure only ONE of these ever exists, and keep it alive across all scenes!
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (bgmSource == null) bgmSource = GetComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.volume = musicVolume;
            bgmSource.playOnAwake = false;

            // Listen for whenever a new scene finishes loading
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            // Destroy duplicates if we revisit a scene with another manager
            Destroy(gameObject);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AudioClip trackToPlay = null;
        string sName = scene.name.ToLower();

        // Determine which track to play based on the scene name
        if (sName.Contains("intro") || sName.Contains("menu")) trackToPlay = introBGM;
        else if (sName == "level_1") trackToPlay = level1BGM;
        else if (sName == "level_2") trackToPlay = level2BGM;
        else if (sName == "level_3") trackToPlay = level3BGM;
        else if (sName == "level_escape") trackToPlay = chaseBGM;
        else if (sName.Contains("ending") || sName.Contains("credit"))
        {
            // Stop the BGM entirely during endings so your cinematic audio can shine!
            bgmSource.Stop();
            return; 
        }

        // Only swap and play if the track is different from what's currently playing
        if (trackToPlay != null)
        {
            if (bgmSource.clip != trackToPlay)
            {
                bgmSource.clip = trackToPlay;
                bgmSource.Play();
            }
        }
        else
        {
            // Failsafe: If no track is assigned, stop the old music so it doesn't bleed into this level!
            bgmSource.Stop();
            bgmSource.clip = null;
        }
    }

    public void StopMusic()
    {
        if (bgmSource != null) bgmSource.Stop();
    }
}
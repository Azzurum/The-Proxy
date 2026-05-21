using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// A persistent singleton that controls background music transitions across different game scenes.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance;

    [Header("Audio Component")]
    [Tooltip("The main audio source used to play background music.")]
    public AudioSource bgmSource;

    [Header("Level Soundtracks")]
    [Tooltip("Track played during the intro and main menu.")]
    public AudioClip introBGM;
    [Tooltip("Track played on Deck 1.")]
    public AudioClip level1BGM;
    [Tooltip("Track played on Deck 2.")]
    public AudioClip level2BGM;
    [Tooltip("Track played on Deck 3.")]
    public AudioClip level3BGM;
    [Tooltip("Track played during the final escape sequence.")]
    public AudioClip chaseBGM;

    [Header("Settings")]
    [Tooltip("Global volume multiplier for the background music.")]
    [Range(0f, 1f)] public float musicVolume = 0.5f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (bgmSource == null) bgmSource = GetComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.volume = musicVolume;
            bgmSource.playOnAwake = false;

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from scene events to prevent memory leaks when returning to the editor or destroying the manager.
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AudioClip trackToPlay = null;
        string sName = scene.name.ToLower();

        if (sName.Contains("intro") || sName.Contains("menu")) trackToPlay = introBGM;
        else if (sName == "level_1") trackToPlay = level1BGM;
        else if (sName == "level_2") trackToPlay = level2BGM;
        else if (sName == "level_3") trackToPlay = level3BGM;
        else if (sName == "level_escape") trackToPlay = chaseBGM;
        else if (sName.Contains("ending") || sName.Contains("credit"))
        {
            bgmSource.Stop();
            return; 
        }

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
            bgmSource.Stop();
            bgmSource.clip = null;
        }
    }

    /// <summary>
    /// Immediately halts the currently playing background music.
    /// </summary>
    public void StopMusic()
    {
        if (bgmSource != null) bgmSource.Stop();
    }
}
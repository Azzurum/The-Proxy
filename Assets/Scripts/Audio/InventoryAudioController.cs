using UnityEngine;

/// <summary>
/// Listens for inventory events and plays corresponding audio clips.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class InventoryAudioController : MonoBehaviour
{
    [Header("Audio SFX")]
    [Tooltip("The sound played each time a new row of corruption is added to the grid.")]
    public AudioClip sfxCorruptionAdded;
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnCorruptionTick += PlayCorruptionSound;
        }
    }

    void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnCorruptionTick -= PlayCorruptionSound;
        }
    }

    private void PlayCorruptionSound()
    {
        if (audioSource != null)
        {
            audioSource.PlayOneShot(sfxCorruptionAdded != null ? sfxCorruptionAdded : ProceduralAudioGen.GenerateErrorBuzz(90f, 0.8f));
        }
    }
}
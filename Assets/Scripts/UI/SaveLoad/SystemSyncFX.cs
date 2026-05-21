using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Coordinates visual and auditory effects triggered during Save, Load, and Purge sequences.
/// </summary>
public class SystemSyncFX : MonoBehaviour
{
    public static SystemSyncFX Instance;

    [Header("UI References")]
    [Tooltip("Full screen overlay used for bright screen flashes.")]
    public Image flashbangImage;

    [Header("Execution Colors")]
    public Color colorLoad = new Color(0f, 0.94f, 1f, 1f);   
    public Color colorSave = new Color(1f, 1f, 1f, 1f);      
    public Color colorPurge = new Color(1f, 0f, 0.23f, 1f);  

    [Header("Audio")]
    [Tooltip("Source for playing execution sounds.")]
    public AudioSource fxSource;
    public AudioClip sfxCrush;
    public AudioClip sfxLoad;
    public AudioClip sfxSave;
    public AudioClip sfxPurge;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        if (flashbangImage != null) flashbangImage.color = new Color(0,0,0,0);
    }

    /// <summary>
    /// Plays the mechanical crush sound effect used when collapsing save slots.
    /// </summary>
    public void PlayCrushSound()
    {
        if (fxSource != null && sfxCrush != null) fxSource.PlayOneShot(sfxCrush);
    }

    /// <summary>
    /// Initiates a screen flash and sound effect matching the specified action type.
    /// </summary>
    public void ExecuteFlash(string type)
    {
        Color targetColor = colorSave;
        AudioClip targetClip = sfxSave;

        switch (type.ToUpper())
        {
            case "LOAD": targetColor = colorLoad; targetClip = sfxLoad; break;
            case "PURGE": targetColor = colorPurge; targetClip = sfxPurge; break;
        }

        if (fxSource != null && targetClip != null) fxSource.PlayOneShot(targetClip);
        
        StopAllCoroutines();
        StartCoroutine(FlashRoutine(targetColor));
    }

    private IEnumerator FlashRoutine(Color flashColor)
    {
        if (flashbangImage == null) yield break;

        flashbangImage.color = flashColor;

        float timer = 0;
        float duration = 0.6f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / duration;
            
            Color c = flashColor;
            c.a = Mathf.Lerp(1f, 0f, t);
            flashbangImage.color = c;
            
            yield return null;
        }

        flashbangImage.color = new Color(0,0,0,0);
    }
}
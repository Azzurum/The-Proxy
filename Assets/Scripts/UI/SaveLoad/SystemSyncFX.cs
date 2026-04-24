using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SystemSyncFX : MonoBehaviour
{
    public static SystemSyncFX Instance;

    [Header("UI References")]
    public Image flashbangImage;

    [Header("Execution Colors")]
    public Color colorLoad = new Color(0f, 0.94f, 1f, 1f);   // Aether Cyan
    public Color colorSave = new Color(1f, 1f, 1f, 1f);      // Blinding White
    public Color colorPurge = new Color(1f, 0f, 0.23f, 1f);  // Mother Red

    [Header("Audio (Optional)")]
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

    public void PlayCrushSound()
    {
        if (fxSource != null && sfxCrush != null) fxSource.PlayOneShot(sfxCrush);
    }

    public void ExecuteFlash(string type)
    {
        Color targetColor = colorSave;
        AudioClip targetClip = sfxSave;

        if (type == "LOAD") { targetColor = colorLoad; targetClip = sfxLoad; }
        else if (type == "PURGE") { targetColor = colorPurge; targetClip = sfxPurge; }

        if (fxSource != null && targetClip != null) fxSource.PlayOneShot(targetClip);
        
        StopAllCoroutines();
        StartCoroutine(FlashRoutine(targetColor));
    }

    private IEnumerator FlashRoutine(Color flashColor)
    {
        if (flashbangImage == null) yield break;

        // Instantly snap to full color
        flashbangImage.color = flashColor;

        // Smoothly fade back to invisible
        float timer = 0;
        float duration = 0.6f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / duration;
            
            // Fade out the alpha
            Color c = flashColor;
            c.a = Mathf.Lerp(1f, 0f, t);
            flashbangImage.color = c;
            
            yield return null;
        }

        flashbangImage.color = new Color(0,0,0,0);
    }
}
using UnityEngine;
using System.Collections;

/// <summary>
/// Controls the countdown, activation, and burnout phases of the decoy tool.
/// </summary>
public class DecoyDevice : MonoBehaviour
{
    [Header("Decoy Settings")]
    [Tooltip("Time in seconds before the decoy activates its noise flare.")]
    public float fuseTime = 7f;
    [Tooltip("Duration in seconds that the decoy produces noise to distract the enemy.")]
    public float noiseDuration = 10f;
    
    [Header("Visuals")]
    [Tooltip("Light component to visually indicate the decoy's current phase.")]
    public UnityEngine.Rendering.Universal.Light2D decoyLight; 

    [Header("Audio SFX")]
    [Tooltip("Audio source for playing decoy sound effects.")]
    public AudioSource audioSource;
    [Tooltip("Sound played during the silent countdown phase.")]
    public AudioClip sfxTick;
    [Tooltip("Sound played continuously during the active noise phase.")]
    public AudioClip sfxBlast;

    private ProxyAI[] _cachedProxies;

    private void Start()
    {
        gameObject.tag = "Untagged";
        foreach (Transform child in transform) child.gameObject.tag = "Untagged";

        if (TryGetComponent<PhysicalItem>(out var pi)) Destroy(pi);
        else
        {
            var childPi = GetComponentInChildren<PhysicalItem>();
            if (childPi != null) Destroy(childPi);
        }

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        
        _cachedProxies = FindObjectsByType<ProxyAI>(FindObjectsInactive.Exclude);

        StartCoroutine(DecoySequence());
    }

    private IEnumerator DecoySequence()
    {
        if (decoyLight != null) decoyLight.color = Color.yellow;
        
        for (int i = 0; i < fuseTime; i++)
        {
            if (audioSource != null) audioSource.PlayOneShot(sfxTick != null ? sfxTick : ProceduralAudioGen.GenerateBeep(1200f, 0.05f));
            yield return new WaitForSeconds(1f);
        }

        if (decoyLight != null) decoyLight.color = Color.cyan;
        
        if (audioSource != null) audioSource.PlayOneShot(sfxBlast != null ? sfxBlast : ProceduralAudioGen.GenerateStaticGlitch(1.5f));
        
        float timer = 0;
        while (timer < noiseDuration)
        {
            foreach (var proxy in _cachedProxies)
            {
                if (proxy != null) 
                {
                    proxy.DistractToLocation(transform.position, noiseDuration - timer);
                    proxy.OnCombatAction(transform.position); 
                }
            }
            yield return new WaitForSeconds(1f);
            timer += 1f;
        }

        Destroy(gameObject);
    }
}
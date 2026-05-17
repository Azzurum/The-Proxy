using UnityEngine;
using System.Collections;

public class DecoyDevice : MonoBehaviour
{
    public float fuseTime = 7f;
    public float noiseDuration = 10f;
    
    // Add a light component to the prefab and assign it here so it flashes!
    public UnityEngine.Rendering.Universal.Light2D decoyLight; 

    [Header("Audio SFX")]
    public AudioSource audioSource;
    public AudioClip sfxTick;
    public AudioClip sfxBlast;

    void Start()
    {
        // Prevent player from picking up the active decoy
        gameObject.tag = "Untagged";
        foreach (Transform child in transform) child.gameObject.tag = "Untagged";

        PhysicalItem pi = GetComponent<PhysicalItem>();
        if (pi == null) pi = GetComponentInChildren<PhysicalItem>();

        if (pi != null) Destroy(pi);

        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        StartCoroutine(DecoySequence());
    }

    private IEnumerator DecoySequence()
    {
        // Phase 1: Silent Countdown
        if (decoyLight != null) decoyLight.color = Color.yellow;
        
        for (int i = 0; i < fuseTime; i++)
        {
            if (audioSource != null) audioSource.PlayOneShot(sfxTick != null ? sfxTick : ProceduralAudioGen.GenerateBeep(1200f, 0.05f));
            yield return new WaitForSeconds(1f);
        }

        // Phase 2: Activation (The Electromagnetic Noise)
        Debug.Log("DECOY ACTIVATED: Broadcasting massive electromagnetic flare!");
        if (decoyLight != null) decoyLight.color = Color.cyan;
        
        if (audioSource != null) audioSource.PlayOneShot(sfxBlast != null ? sfxBlast : ProceduralAudioGen.GenerateStaticGlitch(1.5f));
        
        ProxyAI[] allProxies = FindObjectsByType<ProxyAI>(FindObjectsInactive.Exclude);
        
        float timer = 0;
        while (timer < noiseDuration)
        {
            foreach (var proxy in allProxies)
            {
                if (proxy != null) 
                {
                    proxy.DistractToLocation(transform.position, noiseDuration - timer);
                    proxy.OnCombatAction(transform.position); // Ensure proxy detects the flare
                }
            }
            yield return new WaitForSeconds(1f);
            timer += 1f;
        }

        // Phase 3: Burnout
        Debug.Log("DECOY BURNOUT: Signal lost.");
        Destroy(gameObject); // The battery dies and it disappears/breaks
    }
}
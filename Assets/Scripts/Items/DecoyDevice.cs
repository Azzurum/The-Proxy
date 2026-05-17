using UnityEngine;
using System.Collections;

public class DecoyDevice : MonoBehaviour
{
    public float fuseTime = 7f;
    public float noiseDuration = 10f;
    
    // Add a light component to the prefab and assign it here so it flashes!
    public UnityEngine.Rendering.Universal.Light2D decoyLight; 

    void Start()
    {
        StartCoroutine(DecoySequence());
    }

    private IEnumerator DecoySequence()
    {
        // Phase 1: Silent Countdown
        if (decoyLight != null) decoyLight.color = Color.yellow;
        yield return new WaitForSeconds(fuseTime);

        // Phase 2: Activation (The Electromagnetic Noise)
        Debug.Log("DECOY ACTIVATED: Broadcasting massive electromagnetic flare!");
        if (decoyLight != null) decoyLight.color = Color.cyan;
        
        ProxyAI[] allProxies = FindObjectsByType<ProxyAI>(FindObjectsSortMode.None);
        foreach (var proxy in allProxies)
        {
            proxy.DistractToLocation(transform.position, noiseDuration);
        }

        // Phase 3: Burnout
        yield return new WaitForSeconds(noiseDuration);
        
        Debug.Log("DECOY BURNOUT: Signal lost.");
        Destroy(gameObject); // The battery dies and it disappears/breaks
    }
}
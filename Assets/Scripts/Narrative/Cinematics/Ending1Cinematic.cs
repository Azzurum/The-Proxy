using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

/// <summary>
/// Coordinates the 'Kernel Panic' bad ending cinematic, animating Kaelen's assimilation and transitioning to the end scene.
/// </summary>
public class Ending1Cinematic : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player's Transform, required for forced movement.")]
    public Transform kaelenTransform;
    [Tooltip("The Animator component on the player character.")]
    public Animator kaelenAnimator;
    [Tooltip("A full-screen UI Image used to fade the screen to and from black.")]
    public Image fadeOverlay;
    [Tooltip("The UI Text element for displaying MOTHER's final dialogue.")]
    public TextMeshProUGUI motherSubtitle;

    [Header("Settings")]
    [Tooltip("Speed multiplier for the player's forced, puppet-like walk.")]
    public float walkSpeed = 1.5f;
    [Tooltip("The name of the scene to load upon completion (usually credits).")]
    public string nextSceneName = "UI_Credits"; 

    private void Start()
    {
        if (kaelenTransform != null)
        {
            PlayerController pc = kaelenTransform.GetComponent<PlayerController>();
            if (pc != null) pc.enabled = false;
        }

        StartCoroutine(PlayEnding());
    }

    private IEnumerator PlayEnding()
    {
        Time.timeScale = 1f;

        AudioSource audio = gameObject.AddComponent<AudioSource>();
        AudioSource heartbeat = gameObject.AddComponent<AudioSource>();
        heartbeat.clip = ProceduralAudioGen.GenerateHeartbeat(1.2f);
        heartbeat.loop = true;
        heartbeat.Play();

        AudioSource drone = gameObject.AddComponent<AudioSource>();
        drone.clip = ProceduralAudioGen.GenerateHiss(2f); 
        drone.pitch = 0.2f; 
        drone.loop = true;
        drone.Play();

        if (fadeOverlay == null)
        {
            Debug.LogError("<color=red>[ERROR]</color> The Fade Overlay is missing! Drag the FadeOverlay Image into the CinematicDirector script in the Inspector.");
        }
        else
        {
            fadeOverlay.gameObject.SetActive(true);
        }

        Canvas parentCanvas = fadeOverlay.canvas;
        if (parentCanvas != null)
        {
            parentCanvas.overrideSorting = true;
            parentCanvas.sortingOrder = 100;
        }

        if (fadeOverlay != null) fadeOverlay.color = Color.black;
        if (motherSubtitle != null) motherSubtitle.text = "";

        yield return new WaitForSeconds(3f);

        float timer = 0;
        if (fadeOverlay != null)
        {
            while (timer < 2f) 
            {
                timer += Time.deltaTime;
                fadeOverlay.color = new Color(0, 0, 0, 1f - (timer / 2f));
                yield return null;
            }
        }

        yield return new WaitForSeconds(0.2f); 

        audio.PlayOneShot(ProceduralAudioGen.GenerateErrorBuzz(150f, 0.8f));
        StartCoroutine(ShakeCamera(0.8f, 0.4f));
        
        for (int i = 0; i < 10; i++)
        {
            if (kaelenAnimator != null)
            {
                kaelenAnimator.SetFloat("Horizontal", Random.value > 0.5f ? 1f : -1f);
                kaelenAnimator.SetFloat("Vertical", Random.value > 0.5f ? 1f : -1f);
            }
            yield return new WaitForSeconds(0.08f);
        }

        if (kaelenAnimator != null)
        {
            kaelenAnimator.SetFloat("Horizontal", 0f);
            kaelenAnimator.SetFloat("Speed", 0.4f); 
            kaelenAnimator.SetFloat("Vertical", 1f); 
        }

        timer = 0;
        float nextTwitchTime = Random.Range(0.5f, 1.2f);

        while (timer < 3.5f)
        {
            timer += Time.deltaTime;

            if (timer >= nextTwitchTime)
            {
                audio.PlayOneShot(ProceduralAudioGen.GenerateErrorBuzz(150f, 0.1f));
                StartCoroutine(ShakeCamera(0.15f, 0.2f));
                
                if (kaelenAnimator != null)
                {
                    kaelenAnimator.SetFloat("Horizontal", Random.value > 0.5f ? 1f : -1f);
                    kaelenAnimator.SetFloat("Vertical", Random.value > 0.5f ? 1f : -1f);
                }
                
                yield return new WaitForSeconds(0.1f);
                timer += 0.1f; 
                
                if (kaelenAnimator != null) kaelenAnimator.SetFloat("Horizontal", 0f);
                if (kaelenAnimator != null) kaelenAnimator.SetFloat("Vertical", 1f); 
                
                nextTwitchTime = timer + Random.Range(0.6f, 1.5f);
            }
            else
            {
                if (kaelenTransform != null) kaelenTransform.position += Vector3.up * (walkSpeed * 0.5f) * Time.deltaTime;
            }
            yield return null;
        }

        if (kaelenAnimator != null) kaelenAnimator.SetFloat("Speed", 0f);
        yield return new WaitForSeconds(1f);

        string finalText = "> THE VESSEL IS SECURED.";
        if (motherSubtitle != null)
        {
            motherSubtitle.color = new Color(1f, 0f, 0.23f, 1f); 
            motherSubtitle.text = "";
            for (int i = 0; i < finalText.Length; i++)
            {
                motherSubtitle.text = finalText.Substring(0, i + 1);
                audio.PlayOneShot(ProceduralAudioGen.GenerateClick(800f, 0.02f));
                yield return new WaitForSeconds(0.08f);
            }
        }
        
        yield return new WaitForSeconds(1.5f);

        if (kaelenAnimator != null) 
        {
            kaelenAnimator.SetFloat("Horizontal", 0f);
            kaelenAnimator.SetFloat("Vertical", -1f); 
        }
        
        audio.Stop();
        heartbeat.Stop();
        drone.Stop();
        
        yield return new WaitForSeconds(0.3f);

        audio.PlayOneShot(ProceduralAudioGen.GenerateStaticGlitch(2.0f));
        audio.PlayOneShot(ProceduralAudioGen.GenerateAlarm(1.0f));
        StartCoroutine(ShakeCamera(1.5f, 1.2f)); 

        if (fadeOverlay != null) fadeOverlay.color = new Color(1f, 0f, 0f, 0.7f); 

        float lungeTimer = 0;
        Vector3 startScale = kaelenTransform != null ? kaelenTransform.localScale : Vector3.one;
        Vector3 targetScale = startScale * 6f; 

        while (lungeTimer < 0.15f)
        {
            lungeTimer += Time.deltaTime;
            if (kaelenTransform != null) kaelenTransform.localScale = Vector3.Lerp(startScale, targetScale, lungeTimer / 0.15f);
            yield return null;
        }

        if (fadeOverlay != null) fadeOverlay.color = Color.black;
        if (motherSubtitle != null) motherSubtitle.text = "";
        heartbeat.Stop();
        drone.Stop();

        yield return new WaitForSeconds(3.5f);
        
        // BULLETPROOF FIX: Automatically correct the scene name if the Unity Inspector is holding onto the old value!
        if (nextSceneName == "credits_scene") nextSceneName = "UI_Credits";
        
        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator ShakeCamera(float duration, float magnitude)
    {
        if (Camera.main == null) yield break;
        
        Transform camTransform = Camera.main.transform;
        Vector3 originalPos = camTransform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            camTransform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        camTransform.localPosition = originalPos;
    }
}
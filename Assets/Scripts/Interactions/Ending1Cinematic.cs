using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class Ending1Cinematic : MonoBehaviour
{
    [Header("References")]
    public Transform kaelenTransform;
    public Animator kaelenAnimator;
    public Image fadeOverlay;
    public TextMeshProUGUI motherSubtitle;

    [Header("Settings")]
    public float walkSpeed = 1.5f;
    public string nextSceneName = "credits_scene"; // Change to your credits or main menu scene

    private void Start()
    {
        // Lock Kaelen's player controller if it exists so the player can't move him
        if (kaelenTransform != null)
        {
            PlayerController pc = kaelenTransform.GetComponent<PlayerController>();
            if (pc != null) pc.enabled = false;
        }

        StartCoroutine(PlayEnding());
    }

    private IEnumerator PlayEnding()
    {
        // Failsafe: Ensure Time is running normally just in case we loaded from a paused state
        Time.timeScale = 1f;

        // Setup Audio
        AudioSource audio = gameObject.AddComponent<AudioSource>();
        AudioSource heartbeat = gameObject.AddComponent<AudioSource>();
        heartbeat.clip = ProceduralAudioGen.GenerateHeartbeat(1.2f);
        heartbeat.loop = true;
        heartbeat.Play();

        // Add a low, oppressive drone
        AudioSource drone = gameObject.AddComponent<AudioSource>();
        drone.clip = ProceduralAudioGen.GenerateHiss(2f); 
        drone.pitch = 0.2f; // Pitch it down into a dark rumble
        drone.loop = true;
        drone.Play();

        // Failsafe: Warn the developer if the UI is missing, and force it on if it's hidden
        if (fadeOverlay == null)
        {
            Debug.LogError("<color=red>[ERROR]</color> The Fade Overlay is missing! Drag the FadeOverlay Image into the CinematicDirector script in the Inspector.");
        }
        else
        {
            fadeOverlay.gameObject.SetActive(true);
        }

        // Failsafe: Force the Canvas to render over absolutely everything (since Kaelen is 15)
        Canvas parentCanvas = fadeOverlay.canvas;
        if (parentCanvas != null)
        {
            parentCanvas.overrideSorting = true;
            parentCanvas.sortingOrder = 100;
        }

        // 1. Start completely black
        if (fadeOverlay != null) fadeOverlay.color = Color.black;
        if (motherSubtitle != null) motherSubtitle.text = "";

        // Wait a beat before fading in
        yield return new WaitForSeconds(3f);

        // 2. Fade in a bit faster
        float timer = 0;
        if (fadeOverlay != null)
        {
            while (timer < 2f) // Changed from 4 seconds to 2 seconds
            {
                timer += Time.deltaTime;
                fadeOverlay.color = new Color(0, 0, 0, 1f - (timer / 2f));
                yield return null;
            }
        }

        yield return new WaitForSeconds(0.2f); // Shortened the awkward pause

        // 3. The Seizure (Violent loss of bodily autonomy)
        audio.PlayOneShot(ProceduralAudioGen.GenerateErrorBuzz(150f, 0.8f));
        StartCoroutine(ShakeCamera(0.8f, 0.4f));
        
        for (int i = 0; i < 10; i++)
        {
            if (kaelenAnimator != null)
            {
                // Snap violently in random directions
                kaelenAnimator.SetFloat("Horizontal", Random.value > 0.5f ? 1f : -1f);
                kaelenAnimator.SetFloat("Vertical", Random.value > 0.5f ? 1f : -1f);
            }
            yield return new WaitForSeconds(0.08f);
        }

        // 4. The Puppet Walk
        if (kaelenAnimator != null)
        {
            kaelenAnimator.SetFloat("Horizontal", 0f);
            kaelenAnimator.SetFloat("Speed", 0.4f); // Half speed for a stiff, zombie-like shuffle
            kaelenAnimator.SetFloat("Vertical", 1f); // Force him to walk Upwards
        }

        // Move Kaelen up slowly to look like a stiff, controlled walk
        timer = 0;
        float nextTwitchTime = Random.Range(0.5f, 1.2f);

        while (timer < 3.5f)
        {
            timer += Time.deltaTime;

            // Occasional random twitch while walking
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
                timer += 0.1f; // Account for the paused time
                
                if (kaelenAnimator != null) kaelenAnimator.SetFloat("Horizontal", 0f);
                if (kaelenAnimator != null) kaelenAnimator.SetFloat("Vertical", 1f); // Restore upward walk
                
                nextTwitchTime = timer + Random.Range(0.6f, 1.5f);
            }
            else
            {
                if (kaelenTransform != null) kaelenTransform.position += Vector3.up * (walkSpeed * 0.5f) * Time.deltaTime;
            }
            yield return null;
        }

        // Stop walking
        if (kaelenAnimator != null) kaelenAnimator.SetFloat("Speed", 0f);
        yield return new WaitForSeconds(1f);

        // 5. MOTHER speaks (Typewriter Effect)
        string finalText = "> THE VESSEL IS SECURED.";
        if (motherSubtitle != null)
        {
            motherSubtitle.color = new Color(1f, 0f, 0.23f, 1f); // Mother Red
            for (int i = 0; i < finalText.Length; i++)
            {
                motherSubtitle.text += finalText[i];
                audio.PlayOneShot(ProceduralAudioGen.GenerateClick(800f, 0.02f));
                yield return new WaitForSeconds(0.08f);
            }
        }
        
        yield return new WaitForSeconds(1.5f);

        // 6. The Final Snap & The Drop
        if (kaelenAnimator != null) 
        {
            kaelenAnimator.SetFloat("Horizontal", 0f);
            kaelenAnimator.SetFloat("Vertical", -1f); // Instantly snap to face DOWN at the camera
        }
        
        // THE DROP: Cut all audio to create a terrifying vacuum of silence
        audio.Stop();
        heartbeat.Stop();
        drone.Stop();
        
        yield return new WaitForSeconds(0.3f);

        // 7. The Lunge (Jumpscare)
        audio.PlayOneShot(ProceduralAudioGen.GenerateStaticGlitch(2.0f));
        audio.PlayOneShot(ProceduralAudioGen.GenerateAlarm(1.0f));
        StartCoroutine(ShakeCamera(1.5f, 1.2f)); // Much more violent camera shake

        if (fadeOverlay != null) fadeOverlay.color = new Color(1f, 0f, 0f, 0.7f); // Violent Red Flash

        // Invade the player's personal space by scaling the sprite massively toward the screen
        float lungeTimer = 0;
        Vector3 startScale = kaelenTransform != null ? kaelenTransform.localScale : Vector3.one;
        Vector3 targetScale = startScale * 6f; 

        while (lungeTimer < 0.15f)
        {
            lungeTimer += Time.deltaTime;
            if (kaelenTransform != null) kaelenTransform.localScale = Vector3.Lerp(startScale, targetScale, lungeTimer / 0.15f);
            yield return null;
        }

        // Hard Cut to Black
        if (fadeOverlay != null) fadeOverlay.color = Color.black;
        if (motherSubtitle != null) motherSubtitle.text = "";
        heartbeat.Stop();
        drone.Stop();

        yield return new WaitForSeconds(3.5f);
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
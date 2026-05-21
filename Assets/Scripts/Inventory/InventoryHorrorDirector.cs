using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

/// <summary>
/// Controls the generation of auditory and visual hallucinations based on the player's corruption level.
/// </summary>
public class InventoryHorrorDirector : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The slider representing the time until the next system shock (corruption tick).")]
    public Slider systemShockProgressBar;

    [Header("Hallucination System")]
    private float hallucinationCooldown = 15f;
    private float jumpscareCooldown = 45f; 
    private bool _hasHallucinations = false;

    [Header("Audio")]
    private AudioSource audioSource;

    private ProxyAI _cachedProxy;
    private CameraFollow _cachedCamera;
    private Transform _playerTransform;

    void Start()
    {
        if (systemShockProgressBar == null)
        {
            Slider[] sliders = FindObjectsByType<Slider>(FindObjectsInactive.Include);
            foreach (var slider in sliders)
            {
                string sName = slider.name.ToLower();
                if (sName.Contains("shock") || sName.Contains("corruption"))
                {
                    systemShockProgressBar = slider;
                    break;
                }
            }
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        _cachedProxy = FindAnyObjectByType<ProxyAI>();
        _cachedCamera = FindAnyObjectByType<CameraFollow>();
        _playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        StartCoroutine(InitializeSubscriptions());
    }

    private IEnumerator InitializeSubscriptions()
    {
        yield return new WaitUntil(() => InventoryManager.Instance != null);

        InventoryManager.Instance.OnHealthStateChanged += HandleHealthUpdated;
        InventoryManager.Instance.OnCorruptionTick += HandleCorruptionTick;

        HandleHealthUpdated(1f - InventoryManager.Instance.GetCorruptionPercentage());
    }

    void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnHealthStateChanged -= HandleHealthUpdated;
            InventoryManager.Instance.OnCorruptionTick -= HandleCorruptionTick;
        }
    }

    private void HandleHealthUpdated(float healthPercentage)
    {
        _hasHallucinations = healthPercentage <= 0.5f;
    }

    private void HandleCorruptionTick()
    {
    }

    void Update()
    {
        if (InventoryManager.Instance == null || !InventoryManager.Instance.isSystemActive) return;

        if (systemShockProgressBar != null)
        {
            systemShockProgressBar.value = 1f - (InventoryManager.Instance.shockTimer / InventoryManager.Instance.shockInterval);
        }

        if (_hasHallucinations && !DialogueEngine.isDialogueActive)
        {
            if (jumpscareCooldown > 0f) jumpscareCooldown -= Time.deltaTime;
            hallucinationCooldown -= Time.deltaTime;

            if (hallucinationCooldown <= 0f)
            {
                TriggerRandomHallucination();
                
                float baseCooldown = InventoryManager.Instance.GetCorruptionPercentage() >= 0.8f ? 8f : 22f;
                hallucinationCooldown = baseCooldown + UnityEngine.Random.Range(-4f, 4f);
            }
        }

        if (Input.GetKeyDown(KeyCode.H) && !DialogueEngine.isDialogueActive) 
        {
            StartCoroutine(FakeProxyJumpscareRoutine());
        }
    }

    private void TriggerRandomHallucination()
    {
        float rand = UnityEngine.Random.value;
        
        if (rand < 0.25f && jumpscareCooldown <= 0f) 
        {
            jumpscareCooldown = UnityEngine.Random.Range(80f, 110f); 
            StartCoroutine(FakeProxyJumpscareRoutine());
        }
        else if (audioSource != null) 
        {
            float audioRand = UnityEngine.Random.value;
            if (audioRand < 0.30f) StartCoroutine(ApproachingFootstepsRoutine()); 
            else if (audioRand < 0.60f) PlaySpatialAudio(ProceduralAudioGen.GenerateWhisper()); 
            else if (audioRand < 0.80f) audioSource.PlayOneShot(ProceduralAudioGen.GenerateFootstep(0.4f)); 
            else if (audioRand < 0.90f) audioSource.PlayOneShot(ProceduralAudioGen.GenerateStaticGlitch(0.3f)); 
            else audioSource.PlayOneShot(ProceduralAudioGen.GenerateHiss(0.5f)); 
        }
    }

    private IEnumerator ApproachingFootstepsRoutine()
    {
        if (_playerTransform == null) yield break;

        GameObject tempAudioObj = new GameObject("Hallucination_Footsteps");
        Vector2 startDir = UnityEngine.Random.insideUnitCircle.normalized;
        float distance = 12f;
        tempAudioObj.transform.position = _playerTransform.position + (Vector3)(startDir * distance);

        AudioSource tempSource = tempAudioObj.AddComponent<AudioSource>();
        tempSource.spatialBlend = 0.8f; 
        tempSource.rolloffMode = AudioRolloffMode.Linear;
        tempSource.minDistance = 2f;
        tempSource.maxDistance = 15f;

        int steps = UnityEngine.Random.Range(5, 9);
        float timeBetweenSteps = 0.6f;

        for (int i = 0; i < steps; i++)
        {
            if (_playerTransform == null || tempAudioObj == null) break;

            float progress = (float)i / (steps - 1);
            distance = Mathf.Lerp(12f, 2f, progress);
            
            tempAudioObj.transform.position = _playerTransform.position + (Vector3)(startDir * distance);

            tempSource.volume = Mathf.Lerp(0.2f, 1.0f, progress) * ProceduralAudioGen.globalVolume;
            tempSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
            
            tempSource.PlayOneShot(ProceduralAudioGen.GenerateFootstep(0.4f));

            yield return new WaitForSeconds(timeBetweenSteps + UnityEngine.Random.Range(-0.05f, 0.05f));
        }

        Destroy(tempAudioObj, 1.0f);
    }

    private void PlaySpatialAudio(AudioClip clip)
    {
        if (_playerTransform == null) return;

        GameObject tempAudioObj = new GameObject("Hallucination_Whisper");
        Vector2 sideDir = UnityEngine.Random.value > 0.5f ? Vector2.right : Vector2.left;
        tempAudioObj.transform.position = _playerTransform.position + (Vector3)(sideDir * 1.5f);

        AudioSource tempSource = tempAudioObj.AddComponent<AudioSource>();
        tempSource.spatialBlend = 0.8f; 
        tempSource.rolloffMode = AudioRolloffMode.Linear;
        tempSource.minDistance = 1f;
        tempSource.maxDistance = 5f;
        tempSource.volume = ProceduralAudioGen.globalVolume;
        tempSource.clip = clip;
        tempSource.Play();

        Destroy(tempAudioObj, clip.length + 0.1f);
    }

    private IEnumerator FakeProxyJumpscareRoutine()
    {
        if (_playerTransform == null || _cachedProxy == null) yield break;

        Vector3 bestSpawnPos = Vector3.zero;
        float maxClearDist = 0f;
        float targetSpawnDist = 15f; 

        for (int i = 0; i < 16; i++) 
        {
            Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
            float currentClearDist = targetSpawnDist;

            RaycastHit2D[] hits = Physics2D.RaycastAll(_playerTransform.position, dir, targetSpawnDist);
            foreach (var hit in hits)
            {
                if (!hit.collider.isTrigger && !hit.collider.CompareTag("Player") && !hit.collider.CompareTag("Interactable") && !hit.collider.CompareTag("MasterKey"))
                {
                    if (hit.distance < currentClearDist) currentClearDist = hit.distance;
                }
            }

            if (currentClearDist > maxClearDist)
            {
                maxClearDist = currentClearDist;
                bestSpawnPos = _playerTransform.position + (Vector3)(dir * (currentClearDist - 1f)); 
            }
        }

        if (maxClearDist < 8f)
        {
            if (audioSource != null) audioSource.PlayOneShot(ProceduralAudioGen.GenerateStaticGlitch(0.5f));
            yield break;
        }

        GameObject fakeProxy = Instantiate(_cachedProxy.gameObject, bestSpawnPos, Quaternion.identity);
        fakeProxy.name = "Hallucination_Proxy";

        ProxyAI fakeAI = fakeProxy.GetComponent<ProxyAI>();
        if (fakeAI != null) { fakeAI.enabled = false; Destroy(fakeAI); }
        
        Rigidbody2D rb = fakeProxy.GetComponent<Rigidbody2D>();
        if (rb != null) Destroy(rb);
        
        foreach (var col in fakeProxy.GetComponentsInChildren<Collider2D>()) Destroy(col);

        Animator fakeAnim = fakeProxy.GetComponent<Animator>();
        SpriteRenderer fakeRenderer = fakeProxy.GetComponent<SpriteRenderer>();
        if (fakeRenderer != null) fakeRenderer.sortingOrder = 10;

        if (audioSource != null) audioSource.PlayOneShot(ProceduralAudioGen.GenerateStaticGlitch(1.0f));

        float speed = 25f; 
        
        if (fakeAnim != null)
        {
            fakeAnim.SetFloat("Speed", speed);
            fakeAnim.speed = Mathf.Max(1f, speed / _cachedProxy.baseSpeed); 
        }

        while (fakeProxy != null && _playerTransform != null && Vector3.Distance(fakeProxy.transform.position, _playerTransform.position) > 1.0f)
        {
            fakeProxy.transform.position = Vector3.MoveTowards(fakeProxy.transform.position, _playerTransform.position, speed * Time.deltaTime);
            
            Vector2 dir = (_playerTransform.position - fakeProxy.transform.position).normalized;
            if (fakeAnim != null && fakeRenderer != null)
            {
                if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
                {
                    fakeAnim.SetFloat("Direction", 1f); 
                    fakeRenderer.flipX = dir.x < 0;
                }
                else
                {
                    if (dir.y > 0)
                    {
                        fakeAnim.SetFloat("Direction", 1f); 
                        if (dir.x < -0.01f) fakeRenderer.flipX = true;
                        else if (dir.x > 0.01f) fakeRenderer.flipX = false;
                    }
                    else
                    {
                        fakeAnim.SetFloat("Direction", 0f); 
                        fakeRenderer.flipX = false;
                    }
                }
            }
            yield return null;
        }

        if (fakeProxy != null)
        {
            if (audioSource != null) audioSource.PlayOneShot(ProceduralAudioGen.GenerateErrorBuzz(100f, 0.2f));
            
            if (_cachedCamera != null) _cachedCamera.TriggerShake(0.3f, 0.5f);
            
            if (ScreenEffectManager.Instance != null) ScreenEffectManager.Instance.TriggerFlash(Color.black, 0.15f);
            
            Destroy(fakeProxy);
        }
    }
}
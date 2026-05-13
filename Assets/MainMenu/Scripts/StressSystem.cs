using UnityEngine;
using UnityEngine.UI;

public class StressSystem : MonoBehaviour
{
    public static StressSystem Instance; 

    [Header("Stress Levels")]
    public float currentStress = 0.1f;
    public float targetStress = 0.1f;
    public float stressLerpSpeed = 2f;

    [Header("Proxy Twitch Settings")]
    public RectTransform proxyTransform;
    public Image proxyImage; 
    public Sprite[] twitchSprites; 
    
    // Controls how long the creepy frame stays on screen
    public float glitchHoldTime = 0.3f; 
    
    private Vector2 proxyStartPos;
    private Sprite defaultSprite; 
    private float twitchTimer;
    private float spasmTimer; 

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (proxyTransform != null) proxyStartPos = proxyTransform.anchoredPosition;
        if (proxyImage != null) defaultSprite = proxyImage.sprite;
    }

    void Update()
    {
        currentStress = Mathf.Lerp(currentStress, targetStress, Time.deltaTime * stressLerpSpeed);
        HandleFNAFTwitch();
    }

    public void SetTargetStress(float newStress)
    {
        targetStress = newStress;
    }

    void HandleFNAFTwitch()
    {
        if (proxyTransform == null || proxyImage == null) return;

        twitchTimer -= Time.deltaTime;
        
        if (twitchTimer <= 0)
        {
            float maxDelay = Mathf.Lerp(4f, 0.2f, currentStress); 
            twitchTimer = Random.Range(maxDelay * 0.5f, maxDelay);

            float twitchAmount = Mathf.Lerp(2f, 30f, currentStress);
            Vector2 randomOffset = new Vector2(
                Random.Range(-twitchAmount, twitchAmount), 
                Random.Range(-twitchAmount, twitchAmount)
            );
            
            proxyTransform.anchoredPosition = proxyStartPos + randomOffset;

            if (twitchSprites.Length > 0)
            {
                proxyImage.sprite = twitchSprites[Random.Range(0, twitchSprites.Length)];
                if (AudioEngine.Instance != null) AudioEngine.Instance.PlayGlitchStatic();
                
                // 👇 USE THE NEW VARIABLE HERE
                spasmTimer = glitchHoldTime; 
            }
        }
        else
        {
            proxyTransform.anchoredPosition = Vector2.Lerp(
                proxyTransform.anchoredPosition, 
                proxyStartPos, 
                Time.deltaTime * 15f
            );

            spasmTimer -= Time.deltaTime;
            if (spasmTimer <= 0)
            {
                proxyImage.sprite = defaultSprite;
            }
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BiometricsDisplay : MonoBehaviour
{
    public Image faceImage;
    public Sprite[] faceSprites; // 0: Normal, 1: Stressed, 2: Panicked
    public TextMeshProUGUI bpmText;
    public Animator ekgAnimator; // For EKG line animation
    public TextMeshProUGUI motherLogText;

    private ProxyAI proxyAI;

    void Start()
    {
        proxyAI = FindFirstObjectByType<ProxyAI>();
    }

    void Update()
    {
        if (proxyAI == null) return;

        float distance = Vector3.Distance(transform.position, proxyAI.transform.position);
        int threatLevel = distance > 50f ? 0 : distance > 20f ? 1 : 2;

        faceImage.sprite = faceSprites[threatLevel];
        bpmText.text = $"{60 + (threatLevel * 40)} BPM";
        ekgAnimator.speed = 1f + (threatLevel * 0.5f);

        // Update MOTHER log based on threat
        string[] logs = {
            "> Connection stable. All systems nominal.<br>> <strong>Awaiting directives, Custodian.</strong>",
            "> Proximity alert. Massive anomaly detected.<br>> <strong>Minimize digitization noise immediately.</strong>",
            "> <strong>CRITICAL: ENTITY IS IN VISUAL RANGE.</strong><br>> Host survival probability: 4.2%"
        };
        motherLogText.text = logs[threatLevel];
    }
}
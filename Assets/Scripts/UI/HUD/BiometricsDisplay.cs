using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Coordinates visual biological readouts (EKG, Face, Text) reacting to the proxy's proximity.
/// </summary>
public class BiometricsDisplay : MonoBehaviour
{
    [Header("Display References")]
    [Tooltip("The image component displaying the biological state face.")]
    public Image faceImage;
    [Tooltip("Array of state sprites: 0: Normal, 1: Stressed, 2: Panicked.")]
    public Sprite[] faceSprites; 
    public TextMeshProUGUI bpmText;
    public Animator ekgAnimator; 
    public TextMeshProUGUI motherLogText;

    private ProxyAI _proxyAI;
    private Transform _playerTransform;

    // Cached readonly array prevents string instantiation every frame.
    private readonly string[] _biometricLogs = {
        "> Connection stable. All systems nominal.<br>> <strong>Awaiting directives, Custodian.</strong>",
        "> Proximity alert. Massive anomaly detected.<br>> <strong>Minimize digitization noise immediately.</strong>",
        "> <strong>CRITICAL: ENTITY IS IN VISUAL RANGE.</strong><br>> Host survival probability: 4.2%"
    };

    private void Start()
    {
        _proxyAI = FindAnyObjectByType<ProxyAI>();
        if (GameObject.FindGameObjectWithTag("Player").TryGetComponent(out Transform pt))
        {
            _playerTransform = pt;
        }
    }

    private void Update()
    {
        if (_proxyAI == null || _playerTransform == null) return;

        float distance = Vector3.Distance(_playerTransform.position, _proxyAI.transform.position);
        int threatLevel = distance > 50f ? 0 : distance > 20f ? 1 : 2;

        faceImage.sprite = faceSprites[threatLevel];
        bpmText.SetText("{0} BPM", 60 + (threatLevel * 40));
        ekgAnimator.speed = 1f + (threatLevel * 0.5f);

        motherLogText.text = _biometricLogs[threatLevel];
    }
}
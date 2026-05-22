using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manipulates standard UI boundary colors based on the proximity of the hunting entity.
/// </summary>
public class ThreatUIManager : MonoBehaviour
{
    [Header("Visual Connections")]
    [Tooltip("Target UI Image elements (such as the visor rim) to dynamically tint.")]
    public Image[] uiElementsToColor; 
    
    [Header("Theme Definitions")]
    public Color lowThreatColor = Color.cyan;
    public Color mediumThreatColor = new Color(1f, 0.67f, 0f); 
    public Color highThreatColor = Color.red;

    private ProxyAI _proxyAI;
    private Transform _playerTransform;

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
        Color targetColor;

        if (distance > 50f) targetColor = lowThreatColor;
        else if (distance > 20f) targetColor = mediumThreatColor;
        else targetColor = highThreatColor;

        foreach (var img in uiElementsToColor)
        {
            img.color = Color.Lerp(img.color, targetColor, Time.deltaTime * 2f);
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Coordinates the visualization of the player's physical sprint/exertion status.
/// </summary>
public class UI_KineticCapacitor : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;         
    public TextMeshProUGUI readoutText;       
    [Tooltip("The parent container holding the individual segment blocks to be toggled.")]
    public Transform segmentContainer;        
    public Image leftBorderAccent;            
    public Outline[] slotHighlights;          

    [Header("Settings")]
    [Tooltip("Invert the visualization so that blocks disappear from the right instead of the left.")]
    public bool reverseDrainDirection = false; 

    [Header("Colors")]
    public Color normalColor = new Color(1f, 0.66f, 0f);   
    public Color criticalColor = new Color(1f, 0f, 0.2f);  
    public Color emptyColor = new Color(0.1f, 0.1f, 0.1f); 

    private Image[] _segments;
    private PlayerController _playerController;

    private void Start()
    {
        if (segmentContainer != null)
        {
            int childCount = segmentContainer.childCount;
            _segments = new Image[childCount];
            for (int i = 0; i < childCount; i++)
            {
                _segments[i] = segmentContainer.GetChild(i).GetComponent<Image>();
            }
        }
        
        _playerController = FindAnyObjectByType<PlayerController>();
    }

    private void Update()
    {
        if (_playerController == null || _segments == null || _segments.Length == 0) return;

        float rawExertion = _playerController.SprintMeterThreshold > 0f ? _playerController.SprintMeter / _playerController.SprintMeterThreshold : 0f;
        
        float percent = 1f - Mathf.Clamp01(rawExertion);

        Color currentColor = percent > 0.25f ? normalColor : criticalColor;

        if (readoutText != null) 
        {
            readoutText.color = currentColor;
            readoutText.SetText("{0}<size=50%>%</size>", Mathf.RoundToInt(percent * 100));
        }
        if (titleText != null) titleText.color = currentColor;
        if (leftBorderAccent != null) leftBorderAccent.color = currentColor;

        foreach (Outline outline in slotHighlights)
        {
            if (outline != null) outline.effectColor = currentColor;
        }

        int activeBlocks = Mathf.CeilToInt(percent * _segments.Length);

        for (int i = 0; i < _segments.Length; i++)
        {
            if (_segments[i] == null) continue;

            bool isLit = false;

            if (reverseDrainDirection)
            {
                isLit = i >= (_segments.Length - activeBlocks);
            }
            else
            {
                isLit = i < activeBlocks;
            }

            _segments[i].color = isLit ? currentColor : emptyColor;
        }
    }
}
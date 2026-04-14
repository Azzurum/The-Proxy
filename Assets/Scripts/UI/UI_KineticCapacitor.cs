using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_KineticCapacitor : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;         // SYS.O2 // KINETIC
    public TextMeshProUGUI readoutText;       // 100%
    public Transform segmentContainer;        // Holds the 20 blocks
    public Image leftBorderAccent;            // The thick left accent line
    public Outline[] slotHighlights;          // The glowing borders on the hotbar

    [Header("Settings")]
    public bool reverseDrainDirection = false; // Check to flip visual drain direction

    [Header("Colors")]
    public Color normalColor = new Color(1f, 0.66f, 0f);   // Amber
    public Color criticalColor = new Color(1f, 0f, 0.2f);  // Red
    public Color emptyColor = new Color(0.1f, 0.1f, 0.1f); // Dark Grey

    private Image[] segments;
    private PlayerController playerController;

    void Start()
    {
        // Safely gather ONLY the child segments
        if (segmentContainer != null)
        {
            int childCount = segmentContainer.childCount;
            segments = new Image[childCount];
            for (int i = 0; i < childCount; i++)
            {
                segments[i] = segmentContainer.GetChild(i).GetComponent<Image>();
            }
        }
        
        playerController = FindAnyObjectByType<PlayerController>();
    }

    void Update()
    {
        if (playerController == null || segments == null || segments.Length == 0) return;

        // THE FIX: Get the raw sprint value (0.0 to 1.0)
        float rawExertion = playerController.SprintMeter / playerController.SprintMeterThreshold;
        
        // Invert it! Now 0 exertion = 100% energy.
        float percent = 1f - Mathf.Clamp01(rawExertion);

        // 1. Determine the unified System Color
        Color currentColor = percent > 0.25f ? normalColor : criticalColor;

        // 2. Apply color to Texts and Border
        if (readoutText != null) 
        {
            readoutText.color = currentColor;
            readoutText.text = Mathf.RoundToInt(percent * 100).ToString() + "<size=50%>%</size>";
        }
        if (titleText != null) titleText.color = currentColor;
        if (leftBorderAccent != null) leftBorderAccent.color = currentColor;

        // 3. Apply color to all Hotbar Slot Outlines
        foreach (Outline outline in slotHighlights)
        {
            if (outline != null) outline.effectColor = currentColor;
        }

        // 4. Figure out exactly how many blocks should be lit up
        int activeBlocks = Mathf.CeilToInt(percent * segments.Length);

        // 5. Loop through and paint the blocks based on direction
        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i] == null) continue;

            bool isLit = false;

            if (reverseDrainDirection)
            {
                // Drains from Left to Right (Solid blocks shift to the right)
                isLit = i >= (segments.Length - activeBlocks);
            }
            else
            {
                // Default: Drains from Right to Left (Solid blocks stay glued to the left)
                isLit = i < activeBlocks;
            }

            segments[i].color = isLit ? currentColor : emptyColor;
        }
    }
}
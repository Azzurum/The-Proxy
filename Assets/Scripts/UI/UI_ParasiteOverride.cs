using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_ParasiteOverride : MonoBehaviour
{
    public static UI_ParasiteOverride Instance;

    [Header("Core Engine")]
    public float cycleTime = 60.0f;        
    private float timeLeft;
    
    public int currentStacks = 0;
    public int maxStacks = 10;             

    [Header("Text References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI stackCounterText;
    public TextMeshProUGUI timerReadoutText;
    public TextMeshProUGUI[] slotKeyTexts;

    [Header("Graphic References")]
    public Image timerFill;
    public Outline timerBackgroundOutline;
    public Transform stackContainer;
    public Outline[] slotHighlights;
    public Image rightBorderAccent;        // NEW: The thick right accent line

    [Header("Theme Colors")]
    public Color stableColor = new Color(0f, 1f, 0.8f);
    public Color warningColor = new Color(1f, 0.66f, 0f);
    public Color criticalColor = new Color(1f, 0f, 0.2f);
    public Color emptyBlockColor = new Color(0.04f, 0.04f, 0.04f);

    private Image[] stackBlocks;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        timeLeft = cycleTime;

        if (stackContainer != null)
        {
            int childCount = stackContainer.childCount;
            stackBlocks = new Image[childCount];
            for (int i = 0; i < childCount; i++)
            {
                stackBlocks[i] = stackContainer.GetChild(i).GetComponent<Image>();
            }
        }
    }

    void Update()
    {
        // The UI now ONLY handles the visual draining animation. 
        if (currentStacks < maxStacks)
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft < 0) timeLeft = 0; 
        }

        // Timer Visuals
        if (timerFill != null) timerFill.fillAmount = timeLeft / cycleTime; 
        if (timerReadoutText != null) timerReadoutText.text = timeLeft.ToString("F2") + "s"; 

        // Stack Text
        if (stackCounterText != null) stackCounterText.text = $"[ {currentStacks:D2} / 10 ]";

        // Theme Colors
        Color currentTheme = stableColor;
        string currentTitle = "MOTHER // ASSIMILATING";

        if (currentStacks >= 4 && currentStacks < 8)
        {
            currentTheme = warningColor;
            currentTitle = "MOTHER // WARNING";
        }
        else if (currentStacks >= 8)
        {
            currentTheme = criticalColor;
            currentTitle = "OVERRIDE IMMINENT";
        }

        if (titleText != null)
        {
            titleText.text = currentTitle;
            titleText.color = currentTheme;
        }

        ApplyThemeColor(currentTheme);
    }

    // NEW EXACT SYNC METHOD: The Inventory will use this to force the UI to match reality
    public void SetExactStacks(int physicalItemCount)
    {
        // Every 10 physical items = 1 UI block
        currentStacks = physicalItemCount / 10; 

        if (currentStacks > maxStacks) currentStacks = maxStacks;
        
        // Snap the visual timer back to 60s so Kaelen has time before the next natural tick
        timeLeft = cycleTime; 
    }

    private void ApplyThemeColor(Color theme)
    {
        if (stackCounterText != null) stackCounterText.color = theme;
        if (timerReadoutText != null) timerReadoutText.color = theme;
        if (timerFill != null) timerFill.color = theme;
        if (timerBackgroundOutline != null) timerBackgroundOutline.effectColor = theme;
        
        // NEW: Apply the color to the Right Border
        if (rightBorderAccent != null) rightBorderAccent.color = theme;

        foreach (var txt in slotKeyTexts) { if (txt != null) txt.color = theme; }
        foreach (var outline in slotHighlights) { if (outline != null) outline.effectColor = theme; }

        if (stackBlocks != null)
        {
            for (int i = 0; i < stackBlocks.Length; i++)
            {
                if (stackBlocks[i] == null) continue;
                stackBlocks[i].color = (i < currentStacks) ? theme : emptyBlockColor;
            }
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Coordinates the visualization of the active corruption tier and next shock countdown.
/// </summary>
public class UI_ParasiteOverride : MonoBehaviour
{
    public static UI_ParasiteOverride Instance;

    [Header("Core Engine")]
    [HideInInspector] public float cycleTime = 60.0f;        
    private float _timeLeft;
    
    public int currentStacks = 0;
    [HideInInspector] public int maxStacks = 10;             

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
    public Image rightBorderAccent;

    [Header("Theme Colors")]
    public Color stableColor = new Color(0f, 1f, 0.8f);
    public Color warningColor = new Color(1f, 0.66f, 0f);
    public Color criticalColor = new Color(1f, 0f, 0.2f);
    public Color emptyBlockColor = new Color(0.04f, 0.04f, 0.04f);

    private Image[] _stackBlocks;
    private InventoryManager _invManager;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        _invManager = FindAnyObjectByType<InventoryManager>();
        _timeLeft = cycleTime;
        if (stackContainer != null)
        {
            int childCount = stackContainer.childCount;
            _stackBlocks = new Image[childCount];
            for (int i = 0; i < childCount; i++)
            {
                _stackBlocks[i] = stackContainer.GetChild(i).GetComponent<Image>();
            }
        }
    }

    private void Update()
    {
        if (_invManager != null)
        {
            _timeLeft = currentStacks < maxStacks ? _invManager.shockTimer : 0f;
            cycleTime = _invManager.shockInterval;
        }

        if (timerFill != null) timerFill.fillAmount = _timeLeft / cycleTime;
        if (timerReadoutText != null) timerReadoutText.SetText("{0:F2}s", _timeLeft); 

        if (stackCounterText != null) stackCounterText.SetText("[ {0:D2} / 10 ]", currentStacks);

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

    /// <summary>
    /// Directly calibrates the UI visual logic to mirror physical reality based on the passed physical entity blocks.
    /// </summary>
    public void SetExactStacks(int physicalItemCount)
    {
        int newStacks = physicalItemCount / 10; 
        if (newStacks > maxStacks) newStacks = maxStacks;
        
        if (newStacks != currentStacks)
        {
            currentStacks = newStacks;
        }
    }

    private void ApplyThemeColor(Color theme)
    {
        if (stackCounterText != null) stackCounterText.color = theme;
        if (timerReadoutText != null) timerReadoutText.color = theme;
        if (timerFill != null) timerFill.color = theme;
        if (timerBackgroundOutline != null) timerBackgroundOutline.effectColor = theme;
        if (rightBorderAccent != null) rightBorderAccent.color = theme;

        foreach (var txt in slotKeyTexts) { if (txt != null) txt.color = theme; }
        foreach (var outline in slotHighlights) { if (outline != null) outline.effectColor = theme; }

        if (_stackBlocks != null)
        {
            for (int i = 0; i < _stackBlocks.Length; i++)
            {
                if (_stackBlocks[i] == null) continue;
                _stackBlocks[i].color = (i < currentStacks) ? theme : emptyBlockColor;
            }
        }
    }

    public float GetCurrentTimer()
    {
        return _timeLeft;
    }

    /// <summary>
    /// Restores the visual and logical progression of the corruption timer based on serialized save data.
    /// </summary>
    public void LoadParasiteData(int savedStacks, float savedTimer)
    {
        currentStacks = savedStacks;
        _timeLeft = savedTimer;

        if (_invManager == null) _invManager = FindAnyObjectByType<InventoryManager>();
        if (_invManager != null) _invManager.shockTimer = savedTimer;

        if (timerFill != null) timerFill.fillAmount = _timeLeft / cycleTime;
        if (timerReadoutText != null) timerReadoutText.SetText("{0:F2}s", _timeLeft);
        if (stackCounterText != null) stackCounterText.SetText("[ {0:D2} / 10 ]", currentStacks);
    }
}
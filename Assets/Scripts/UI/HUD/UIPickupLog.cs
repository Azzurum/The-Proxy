using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Coordinates an on-screen console logging system intended to verify pickups and action completions.
/// </summary>
public class UIPickupLog : MonoBehaviour
{
    public static UIPickupLog Instance;

    [Header("UI Reference")]
    public TextMeshProUGUI logText;

    [Header("Settings")]
    public float messageDuration = 4f;
    public float fadeDuration = 1f;

    [Header("Audio SFX")]
    public AudioSource audioSource;
    public AudioClip sfxBlip;

    private struct LogMessage
    {
        public string text;
        public float timeLeft;
    }

    private List<LogMessage> _activeLogs = new List<LogMessage>();
    private StringBuilder _sb = new StringBuilder();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else 
        {
            Destroy(gameObject);
        }
        
        if (logText == null) logText = GetComponent<TextMeshProUGUI>();
        if (logText == null) logText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (logText != null) logText.text = "";

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    /// <summary>
    /// Dispatches a formatted notification entry to the log renderer and attempts to play an affirmative blip.
    /// </summary>
    public void AddLog(string itemName, Color highlightColor, string prefix = "Acquired")
    {
        string hexColor = ColorUtility.ToHtmlStringRGB(highlightColor);
        string newMsg = $"> {prefix}: <color=#{hexColor}>{itemName}</color>";
        
        _activeLogs.Add(new LogMessage { text = newMsg, timeLeft = messageDuration });
        
        if (_activeLogs.Count > 5) _activeLogs.RemoveAt(0);

        if (audioSource != null)
        {
            audioSource.PlayOneShot(sfxBlip != null ? sfxBlip : ProceduralAudioGen.GenerateClick(1200f, 0.03f));
        }
    }

    private void Update()
    {
        if (_activeLogs.Count == 0 && (logText != null && logText.text != ""))
        {
            logText.text = "";
            return;
        }

        for (int i = _activeLogs.Count - 1; i >= 0; i--)
        {
            var log = _activeLogs[i];
            log.timeLeft -= Time.deltaTime;
            _activeLogs[i] = log;

            if (_activeLogs[i].timeLeft <= 0) _activeLogs.RemoveAt(i);
        }

        UpdateTextDisplay();
    }

    private void UpdateTextDisplay()
    {
        if (logText == null) return;

        if (!logText.gameObject.activeSelf) logText.gameObject.SetActive(true);

        _sb.Clear();
        foreach (var log in _activeLogs)
        {
            float alpha = 1f;
            if (log.timeLeft < fadeDuration) alpha = log.timeLeft / fadeDuration;
            
            int alphaHex = Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255);
            
            _sb.Append($"<alpha=#{alphaHex:X2}>{log.text}\n");
        }
        logText.text = _sb.ToString();
    }
}
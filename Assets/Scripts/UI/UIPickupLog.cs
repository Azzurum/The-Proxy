using UnityEngine;
using TMPro;
using System.Collections.Generic;

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

    private class LogMessage
    {
        public string text;
        public float timeLeft;
    }

    private List<LogMessage> activeLogs = new List<LogMessage>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        if (logText != null) logText.text = "";

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void AddLog(string itemName, Color highlightColor, string prefix = "Acquired")
    {
        string hexColor = ColorUtility.ToHtmlStringRGB(highlightColor);
        string newMsg = $"> {prefix}: <color=#{hexColor}>{itemName}</color>";
        
        activeLogs.Add(new LogMessage { text = newMsg, timeLeft = messageDuration });
        
        // Limit to max 5 logs at once so it doesn't cover the screen
        if (activeLogs.Count > 5) activeLogs.RemoveAt(0);

        // Play a very fast, soft procedural UI blip
        if (audioSource != null)
        {
            audioSource.PlayOneShot(sfxBlip != null ? sfxBlip : ProceduralAudioGen.GenerateClick(1200f, 0.03f));
        }
    }

    void Update()
    {
        if (activeLogs.Count == 0 && (logText != null && logText.text != ""))
        {
            logText.text = "";
            return;
        }

        for (int i = activeLogs.Count - 1; i >= 0; i--)
        {
            activeLogs[i].timeLeft -= Time.deltaTime;
            if (activeLogs[i].timeLeft <= 0) activeLogs.RemoveAt(i);
        }

        UpdateTextDisplay();
    }

    private void UpdateTextDisplay()
    {
        if (logText == null) return;

        string fullText = "";
        foreach (var log in activeLogs)
        {
            // Calculate alpha (fade out smoothly during the last 'fadeDuration' seconds)
            float alpha = 1f;
            if (log.timeLeft < fadeDuration) alpha = log.timeLeft / fadeDuration;
            
            int alphaHex = Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255);
            
            // Uses TextMeshPro's alpha tag to fade specific lines of text
            fullText += $"<alpha=#{alphaHex:X2}>{log.text}\n";
        }
        logText.text = fullText;
    }
}
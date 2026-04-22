using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class SystemLogger : MonoBehaviour
{
    public static SystemLogger Instance;

    [Header("UI Reference")]
    public TextMeshProUGUI logText;

    [Header("Settings")]
    public int maxLines = 15;

    private List<string> logLines = new List<string>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (logText != null) logText.text = "";
        Log("VIRTUAL FILE SYSTEM MOUNTED.", "#00F0FF"); // Cyan
        Log("AWAITING USER DIRECTIVE...", "#5E7382");   // Gray
    }

    public void Log(string message, string hexColor = "#FFAA00") // Defaults to Amber
    {
        if (logText == null) return;

        // Generate a real-time timestamp like [09:54:40.920]
        string time = System.DateTime.Now.ToString("HH:mm:ss.fff");
        
        // Wrap the message in Unity's Rich Text color tags
        string newLine = $"<color={hexColor}>[{time}] {message}</color>";

        logLines.Add(newLine);

        // If we exceed our max lines, remove the oldest one at the top
        if (logLines.Count > maxLines)
        {
            logLines.RemoveAt(0);
        }

        // Combine all lines with a line-break (\n) and push to the UI
        logText.text = string.Join("\n", logLines);
    }
}
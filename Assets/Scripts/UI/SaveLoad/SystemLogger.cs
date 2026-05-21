using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// A global UI logger that outputs system events and narrative cues to an on-screen console.
/// </summary>
public class SystemLogger : MonoBehaviour
{
    public static SystemLogger Instance;

    [Header("UI Reference")]
    [Tooltip("The TextMeshPro component used to render the log history.")]
    public TextMeshProUGUI logText;

    [Header("Settings")]
    [Tooltip("The maximum number of distinct log lines visible before older entries are pushed out.")]
    public int maxLines = 15;

    private Queue<string> _logLines = new Queue<string>();
    private StringBuilder _stringBuilder = new StringBuilder();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (logText != null) logText.text = "";
        Log("VIRTUAL FILE SYSTEM MOUNTED.", "#00F0FF"); 
        Log("AWAITING USER DIRECTIVE...", "#5E7382");   
    }

    /// <summary>
    /// Pushes a new formatted message into the system console.
    /// </summary>
    public void Log(string message, string hexColor = "#FFAA00") 
    {
        if (logText == null) return;

        string time = System.DateTime.Now.ToString("HH:mm:ss.fff");
        string newLine = $"<color={hexColor}>[{time}] {message}</color>";

        _logLines.Enqueue(newLine);
        if (_logLines.Count > maxLines) _logLines.Dequeue();

        _stringBuilder.Clear();
        foreach (string line in _logLines)
        {
            _stringBuilder.AppendLine(line);
        }

        logText.text = _stringBuilder.ToString();
    }
}
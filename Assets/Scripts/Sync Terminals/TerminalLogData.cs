using UnityEngine;

/// <summary>
/// A ScriptableObject containing the text, metadata, and pagination for an in-game terminal log.
/// </summary>
[CreateAssetMenu(fileName = "New Terminal Log", menuName = "Wayfarer OS/Terminal Log")]
public class TerminalLogData : ScriptableObject
{
    [Header("Metadata")]
    [Tooltip("The display title of the log entry.")]
    public string logTitle = "SYSTEM LOG // ";
    [Tooltip("The designated author of the log.")]
    public string author = "UNKNOWN";
    [Tooltip("The chronological day number of the entry.")]
    public string dayNumber = "00";

    [Header("Content")]
    [Tooltip("The body of the log, separated into individual pages for terminal rendering.")]
    [TextArea(5, 12)] 
    public string[] logPages; 
}
using UnityEngine;

[CreateAssetMenu(fileName = "New Terminal Log", menuName = "Wayfarer OS/Terminal Log")]
public class TerminalLogData : ScriptableObject
{
    [Header("Metadata")]
    public string logTitle = "SYSTEM LOG // ";
    public string author = "UNKNOWN";
    public string dayNumber = "00";

    [Header("Content (Split long text into multiple pages)")]
    [TextArea(5, 12)] 
    public string[] logPages; // Changed from a single string to an Array!
}
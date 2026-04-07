using UnityEngine;
using UnityEngine.UI;

public class ThreatUIManager : MonoBehaviour
{
    public Image[] uiElementsToColor; // Assign visor borders, etc.
    public Color lowThreatColor = Color.cyan;
    public Color mediumThreatColor = new Color(1f, 0.67f, 0f); // Amber
    public Color highThreatColor = Color.red;

    private ProxyAI proxyAI;

    void Start()
    {
        proxyAI = FindFirstObjectByType<ProxyAI>();
    }

    void Update()
    {
        if (proxyAI == null) return;

        float distance = Vector3.Distance(transform.position, proxyAI.transform.position);
        Color targetColor;

        if (distance > 50f) targetColor = lowThreatColor;
        else if (distance > 20f) targetColor = mediumThreatColor;
        else targetColor = highThreatColor;

        foreach (var img in uiElementsToColor)
        {
            img.color = Color.Lerp(img.color, targetColor, Time.deltaTime * 2f);
        }
    }
}
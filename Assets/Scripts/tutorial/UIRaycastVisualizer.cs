using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class UIRaycastVisualizer : MonoBehaviour
{
    void Update()
    {
        // Find every single graphic element inside this UI structure
        foreach (Graphic g in FindObjectsByType<Graphic>(FindObjectsInactive.Include))
        {
            // If it is actively blocking your mouse clicks...
            if (g.raycastTarget && g.enabled && g.gameObject.activeInHierarchy)
            {
                // And it has a completely invisible alpha channel color...
                if (g.color.a == 0f)
                {
                    // Tint it bright warning magenta so your eyes can see it!
                    g.color = new Color(1f, 0f, 1f, 0.35f);
                }
            }
        }
    }
}
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor utility to identify invisible UI elements that are inappropriately blocking raycasts.
/// </summary>
[ExecuteInEditMode]
public class UIRaycastVisualizer : MonoBehaviour
{
    private float _scanTimer = 0f;

    private void Update()
    {
        if (Application.isPlaying) 
        {
            Destroy(this);
            return;
        }

        _scanTimer -= Time.deltaTime;
        if (_scanTimer > 0f) return;
        _scanTimer = 1f; 

        foreach (Graphic g in FindObjectsByType<Graphic>(FindObjectsInactive.Include))
        {
            if (g.raycastTarget && g.enabled && g.gameObject.activeInHierarchy)
            {
                if (g.color.a == 0f)
                {
                    g.color = new Color(1f, 0f, 1f, 0.35f);
                }
            }
        }
    }
}
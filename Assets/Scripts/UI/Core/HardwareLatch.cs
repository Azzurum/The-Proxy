using UnityEngine;

/// <summary>
/// A simple bridging script used by physical UI buttons to trigger tray animations.
/// </summary>
public class HardwareLatch : MonoBehaviour
{
    [Tooltip("The animator responsible for sliding this latch's corresponding tray.")]
    public UITrayAnimator trayAnimator;

    /// <summary>
    /// Hooks into the OnClick event of the hardware UI element.
    /// </summary>
    public void OnLatchClicked()
    {
        if (trayAnimator != null) trayAnimator.ToggleTray();
    }
}
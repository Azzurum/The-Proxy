using UnityEngine;

public class HardwareLatch : MonoBehaviour
{
    public UITrayAnimator trayAnimator;

    public void OnLatchClicked()
    {
        trayAnimator.ToggleTray();
    }
}
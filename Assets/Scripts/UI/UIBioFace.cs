using UnityEngine;
using UnityEngine.UI;

public class UIBioFace : MonoBehaviour
{
    [Header("Face UI References")]
    public Image faceImage;

    [Header("Kaelen's Face States")]
    public Sprite healthyFace;
    public Sprite hurtFace;
    public Sprite criticalFace;
    public Sprite deadFace;

    void Start()
    {
        // Force the healthy face to appear by default when the game starts!
        if (faceImage != null && healthyFace != null)
        {
            faceImage.sprite = healthyFace;
            faceImage.color = Color.white; // Ensure it isn't transparent
        }
    }

    // Call this method whenever Kaelen takes damage or heals!
    public void UpdateFace(float healthPercentage)
    {
        if (faceImage == null) return;

        if (healthPercentage <= 0f)
            faceImage.sprite = deadFace;
        else if (healthPercentage < 0.3f) // Under 30% health
            faceImage.sprite = criticalFace;
        else if (healthPercentage < 0.7f) // Under 70% health
            faceImage.sprite = hurtFace;
        else
            faceImage.sprite = healthyFace; // 70% to 100% health
    }
}
using System.Collections;
using UnityEngine;
using TMPro;

public class CinematicTypewriter : MonoBehaviour
{
    [Header("Text Objects")]
    public TextMeshProUGUI spaceText;  // Drag Galaxy_Text here
    public TextMeshProUGUI hangarText; // Drag Hangar_Text here

    [Header("Settings")]
    public float typingSpeed = 0.04f;

    private string fullSpaceText;
    private string fullHangarText;

    void Start()
    {
        // Cache and clear Space text
        if (spaceText != null)
        {
            fullSpaceText = spaceText.text;
            spaceText.text = "";
        }

        // Cache and clear Hangar text
        if (hangarText != null)
        {
            fullHangarText = hangarText.text;
            hangarText.text = "";
        }
    }

    // Trigger for the opening space text
    public void StartSpaceTyping()
    {
        if (spaceText != null)
        {
            StopAllCoroutines();
            StartCoroutine(TypeText(spaceText, fullSpaceText));
        }
    }

    // Trigger for the closing hangar text
    public void StartHangarTyping()
    {
        if (hangarText != null)
        {
            StopAllCoroutines();
            StartCoroutine(TypeText(hangarText, fullHangarText));
        }
    }

    IEnumerator TypeText(TextMeshProUGUI targetComponent, string targetText)
    {
        targetComponent.text = "";
        foreach (char letter in targetText.ToCharArray())
        {
            targetComponent.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
    }
}
using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Handles the zero-allocation typewriter reveal effect for cinematic text elements.
/// </summary>
public class CinematicTypewriter : MonoBehaviour
{
    [Header("Text Objects")]
    [Tooltip("The text component for the introductory space sequence.")]
    public TextMeshProUGUI spaceText;  
    [Tooltip("The text component for the hangar arrival sequence.")]
    public TextMeshProUGUI hangarText; 

    [Header("Settings")]
    [Tooltip("Delay in seconds between revealing each character.")]
    public float typingSpeed = 0.04f;

    private string _fullSpaceText;
    private string _fullHangarText;
    private WaitForSeconds _typingDelay;

    private void Awake()
    {
        _typingDelay = new WaitForSeconds(typingSpeed);

        if (spaceText != null)
        {
            _fullSpaceText = spaceText.text;
            spaceText.text = ""; // Force clear to prevent double-vision ghosting
            spaceText.maxVisibleCharacters = 0;
        }

        if (hangarText != null)
        {
            _fullHangarText = hangarText.text;
            hangarText.text = ""; // Force clear to prevent double-vision ghosting
            hangarText.maxVisibleCharacters = 0;
        }
    }

    /// <summary>
    /// Begins the typewriter effect on the space cinematic text.
    /// </summary>
    public void StartSpaceTyping()
    {
        if (spaceText != null)
        {
            StopAllCoroutines();
            StartCoroutine(TypeTextRoutine(spaceText, _fullSpaceText));
        }
    }

    /// <summary>
    /// Begins the typewriter effect on the hangar cinematic text.
    /// </summary>
    public void StartHangarTyping()
    {
        if (hangarText != null)
        {
            StopAllCoroutines();
            StartCoroutine(TypeTextRoutine(hangarText, _fullHangarText));
        }
    }

    private IEnumerator TypeTextRoutine(TextMeshProUGUI targetComponent, string targetText)
    {
        targetComponent.text = targetText;
        targetComponent.ForceMeshUpdate();
        
        int totalCharacters = targetComponent.textInfo.characterCount;

        for (int i = 0; i <= totalCharacters; i++)
        {
            targetComponent.maxVisibleCharacters = i;
            yield return _typingDelay;
        }
    }
}
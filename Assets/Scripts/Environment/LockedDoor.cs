using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider2D), typeof(AudioSource))]
public class LockedDoor : MonoBehaviour
{
    [Header("Lock Settings")]
    [Tooltip("The Item ID of the key required to open this door (e.g., 'KEY-MSTR-3').")]
    public string requiredItemID;

    [Header("Components")]
    [Tooltip("The Animator component that has the 'Open' trigger.")]
    public Animator doorAnimator;
    [Tooltip("The exact case-sensitive name of the Trigger parameter in your Animator.")]
    public string openTriggerName = "Open";
    private BoxCollider2D doorCollider;
    private AudioSource audioSource;

    [Header("Depth Sorting")]
    [Tooltip("Make sure this exactly matches Kaelen's sorting layer!")]
    public string sortingLayerName = "Player";
    public float depthOffset = -0.5f;
    private SpriteRenderer spriteRenderer;

    [Header("Audio")]
    public AudioClip sfxUnlockSuccess;
    public AudioClip sfxUnlockFail;

    private bool isLocked = true;

    void Start()
    {
        doorCollider = GetComponent<BoxCollider2D>();
        audioSource = GetComponent<AudioSource>();

        // AUTO-WIRING: Grab the Animator just in case it wasn't dragged into the Inspector!
        if (doorAnimator == null) doorAnimator = GetComponent<Animator>();
        if (doorAnimator == null) doorAnimator = GetComponentInChildren<Animator>();
        
        if (string.IsNullOrEmpty(requiredItemID)) 
            Debug.LogWarning($"<color=yellow>[WARNING]</color> LockedDoor '{gameObject.name}' does not have a Required Item ID set in the Inspector!");

        // DYNAMIC DEPTH SORTING
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingLayerName = sortingLayerName;
            spriteRenderer.sortingOrder = Mathf.RoundToInt((transform.position.y + depthOffset) * -10f);
        }
    }

    public void AttemptUnlock()
    {
        Debug.Log($"<color=cyan>[LOCKED DOOR]</color> Interaction received on '{gameObject.name}'!");

        if (!isLocked) return; // Door is already open

        InventoryManager invManager = FindAnyObjectByType<InventoryManager>();
        if (invManager == null)
        {
            Debug.LogError("LockedDoor: Cannot find InventoryManager in the scene!");
            return;
        }

        // Check if the player has the required key in their inventory
        if (invManager.HasItem(requiredItemID))
        {
            // SUCCESS!
            Debug.Log($"<color=green>[DOOR UNLOCKED]</color> Verified {requiredItemID}. Playing animation!");
            isLocked = false;

            // Consume the key from the inventory ONLY if it is not a Master Key!
            if (!requiredItemID.Contains("MSTR"))
            {
                invManager.ConsumeItem(requiredItemID);
            }

            if (audioSource != null && sfxUnlockSuccess != null) audioSource.PlayOneShot(sfxUnlockSuccess);
            if (doorAnimator != null) doorAnimator.SetTrigger(openTriggerName);

            // Disable the physical barrier so Kaelen can walk through
            doorCollider.enabled = false;
            
            // Make it non-interactable from now on
            gameObject.tag = "Untagged";
        }
        else
        {
            // FAILURE!
            Debug.Log($"Door is locked. Requires {requiredItemID}.");
            if (audioSource != null && sfxUnlockFail != null) audioSource.PlayOneShot(sfxUnlockFail);
            if (UIPickupLog.Instance != null) UIPickupLog.Instance.AddLog($"Requires {requiredItemID}", Color.red, "ACCESS DENIED");
        }
    }
}
using UnityEngine;
using System.Collections;

/// <summary>
/// Manages a door that requires a specific item from the player's inventory to be unlocked.
/// </summary>
[RequireComponent(typeof(BoxCollider2D), typeof(AudioSource))]
public class LockedDoor : MonoBehaviour
{
    [Header("Lock Settings")]
    [Tooltip("The unique Item ID of the key required to open this door (e.g., 'KEY-MSTR-3').")]
    public string requiredItemID;

    [Header("Components")]
    [Tooltip("The Animator component that controls the door's open/close animations.")]
    public Animator doorAnimator;
    [Tooltip("The exact, case-sensitive name of the Trigger parameter in the Animator to play the open animation.")]
    public string openTriggerName = "Open";
    private AudioSource audioSource;

    [Header("Depth Sorting")]
    [Tooltip("The name of the Sorting Layer to use for this door, which should match the player.")]
    public string sortingLayerName = "Player";
    [Tooltip("A vertical offset to adjust the door's perceived depth for correct 2.5D layering.")]
    public float depthOffset = -0.5f;
    private SpriteRenderer spriteRenderer;

    [Header("Audio")]
    [Tooltip("Sound effect played when the door is successfully unlocked.")]
    public AudioClip sfxUnlockSuccess;
    [Tooltip("Sound effect played when the unlock attempt fails.")]
    public AudioClip sfxUnlockFail;

    private bool isLocked = true;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // Auto-wire references if they were not assigned in the Inspector.
        if (doorAnimator == null) doorAnimator = GetComponent<Animator>();
        if (doorAnimator == null) doorAnimator = GetComponentInChildren<Animator>();
        
        if (string.IsNullOrEmpty(requiredItemID)) 
            Debug.LogWarning($"<color=yellow>[WARNING]</color> LockedDoor '{gameObject.name}' does not have a Required Item ID set in the Inspector!");

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingLayerName = sortingLayerName;
            // Apply depth sorting based on Y-position.
            spriteRenderer.sortingOrder = Mathf.RoundToInt((transform.position.y + depthOffset) * -10f);
        }
    }

    /// <summary>
    /// Called by the PlayerInteraction script to attempt to unlock the door.
    /// </summary>
    public void AttemptUnlock()
    {
        if (!isLocked) return;

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("LockedDoor: Cannot find InventoryManager in the scene!");
            return;
        }

        if (InventoryManager.Instance.HasItem(requiredItemID))
        {
            isLocked = false;

            // Per the GDD, Master Keys are permanent progression items and should not be consumed.
            if (!requiredItemID.Contains("MSTR"))
            {
                InventoryManager.Instance.ConsumeItem(requiredItemID);
            }

            if (audioSource != null && sfxUnlockSuccess != null) audioSource.PlayOneShot(sfxUnlockSuccess);
            if (doorAnimator != null) doorAnimator.SetTrigger(openTriggerName);

            // Disable all colliders on this object and its children to allow passage.
            Collider2D[] allColliders = GetComponentsInChildren<Collider2D>();
            foreach (Collider2D col in allColliders)
            {
                col.enabled = false;
            }
            
            // Remove the interactable tag to prevent further interaction prompts.
            gameObject.tag = "Untagged";
        }
        else
        {
            if (audioSource != null && sfxUnlockFail != null) audioSource.PlayOneShot(sfxUnlockFail);
            if (UIPickupLog.Instance != null) UIPickupLog.Instance.AddLog($"Requires {requiredItemID}", Color.red, "ACCESS DENIED");
        }
    }
}
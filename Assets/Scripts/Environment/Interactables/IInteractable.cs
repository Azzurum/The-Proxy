using UnityEngine;

/// <summary>
/// Standardizes interaction logic across all usable objects in the environment.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Executes the core interaction logic for this usable object.
    /// </summary>
    /// <param name="interactor">The GameObject initiating the interaction (usually the Player).</param>
    void Interact(GameObject interactor);
    
    /// <summary>
    /// Determines if the object can currently be interacted with.
    /// </summary>
    bool CanInteract();
}
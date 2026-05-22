using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the player's interaction with an elevator, including the hold-to-enter mechanic and cinematic sequences.
/// </summary>
public class ElevatorInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("The time in seconds the player must hold the interaction key to enter the elevator.")]
    public float requiredHoldTime = 1.5f;

    [Header("UI References")]
    [Tooltip("The parent Canvas for the hold-to-interact UI prompt.")]
    public Canvas elevatorCanvas; 
    [Tooltip("The UI Image used as a radial fill to show hold progress.")]
    public Image fillRing;
    [Tooltip("The UI Canvas for the deck selection terminal.")]
    public GameObject deckTerminalCanvas; 

    [Header("Animation & Logic")]
    [Tooltip("The Animator component for the elevator doors.")]
    public Animator elevatorAnimator;
    [Tooltip("The target position for the player to walk to inside the elevator.")]
    public Transform walkInTarget;
    [Tooltip("The target position for the player to walk to when exiting the elevator.")]
    public Transform walkOutTarget; 
    [Tooltip("The speed at which the player character walks during cinematic sequences.")]
    public float walkInSpeed = 2.5f;

    [Header("Linking")]
    [Tooltip("A unique ID for this elevator, used to link departure and arrival points across scenes.")]
    public string elevatorID; 

    private bool _isPlayerNear = false;
    private float _currentHoldTime = 0f;
    private Transform _playerTransform;
    private PlayerController _playerController;
    private Rigidbody2D _playerRigidbody;
    private bool _sequenceStarted = false;

    void Start()
    {
        if (elevatorCanvas != null) elevatorCanvas.gameObject.SetActive(false);
        if (deckTerminalCanvas != null) deckTerminalCanvas.SetActive(false);
        if (fillRing != null) fillRing.fillAmount = 0;
    }

    void Update()
    {
        // Disable interaction logic while a cinematic sequence is active.
        if (_sequenceStarted) return;

        if (_isPlayerNear)
        {
            // Handle the hold-to-interact input.
            if (Input.GetKey(KeyCode.E))
            {
                _currentHoldTime += Time.deltaTime;
                if (fillRing != null) fillRing.fillAmount = _currentHoldTime / requiredHoldTime;

                if (_currentHoldTime >= requiredHoldTime)
                {
                    StartCoroutine(EnterElevatorSequence());
                }
            }
            else
            {
                _currentHoldTime = 0f;
                if (fillRing != null) fillRing.fillAmount = 0;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !_sequenceStarted)
        {
            _isPlayerNear = true;
            _playerTransform = collision.transform;
            if (elevatorCanvas != null) elevatorCanvas.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !_sequenceStarted)
        {
            _isPlayerNear = false;
            _currentHoldTime = 0f;
            if (fillRing != null) fillRing.fillAmount = 0;
            if (elevatorCanvas != null) elevatorCanvas.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Initiates and controls the cinematic sequence of the player walking into the elevator.
    /// </summary>
    private IEnumerator EnterElevatorSequence()
    {
        _sequenceStarted = true;
        _currentHoldTime = 0f;
        if (fillRing != null) fillRing.fillAmount = 0;
        if (elevatorCanvas != null) elevatorCanvas.gameObject.SetActive(false);

        // Disable player controls and physics for the cinematic.
        if (_playerRigidbody != null) 
        {
            _playerRigidbody.linearVelocity = Vector2.zero; 
            _playerRigidbody.bodyType = RigidbodyType2D.Kinematic; 
            _playerRigidbody.interpolation = RigidbodyInterpolation2D.None; 
        }
        if (_playerController != null) _playerController.enabled = false;

        // Hide the player's minimap blip as they are entering a "vehicle".
        Transform blip = _playerTransform.Find("Minimap_Blip_Player");
        if (blip != null) blip.gameObject.SetActive(false);

        // Trigger walk animation.
        Animator playerAnim = _playerTransform.GetComponentInChildren<Animator>();
        if (playerAnim != null)
        {
            playerAnim.SetFloat("Speed", 1f);      
            playerAnim.SetFloat("Vertical", 1f);   
            playerAnim.SetFloat("Horizontal", 0f);
        }

        // Open elevator doors and wait.
        if (elevatorAnimator != null) elevatorAnimator.Play("Elevator_Open");
        yield return new WaitForSeconds(1f); 

        // Move player to the designated spot inside the elevator.
        if (_playerTransform != null && walkInTarget != null)
        {
            Vector3 lockedTargetPos = new Vector3(walkInTarget.position.x, walkInTarget.position.y, _playerTransform.position.z);
            while (Vector3.Distance(_playerTransform.position, lockedTargetPos) > 0.01f)
            {
                _playerTransform.position = Vector3.MoveTowards(_playerTransform.position, lockedTargetPos, walkInSpeed * Time.deltaTime);
                yield return null; 
            }
        }

        // Hide the player sprite by pushing it to a background sorting layer.
        SpriteRenderer[] allPlayerSprites = _playerTransform.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sprite in allPlayerSprites)
        {
            if (sprite.gameObject.name.Contains("Blip")) continue;
            sprite.sortingLayerName = "Default"; 
            sprite.sortingOrder = -5; 
        }

        // Stop walk animation and close doors.
        if (playerAnim != null) playerAnim.SetFloat("Speed", 0f);

        if (elevatorAnimator != null) elevatorAnimator.Play("Elevator_Close");
        yield return new WaitForSeconds(1f); 

        // Show the deck selection terminal.
        if (deckTerminalCanvas != null) deckTerminalCanvas.SetActive(true);
    }

    /// <summary>
    /// Confirms the travel decision, stores this elevator's ID, and loads the target scene.
    /// </summary>
    /// <param name="targetScene">The name of the scene to load.</param>
    public void ConfirmDeparture(string targetScene)
    {
        // Let the static manager know which elevator was used so the arrival script can trigger.
        ElevatorManager.LastUsedElevatorID = elevatorID;
        SceneManager.LoadScene(targetScene);
    }

    /// <summary>
    /// Called when the player cancels deck selection, triggering the exit sequence.
    /// </summary>
    public void CancelDeparture()
    {
        StartCoroutine(ExitElevatorSequence());
    }

    /// <summary>
    /// Controls the cinematic sequence of the player walking back out of the elevator.
    /// </summary>
    private IEnumerator ExitElevatorSequence()
    {
        // Open doors and wait.
        if (elevatorAnimator != null) elevatorAnimator.Play("Elevator_Open");
        yield return new WaitForSeconds(1f);

        SpriteRenderer[] allPlayerSprites = _playerTransform.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sprite in allPlayerSprites)
        {
            if (sprite.gameObject.name.Contains("Blip")) 
            {
                sprite.gameObject.SetActive(true);
                continue;
            }
            sprite.sortingOrder = 20; 
        }

        // Trigger walk animation.
        Animator playerAnim = _playerTransform.GetComponentInChildren<Animator>();
        if (playerAnim != null)
        {
            playerAnim.SetFloat("Speed", 1f);      
            playerAnim.SetFloat("Vertical", -1f); 
        }

        if (_playerTransform != null && walkOutTarget != null)
        {
            Vector3 targetPos = new Vector3(walkOutTarget.position.x, walkOutTarget.position.y, _playerTransform.position.z);
            while (Vector3.Distance(_playerTransform.position, targetPos) > 0.05f)
            {
                _playerTransform.position = Vector3.MoveTowards(_playerTransform.position, targetPos, walkInSpeed * Time.deltaTime);
                yield return null; 
            }
        }

        // Stop animation and restore player controls.
        if (playerAnim != null) playerAnim.SetFloat("Speed", 0f);

        if (_playerRigidbody != null) 
        {
            _playerRigidbody.bodyType = RigidbodyType2D.Dynamic; 
            _playerRigidbody.interpolation = RigidbodyInterpolation2D.Interpolate; 
        }

        if (_playerController != null) _playerController.enabled = true;

        // Close doors and reset state.
        if (elevatorAnimator != null) elevatorAnimator.Play("Elevator_Close");
        
        // Allow the player to interact with the elevator again.
        _sequenceStarted = false; 
    }
}
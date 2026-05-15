using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ElevatorInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float requiredHoldTime = 1.5f;
    public string nextFloorScene = "Level_2"; // Change to your actual scene name!

    [Header("UI References")]
    public Canvas elevatorCanvas;
    public Image fillRing;

    [Header("Animation & Logic")]
    public Animator elevatorAnimator;
    public Transform walkInTarget;
    public float walkInSpeed = 2.5f;

    [Header("Linking")]
    public string elevatorID; // Name this "Elevator_1", "Elevator_2", etc., in the Inspector

    private bool _isPlayerNear = false;
    private float _currentHoldTime = 0f;
    private Transform _playerTransform;
    private bool _sequenceStarted = false;

    void Start()
    {
        if (elevatorCanvas != null) elevatorCanvas.gameObject.SetActive(false);
        if (fillRing != null) fillRing.fillAmount = 0;
    }

    void Update()
    {
        if (_sequenceStarted) return;

        if (_isPlayerNear)
        {
            if (Input.GetKey(KeyCode.E))
            {
                _currentHoldTime += Time.deltaTime;
                
                if (fillRing != null) fillRing.fillAmount = _currentHoldTime / requiredHoldTime;

                if (_currentHoldTime >= requiredHoldTime)
                {
                    StartCoroutine(ElevatorDepartureSequence());
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

    private IEnumerator ElevatorDepartureSequence()
    {
        _sequenceStarted = true;
        
        // 1. Hide the UI
        if (elevatorCanvas != null) elevatorCanvas.gameObject.SetActive(false);

        // 2. DISABLE PLAYER & PHYSICS
        Rigidbody2D playerRb = _playerTransform.GetComponent<Rigidbody2D>();
        if (playerRb != null) 
        {
            playerRb.linearVelocity = Vector2.zero; 
            playerRb.bodyType = RigidbodyType2D.Kinematic; 
            // FIX: Turn off interpolation to stop the sprite flickering/jittering 
            // when we move him manually via script.
            playerRb.interpolation = RigidbodyInterpolation2D.None; 
        }

        PlayerController playerMovement = _playerTransform.GetComponent<PlayerController>();
        if (playerMovement != null) playerMovement.enabled = false;

        // 3. HANDLE MINIMAP BLIP & SPRITES
        // Find and hide the Minimap Blip specifically so it doesn't float in the elevator
        Transform blip = _playerTransform.Find("Minimap_Blip_Player");
        if (blip != null) blip.gameObject.SetActive(false);

        // 4. PREPARE ANIMATION
        Animator playerAnim = _playerTransform.GetComponent<Animator>();
        if (playerAnim == null) playerAnim = _playerTransform.GetComponentInChildren<Animator>();
        if (playerAnim != null)
        {
            playerAnim.SetFloat("Speed", 1f);      
            playerAnim.SetFloat("Vertical", 1f);   
            playerAnim.SetFloat("Horizontal", 0f);
        }

        // 5. OPEN DOORS
        if (elevatorAnimator != null) elevatorAnimator.Play("Elevator_Open");
        yield return new WaitForSeconds(1f); 

        // 6. WALK INSIDE
        if (_playerTransform != null && walkInTarget != null)
        {
            Vector3 lockedTargetPos = new Vector3(walkInTarget.position.x, walkInTarget.position.y, _playerTransform.position.z);

            while (Vector3.Distance(_playerTransform.position, lockedTargetPos) > 0.01f)
            {
                _playerTransform.position = Vector3.MoveTowards(_playerTransform.position, lockedTargetPos, walkInSpeed * Time.deltaTime);
                yield return null; 
            }
        }

        // 7. FINAL SORTING ADJUSTMENT
        SpriteRenderer[] allPlayerSprites = _playerTransform.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sprite in allPlayerSprites)
        {
            if (sprite.gameObject.name.Contains("Blip")) continue;

            // Pushing Kaelen deep into the background (Order -5) 
            // This should place him behind the doors AND the wall frame (walls_1.1)
            sprite.sortingLayerName = "Default"; 
            sprite.sortingOrder = -5; 
        }

        if (playerAnim != null) playerAnim.SetFloat("Speed", 0f);

        // 8. CLOSE DOORS
        if (elevatorAnimator != null) elevatorAnimator.Play("Elevator_Close");
        yield return new WaitForSeconds(1f); 

        // 9. LOAD NEXT DECK
        ElevatorManager.LastUsedElevatorID = elevatorID;
        SceneManager.LoadScene(nextFloorScene);
    }
}
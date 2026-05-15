using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ElevatorInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float requiredHoldTime = 1.5f;

    [Header("UI References")]
    public Canvas elevatorCanvas; 
    public Image fillRing;
    public GameObject deckTerminalCanvas; 

    [Header("Animation & Logic")]
    public Animator elevatorAnimator;
    public Transform walkInTarget;
    public Transform walkOutTarget; 
    public float walkInSpeed = 2.5f;

    [Header("Linking")]
    public string elevatorID; 

    private bool _isPlayerNear = false;
    private float _currentHoldTime = 0f;
    private Transform _playerTransform;
    private bool _sequenceStarted = false;

    void Start()
    {
        if (elevatorCanvas != null) elevatorCanvas.gameObject.SetActive(false);
        if (deckTerminalCanvas != null) deckTerminalCanvas.SetActive(false);
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

    // --- PHASE 1: WALKING IN ---
    private IEnumerator EnterElevatorSequence()
    {
        _sequenceStarted = true;
        _currentHoldTime = 0f;
        if (fillRing != null) fillRing.fillAmount = 0;
        if (elevatorCanvas != null) elevatorCanvas.gameObject.SetActive(false);

        Rigidbody2D playerRb = _playerTransform.GetComponent<Rigidbody2D>();
        PlayerController playerMovement = _playerTransform.GetComponent<PlayerController>();
        Animator playerAnim = _playerTransform.GetComponentInChildren<Animator>();

        if (playerRb != null) 
        {
            playerRb.linearVelocity = Vector2.zero; 
            playerRb.bodyType = RigidbodyType2D.Kinematic; 
            playerRb.interpolation = RigidbodyInterpolation2D.None; 
        }
        if (playerMovement != null) playerMovement.enabled = false;

        Transform blip = _playerTransform.Find("Minimap_Blip_Player");
        if (blip != null) blip.gameObject.SetActive(false);

        if (playerAnim != null)
        {
            playerAnim.SetFloat("Speed", 1f);      
            playerAnim.SetFloat("Vertical", 1f);   
            playerAnim.SetFloat("Horizontal", 0f);
        }

        if (elevatorAnimator != null) elevatorAnimator.Play("Elevator_Open");
        yield return new WaitForSeconds(1f); 

        if (_playerTransform != null && walkInTarget != null)
        {
            Vector3 lockedTargetPos = new Vector3(walkInTarget.position.x, walkInTarget.position.y, _playerTransform.position.z);
            while (Vector3.Distance(_playerTransform.position, lockedTargetPos) > 0.01f)
            {
                _playerTransform.position = Vector3.MoveTowards(_playerTransform.position, lockedTargetPos, walkInSpeed * Time.deltaTime);
                yield return null; 
            }
        }

        SpriteRenderer[] allPlayerSprites = _playerTransform.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sprite in allPlayerSprites)
        {
            if (sprite.gameObject.name.Contains("Blip")) continue;
            sprite.sortingLayerName = "Default"; 
            sprite.sortingOrder = -5; 
        }

        if (playerAnim != null) playerAnim.SetFloat("Speed", 0f);

        if (elevatorAnimator != null) elevatorAnimator.Play("Elevator_Close");
        yield return new WaitForSeconds(1f); 

        if (deckTerminalCanvas != null) deckTerminalCanvas.SetActive(true);
    }

    // --- PHASE 2: CONFIRMATION (Proceed to next deck) ---
    public void ConfirmDeparture(string targetScene)
    {
        ElevatorManager.LastUsedElevatorID = elevatorID;
        SceneManager.LoadScene(targetScene);
    }

    // --- PHASE 3: CANCELLATION (Walk back out) ---
    public void CancelDeparture()
    {
        StartCoroutine(ExitElevatorSequence());
    }

    private IEnumerator ExitElevatorSequence()
    {
        if (elevatorAnimator != null) elevatorAnimator.Play("Elevator_Open");
        yield return new WaitForSeconds(1f);

        SpriteRenderer[] allPlayerSprites = _playerTransform.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sprite in allPlayerSprites)
        {
            if (sprite.gameObject.name.Contains("Blip")) 
            {
                sprite.gameObject.SetActive(true);
                continue; // FIX: Skip the blip
            }
            sprite.sortingOrder = 20; 
        }

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

        if (playerAnim != null) playerAnim.SetFloat("Speed", 0f);

        Rigidbody2D playerRb = _playerTransform.GetComponent<Rigidbody2D>();
        if (playerRb != null) 
        {
            playerRb.bodyType = RigidbodyType2D.Dynamic; 
            playerRb.interpolation = RigidbodyInterpolation2D.Interpolate; 
        }

        PlayerController playerMovement = _playerTransform.GetComponent<PlayerController>();
        if (playerMovement != null) playerMovement.enabled = true;

        if (elevatorAnimator != null) elevatorAnimator.Play("Elevator_Close");
        
        _sequenceStarted = false; 
    }
}
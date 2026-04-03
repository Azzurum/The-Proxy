using UnityEngine;
using System.Collections;

public class ShatterAnimator : MonoBehaviour
{
    [Header("Connections")]
    public Transform glassParent; 
    public CanvasGroup uiGroup;   

    [Header("Cinematic Settings")]
    public float animationDuration = 0.5f; 
    
    [Tooltip("X is extremely low (0.3) to prevent the horizontal hole. Y is high (3.5) to open top/bottom.")]
    public Vector2 explosionForce = new Vector2(0.3f, 3.5f); 
    
    [Tooltip("A tiny shrink emphasizes the jagged black gaps without breaking layout (0.98 = 98% size)")]
    public float shardShrinkScale = 0.98f;

    [Header("Horror / Glitch Settings")]
    public bool enableJitter = true;
    [Tooltip("How violently the glass shakes while paused")]
    public float jitterIntensity = 0.01f;

    private Vector3[] _originalPositions;
    private Quaternion[] _originalRotations;
    private Vector3[] _originalScales;
    private Transform[] _shards;

    // Jitter state variables
    private Vector3[] _targetPositions; 
    private bool _isFullyShattered = false;

    public void InitializeShards()
    {
        int childCount = glassParent.childCount - 1; // Ignore the Abyss background
        _shards = new Transform[childCount];
        _originalPositions = new Vector3[childCount];
        _originalRotations = new Quaternion[childCount];
        _originalScales = new Vector3[childCount];
        _targetPositions = new Vector3[childCount]; // Added initialization for jitter

        Camera cam = Camera.main;
        int shardIndex = 0;

        for (int i = 0; i < glassParent.childCount; i++)
        {
            Transform child = glassParent.GetChild(i);
            if (child.name == "Pause_Background") continue; 

            _shards[shardIndex] = child;
            _originalPositions[shardIndex] = child.localPosition;
            _originalRotations[shardIndex] = child.localRotation;
            _originalScales[shardIndex] = child.localScale;

            MeshFilter mf = child.GetComponent<MeshFilter>();
            if (mf != null)
            {
                Mesh uniqueMesh = Instantiate(mf.sharedMesh);
                Vector3[] vertices = uniqueMesh.vertices;
                Vector2[] uvs = new Vector2[vertices.Length];

                for (int v = 0; v < vertices.Length; v++)
                {
                    Vector3 worldPos = child.TransformPoint(vertices[v]);
                    Vector3 screenPos = cam.WorldToViewportPoint(worldPos);
                    
                    // Fixed Y-axis flip to correct URP texture mapping
                    uvs[v] = new Vector2(screenPos.x, 1f - screenPos.y);
                }

                uniqueMesh.uv = uvs;
                mf.mesh = uniqueMesh;
            }
            shardIndex++;
        }
    }

    public void PlayShatter() 
    { 
        StopAllCoroutines(); 
        _isFullyShattered = false; // Turn off jitter while moving
        StartCoroutine(AnimateTransition(true)); 
    }
    
    public void PlayAssemble() 
    { 
        StopAllCoroutines(); 
        _isFullyShattered = false; // Turn off jitter while moving
        StartCoroutine(AnimateTransition(false)); 
    }

    private IEnumerator AnimateTransition(bool isShattering)
    {
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            
            // "Cubic Ease Out" - Starts fast, settles into place very smoothly
            float ease = isShattering ? (1 - Mathf.Pow(1 - t, 3)) : Mathf.Pow(1 - t, 3);

            for (int i = 0; i < _shards.Length; i++)
            {
                Vector3 pushDirection = _originalPositions[i].normalized;
                if (pushDirection == Vector3.zero) pushDirection = new Vector3(0.1f, 0.1f, 0); 
                
                // Apply the independent X and Y forces to prevent wide gaps
                Vector3 directionalForce = new Vector3(
                    pushDirection.x * explosionForce.x, 
                    pushDirection.y * explosionForce.y, 
                    0f
                );
                
                Vector3 targetPos = _originalPositions[i] + directionalForce;
                
                _shards[i].localPosition = Vector3.Lerp(_originalPositions[i], targetPos, ease);
                
                // Keep orientation completely flat
                _shards[i].localRotation = _originalRotations[i];
                
                _shards[i].localScale = Vector3.Lerp(_originalScales[i], _originalScales[i] * shardShrinkScale, ease);
            }

            if (uiGroup != null) uiGroup.alpha = ease;
            
            yield return null;
        }

        // --- HORROR JITTER LOGIC TRIGGER ---
        if (isShattering)
        {
            // Save their final resting spots so they don't drift away when shaking
            for (int i = 0; i < _shards.Length; i++)
            {
                _targetPositions[i] = _shards[i].localPosition;
            }
            _isFullyShattered = true; // Turn on the jitter!
        }
    }

    // This runs every single frame, even when Time.timeScale is 0!
    void Update()
    {
        if (_isFullyShattered && enableJitter)
        {
            for (int i = 0; i < _shards.Length; i++)
            {
                // Create a tiny, random vibration
                Vector3 randomJitter = new Vector3(
                    Random.Range(-jitterIntensity, jitterIntensity),
                    Random.Range(-jitterIntensity, jitterIntensity),
                    0f
                );
                
                // Apply the vibration to their locked resting positions
                _shards[i].localPosition = _targetPositions[i] + randomJitter;
            }
        }
    }
}
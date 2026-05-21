using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages the procedural shattering and assembling animation of a UI element composed of multiple mesh shards.
/// </summary>
public class ShatterAnimator : MonoBehaviour
{
    [Header("Connections")]
    [Tooltip("The parent Transform containing all the individual glass shard GameObjects.")]
    public Transform glassParent; 

    [Header("Cinematic Settings")]
    [Tooltip("The duration of the shatter/assemble animation in seconds.")]
    public float animationDuration = 0.5f; 
    
    [Tooltip("The force applied to shards. X is low to prevent wide horizontal gaps, Y is high to create a vertical opening.")]
    public Vector2 explosionForce = new Vector2(0.3f, 3.5f); 
    
    [Tooltip("How much each shard shrinks to create visible gaps between them (e.g., 0.98 = 98% of original size).")]
    public float shardShrinkScale = 0.98f;

    [Header("Horror / Glitch Settings")]
    [Tooltip("If enabled, the shards will vibrate randomly when in the shattered state.")]
    public bool enableJitter = true;
    [Tooltip("The maximum distance a shard can move from its resting position during a jitter frame.")]
    public float jitterIntensity = 0.01f;

    private Vector3[] _originalPositions;
    private Quaternion[] _originalRotations;
    private Vector3[] _originalScales;
    private Transform[] _shards;

    private Vector3[] _targetPositions; 
    private bool _isFullyShattered = false;
    private List<Mesh> _instantiatedMeshes = new List<Mesh>();

    /// <summary>
    /// Caches the initial state of all shards and remaps their UVs to display a screen texture.
    /// </summary>
    public void InitializeShards()
    {
        int totalChildren = glassParent.childCount;
        int shardCount = 0;
        
        for (int i = 0; i < totalChildren; i++)
        {
            if (glassParent.GetChild(i).name != "Pause_Background") shardCount++;
        }

        foreach (Mesh m in _instantiatedMeshes)
        {
            if (m != null) Destroy(m);
        }
        _instantiatedMeshes.Clear();

        _shards = new Transform[shardCount];
        _originalPositions = new Vector3[shardCount];
        _originalRotations = new Quaternion[shardCount];
        _originalScales = new Vector3[shardCount];
        _targetPositions = new Vector3[shardCount];

        Camera cam = Camera.main;
        int shardIndex = 0;

        for (int i = 0; i < totalChildren; i++)
        {
            Transform child = glassParent.GetChild(i);
            if (child.name == "Pause_Background") continue; 

            _shards[shardIndex] = child;
            _originalPositions[shardIndex] = child.localPosition;
            _originalRotations[shardIndex] = child.localRotation;
            _originalScales[shardIndex] = child.localScale;

            MeshFilter mf = child.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                Mesh uniqueMesh = Instantiate(mf.sharedMesh);
                _instantiatedMeshes.Add(uniqueMesh);
                Vector3[] vertices = uniqueMesh.vertices;
                Vector2[] uvs = new Vector2[vertices.Length];

                for (int v = 0; v < vertices.Length; v++)
                {
                    Vector3 worldPos = child.TransformPoint(vertices[v]);
                    Vector3 screenPos = cam.WorldToViewportPoint(worldPos);
                    
                    uvs[v] = new Vector2(screenPos.x, 1f - screenPos.y);
                }

                uniqueMesh.uv = uvs;
                mf.mesh = uniqueMesh;
            }
            shardIndex++;
        }
    }

    void OnDestroy()
    {
        foreach (Mesh m in _instantiatedMeshes)
        {
            if (m != null) Destroy(m);
        }
        _instantiatedMeshes.Clear();
    }

    /// <summary>
    /// Plays the shattering animation.
    /// </summary>
    public void PlayShatter() 
    { 
        StopAllCoroutines(); 
        _isFullyShattered = false;
        StartCoroutine(AnimateTransition(true)); 
    }
    
    /// <summary>
    /// Plays the assembling animation.
    /// </summary>
    public void PlayAssemble() 
    { 
        StopAllCoroutines(); 
        _isFullyShattered = false;
        StartCoroutine(AnimateTransition(false)); 
    }

    /// <summary>
    /// The main coroutine for animating the transition between shattered and assembled states.
    /// </summary>
    private IEnumerator AnimateTransition(bool isShattering)
    {
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            
            float ease = isShattering ? (1 - Mathf.Pow(1 - t, 3)) : Mathf.Pow(1 - t, 3);

            for (int i = 0; i < _shards.Length; i++)
            {
                Vector3 pushDirection = _originalPositions[i].normalized;
                if (pushDirection == Vector3.zero) pushDirection = new Vector3(0.1f, 0.1f, 0); 
                
                Vector3 directionalForce = new Vector3(
                    pushDirection.x * explosionForce.x, 
                    pushDirection.y * explosionForce.y, 
                    0f
                );
                
                Vector3 targetPos = _originalPositions[i] + directionalForce;
                
                _shards[i].localPosition = Vector3.Lerp(_originalPositions[i], targetPos, ease);
                
                _shards[i].localRotation = _originalRotations[i];
                
                _shards[i].localScale = Vector3.Lerp(_originalScales[i], _originalScales[i] * shardShrinkScale, ease);
            }
            
            yield return null;
        }

        if (isShattering)
        {
            for (int i = 0; i < _shards.Length; i++)
            {
                _targetPositions[i] = _shards[i].localPosition;
            }
            _isFullyShattered = true;
        }
    }

    /// <summary>
    /// This runs every frame (even when paused) to apply the jitter effect if enabled.
    /// </summary>
    void Update()
    {
        if (_isFullyShattered && enableJitter)
        {
            for (int i = 0; i < _shards.Length; i++)
            {
                Vector3 randomJitter = new Vector3(
                    Random.Range(-jitterIntensity, jitterIntensity),
                    Random.Range(-jitterIntensity, jitterIntensity),
                    0f
                );
                
                _shards[i].localPosition = _targetPositions[i] + randomJitter;
            }
        }
    }

    /// <summary>
    /// A fast animation that blasts the glass shards off-screen.
    /// </summary>
    public IEnumerator BlowbackRoutine()
    {
        _isFullyShattered = false;
        float elapsed = 0f;
        float duration = 0.2f; 

        Vector3[] startPositions = new Vector3[_shards.Length];
        for (int i = 0; i < _shards.Length; i++) startPositions[i] = _shards[i].localPosition;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Pow(Mathf.Clamp01(elapsed / duration), 2); 

            for (int i = 0; i < _shards.Length; i++)
            {
                Vector3 pushDirection = _originalPositions[i].normalized;
                if (pushDirection == Vector3.zero) pushDirection = new Vector3(0.1f, 0.1f, 0);
                
                Vector3 massiveForce = pushDirection * 25f; 
                
                _shards[i].localPosition = Vector3.Lerp(startPositions[i], _originalPositions[i] + massiveForce, t);
            }
            yield return null;
        }
        glassParent.gameObject.SetActive(false); 
    }

    /// <summary>
    /// A fast animation that pulls the glass shards back to their shattered resting positions.
    /// </summary>
    public IEnumerator RestoreRoutine()
    {
        glassParent.gameObject.SetActive(true);
        float elapsed = 0f;
        float duration = 0.25f;

        Vector3[] startPositions = new Vector3[_shards.Length];
        for (int i = 0; i < _shards.Length; i++) startPositions[i] = _shards[i].localPosition;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), 3f); 

            for (int i = 0; i < _shards.Length; i++)
            {
                _shards[i].localPosition = Vector3.Lerp(startPositions[i], _targetPositions[i], t);
            }
            yield return null;
        }
        _isFullyShattered = true;
    }
}
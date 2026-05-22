using UnityEngine;

/// <summary>
/// Manipulates the texture offset of a material to simulate aggressive screen static and tearing.
/// </summary>
public class StaticAnimator : MonoBehaviour
{
    [Tooltip("Drag your Mat_GlitchStatic material here")]
    public Material staticMaterial;

    [Tooltip("How fast the static flickers (lower = slower, creeping movement)")]
    public float flickerSpeed = 3f;

    [Tooltip("How far the image jumps (lower = subtle rumble, higher = aggressive tear)")]
    public float flickerIntensity = 0.1f;

    private float _timer;

    void Update()
    {
        if (staticMaterial == null) return;

        _timer += Time.unscaledDeltaTime;

        if (_timer > (1f / Mathf.Max(0.01f, flickerSpeed)))
        {
            _timer = 0f;

            float randomX = Random.Range(-flickerIntensity, flickerIntensity);
            float randomY = Random.Range(-flickerIntensity, flickerIntensity);
            
            staticMaterial.SetTextureOffset("_BaseMap", new Vector2(randomX, randomY));
        }
    }
}
using UnityEngine;

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

        // We use unscaledDeltaTime so the static still moves even when Time.timeScale is 0!
        _timer += Time.unscaledDeltaTime;

        if (_timer > (1f / flickerSpeed))
        {
            _timer = 0f;

            // THE FIX: Instead of jumping anywhere across the whole image, 
            // we only jump a tiny amount forward or backward based on your intensity.
            float randomX = Random.Range(-flickerIntensity, flickerIntensity);
            float randomY = Random.Range(-flickerIntensity, flickerIntensity);
            
            // _BaseMap is the code name for the main texture in URP Unlit shaders
            staticMaterial.SetTextureOffset("_BaseMap", new Vector2(randomX, randomY));
        }
    }
}
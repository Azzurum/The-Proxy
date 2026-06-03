using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Coordinates the saving, loading, and application of player settings via PlayerPrefs.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    [Header("Audio System")]
    [Tooltip("The master Audio Mixer governing game sound levels.")]
    public AudioMixer mainMixer; 
    [Tooltip("The UI Slider mapped to the master volume.")]
    public Slider volSlider;
    [Tooltip("The UI Text displaying the current volume percentage.")]
    public TextMeshProUGUI volDisplay;

    [Header("Breaker Switches")]
    [Tooltip("Toggle controlling the CRT distortion post-processing effect.")]
    public Toggle crtToggle;
    public Image crtBulb;
    
    [Tooltip("Toggle controlling fullscreen application mode.")]
    public Toggle fsToggle;
    public Image fsBulb;

    [Tooltip("Toggle controlling the kinetic screen shake (tremor) effects.")]
    public Toggle shakeToggle;
    public Image shakeBulb;

    [Header("Indicator Colors")]
    [Tooltip("The color applied to the indicator bulb when a setting is active.")]
    public Color bulbOnColor;
    [Tooltip("The color applied to the indicator bulb when a setting is inactive.")]
    public Color bulbOffColor;

    private void Start()
    {
        if (volSlider != null)
        {
            float savedVol = PlayerPrefs.GetFloat("SysVolMaster", 75f);
            volSlider.value = savedVol; 
            
            volSlider.onValueChanged.AddListener(UpdateVolume);
            UpdateVolume(savedVol); 
        }

        if (crtToggle != null)
        {
            bool savedCRT = PlayerPrefs.GetInt("CrtDistortion", 1) == 1;
            crtToggle.isOn = savedCRT;

            crtToggle.onValueChanged.AddListener(UpdateCRT);
            crtToggle.onValueChanged.AddListener(_ => UpdateBulb(crtToggle, crtBulb));
            UpdateBulb(crtToggle, crtBulb);
        }

        if (fsToggle != null)
        {
            bool savedFS = PlayerPrefs.GetInt("ViewportOverride", 1) == 1;
            fsToggle.isOn = savedFS;

            fsToggle.onValueChanged.AddListener(SetFullscreen);
            fsToggle.onValueChanged.AddListener(_ => UpdateBulb(fsToggle, fsBulb));
            
            SetFullscreen(savedFS);
            UpdateBulb(fsToggle, fsBulb);
        }

        if (shakeToggle != null)
        {
            bool savedShake = PlayerPrefs.GetInt("KineticTremor", 1) == 1;
            shakeToggle.isOn = savedShake;

            shakeToggle.onValueChanged.AddListener(UpdateShake);
            shakeToggle.onValueChanged.AddListener(_ => UpdateBulb(shakeToggle, shakeBulb));
            UpdateBulb(shakeToggle, shakeBulb);
        }
    }

    private void UpdateVolume(float value)
    {
        PlayerPrefs.SetFloat("SysVolMaster", value);
        PlayerPrefs.Save();

        if (volDisplay != null) volDisplay.SetText("{0:0}", value); 

        if (mainMixer != null)
        {
            if (value <= 0) mainMixer.SetFloat("MasterVolume", -80f);
            else mainMixer.SetFloat("MasterVolume", Mathf.Log10(value / 100f) * 20f);
        }

        ProceduralAudioGen.SetGlobalVolume(value / 100f); 
    }

    private void SetFullscreen(bool isFullscreen)
    {
        PlayerPrefs.SetInt("ViewportOverride", isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
        Screen.fullScreen = isFullscreen;
    }

    private void UpdateCRT(bool isEnabled)
    {
        PlayerPrefs.SetInt("CrtDistortion", isEnabled ? 1 : 0);
        PlayerPrefs.Save();
        
        // DYNAMIC FIX: Find ALL CRT Effects in the scene (both scanlines and flickers) and toggle them!
        UICRTPattern[] crtPatterns = FindObjectsByType<UICRTPattern>(FindObjectsInactive.Include);
        foreach (UICRTPattern pattern in crtPatterns)
        {
            pattern.enabled = isEnabled;
            if (pattern.TryGetComponent(out RawImage rawImage)) rawImage.enabled = isEnabled;
        }

        CRTEffects[] crtFX = FindObjectsByType<CRTEffects>(FindObjectsInactive.Include);
        foreach (CRTEffects fx in crtFX)
        {
            fx.enabled = isEnabled;
            if (fx.flickerImage != null) fx.flickerImage.enabled = isEnabled;
        }
    }

    private void UpdateShake(bool isEnabled)
    {
        PlayerPrefs.SetInt("KineticTremor", isEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void UpdateBulb(Toggle toggle, Image bulb)
    {
        if (bulb == null) return;

        bulb.color = toggle.isOn ? bulbOnColor : bulbOffColor;

        if (bulb.transform.childCount > 0)
        {
            if (bulb.transform.GetChild(0).TryGetComponent<Image>(out var aura))
            {
                aura.color = toggle.isOn ? new Color(bulbOnColor.r, bulbOnColor.g, bulbOnColor.b, 0.4f) : new Color(0, 0, 0, 0f);
            }
        }
    }

    /// <summary>
    /// Triggers a scene load to return the player to the Main Menu.
    /// </summary>
    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu"); 
    }
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using UnityEngine.SceneManagement;

public class SettingsManager : MonoBehaviour
{
    [Header("Audio System")]
    public AudioMixer mainMixer; 
    public Slider volSlider;
    public TextMeshProUGUI volDisplay;

    [Header("Breaker Switches")]
    public Toggle crtToggle;
    public Image crtBulb;
    
    public Toggle fsToggle;
    public Image fsBulb;

    public Toggle shakeToggle;
    public Image shakeBulb;

    [Header("Indicator Colors")]
    public Color bulbOnColor;
    public Color bulbOffColor;

    void Start()
    {
        // --- 1. COMMS GAIN (Volume) ---
        if (volSlider != null)
        {
            // Load saved memory. If no memory exists, default to 75.
            float savedVol = PlayerPrefs.GetFloat("SysVolMaster", 75f);
            volSlider.value = savedVol; 
            
            volSlider.onValueChanged.AddListener(UpdateVolume);
            UpdateVolume(savedVol); 
        }

        // --- 2. VISOR OPTICS (CRT) ---
        if (crtToggle != null)
        {
            // Load memory. Default to 1 (True/ON).
            bool savedCRT = PlayerPrefs.GetInt("CrtDistortion", 1) == 1;
            crtToggle.isOn = savedCRT;

            crtToggle.onValueChanged.AddListener(UpdateCRT);
            crtToggle.onValueChanged.AddListener(delegate { UpdateBulb(crtToggle, crtBulb); });
            UpdateBulb(crtToggle, crtBulb);
        }

        // --- 3. VIEWPORT MAX (Fullscreen) ---
        if (fsToggle != null)
        {
            bool savedFS = PlayerPrefs.GetInt("ViewportOverride", 1) == 1;
            fsToggle.isOn = savedFS;

            fsToggle.onValueChanged.AddListener(SetFullscreen);
            fsToggle.onValueChanged.AddListener(delegate { UpdateBulb(fsToggle, fsBulb); });
            
            // Apply the actual screen resolution override
            SetFullscreen(savedFS);
            UpdateBulb(fsToggle, fsBulb);
        }

        // --- 4. KINETIC FEEDBACK (Shake) ---
        if (shakeToggle != null)
        {
            bool savedShake = PlayerPrefs.GetInt("KineticTremor", 1) == 1;
            shakeToggle.isOn = savedShake;

            shakeToggle.onValueChanged.AddListener(UpdateShake);
            shakeToggle.onValueChanged.AddListener(delegate { UpdateBulb(shakeToggle, shakeBulb); });
            UpdateBulb(shakeToggle, shakeBulb);
        }
    }

    // ==========================================
    // THE SAVE PROTOCOLS
    // ==========================================

    private void UpdateVolume(float value)
    {
        // Save to hard drive
        PlayerPrefs.SetFloat("SysVolMaster", value);
        PlayerPrefs.Save();

        if (volDisplay != null) volDisplay.text = value.ToString("0"); 

        if (mainMixer != null)
        {
            if (value <= 0) mainMixer.SetFloat("MasterVolume", -80f);
            else mainMixer.SetFloat("MasterVolume", Mathf.Log10(value / 100f) * 20f);
        }

        // Also update our mathematical sound generator!
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
        
        // Instantly update the CRT overlay if it's currently in the scene
        UICRTPattern crtOverlay = FindAnyObjectByType<UICRTPattern>();
        if (crtOverlay != null)
        {
            crtOverlay.enabled = isEnabled;
            if (crtOverlay.TryGetComponent(out RawImage rawImage)) rawImage.enabled = isEnabled;
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

        // 1. Change the solid glass color
        bulb.color = toggle.isOn ? bulbOnColor : bulbOffColor;

        // 2. Find the new glowing Aura we just added as a child
        if (bulb.transform.childCount > 0)
        {
            Image aura = bulb.transform.GetChild(0).GetComponent<Image>();
            if (aura != null)
            {
                // If ON, ignite the bloom (Amber with 40% transparency). If OFF, kill the light entirely.
                aura.color = toggle.isOn ? new Color(bulbOnColor.r, bulbOnColor.g, bulbOnColor.b, 0.4f) : new Color(0, 0, 0, 0f);
            }
        }
    }

    // ==========================================
    // NAVIGATION
    // ==========================================
    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu"); // Change this string if your main menu scene is named differently!
    }
}
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement; 

/// <summary>
/// Controls the complex visual state, animations, and data rendering of an individual save slot in the memory terminal.
/// </summary>
[RequireComponent(typeof(LayoutElement))]
public class UISaveSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Slot Identity")]
    [Tooltip("The unique identifier (0, 1, 2) mapped to this specific save file.")]
    public int slotID = 1;

    [Header("UI Containers")]
    public LayoutElement layoutElement;
    public CanvasGroup containerReadout;
    public CanvasGroup containerMatrix;
    public Image backgroundImage;

    [Header("Polish Elements")]
    public Image hoverWipe;               
    public CanvasGroup watermarkGroup;    

    [Header("UI Text Elements")]
    public TextMeshProUGUI idText;
    public TextMeshProUGUI actionText;        
    public TextMeshProUGUI dataLeftText;      
    public TextMeshProUGUI dataRightText;     

    [Header("Matrix Command Bars")]
    public UICommandBar cmdLoad;
    public UICommandBar cmdOverwrite;
    public UICommandBar cmdDelete;
    public UICommandBar cmdAbort;               

    [Header("Matrix Fills")]
    public Image fillLoad;                    
    public Image fillOverwrite;
    public Image fillDelete;

    public TextMeshProUGUI matrixHeaderText;

    [Header("Colors & Dimensions")]
    public Color colorNormal = new Color(0.05f, 0.06f, 0.07f, 0.9f);
    public Color colorExpanded = new Color(0.93f, 0.96f, 0.98f, 1f); 
    public Color colorDanger = new Color(0.2f, 0f, 0f, 0.9f);
    public Color hoverCyan = new Color(0f, 0.94f, 1f, 1f);
    public Color hoverRed = new Color(1f, 0f, 0.23f, 1f);

    private float heightResting = 140f;
    private float heightCrushed = 50f;
    private float heightExpanded = 320f;

    private bool _isExpanded = false;
    private bool _isExecuting = false; 
    private string _targetDecryptedText;
    private string _encryptedString;
    
    private Coroutine _scrambleCoroutine;
    private Coroutine _animationCoroutine;
    private Coroutine _hoverCoroutine;
    private UISaveSlot[] _siblingSlots;

    private void Awake()
    {
        if (layoutElement == null) layoutElement = GetComponent<LayoutElement>();
        if (backgroundImage == null) backgroundImage = GetComponent<Image>();
        
        if (transform.parent != null)
        {
            _siblingSlots = transform.parent.GetComponentsInChildren<UISaveSlot>();
        }
    }

    private void OnEnable()
    {
        ResetSlotStateInstantly();
        RefreshDataFromDisk();
    }

    /// <summary>
    /// Queries the disk for corresponding save data and updates all physical UI labels and states.
    /// </summary>
    public void RefreshDataFromDisk()
    {
        if (SaveLoadManager.Instance == null) return;

        bool isMainMenu = SceneManager.GetActiveScene().name == "MainMenu_Scene";
        bool hasData = SaveLoadManager.Instance.DoesSaveExist(slotID);
        
        if (matrixHeaderText != null) matrixHeaderText.text = $"SECTOR_{slotID:D2} IDENTIFIED";

        if (hasData)
        {
            SaveData data = SaveLoadManager.Instance.ReadSaveData(slotID);
            bool isCorrupted = data.motherCorruptionPercent > 0.5f;

            _targetDecryptedText = isCorrupted ? "[ FATAL_RECORD ]" : "[ MEMORY_LOG ]";
            System.TimeSpan t = System.TimeSpan.FromSeconds(data.playTimeInSeconds);
            string timeStr = string.Format("{0:D2}:{1:D2}:{2:D2}", t.Hours, t.Minutes, t.Seconds);

            if (actionText != null) actionText.alignment = TextAlignmentOptions.TopRight;
            if (idText != null) idText.text = $"> BLK_0{slotID}";
            if (dataLeftText != null) dataLeftText.text = $"LOC: <color=#596877>{data.currentDeckLocation}</color>\n<b><size=130%>UPTIME: <color=#596877>{timeStr}</color></size></b>";
            
            if (dataRightText != null)
            {
                if (isCorrupted) dataRightText.text = $"<color=#ff003c>CORRUPTION: {Mathf.RoundToInt(data.motherCorruptionPercent * 100)}%</color>";
                else dataRightText.text = ""; 
            }

            if (backgroundImage != null) backgroundImage.color = isCorrupted ? colorDanger : colorNormal;
            if (hoverWipe != null) hoverWipe.color = isCorrupted ? hoverRed : hoverCyan;

            if (cmdLoad != null) { cmdLoad.gameObject.SetActive(true); cmdLoad.txtIndex.text = "// 01"; cmdLoad.txtCommand.text = "EXECUTE LOAD"; }
            
            if (cmdOverwrite != null) { cmdOverwrite.gameObject.SetActive(!isMainMenu); cmdOverwrite.txtIndex.text = "// 02"; cmdOverwrite.txtCommand.text = "FORCE OVERWRITE"; }
            
            if (cmdDelete != null) { cmdDelete.gameObject.SetActive(true); cmdDelete.txtIndex.text = "// 03"; cmdDelete.txtCommand.text = "PURGE DATA"; }
            if (cmdAbort != null) { cmdAbort.gameObject.SetActive(true); cmdAbort.txtIndex.text = "// 04"; cmdAbort.txtCommand.text = "ABORT SEQUENCE"; }
        }
        else
        {
            _targetDecryptedText = "[ UNALLOCATED BUFFER ]";
            if (actionText != null) actionText.alignment = TextAlignmentOptions.Center;
            if (idText != null) idText.text = "";
            if (dataLeftText != null) dataLeftText.text = "";
            if (dataRightText != null) dataRightText.text = "";
            if (backgroundImage != null) backgroundImage.color = colorNormal;
            if (hoverWipe != null) hoverWipe.color = hoverCyan;

            if (cmdLoad != null) cmdLoad.gameObject.SetActive(false);
            if (cmdDelete != null) cmdDelete.gameObject.SetActive(false);
            
            if (cmdOverwrite != null) { cmdOverwrite.gameObject.SetActive(!isMainMenu); cmdOverwrite.txtIndex.text = "// 01"; cmdOverwrite.txtCommand.text = "ALLOCATE BUFFER"; }
            
            if (cmdAbort != null) { cmdAbort.gameObject.SetActive(true); cmdAbort.txtIndex.text = "// 02"; cmdAbort.txtCommand.text = "ABORT SEQUENCE"; }
        }

        if (fillLoad) fillLoad.fillAmount = 0;
        if (fillOverwrite) fillOverwrite.fillAmount = 0;
        if (fillDelete) fillDelete.fillAmount = 0;

        char[] encryptedArr = _targetDecryptedText.ToCharArray();
        for (int i = 0; i < encryptedArr.Length; i++)
        {
            if (char.IsLetterOrDigit(encryptedArr[i])) encryptedArr[i] = '█';
        }
        _encryptedString = new string(encryptedArr);

        if (actionText != null) actionText.text = _encryptedString;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isExpanded || _isExecuting) return;
        _isExpanded = true;
        
        if (SystemLogger.Instance != null) 
            SystemLogger.Instance.Log($"TERMINAL EXPANDED. SECTOR 0{slotID} ACCESSED.", "#FFAA00");
        if (hoverWipe != null) hoverWipe.fillAmount = 0;
        
        if (_siblingSlots != null)
        {
            foreach (var slot in _siblingSlots)
            {
                if (slot != this && slot != null) slot.CrushSlot();
            }
        }

        if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
        _animationCoroutine = StartCoroutine(AnimateSlot(heightExpanded, 0f, 1f, colorExpanded));
    }

    /// <summary>
    /// Animates the slot collapsing to its smallest vertical dimension to make room for siblings.
    /// </summary>
    public void CrushSlot()
    {
        _isExpanded = false;
        if (hoverWipe != null) hoverWipe.fillAmount = 0;
        if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
        _animationCoroutine = StartCoroutine(AnimateSlot(heightCrushed, 0f, 0f, colorNormal));
    }

    /// <summary>
    /// Immediately forces the slot back to its unexpanded, default appearance without animations.
    /// </summary>
    public void ResetSlotStateInstantly()
    {
        _isExpanded = false;
        _isExecuting = false;
        if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
        
        layoutElement.preferredHeight = heightResting;
        if (containerReadout != null) 
        {
            containerReadout.alpha = 1f;
            containerReadout.transform.localPosition = new Vector3(containerReadout.transform.localPosition.x, 0f, 0f);
        }
        if (watermarkGroup != null) watermarkGroup.alpha = 0f;
        if (hoverWipe != null) hoverWipe.fillAmount = 0f;

        if (containerMatrix != null)
        {
            containerMatrix.alpha = 0f;
            containerMatrix.interactable = false;
            containerMatrix.blocksRaycasts = false;
            containerMatrix.transform.localPosition = new Vector3(containerMatrix.transform.localPosition.x, -20f, 0f);
        }
        if (backgroundImage != null) backgroundImage.color = colorNormal;
    }

    public void Command_Load() 
    { 
        if (_isExecuting) return;
        StartCoroutine(ExecuteActionRoutine(fillLoad, "LOAD", () => {
            SaveLoadManager.Instance.LoadGame(slotID);
        }));
    }
    
    public void Command_Overwrite() 
    { 
        if (_isExecuting) return;
        StartCoroutine(ExecuteActionRoutine(fillOverwrite, "SAVE", () => {
            SaveLoadManager.Instance.SaveGame(slotID);
        }));
    }
    
    public void Command_Delete() 
    { 
        if (_isExecuting) return;
        StartCoroutine(ExecuteActionRoutine(fillDelete, "PURGE", () => {
            SaveLoadManager.Instance.DeleteSaveGame(slotID);
        }));
    }
    
    public void Command_Abort() 
    { 
        if (_isExecuting) return;
        if (SystemLogger.Instance != null) 
            SystemLogger.Instance.Log("OPERATION ABORTED BY USER.", "#5E7382");
        ReleaseAllSlots(); 
    }

    private IEnumerator ExecuteActionRoutine(Image targetFill, string flashType, System.Action finalAction)
    {
        _isExecuting = true;
        if (SystemLogger.Instance != null)
        {
            SystemLogger.Instance.Log($"INITIATING DIRECTIVE: [{flashType}]...", "#FFAA00");
            if (flashType == "LOAD") SystemLogger.Instance.Log("DECRYPTING ARCHIVE...", "#00F0FF"); 
            else if (flashType == "PURGE") SystemLogger.Instance.Log("OVERRIDING SAFETY PROTOCOLS...", "#FF003C");
            else SystemLogger.Instance.Log("PACKING AETHER-CORE GRID POSITIONS...", "#00F0FF"); 
        }

        if (targetFill != null)
        {
            float timer = 0f;
            float duration = 0.6f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                targetFill.fillAmount = timer / duration;
                yield return null;
            }
            targetFill.fillAmount = 1f;
        }

        if (SystemLogger.Instance != null)
        {
            if (flashType == "LOAD") SystemLogger.Instance.Log("RESTORING PHYSICAL SUIT STATE...", "#00F0FF");
            else if (flashType == "PURGE") SystemLogger.Instance.Log("PURGING SECTOR DATA...", "#FF003C"); 
            else SystemLogger.Instance.Log("WRITING TO PHYSICAL DISK...", "#00F0FF");
        }

        if (SystemSyncFX.Instance != null) SystemSyncFX.Instance.ExecuteFlash(flashType);

        finalAction?.Invoke();
        
        if (SystemLogger.Instance != null && flashType != "PURGE")
        {
            SystemLogger.Instance.Log("CHECKSUM VALID. DATA SECURED.", "#00FF66");
        }

        ReleaseAllSlots();
        _isExecuting = false;
    }

    private void ReleaseAllSlots()
    {
        if (_siblingSlots != null)
        {
            foreach (var slot in _siblingSlots)
            {
                if (slot == null) continue;
                slot._isExpanded = false;
                slot._isExecuting = false;
                if (slot._animationCoroutine != null) StopCoroutine(slot._animationCoroutine);
                slot._animationCoroutine = StartCoroutine(slot.AnimateSlot(slot.heightResting, 1f, 0f, slot.colorNormal));
                slot.RefreshDataFromDisk();
            }
        }
    }

    private IEnumerator AnimateSlot(float targetHeight, float targetReadoutAlpha, float targetMatrixAlpha, Color targetColor)
    {
        float timer = 0;
        float duration = 0.35f; 

        float startHeight = layoutElement.preferredHeight;
        float startReadoutAlpha = containerReadout != null ? containerReadout.alpha : 1f;
        float startMatrixAlpha = containerMatrix != null ? containerMatrix.alpha : 0f;
        float startWatermarkAlpha = watermarkGroup != null ? watermarkGroup.alpha : 0f;
        Color startColor = backgroundImage != null ? backgroundImage.color : colorNormal;
        
        Vector3 startMatrixPos = containerMatrix != null ? containerMatrix.transform.localPosition : Vector3.zero;
        float targetMatrixY = targetMatrixAlpha > 0.5f ? 0f : -20f; 
        
        Vector3 startReadoutPos = containerReadout != null ? containerReadout.transform.localPosition : Vector3.zero;
        float targetReadoutY = targetReadoutAlpha > 0.5f ? 0f : -20f; 

        if (containerMatrix != null)
        {
            containerMatrix.interactable = false;
            containerMatrix.blocksRaycasts = false;
        }

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / duration;
            t = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
            
            layoutElement.preferredHeight = Mathf.Lerp(startHeight, targetHeight, t);
            
            if (containerReadout != null) 
            {
                containerReadout.alpha = Mathf.Lerp(startReadoutAlpha, targetReadoutAlpha, t);
                containerReadout.transform.localPosition = new Vector3(startReadoutPos.x, Mathf.Lerp(startReadoutPos.y, targetReadoutY, t), startReadoutPos.z);
            }
            
            if (containerMatrix != null) 
            {
                containerMatrix.alpha = Mathf.Lerp(startMatrixAlpha, targetMatrixAlpha, t);
                containerMatrix.transform.localPosition = new Vector3(startMatrixPos.x, Mathf.Lerp(startMatrixPos.y, targetMatrixY, t), startMatrixPos.z);
            }
            
            if (watermarkGroup != null) watermarkGroup.alpha = Mathf.Lerp(startWatermarkAlpha, targetMatrixAlpha, t);
            if (backgroundImage != null) backgroundImage.color = Color.Lerp(startColor, targetColor, t);

            yield return null;
        }

        layoutElement.preferredHeight = targetHeight;
        if (containerReadout != null) 
        {
            containerReadout.alpha = targetReadoutAlpha;
            containerReadout.transform.localPosition = new Vector3(startReadoutPos.x, targetReadoutY, startReadoutPos.z);
        }
        if (watermarkGroup != null) watermarkGroup.alpha = targetMatrixAlpha;
        if (containerMatrix != null)
        {
            containerMatrix.alpha = targetMatrixAlpha;
            containerMatrix.transform.localPosition = new Vector3(startMatrixPos.x, targetMatrixY, startMatrixPos.z);
            if (targetMatrixAlpha >= 1f)
            {
                containerMatrix.interactable = true;
                containerMatrix.blocksRaycasts = true;
            }
        }
        if (backgroundImage != null) backgroundImage.color = targetColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_isExpanded) return;
        
        if (_scrambleCoroutine != null) StopCoroutine(_scrambleCoroutine);
        _scrambleCoroutine = StartCoroutine(ScrambleTextRoutine(_targetDecryptedText));

        if (_hoverCoroutine != null) StopCoroutine(_hoverCoroutine);
        _hoverCoroutine = StartCoroutine(AnimateHoverWipe(1f));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_isExpanded) return;
        
        if (_scrambleCoroutine != null) StopCoroutine(_scrambleCoroutine);
        if (actionText != null) actionText.text = _encryptedString;

        if (_hoverCoroutine != null) StopCoroutine(_hoverCoroutine);
        _hoverCoroutine = StartCoroutine(AnimateHoverWipe(0f));
    }

    private IEnumerator AnimateHoverWipe(float targetFill)
    {
        if (hoverWipe == null) yield break;
        float startFill = hoverWipe.fillAmount;
        float timer = 0f;
        float duration = 0.15f;
        
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            hoverWipe.fillAmount = Mathf.Lerp(startFill, targetFill, timer / duration);
            yield return null;
        }
        hoverWipe.fillAmount = targetFill;
    }

    private IEnumerator ScrambleTextRoutine(string targetText)
    {
        if (actionText == null || string.IsNullOrEmpty(targetText)) yield break;
        float iter = 0; 
        int len = targetText.Length;
        string glyphs = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!<>-_\\/[]{}—=+*^?#";
        char[] currentText = new char[len];
        
        while (iter < len)
        {
            for (int i = 0; i < len; i++)
            {
                if (i < iter) currentText[i] = targetText[i];
                else currentText[i] = glyphs[Random.Range(0, glyphs.Length)];
            }
            actionText.text = new string(currentText);
            iter += 0.5f; 
            yield return new WaitForSecondsRealtime(0.02f);
        }
        actionText.text = targetText;
    }

    public void SetHoverBackground(Color hoverColor)
    {
        if (_isExpanded && backgroundImage != null) backgroundImage.color = hoverColor;
    }

    public void ResetHoverBackground()
    {
        if (_isExpanded && backgroundImage != null) backgroundImage.color = colorExpanded;
    }
}
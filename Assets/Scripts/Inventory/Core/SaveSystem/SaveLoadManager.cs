using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// Coordinates global data persistence, handling the saving and loading of the entire game state.
/// </summary>
public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance;

    [HideInInspector]
    public static int pendingLoadSlot = -1;

    [Header("World References")]
    [Tooltip("Reference to the player avatar.")]
    public Transform playerKaelen; 
    [Tooltip("Reference to the antagonist AI.")]
    public Transform enemyProxy;       
    [Tooltip("Reference to the central inventory manager.")]
    public InventoryManager metRigInventory; 

    [Header("Dynamic Telemetry")]
    [Tooltip("The display name of the player's current location.")]
    public string currentZoneName = "USC WAYFARER - UNKNOWN DECK";
    private float _accumulatedPlayTime = 0f; 
    private float _sessionStartTime = 0f;    

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _sessionStartTime = Time.unscaledTime;
    }

    private void Start()
    {
        if (pendingLoadSlot != -1)
        {
            int slotToLoad = pendingLoadSlot;
            pendingLoadSlot = -1; 
            
            LoadGame(slotToLoad, true); 
        }
    }

    private string GetSavePath(int slotIndex)
    {
        return Path.Combine(Application.persistentDataPath, $"aether_sync_{slotIndex}.json");
    }

    public bool DoesSaveExist(int slotIndex)
    {
        return File.Exists(GetSavePath(slotIndex));
    }

    /// <summary>
    /// Compiles all active game states into a SaveData object and writes it to a JSON file.
    /// </summary>
    /// <param name="slotIndex">The save slot to write to (0, 1, or 2).</param>
    public void SaveGame(int slotIndex)
    {
        SaveData data = new SaveData();

        float currentSessionTime = Time.unscaledTime - _sessionStartTime;
        data.playTimeInSeconds = _accumulatedPlayTime + currentSessionTime; 
        data.currentDeckLocation = currentZoneName; 
        data.currentSceneName = SceneManager.GetActiveScene().name;
        data.saveDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        if (playerKaelen != null)
        {
            data.kaelenPosition = playerKaelen.position;
            
            PlayerController kaelenController = playerKaelen.GetComponent<PlayerController>();
            if (kaelenController != null)
            {
                data.sprintMeter = kaelenController.SprintMeter;
                data.sprintThreshold = kaelenController.SprintMeterThreshold;
            }
        }

        if (enemyProxy != null)
        {
            data.enemyPosition = enemyProxy.position;
            data.isEnemyActive = enemyProxy.gameObject.activeSelf;
        }

        if (UI_ParasiteOverride.Instance != null)
        {
            data.parasiteTimer = UI_ParasiteOverride.Instance.GetCurrentTimer();
            data.parasiteStacks = UI_ParasiteOverride.Instance.currentStacks;
        }

        UIPurgeSystem purgeSystem = FindAnyObjectByType<UIPurgeSystem>();
        if (purgeSystem != null)
        {
            data.purgeCooldownTimer = purgeSystem.GetCurrentCooldown();
        }

        if (metRigInventory != null)
        {
            metRigInventory.SyncDataFromUI();
            data.motherCorruptionPercent = metRigInventory.GetCorruptionPercentage();
            if (InventorySaveHandler.Instance != null) data.gridInventoryItems = InventorySaveHandler.Instance.ExportInventoryForSave();
        }

        WriteSaveData(slotIndex, data);

        _accumulatedPlayTime = data.playTimeInSeconds;
        _sessionStartTime = Time.unscaledTime;
    }

    /// <summary>
    /// Reads a save file and applies its data to restore the game state. Can handle loading from the main menu.
    /// </summary>
    /// <param name="slotIndex">The save slot to load from.</param>
    /// <param name="isStartupLoad">Is this load being triggered on game startup?</param>
    public void LoadGame(int slotIndex, bool isStartupLoad = false)
    {
        SaveData data = ReadSaveData(slotIndex);

        if (playerKaelen == null) 
        {
            Debug.Log($"<color=cyan>SYSTEM SYNC:</color> Sector 0{slotIndex} located. Booting sequence...");
            pendingLoadSlot = slotIndex; 
            string sceneToLoad = (data != null && !string.IsNullOrEmpty(data.currentSceneName)) ? data.currentSceneName : "level_1";
            SceneManager.LoadScene(sceneToLoad);
            return; 
        }

        if (data != null)
        {
            _accumulatedPlayTime = data.playTimeInSeconds;
            _sessionStartTime = Time.unscaledTime;
            currentZoneName = data.currentDeckLocation;

            if (playerKaelen != null)
            {
                playerKaelen.position = data.kaelenPosition;
                
                PlayerController kaelenController = playerKaelen.GetComponent<PlayerController>();
                if (kaelenController != null) kaelenController.LoadStaminaState(data.sprintMeter, data.sprintThreshold);
            }

            if (enemyProxy != null)
            {
                enemyProxy.position = data.enemyPosition;
                enemyProxy.gameObject.SetActive(data.isEnemyActive);
            }

            if (metRigInventory != null)
            {
                if (InventorySaveHandler.Instance != null) InventorySaveHandler.Instance.LoadInventoryFromSave(data.gridInventoryItems, data.motherCorruptionPercent);
            }

            if (UI_ParasiteOverride.Instance != null)
            {
                UI_ParasiteOverride.Instance.LoadParasiteData(data.parasiteStacks, data.parasiteTimer);
            }

            UIPurgeSystem purgeSystem = FindAnyObjectByType<UIPurgeSystem>();
            if (purgeSystem != null)
            {
                purgeSystem.LoadCooldownState(data.purgeCooldownTimer);
            }

            Debug.Log($"<color=cyan>SYSTEM SYNC:</color> Successfully loaded memory sector 0{slotIndex}.");

            PauseManager pauseManager = FindAnyObjectByType<PauseManager>();
            if (pauseManager != null) 
            {
                if (isStartupLoad) 
                {
                    pauseManager.ForceResumeGame();
                }
                else 
                {
                    pauseManager.TogglePause();
                }
            }
        }
    }

    public void DeleteSaveGame(int slotIndex)
    {
        string path = GetSavePath(slotIndex);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"<color=red>SYSTEM ALERT:</color> Memory Sector 0{slotIndex} permanently purged.");
        }
        else
        {
            Debug.LogWarning("Cannot purge. Sector is already empty.");
        }
    }

    private void WriteSaveData(int slotIndex, SaveData data)
    {
        string path = GetSavePath(slotIndex);
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
        Debug.Log($"<color=cyan>SYSTEM SYNC:</color> Data securely written to {path}");
    }

    public SaveData ReadSaveData(int slotIndex)
    {
        string path = GetSavePath(slotIndex);
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<SaveData>(json);
        }
        return null; 
    }
}
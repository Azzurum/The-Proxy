using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance;

    // Remembers which slot to load when transitioning from the Main Menu
    public static int pendingLoadSlot = -1;

    [Header("World References")]
    public Transform playerKaelen; 
    public Transform enemyProxy;       
    public InventoryManager metRigInventory; 

    [Header("Dynamic Telemetry")]
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
        // If we just arrived from the Main Menu, trigger the delayed load
        if (pendingLoadSlot != -1)
        {
            int slotToLoad = pendingLoadSlot;
            pendingLoadSlot = -1; // Reset memory immediately to prevent load loops
            
            // Add 'true' here to tell the script this is a startup load!
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

    // ==========================================
    // DATA HARVESTING & RESTORATION
    // ==========================================
    public void SaveGame(int slotIndex)
    {
        SaveData data = new SaveData();

        // 1. Gather TRUE Playtime & Location
        float currentSessionTime = Time.unscaledTime - _sessionStartTime;
        data.playTimeInSeconds = _accumulatedPlayTime + currentSessionTime; 
        data.currentDeckLocation = currentZoneName; 

        // 2. Gather Kaelen's Physical State & STAMINA
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

        // 3. Gather Enemy State
        if (enemyProxy != null)
        {
            data.enemyPosition = enemyProxy.position;
            data.isEnemyActive = enemyProxy.gameObject.activeSelf;
        }

        // 4. Gather Parasite HUD
        if (UI_ParasiteOverride.Instance != null)
        {
            data.parasiteTimer = UI_ParasiteOverride.Instance.GetCurrentTimer();
            data.parasiteStacks = UI_ParasiteOverride.Instance.currentStacks;
        }

        // 5. Gather Purge Cooldown
        UIPurgeSystem purgeSystem = FindAnyObjectByType<UIPurgeSystem>();
        if (purgeSystem != null)
        {
            data.purgeCooldownTimer = purgeSystem.GetCurrentCooldown();
        }

        // 6. Gather M.E.T. Rig Grid & Corruption
        if (metRigInventory != null)
        {
            metRigInventory.SyncDataFromUI();
            data.motherCorruptionPercent = metRigInventory.GetCorruptionPercentage();
            data.gridInventoryItems = metRigInventory.ExportInventoryForSave();
        }

        WriteSaveData(slotIndex, data);

        // Reset session time after saving to prevent double-counting
        _accumulatedPlayTime = data.playTimeInSeconds;
        _sessionStartTime = Time.unscaledTime;
    }

    public void LoadGame(int slotIndex, bool isStartupLoad = false)
    {
        // --- BULLETPROOF MENU CHECK ---
        // If Kaelen is completely empty/unassigned, we MUST be in the Main Menu!
        if (playerKaelen == null) 
        {
            Debug.Log($"<color=cyan>SYSTEM SYNC:</color> Sector 0{slotIndex} located. Booting sequence...");
            pendingLoadSlot = slotIndex; 
            SceneManager.LoadScene("MainGame");
            return; // Stop running this function, wait for MainGame to load
        }

        // --- ACTUAL LOAD LOGIC ---
        SaveData data = ReadSaveData(slotIndex);

        if (data != null)
        {
            _accumulatedPlayTime = data.playTimeInSeconds;
            _sessionStartTime = Time.unscaledTime;
            currentZoneName = data.currentDeckLocation;

            // 1. Restore Kaelen
            if (playerKaelen != null)
            {
                playerKaelen.position = data.kaelenPosition;
                
                PlayerController kaelenController = playerKaelen.GetComponent<PlayerController>();
                if (kaelenController != null) kaelenController.LoadStaminaState(data.sprintMeter, data.sprintThreshold);
            }

            // 2. Restore Enemy
            if (enemyProxy != null)
            {
                enemyProxy.position = data.enemyPosition;
                enemyProxy.gameObject.SetActive(data.isEnemyActive);
            }

            // 3. Restore Inventory (Crucial anti-bleed order)
            if (metRigInventory != null)
            {
                metRigInventory.LoadInventoryFromSave(data.gridInventoryItems, data.motherCorruptionPercent);
            }

            // 4. Restore Parasite HUD
            if (UI_ParasiteOverride.Instance != null)
            {
                UI_ParasiteOverride.Instance.LoadParasiteData(data.parasiteStacks, data.parasiteTimer);
            }

            // 5. Restore Purge Cooldown
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
                    // If we just booted from the Main Menu, FORCE the pause menu closed
                    pauseManager.ForceResumeGame();
                }
                else 
                {
                    // If we are already playing and just loaded a different save, flip it normally
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
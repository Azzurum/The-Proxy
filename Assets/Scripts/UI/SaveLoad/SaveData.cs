using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    // --- METADATA ---
    public string saveDate;
    public string currentDeckLocation; 
    public float playTimeInSeconds;

    // --- PHYSICAL STATE ---
    public Vector3 kaelenPosition;
    public Vector3 enemyPosition;        // NEW: Tracks the Proxy
    public bool isEnemyActive;           // NEW: Tracks if the Proxy is spawned
    
    // --- STAMINA & FATIGUE ---
    public float sprintMeter;
    public float sprintThreshold;

    // --- PARASITE HUD ---
    public float parasiteTimer;
    public int parasiteStacks;
    public float motherCorruptionPercent; 

    public float purgeCooldownTimer;
    // --- INVENTORY ---
    public List<SavedGridItem> gridInventoryItems = new List<SavedGridItem>();

    public SaveData()
    {
        saveDate = string.Empty;
        currentDeckLocation = "USC WAYFARER - UNKNOWN DECK";
        playTimeInSeconds = 0f;
        kaelenPosition = Vector3.zero;
        enemyPosition = Vector3.zero;
        isEnemyActive = false;
        
        sprintMeter = 0f;
        sprintThreshold = 5f; 
        parasiteTimer = 0f;
        parasiteStacks = 0;
        motherCorruptionPercent = 0f;

        purgeCooldownTimer = 0f;
        
        gridInventoryItems = new List<SavedGridItem>();
    }
}

[System.Serializable]
public class SavedGridItem
{
    public string itemID; 
    public int gridPosX;
    public int gridPosY;
    public bool isRotated;

    public SavedGridItem(string id, int x, int y, bool rotated)
    {
        itemID = id;
        gridPosX = x;
        gridPosY = y;
        isRotated = rotated;
    }
}
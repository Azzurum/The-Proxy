using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A serialized data container representing the complete state of a player's game session.
/// </summary>
[System.Serializable]
public class SaveData
{
    public string saveDate;
    public string currentDeckLocation; 
    public string currentSceneName;
    public float playTimeInSeconds;

    public Vector3 kaelenPosition;
    public Vector3 enemyPosition;        
    public bool isEnemyActive;           
    
    public float sprintMeter;
    public float sprintThreshold;

    public float parasiteTimer;
    public int parasiteStacks;
    public float motherCorruptionPercent; 

    public float purgeCooldownTimer;
    public List<SavedGridItem> gridInventoryItems = new List<SavedGridItem>();

    public SaveData()
    {
        saveDate = string.Empty;
        currentDeckLocation = "USC WAYFARER - UNKNOWN DECK";
        currentSceneName = "level_1";
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

/// <summary>
/// A serialized representation of a single physical item residing on the inventory grid.
/// </summary>
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
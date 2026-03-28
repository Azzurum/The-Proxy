using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    [Header("Connected Grids")]
    public InventoryGrid mainRigGrid;
    public InventoryGrid externalStorageGrid;

    [Header("Visual UI Setup")]
    public GameObject slotPrefab;

    [Header("World Spawning")]
    public Transform playerTransform;
    public GameObject physicalBatteryPrefab;

    [Header("Corruption Setup")]
    public GameObject corruptionPrefab;

    [Header("MOTHER-v4 System Shock")]
    public float shockInterval = 10f;
    private float shockTimer;
    public bool isSystemActive = true;
    public Slider systemShockProgressBar;

    void Start()
    {
        if (mainRigGrid != null) mainRigGrid.activeItems.Clear();
        if (externalStorageGrid != null) externalStorageGrid.activeItems.Clear();

        if (mainRigGrid != null) mainRigGrid.InitializeGridVisuals(slotPrefab);
        if (externalStorageGrid != null)
        {
            externalStorageGrid.InitializeGridVisuals(slotPrefab);
            externalStorageGrid.gameObject.SetActive(false);
        }

        if (mainRigGrid != null && externalStorageGrid != null)
        {
            RectTransform mainRect = mainRigGrid.GetComponent<RectTransform>();
            mainRect.anchorMin = new Vector2(0.5f, 0.5f);
            mainRect.anchorMax = new Vector2(0.5f, 0.5f);
            mainRect.pivot = new Vector2(0.5f, 0.5f);
            mainRect.anchoredPosition = Vector2.zero;

            RectTransform extRect = externalStorageGrid.GetComponent<RectTransform>();
            extRect.SetParent(mainRect.parent);
            extRect.anchorMin = new Vector2(0.5f, 0.5f);
            extRect.anchorMax = new Vector2(0.5f, 0.5f);
            extRect.pivot = new Vector2(0.5f, 0.5f);
            extRect.anchoredPosition = new Vector2(540f, 0f);
        }

        shockTimer = shockInterval;
    }

    void Update()
    {
        if (isSystemActive)
        {
            shockTimer -= Time.deltaTime;
            if (systemShockProgressBar != null) systemShockProgressBar.value = 1f - (shockTimer / shockInterval);

            if (shockTimer <= 0f)
            {
                ResolveCorruptionTick();
                shockTimer = shockInterval;
            }
        }
        if (Input.GetKeyDown(KeyCode.C)) ExecuteCleanProtocol();
    }

    public void ResolveCorruptionTick()
    {
        // 1. Move all grid items UP
        for (int i = 0; i < mainRigGrid.activeItems.Count; i++)
        {
            InventoryItem item = mainRigGrid.activeItems[i];
            item.position = new Vector2Int(item.position.x, item.position.y + 1);
            mainRigGrid.activeItems[i] = item;

            if (item.uiObject != null)
            {
                RectTransform rect = item.uiObject.GetComponent<RectTransform>();
                rect.localPosition = mainRigGrid.GetSnapPosition(item.position, item.size.x, item.size.y);
            }
        }

        // --- NEW: Shift the Dragged Item's Live Memory UP ---
        if (DraggableItem.itemBeingDragged != null && DraggableItem.itemBeingDragged.originalGrid == mainRigGrid)
        {
            DraggableItem.itemBeingDragged.originalAnchorCoord.y += 1;
            DraggableItem.itemBeingDragged.originalLocalPosition.y += 80f;

            // If it gets pushed completely out of the grid while we are holding it, flag it for ejection
            int topEdge = DraggableItem.itemBeingDragged.originalAnchorCoord.y + DraggableItem.itemBeingDragged.sizeY - 1;
            if (topEdge > 9) DraggableItem.itemBeingDragged.ejectedFromRig = true;
        }

        // 2. Eject top items
        for (int i = mainRigGrid.activeItems.Count - 1; i >= 0; i--)
        {
            InventoryItem item = mainRigGrid.activeItems[i];
            int topEdge = item.position.y + item.size.y - 1;

            if (topEdge > 9)
            {
                if (!item.isCorruption && item.uiObject != null && physicalBatteryPrefab != null && playerTransform != null)
                {
                    Instantiate(physicalBatteryPrefab, playerTransform.position, Quaternion.identity);
                }
                if (item.uiObject != null) Destroy(item.uiObject);
                mainRigGrid.activeItems.RemoveAt(i);
            }
        }
        SpawnCorruptionAtRowZero();
    }

    public void ExecuteCleanProtocol()
    {
        bool didCleanAnything = false;
        for (int i = mainRigGrid.activeItems.Count - 1; i >= 0; i--)
        {
            InventoryItem item = mainRigGrid.activeItems[i];
            if (item.isCorruption && item.position.y == 0)
            {
                if (item.uiObject != null) Destroy(item.uiObject);
                mainRigGrid.activeItems.RemoveAt(i);
                didCleanAnything = true;
            }
        }

        if (didCleanAnything)
        {
            // Move grid items DOWN
            for (int i = 0; i < mainRigGrid.activeItems.Count; i++)
            {
                InventoryItem item = mainRigGrid.activeItems[i];
                item.position = new Vector2Int(item.position.x, item.position.y - 1);
                mainRigGrid.activeItems[i] = item;

                if (item.uiObject != null)
                {
                    RectTransform rect = item.uiObject.GetComponent<RectTransform>();
                    rect.localPosition = mainRigGrid.GetSnapPosition(item.position, item.size.x, item.size.y);
                }
            }

            // --- NEW: Shift the Dragged Item's Live Memory DOWN ---
            if (DraggableItem.itemBeingDragged != null && DraggableItem.itemBeingDragged.originalGrid == mainRigGrid)
            {
                DraggableItem.itemBeingDragged.originalAnchorCoord.y -= 1;
                DraggableItem.itemBeingDragged.originalLocalPosition.y -= 80f;
                DraggableItem.itemBeingDragged.ejectedFromRig = false; // It is safe inside the grid again
            }
        }
    }

    private void SpawnCorruptionAtRowZero()
    {
        for (int x = 0; x < mainRigGrid.gridWidth; x++)
        {
            Vector2Int spawnCoord = new Vector2Int(x, 0);

            GameObject newBlock = Instantiate(corruptionPrefab, mainRigGrid.transform);
            RectTransform rect = newBlock.GetComponent<RectTransform>();

            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(80f, 80f);
            rect.localPosition = mainRigGrid.GetSnapPosition(spawnCoord, 1, 1);
            rect.localScale = Vector3.one;

            mainRigGrid.RegisterItem(newBlock, spawnCoord, 1, 1, false);
            mainRigGrid.activeItems[mainRigGrid.activeItems.Count - 1].isCorruption = true;
        }
    }

    public bool TryPickupItem(GameObject uiPrefabToSpawn, int sizeX, int sizeY, bool isQuestItem = false)
    {
        if (externalStorageGrid.activeItems.Count > 0)
        {
            Debug.LogWarning($"<color=yellow>BUFFER FULL:</color> You must organize your gear!");
            return false;
        }

        externalStorageGrid.gameObject.SetActive(true);
        externalStorageGrid.SetMode(3, false);

        Vector2Int memCoord = new Vector2Int(1, 8);
        if (sizeX == 3 && sizeY == 3) memCoord = new Vector2Int(0, 7);
        else if (sizeX == 2 && sizeY == 1) memCoord = new Vector2Int(0, 8);
        else if (sizeX == 1 && sizeY == 2) memCoord = new Vector2Int(1, 8);

        GameObject newItem = Instantiate(uiPrefabToSpawn, externalStorageGrid.transform);

        RectTransform rect = newItem.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(sizeX * 80f, sizeY * 80f);

        rect.localPosition = externalStorageGrid.GetSnapPosition(memCoord, sizeX, sizeY);
        rect.localScale = Vector3.one;
        Vector3 lp = rect.localPosition; lp.z = 0; rect.localPosition = lp;

        externalStorageGrid.RegisterItem(newItem, memCoord, sizeX, sizeY, false);
        return true;
    }

    public void DiscardItemToWorld(GameObject uiItem)
    {
        mainRigGrid.RemoveItem(uiItem);
        externalStorageGrid.RemoveItem(uiItem);

        if (physicalBatteryPrefab != null && playerTransform != null) Instantiate(physicalBatteryPrefab, playerTransform.position, Quaternion.identity);
        Destroy(uiItem);
    }

    public bool TryConsumeBatteries(int amountRequired)
    {
        List<InventoryItem> foundBatteries = new List<InventoryItem>();
        foreach (var item in mainRigGrid.activeItems)
        {
            if (!item.isCorruption && !item.isQuestItem) foundBatteries.Add(item);
        }

        if (foundBatteries.Count >= amountRequired)
        {
            for (int i = 0; i < amountRequired; i++)
            {
                InventoryItem batteryToBurn = foundBatteries[i];
                if (batteryToBurn.uiObject != null) Destroy(batteryToBurn.uiObject);
                mainRigGrid.activeItems.Remove(batteryToBurn);
            }
            return true;
        }
        return false;
    }
}
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIItem : MonoBehaviour
{
    public ItemData myData;
    
    [Header("Visual References")]
    public Image displayImage; 
    public Image backgroundImage; 

    private List<GameObject> generatedBackgroundBlocks = new List<GameObject>();

    public void Initialize(ItemData data, float cellSize)
    {
        myData = data;

        // FOOLPROOF TETHER: Auto-grab the root image if it's not assigned
        if (backgroundImage == null) backgroundImage = GetComponent<Image>();

        // THE FIX: Stop the root background from eating mouse clicks!
        if (backgroundImage != null) backgroundImage.raycastTarget = false;

        if (data != null && displayImage != null && data.icon != null)
        {
            // THE FIX: Stop the transparent pixels of the gun from blocking the empty space!
            displayImage.raycastTarget = false;
            displayImage.enabled = true;

            displayImage.sprite = data.icon;
            displayImage.preserveAspect = true; 
            
            RectTransform iconRect = displayImage.GetComponent<RectTransform>();
            if (iconRect != null)
            {
                iconRect.anchorMin = Vector2.zero; 
                iconRect.anchorMax = Vector2.one;  
                iconRect.offsetMin = Vector2.zero;   
                iconRect.offsetMax = Vector2.zero; 
            }
        }
        else if (displayImage != null)
        {
            displayImage.enabled = false;
        }

        DraggableItem drag = GetComponent<DraggableItem>();
        if (drag != null)
        {
            drag.itemData = data;
            drag.itemName = data.itemName;
            drag.itemDescription = data.description;
            drag.isRotated = data.isRotated; 

            ItemFootprint baseFp = data.GetFootprint();
            drag.SetFootprint(drag.isRotated ? baseFp.GetRotated() : baseFp);

            GenerateShapeBackground(baseFp, cellSize, drag);
        }
    }
    
    public void GenerateShapeBackground(ItemFootprint activeFootprint, float cellSize, DraggableItem drag)
    {
        // PERMANENTLY hide the giant rectangular bounding box
        if (backgroundImage != null) backgroundImage.enabled = false;

        foreach (GameObject block in generatedBackgroundBlocks) Destroy(block);
        generatedBackgroundBlocks.Clear();

        // --- HOLOGRAPHIC M.E.T. RIG COLORS ---
        bool isCorruption = myData != null && myData.itemID == "CRPT";
        Color fillNormal = isCorruption ? new Color(0.5f, 0f, 0f, 0.8f) : new Color(0f, 0.4f, 0.4f, 0.5f); // Translucent Dark Teal (or Dark Red)
        Color outlineNormal = isCorruption ? new Color(0.8f, 0f, 0f, 0.9f) : new Color(0f, 1f, 1f, 0.8f);  // Bright Neon Cyan (or Bright Red)

        for (int y = 0; y < activeFootprint.height; y++)
        {
            for (int x = 0; x < activeFootprint.width; x++)
            {
                if (activeFootprint.GetCell(x, y))
                {
                    // 1. THE OUTLINE (Parent Block)
                    // This block sits perfectly flush in the grid and holds the bright cyan color
                    GameObject outlineObj = new GameObject("BG_Outline_" + x + "_" + y);
                    outlineObj.transform.SetParent(this.transform, false);
                    outlineObj.transform.SetAsFirstSibling(); 
                    
                    Image outlineImg = outlineObj.AddComponent<Image>();
                    outlineImg.color = outlineNormal; 

                    RectTransform outlineRect = outlineObj.GetComponent<RectTransform>();
                    outlineRect.sizeDelta = new Vector2(cellSize, cellSize);
                    outlineRect.anchorMin = new Vector2(0f, 1f); 
                    outlineRect.anchorMax = new Vector2(0f, 1f);
                    outlineRect.pivot = new Vector2(0f, 1f);
                    outlineRect.anchoredPosition = new Vector2(x * cellSize, -y * cellSize);

                    // 2. THE FILL (Child Block)
                    // This block sits inside the outline and is shrunk to create the inward border
                    GameObject fillObj = new GameObject("Fill");
                    fillObj.transform.SetParent(outlineObj.transform, false);
                    
                    Image fillImg = fillObj.AddComponent<Image>();
                    fillImg.color = fillNormal; 

                    RectTransform fillRect = fillObj.GetComponent<RectTransform>();
                    fillRect.anchorMin = Vector2.zero; // Stretch to fill parent
                    fillRect.anchorMax = Vector2.one;
                    
                    // THIS CREATES THE INWARD OUTLINE (2 pixels thick on all sides)
                    float borderThickness = 2f; 
                    fillRect.offsetMin = new Vector2(borderThickness, borderThickness);
                    fillRect.offsetMax = new Vector2(-borderThickness, -borderThickness);

                    // Save the parent so we can destroy/toggle it later
                    generatedBackgroundBlocks.Add(outlineObj);
                }
            }
        }

        if (drag != null) drag.SetCellSize(cellSize);
    }

    public void SetTetrisGridVisibility(bool isVisible)
    {
        foreach (GameObject block in generatedBackgroundBlocks)
        {
            if (block != null) block.SetActive(isVisible);
        }
    }

    void Update()
    {
        // This ensures the custom L-shape flashes red when Kaelen tries to drop it in an invalid slot!
        if (backgroundImage != null && generatedBackgroundBlocks.Count > 0)
        {
            // Detect if DraggableItem is trying to flash the hidden background red
            bool isPulsingRed = backgroundImage.color.r > 0.5f && backgroundImage.color.g < 0.5f;
            bool isCorruption = myData != null && myData.itemID == "CRPT";

            foreach (GameObject block in generatedBackgroundBlocks)
            {
                if (block != null)
                {
                    // The parent object is our Outline
                    Image outlineImg = block.GetComponent<Image>();
                    
                    // The child object is our Fill
                    Image fillImg = null;
                    if (block.transform.childCount > 0)
                    {
                        fillImg = block.transform.GetChild(0).GetComponent<Image>();
                    }

                    if (outlineImg != null && fillImg != null)
                    {
                        if (isCorruption)
                        {
                            // Glitch effect for MOTHER-v4 Corruption blocks!
                            if (Random.value > 0.92f)
                            {
                                float rand = Random.value;
                                fillImg.color = rand > 0.5f ? new Color(1f, 0f, 0f, 0.9f) : new Color(0.1f, 0f, 0f, 0.8f);
                                outlineImg.color = rand > 0.8f ? Color.white : new Color(1f, 0.2f, 0.2f, 0.5f);
                                
                                // Randomly stretch the block to simulate UI tearing
                                outlineImg.rectTransform.localScale = new Vector3(Random.Range(0.9f, 1.1f), Random.Range(0.8f, 1.2f), 1f);
                            }
                            else
                            {
                                fillImg.color = new Color(0.5f, 0f, 0f, 0.8f); 
                                outlineImg.color = new Color(0.8f, 0f, 0f, 0.9f); 
                                outlineImg.rectTransform.localScale = Vector3.one;
                            }
                        }
                        else if (isPulsingRed)
                        {
                            fillImg.color = new Color(0.8f, 0f, 0f, 0.6f); // Warning Red Fill
                            outlineImg.color = Color.red;                  // Warning Red Outline
                        }
                        else
                        {
                            fillImg.color = new Color(0f, 0.4f, 0.4f, 0.5f);   // Normal Teal Fill
                            outlineImg.color = new Color(0f, 1f, 1f, 0.8f);    // Normal Cyan Outline
                        }
                    }
                }
            }
        }
    }
}
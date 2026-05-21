using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Handles the visual representation of an item on the UI grid, drawing its custom holographic footprint background.
/// </summary>
public class UIItem : MonoBehaviour
{
    [Tooltip("The core data reference driving this visual item.")]
    public ItemData myData;
    
    [Header("Visual References")]
    [Tooltip("The Image component displaying the item's sprite icon.")]
    public Image displayImage; 
    [Tooltip("The base Image component, usually disabled and replaced by procedural footprint blocks.")]
    public Image backgroundImage; 

    private List<GameObject> generatedBackgroundBlocks = new List<GameObject>();
    private RectTransform _cachedOutlineRect;

    /// <summary>
    /// Initializes the UI components and triggers the generation of the visual footprint.
    /// </summary>
    public void Initialize(ItemData data, float cellSize, bool overrideRotation = false)
    {
        myData = data;

        if (backgroundImage == null) backgroundImage = GetComponent<Image>();

        if (backgroundImage != null) backgroundImage.raycastTarget = false;

        if (data != null && displayImage != null && data.icon != null)
        {
            displayImage.raycastTarget = false;
            displayImage.enabled = true;

            displayImage.sprite = data.icon;
            displayImage.preserveAspect = true; 
            
            if (displayImage.TryGetComponent<RectTransform>(out var iconRect))
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

        if (TryGetComponent<DraggableItem>(out var drag))
        {
            drag.itemData = data;
            drag.itemName = data.itemName;
            drag.itemDescription = data.description;
            drag.isRotated = overrideRotation; 

            ItemFootprint baseFp = data.GetFootprint();
            drag.SetFootprint(drag.isRotated ? baseFp.GetRotated() : baseFp);

            GenerateShapeBackground(baseFp, cellSize, drag);
        }
    }
    
    /// <summary>
    /// Procedurally creates square UI images to represent the complex grid shape of the item.
    /// </summary>
    public void GenerateShapeBackground(ItemFootprint activeFootprint, float cellSize, DraggableItem drag)
    {
        if (backgroundImage != null) backgroundImage.enabled = false;

        foreach (GameObject block in generatedBackgroundBlocks) Destroy(block);
        generatedBackgroundBlocks.Clear();

        bool isCorruption = myData != null && myData.itemID == "CRPT";
        Color fillNormal = isCorruption ? new Color(0.5f, 0f, 0f, 0.8f) : new Color(0f, 0.4f, 0.4f, 0.5f); 
        Color outlineNormal = isCorruption ? new Color(0.8f, 0f, 0f, 0.9f) : new Color(0f, 1f, 1f, 0.8f);  

        for (int y = 0; y < activeFootprint.height; y++)
        {
            for (int x = 0; x < activeFootprint.width; x++)
            {
                if (activeFootprint.GetCell(x, y))
                {
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

                    GameObject fillObj = new GameObject("Fill");
                    fillObj.transform.SetParent(outlineObj.transform, false);
                    
                    Image fillImg = fillObj.AddComponent<Image>();
                    fillImg.color = fillNormal; 

                    RectTransform fillRect = fillObj.GetComponent<RectTransform>();
                    fillRect.anchorMin = Vector2.zero; 
                    fillRect.anchorMax = Vector2.one;
                    
                    float borderThickness = 2f; 
                    fillRect.offsetMin = new Vector2(borderThickness, borderThickness);
                    fillRect.offsetMax = new Vector2(-borderThickness, -borderThickness);

                    generatedBackgroundBlocks.Add(outlineObj);
                }
            }
        }

        if (drag != null) drag.SetCellSize(cellSize);
    }

    /// <summary>
    /// Toggles the rendering of the procedural background footprint without affecting the main icon.
    /// </summary>
    public void SetTetrisGridVisibility(bool isVisible)
    {
        foreach (GameObject block in generatedBackgroundBlocks)
        {
            if (block != null) block.SetActive(isVisible);
        }
    }

    void Update()
    {
        if (backgroundImage != null && generatedBackgroundBlocks.Count > 0)
        {
            bool isPulsingRed = backgroundImage.color.r > 0.5f && backgroundImage.color.g < 0.5f;
            bool isCorruption = myData != null && myData.itemID == "CRPT";

            foreach (GameObject block in generatedBackgroundBlocks)
            {
                if (block != null)
                {
                    if (block.TryGetComponent<Image>(out var outlineImg) && block.transform.childCount > 0 && block.transform.GetChild(0).TryGetComponent<Image>(out var fillImg))
                    {
                        if (_cachedOutlineRect == null) _cachedOutlineRect = outlineImg.rectTransform;
                        
                        if (isCorruption)
                        {
                            if (Random.value > 0.92f)
                            {
                                float rand = Random.value;
                                fillImg.color = rand > 0.5f ? new Color(1f, 0f, 0f, 0.9f) : new Color(0.1f, 0f, 0f, 0.8f);
                                outlineImg.color = rand > 0.8f ? Color.white : new Color(1f, 0.2f, 0.2f, 0.5f);
                                
                                if (_cachedOutlineRect != null) _cachedOutlineRect.localScale = new Vector3(Random.Range(0.9f, 1.1f), Random.Range(0.8f, 1.2f), 1f);
                            }
                            else
                            {
                                fillImg.color = new Color(0.5f, 0f, 0f, 0.8f); 
                                outlineImg.color = new Color(0.8f, 0f, 0f, 0.9f); 
                                if (_cachedOutlineRect != null) _cachedOutlineRect.localScale = Vector3.one;
                            }
                        }
                        else if (isPulsingRed)
                        {
                            fillImg.color = new Color(0.8f, 0f, 0f, 0.6f); 
                            outlineImg.color = Color.red;                  
                        }
                        else
                        {
                            fillImg.color = new Color(0f, 0.4f, 0.4f, 0.5f);   
                            outlineImg.color = new Color(0f, 1f, 1f, 0.8f);    
                        }
                    }
                }
            }
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// A mesh modifier that overlays a two-tone vertical gradient directly onto a UI element's vertex data.
/// </summary>
public class UIVerticalGradient : BaseMeshEffect
{
    [Header("Gradient Colors")]
    [Tooltip("Color applied to the top and bottom edges of the UI element.")]
    public Color edgeColor = new Color(0, 0.94f, 1f, 0f);    
    [Tooltip("Color applied to the exact center line of the UI element.")]
    public Color centerColor = new Color(0, 0.94f, 1f, 0.4f); 

    private readonly List<UIVertex> _verts = new List<UIVertex>();

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        _verts.Clear();
        vh.GetUIVertexStream(_verts);
        if (_verts.Count == 0) return;

        vh.Clear();

        Rect rect = GetComponent<RectTransform>().rect;
        float xMin = rect.xMin;
        float xMax = rect.xMax;
        float yMin = rect.yMin;
        float yMax = rect.yMax;
        float yMid = rect.center.y;

        AddVert(vh, xMin, yMax, edgeColor);
        AddVert(vh, xMax, yMax, edgeColor);
        
        AddVert(vh, xMin, yMid, centerColor);
        AddVert(vh, xMax, yMid, centerColor);
        
        AddVert(vh, xMin, yMin, edgeColor);
        AddVert(vh, xMax, yMin, edgeColor);

        vh.AddTriangle(0, 1, 3);
        vh.AddTriangle(3, 2, 0);
        
        vh.AddTriangle(2, 3, 5);
        vh.AddTriangle(5, 4, 2);
    }

    private void AddVert(VertexHelper vh, float x, float y, Color color)
    {
        UIVertex v = UIVertex.simpleVert;
        v.position = new Vector3(x, y, 0);
        v.color = color;
        vh.AddVert(v);
    }
}
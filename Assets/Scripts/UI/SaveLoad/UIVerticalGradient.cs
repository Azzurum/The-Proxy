using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIVerticalGradient : BaseMeshEffect
{
    public Color edgeColor = new Color(0, 0.94f, 1f, 0f);    // Transparent Cyan
    public Color centerColor = new Color(0, 0.94f, 1f, 0.4f); // Glowing Center

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        // 1. Get the current corners
        List<UIVertex> verts = new List<UIVertex>();
        vh.GetUIVertexStream(verts);
        if (verts.Count == 0) return;

        // 2. Clear the mesh so we can rebuild it with extra "rows"
        vh.Clear();

        // Calculate positions
        Rect rect = GetComponent<RectTransform>().rect;
        float xMin = rect.xMin;
        float xMax = rect.xMax;
        float yMin = rect.yMin;
        float yMax = rect.yMax;
        float yMid = rect.center.y;

        // 3. Add 6 Vertices (Top Row, Middle Row, Bottom Row)
        // Top Row (Transparent)
        AddVert(vh, xMin, yMax, edgeColor);
        AddVert(vh, xMax, yMax, edgeColor);
        
        // Middle Row (Solid/Glowing)
        AddVert(vh, xMin, yMid, centerColor);
        AddVert(vh, xMax, yMid, centerColor);
        
        // Bottom Row (Transparent)
        AddVert(vh, xMin, yMin, edgeColor);
        AddVert(vh, xMax, yMin, edgeColor);

        // 4. Connect them into 4 Triangles (making two quads)
        // Top Quad
        vh.AddTriangle(0, 1, 3);
        vh.AddTriangle(3, 2, 0);
        
        // Bottom Quad
        vh.AddTriangle(2, 3, 5);
        vh.AddTriangle(5, 4, 2);
    }

    void AddVert(VertexHelper vh, float x, float y, Color color)
    {
        UIVertex v = UIVertex.simpleVert;
        v.position = new Vector3(x, y, 0);
        v.color = color;
        vh.AddVert(v);
    }
}
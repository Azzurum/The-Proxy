using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Procedurally generates fractured glass mesh shards based on screen dimensions for shatter effects.
/// </summary>
public class ProceduralGlassGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    [Tooltip("The material applied to the generated glass shards.")]
    public Material glassMaterial;

    /// <summary>
    /// Calculates screen boundaries and constructs individual shard meshes.
    /// </summary>
    public void GenerateShards()
    {
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);

        Camera cam = Camera.main;
        float zDistance = Mathf.Abs(transform.localPosition.z);
        float screenHeight;

        if (cam.orthographic) screenHeight = cam.orthographicSize * 2f;
        else screenHeight = 2.0f * zDistance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);

        screenHeight *= 1.05f;
        
        float halfH = screenHeight / 2f;
        float aspect = cam.aspect; 

        Vector2 pC = new Vector2(-0.05f * aspect, 0.05f);

        Vector2[] c0 = { pC, new Vector2(0.1f * aspect, 0.4f),  new Vector2(0.0f * aspect, 0.8f),  new Vector2(0.2f * aspect, 1.2f) };
        Vector2[] c1 = { pC, new Vector2(0.4f * aspect, 0.2f),  new Vector2(0.7f * aspect, 0.3f),  new Vector2(1.2f * aspect, 0.1f) };
        Vector2[] c2 = { pC, new Vector2(0.3f * aspect, -0.3f), new Vector2(0.5f * aspect, -0.6f), new Vector2(0.4f * aspect, -1.2f) };
        Vector2[] c3 = { pC, new Vector2(-0.2f * aspect, -0.4f),new Vector2(-0.1f * aspect, -0.8f),new Vector2(-0.4f * aspect, -1.2f) };
        Vector2[] c4 = { pC, new Vector2(-0.5f * aspect, -0.1f),new Vector2(-0.8f * aspect, -0.3f),new Vector2(-1.2f * aspect, -0.2f) };
        Vector2[] c5 = { pC, new Vector2(-0.4f * aspect, 0.3f), new Vector2(-0.6f * aspect, 0.6f), new Vector2(-0.2f * aspect, 1.2f) };

        Vector2 tr = new Vector2(aspect, 1.0f);
        Vector2 br = new Vector2(aspect, -1.0f);
        Vector2 bl = new Vector2(-aspect, -1.0f);
        Vector2 tl = new Vector2(-aspect, 1.0f);

        BuildShard(c5, c0, new Vector2[] { }, halfH, 0);            
        BuildShard(c0, c1, new Vector2[] { tr }, halfH, 1);         
        BuildShard(c1, c2, new Vector2[] { br }, halfH, 2);         
        BuildShard(c2, c3, new Vector2[] { }, halfH, 3);            
        BuildShard(c3, c4, new Vector2[] { bl }, halfH, 4);         
        BuildShard(c4, c5, new Vector2[] { tl }, halfH, 5);         
    }

    private void BuildShard(Vector2[] cutL, Vector2[] cutR, Vector2[] corners, float halfH, int index)
    {
        int numVerts = 7 + corners.Length;
        Vector3[] vertices = new Vector3[numVerts];

        vertices[0] = Vector3.zero; 

        vertices[1] = new Vector3(cutL[1].x * halfH, cutL[1].y * halfH, 0);
        vertices[2] = new Vector3(cutL[2].x * halfH, cutL[2].y * halfH, 0);
        vertices[3] = new Vector3(cutL[3].x * halfH, cutL[3].y * halfH, 0);

        int cornerOffset = 4;
        for (int i = 0; i < corners.Length; i++)
        {
            vertices[cornerOffset + i] = new Vector3(corners[i].x * halfH, corners[i].y * halfH, 0);
        }

        int rStart = cornerOffset + corners.Length;
        vertices[rStart] = new Vector3(cutR[3].x * halfH, cutR[3].y * halfH, 0);
        vertices[rStart + 1] = new Vector3(cutR[2].x * halfH, cutR[2].y * halfH, 0);
        vertices[rStart + 2] = new Vector3(cutR[1].x * halfH, cutR[1].y * halfH, 0);

        List<int> tris = new List<int>
        {
            0, 1, rStart + 2,
            1, 2, rStart + 2,
            rStart + 2, 2, rStart + 1,
            2, 3, rStart + 1,
            rStart + 1, 3, rStart
        };

        int lastPoint = 3;
        for (int i = 0; i < corners.Length; i++)
        {
            int currentCorner = cornerOffset + i;
            tris.Add(lastPoint);
            tris.Add(currentCorner);
            tris.Add(rStart);
            lastPoint = currentCorner;
        }

        Vector3 cog = Vector3.zero;
        for (int i = 0; i < vertices.Length; i++) cog += vertices[i];
        cog /= vertices.Length;

        for (int i = 0; i < vertices.Length; i++) vertices[i] -= cog;

        Mesh mesh = new Mesh { vertices = vertices, triangles = tris.ToArray() };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject shardObj = new GameObject("Shard_" + index);
        shardObj.transform.SetParent(this.transform);
        shardObj.transform.localPosition = cog;
        shardObj.transform.localRotation = Quaternion.identity;
        shardObj.transform.localScale = Vector3.one;

        shardObj.AddComponent<MeshFilter>().mesh = mesh;
        shardObj.AddComponent<MeshRenderer>().material = glassMaterial;
    }
}
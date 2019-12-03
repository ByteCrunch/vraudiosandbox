using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]

public class SpectrumMesh : MonoBehaviour
{
    public float pipeRadius;

    private AudioEngine audioEngine;

    //Mesh Render
    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;


    private void Awake()
    {
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "FFT Mesh";
    }

    private void Start()
    {
        this.audioEngine = GameObject.Find("Audio").GetComponent<AudioEngine>();
        Debug.Log("fftBinCount: " + this.audioEngine.fftBinCount.ToString());

        mesh.Clear();
        SetVertices();
        SetTriangles();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }
    private Vector3 GetPointOnTorus(float u, float v)
    {
        Vector3 p;
        float r = (pipeRadius * Mathf.Cos(v));
        p.x = r * Mathf.Sin(u);
        p.y = r * Mathf.Cos(u);
        p.z = pipeRadius * Mathf.Sin(v);
        return p;
    }
    
    
    // Visualize for Scene view
    private void OnDrawGizmos()
    {
        if (this.audioEngine == null)
        {
            return;
        }

        /*
        // Draw Spheres for Torus Points
        float vStep = (2f * Mathf.PI) / this.audioEngine.fftBinCount;
        for (int v = 0; v < this.audioEngine.fftBinCount; v++)
        {
            Vector3 point = GetPointOnTorus(0f, v * vStep);
            Gizmos.DrawSphere(point, 0.01f);
        }*/

        // Draw Spheres for vertices
        Mesh m = GetComponent<MeshFilter>().sharedMesh;
        Gizmos.color = Color.white;
        var transform = this.transform;
        foreach (var vert in m.vertices)
            Gizmos.DrawSphere(transform.TransformPoint(vert), 0.001f);
    }

    private void SetVertices()
    {
        vertices = new Vector3[this.audioEngine.fftBinCount * 4];

        float vStep = (2f * Mathf.PI) / this.audioEngine.fftBinCount;
        float offset = 0.5f;

        for (int i = 3; i < vertices.Length; i+=4)
        {
            Vector3 vertex = GetPointOnTorus(0f, i * vStep);
            vertices[i-3] = new Vector3(vertex.x, (vertex.y + (2 * (float)this.audioEngine.fftData[0][i/4])), vertex.z);
            vertices[i-2] = new Vector3(vertex.x, vertex.y - offset, vertex.z);
            vertices[i-1] = new Vector3(vertex.x - offset, vertex.y - offset, vertex.z + offset);
            vertices[i] = new Vector3(vertex.x, vertex.y - offset, vertex.z + offset);
        }
        
        mesh.vertices = vertices;
    }
    private void SetTriangles()
    {
        triangles = new int[this.audioEngine.fftBinCount * 4 * 3];
        for (int i = 11; i < triangles.Length; i+=12)
        {
            triangles[i - 11] = i / 3;
            triangles[i - 10] = (i - 2) / 3;
            triangles[i - 9] = (i - 1) / 3;

            triangles[i - 8] = i / 3;
            triangles[i - 7] = (i - 2) / 3;
            triangles[i - 6] = (i - 3) / 3;

            triangles[i - 5] = (i - 2) / 3;
            triangles[i - 4] = (i - 1) / 3;
            triangles[i - 3] = (i - 3) / 3;

            triangles[i - 2] = i / 3;
            triangles[i - 1] = (i - 3) / 3;
            triangles[i] = (i - 1) / 3;
        }
        mesh.triangles = triangles;
    }
}

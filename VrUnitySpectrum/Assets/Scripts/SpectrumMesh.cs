using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]

public class SpectrumMesh : MonoBehaviour
{
    public float ringRadius;
    public float offsetScale;

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
        mesh.Optimize();
    }   
    
    // Visualize for Scene view and testing
    private void OnDrawGizmos()
    {
        if (this.audioEngine == null)
        {
            return;
        }

        // Draw Spheres for vertices
        Mesh m = GetComponent<MeshFilter>().sharedMesh;
        Gizmos.color = Color.white;
        var transform = this.transform;
        foreach (var vert in m.vertices)
            Gizmos.DrawSphere(transform.TransformPoint(vert), 0.001f);
    }

    private Vector3 GetPointOnRing(float radius, float v, float valueOffset)
    {
        Vector3 p;
        float r = ((radius + valueOffset * this.offsetScale) * Mathf.Cos(v));
        p.x = 0f;
        p.y = r;
        p.z = radius * Mathf.Sin(v);
        return p;
    }

    private void SetVertices()
    {
        vertices = new Vector3[this.audioEngine.fftBinCount * 4];

        float vStep = (2f * Mathf.PI) / this.audioEngine.fftBinCount;

        //for (int f = 0; f < this.audioEngine.fftData.Length; f++)
        for (int f = 0; f < 1; f++)
        {
            for (int i = 3; i < vertices.Length; i += 4)
            //for (int i = 3; i < 4; i += 4)
            {
                // Add vertices for tetrahedron
                Vector3 fftValuePoint = GetPointOnRing(this.ringRadius, (i - 2) * vStep, (float)this.audioEngine.fftData[f][i / 4]);

                // Set positions of corner points for the ground area
                Vector3 center = GetPointOnRing(this.ringRadius, (i - 2) * vStep, 0f); // center of ground area

                // Shift points at x axis
                Vector3 tmp = fftValuePoint;
                tmp.x = fftValuePoint.x + f / 10;
                fftValuePoint = tmp;

                tmp = center;
                tmp.x = center.x + f / 10;
                center = tmp;

                vertices[i - 3] = fftValuePoint;

                for (int j = 0; j < 3; j++)
                {
                    float ang = j * 120;
                    float radius = 0.01f;
                    Vector3 pos;
                    pos.z = center.z + radius * Mathf.Sin(ang * Mathf.Deg2Rad);
                    pos.x = center.x + radius * Mathf.Cos(ang * Mathf.Deg2Rad);
                    pos.y = center.y;

                    vertices[i - j] = pos;
                }
            }
        }

        mesh.vertices = vertices;
    }
    private void SetTriangles()
    {
        triangles = new int[this.audioEngine.fftBinCount * 3];
        for (int i = 0; i < triangles.Length-12; i+=12)
        //for (int i = 0; i < 12; i += 12)
        {
            triangles[i] = i + 2;
            triangles[i + 1] = i + 1;
            triangles[i + 2] = i;

            triangles[i + 3] = i + 2;
            triangles[i + 4] = i;
            triangles[i + 5] = i + 3;

            triangles[i + 6] = i;
            triangles[i + 7] = i + 1;
            triangles[i + 8] = i + 3;

            triangles[i + 9] = i + 1;
            triangles[i + 10] = i + 2;
            triangles[i + 11] = i + 3;
        }
        mesh.triangles = triangles;
    }
}

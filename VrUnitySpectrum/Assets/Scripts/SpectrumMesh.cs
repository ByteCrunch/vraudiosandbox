using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]

public class SpectrumMesh : MonoBehaviour
{
    public float dataScale;
    public float edgeLengthOfRaster;

    private AudioEngine audioEngine;

    //Mesh Render
    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;

    private int countOfRasterVertices;
    private int countOfPeakVertices;


    private void Awake()
    {
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "FFT Mesh";
    }

    private void Start()
    {
        this.audioEngine = GameObject.Find("Audio").GetComponent<AudioEngine>();
        Debug.Log("fftBinCount: " + this.audioEngine.fftBinCount.ToString());
        Debug.Log("fftData.Length: " + this.audioEngine.fftData.Length.ToString());

        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // Increase from 16bit as this would only allow 65.536 vertices per mesh
        mesh.Clear();

        SetVertices();
        SetTriangles();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.Optimize();
    }   
    
    
    // Visualize for Scene view and testing (huge performance killer for lots of vertices)
    /*private void OnDrawGizmos()
    {
        if (this.audioEngine == null)
        {
            return;
        }

        // Draw Spheres for vertices
        Mesh m = GetComponent<MeshFilter>().sharedMesh;

        int c = 0;
        var transform = this.transform;
        foreach (var vert in m.vertices)
        {
            if (c < this.countOfRasterVertices)
                Gizmos.color = Color.green;
            else
                Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.TransformPoint(vert), 0.003f);
            c++;
        }
            
    }*/

    private void SetVertices()
    {
        //this.countOfRasterVertices = (this.audioEngine.fftData.Length + 1) * (this.audioEngine.fftBinCount + 1);
        //this.countOfPeakVertices = this.audioEngine.fftData.Length * this.audioEngine.fftBinCount;

        this.countOfRasterVertices = (4 + 1) * (this.audioEngine.fftBinCount + 1);
        this.countOfPeakVertices = 4 * this.audioEngine.fftBinCount;

        vertices = new Vector3[this.countOfRasterVertices + this.countOfPeakVertices];

        Vector3 center = transform.position;

        // Add vertices
        // first the ground raster vertices, then all fft value peak vertices
        int rasterIdx = 0;
        int peakIdx = this.countOfRasterVertices;
        //for (int i = 0; i <= this.audioEngine.fftData.Length; i++)
        for (int i = 0; i <= 4; i++)
        {
            for (int j = 0; j <= this.audioEngine.fftBinCount; j++)
            {
                // ground raster vertex
                Vector3 r;
                r.x = center.x + j * this.edgeLengthOfRaster;
                r.y = center.y;
                r.z = center.z + i * this.edgeLengthOfRaster;

                vertices[rasterIdx] = r;

                // don't try to add peak vertices for the last raster row
                //if (i < this.audioEngine.fftData.Length && j < this.audioEngine.fftBinCount)
                if (i < 4 && j < this.audioEngine.fftBinCount)
                {
                    // peak vertex
                    Vector3 p;
                    p.x = center.x + j * this.edgeLengthOfRaster + this.edgeLengthOfRaster / 2;
                    p.y = center.y + (float)this.audioEngine.fftData[i][j] * this.dataScale;
                    p.z = center.z + i * this.edgeLengthOfRaster + this.edgeLengthOfRaster / 2;

                    vertices[peakIdx] = p;
                    Debug.Log(peakIdx);
                    peakIdx++;
                }
                rasterIdx++;
            }
        }
        mesh.vertices = vertices;

        int x = 0;
        foreach (var v in mesh.vertices)
        {
            Debug.Log(x.ToString()+": "+v.ToString());
            x++;
        }
    }
    private void SetTriangles()
    {
        //triangles = new int[this.audioEngine.fftBinCount * 4 * 3 * this.audioEngine.fftData.Length]; //4 pyramid sides per fft bin (without base), 3 vertices indices per side
        triangles = new int[this.audioEngine.fftBinCount * 4 * 3 * 4]; //4 pyramid sides per fft bin (without base), 3 vertices indices per side

        int offset = 0;
        for (int i = 0; i < triangles.Length-12; i+=12)
        {
            if (i / 12 % this.audioEngine.fftBinCount == 0 && i / 12 > 0)
            {
                offset++;
            }

            triangles[i] = this.countOfRasterVertices + i / 12;
            triangles[i + 1] = i / 12 + offset;
            triangles[i + 2] = this.audioEngine.fftBinCount + 1 + i / 12 + offset;

            triangles[i + 3] = this.countOfRasterVertices + i / 12;
            triangles[i + 4] = this.audioEngine.fftBinCount + 2 + i / 12 + offset;
            triangles[i + 5] = 1 + i / 12 + offset;

            triangles[i + 6] = this.countOfRasterVertices + i / 12;
            triangles[i + 7] = this.audioEngine.fftBinCount + 1 + i / 12 + offset;
            triangles[i + 8] = this.audioEngine.fftBinCount + 2 + i / 12 + offset;

            triangles[i + 9] = this.countOfRasterVertices + i / 12;
            triangles[i + 10] = 1 + i / 12 + offset;
            triangles[i + 11] = i / 12 + offset;
        }
        mesh.triangles = triangles;
    }
}

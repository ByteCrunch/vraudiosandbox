using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]

public class SpectrumMesh : MonoBehaviour
{
    public float dataScale;
    public float edgeLengthOfRaster;

    private AudioEngine audioEngine;

    private GameObject[] meshObj;
    private MeshFilter[] mFilters;
    private MeshRenderer[] mRenderers;
    private Mesh[] meshes;
    private Vector3[][] vertices;
    private int[][] triangles;
    private Color32[][] colors;

    private int countOfRasterVertices;
    private int countOfPeakVertices;


    private void Awake()
    {
        
    }

    private void Start()
    {
        this.audioEngine = GameObject.Find("Audio").GetComponent<AudioEngine>();
        Debug.Log("<SpectrumMesh> fftBinCount: " + this.audioEngine.fftBinCount.ToString());
        Debug.Log("<SpectrumMesh> fftData.Length: " + this.audioEngine.fftData.Length.ToString());

        this.meshObj = new GameObject[this.audioEngine.fftData.Length];
        this.mFilters = new MeshFilter[this.audioEngine.fftData.Length];
        this.mRenderers = new MeshRenderer[this.audioEngine.fftData.Length];

        this.meshes = new Mesh[this.audioEngine.fftData.Length];
        this.vertices = new Vector3[this.audioEngine.fftData.Length][];
        this.triangles = new int[this.audioEngine.fftData.Length][];
        this.colors = new Color32[this.audioEngine.fftData.Length][];

        for (int i=0; i < this.audioEngine.fftData.Length; i++)
        //for (int i=0; i < 1; i++)
        {
            // Add GOs, MFs and MRs
            this.meshObj[i] = new GameObject("spectrumMesh" + i.ToString());
            this.meshObj[i].transform.parent = this.transform;
            
            this.mFilters[i] = meshObj[i].AddComponent<MeshFilter>();
            this.mFilters[i].name = "FFTData" + i.ToString();

            this.mRenderers[i] = meshObj[i].AddComponent<MeshRenderer>();
            this.mRenderers[i].material = Resources.Load("Materials/SpectrumMat") as Material;

            this.meshes[i] = new Mesh();
            //this.meshes[i].indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // Increase from 16bit as this would only allow 65.536 vertices per mesh
            this.meshes[i].Clear();
            this.mFilters[i].mesh = this.meshes[i];
            
            // Create Spectrum polygons
            this.SetVertices(i);
            this.SetMeshColors(i);
            this.SetTriangles(i);
            this.meshes[i].RecalculateBounds();
            this.meshes[i].RecalculateNormals();
            this.meshes[i].Optimize();

            // Text for frequency legend - very crude code for testing
            TextMesh textMesh = GameObject.Find("FreqLegend").GetComponent<TextMesh>();
            string frequencies = "";

            for (int f = this.audioEngine.fftFrequencies.Length - 1; f >= 0; f--)
            {
                frequencies += System.Math.Round(this.audioEngine.fftFrequencies[f], 2).ToString() + " Hz\n";
            }
            textMesh.text = frequencies;
        }


        /*
        // Testing output

        for (int i=0; i < this.meshes[0].vertices.GetLength(0); i++)
        {
            Debug.Log("<SpectrumMesh> " + this.meshes[0].vertices[i].x.ToString() + " " + this.meshes[0].vertices[i].y.ToString() + " " + this.meshes[0].vertices[i].z.ToString());
        }
        */
    }

    /*
    // Visualize vertices for Scene view and testing
    // (huge performance killer for lots of vertices, will freeze unity if used for the whole fft data)
    private void OnDrawGizmos()
    {
        if (this.audioEngine == null)
        {
            return;
        }

        // Draw Spheres for vertices
        var transform = this.transform;
        foreach (var vert in this.meshes[0].vertices)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.TransformPoint(vert), 0.003f);
        }
            
    }*/

    private void SetVertices(int meshIdx)
    {
        this.countOfRasterVertices = 2 * (this.audioEngine.fftBinCount + 1);
        this.countOfPeakVertices = this.audioEngine.fftBinCount;

        this.vertices[meshIdx] = new Vector3[this.countOfRasterVertices + this.countOfPeakVertices];

        Vector3 center = transform.parent.position;
        //Vector3 center = new Vector3(0f, 0f, 0f);

        // Add vertices
        // first the ground raster vertices, then all fft value peak vertices

        // Add ground raster vertices
        for (int i = 0; i < this.countOfRasterVertices / 2; i++)
        {
            Vector3 r1;
            r1.x = center.x + i * this.edgeLengthOfRaster;
            r1.y = center.y;
            r1.z = center.z + meshIdx * this.edgeLengthOfRaster;

            Vector3 r2;
            r2.x = center.x + i * this.edgeLengthOfRaster;
            r2.y = center.y;
            r2.z = center.z + meshIdx * this.edgeLengthOfRaster + this.edgeLengthOfRaster;

            this.vertices[meshIdx][i] = r1; 
            this.vertices[meshIdx][i + this.audioEngine.fftBinCount + 1] = r2;
        }

        // Add peak vertices
        for (int i = 0; i < this.countOfPeakVertices; i++) {
            Vector3 p;
            p.x = center.x + i * this.edgeLengthOfRaster + this.edgeLengthOfRaster / 2;
            p.y = center.y + (float)this.audioEngine.fftData[meshIdx][i] * this.dataScale;
            p.z = center.z + meshIdx * this.edgeLengthOfRaster + this.edgeLengthOfRaster / 2;

            this.vertices[meshIdx][this.countOfRasterVertices+i] = p;
        }
        this.meshes[meshIdx].vertices = this.vertices[meshIdx];
    }

    private void SetMeshColors(int meshIdx)
    {
        this.colors[meshIdx] = new Color32[this.vertices[meshIdx].Length];

        for (int i = 0; i < this.vertices[meshIdx].Length; i++)
            this.colors[meshIdx][i] = Color.Lerp(Color.green, Color.red, this.vertices[meshIdx][i].y);

        this.meshes[meshIdx].colors32 = this.colors[meshIdx];
    }

    private void SetTriangles(int meshIdx)
    {
        this.triangles[meshIdx] = new int[this.audioEngine.fftBinCount * 4 * 3]; //4 pyramid sides per fft bin (without base), 3 vertices indices per side

        for (int i = 0; i < this.triangles[meshIdx].Length - 12; i += 12)
        {
            this.triangles[meshIdx][i] = this.countOfRasterVertices + i / 12;
            this.triangles[meshIdx][i + 1] = i / 12;
            this.triangles[meshIdx][i + 2] = this.audioEngine.fftBinCount + 1 + i / 12;

            this.triangles[meshIdx][i + 3] = this.countOfRasterVertices + i / 12;
            this.triangles[meshIdx][i + 4] = this.audioEngine.fftBinCount + 2 + i / 12;
            this.triangles[meshIdx][i + 5] = 1 + i / 12;

            this.triangles[meshIdx][i + 6] = this.countOfRasterVertices + i / 12;
            this.triangles[meshIdx][i + 7] = this.audioEngine.fftBinCount + 1 + i / 12;
            this.triangles[meshIdx][i + 8] = this.audioEngine.fftBinCount + 2 + i / 12;
            this.triangles[meshIdx][i + 9] = this.countOfRasterVertices + i / 12;
            this.triangles[meshIdx][i + 10] = 1 + i / 12;
            this.triangles[meshIdx][i + 11] = i / 12 ;

        }
        this.meshes[meshIdx].triangles = this.triangles[meshIdx];
    }
}

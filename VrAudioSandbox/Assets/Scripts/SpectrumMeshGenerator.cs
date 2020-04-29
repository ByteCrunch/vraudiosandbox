using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpectrumMeshGenerator : MonoBehaviour
{
    public float edgeLengthOfRaster;

    private AudioEngine audioEngine;

    private GameObject[] meshObj;
    private MeshFilter[] mFilters;
    private MeshRenderer[] mRenderers;
    private Collider[] mColliders;
    public Mesh[] meshes;

    private Vector3[][] vertices;
    private int[][] triangles;
    private Color32[][] colors;

    private int countOfRasterVertices;
    public int startIndexOfPeakVertices { get { return this.countOfRasterVertices; } }

    private int countOfPeakVertices;

    private void Update()
    {
        if (Input.GetButtonDown("ScaleMeshYDec"))
        {
            this.ScaleMeshY(-0.05f);
        }

        if (Input.GetButtonUp("ScaleMeshYInc"))
        {
            this.ScaleMeshY(0.05f);
        }

        // Color meshes according to play position
        if (this.audioEngine.isPlaying)
        {
            double msPerChunk = this.audioEngine.importDurationInMs / this.audioEngine.ifftData.Length / 2;
            int posIdx = (int)(this.audioEngine.GetPositionInMs() / msPerChunk);

            for (int i = 0; i < posIdx; i++)
            {
                if (i < this.mRenderers.Length)
                    this.mRenderers[i].material = Resources.Load("Materials/SpectrumMatPlaying") as Material;
            }

            // workaround until loopstream event will work to trigger reset of spectrum material
            for (int i = posIdx; i < this.audioEngine.fftData.Length; i++)
            {
                this.mRenderers[i].material = Resources.Load("Materials/SpectrumMat") as Material;
            }
        }
    }

    private void Start()
    {

    }


    public void Init()
    {
        this.audioEngine = GameObject.Find("Audio").GetComponent<AudioEngine>();
        this.GenerateMeshFromAudioData();
    }

    /// <summary>
    /// Generates Meshes from FFT data
    /// </summary>
    public void GenerateMeshFromAudioData()
    {
        this.meshObj = new GameObject[this.audioEngine.fftData.Length];
        this.mFilters = new MeshFilter[this.audioEngine.fftData.Length];
        this.mRenderers = new MeshRenderer[this.audioEngine.fftData.Length];
        this.mColliders = new MeshCollider[this.audioEngine.fftData.Length];

        this.meshes = new Mesh[this.audioEngine.fftData.Length];
        this.vertices = new Vector3[this.audioEngine.fftData.Length][];
        this.triangles = new int[this.audioEngine.fftData.Length][];
        this.colors = new Color32[this.audioEngine.fftData.Length][];

        for (int i=0; i < this.audioEngine.fftData.Length; i++)
        {
            // Add GOs, MFs and MRs
            this.meshObj[i] = new GameObject("spectrumMesh" + i.ToString());
            this.meshObj[i].transform.parent = this.transform;
            
            this.mFilters[i] = meshObj[i].AddComponent<MeshFilter>();
            this.mFilters[i].name = "FFTData" + i.ToString();

            this.mRenderers[i] = meshObj[i].AddComponent<MeshRenderer>();
            this.mRenderers[i].material = Resources.Load("Materials/SpectrumMat") as Material;

            this.mColliders[i] = meshObj[i].AddComponent<MeshCollider>();

            this.meshes[i] = new Mesh();
            this.meshes[i].Clear();
            this.mFilters[i].mesh = this.meshes[i];
            
            // Create Spectrum polygons
            this.SetVertices(i);
            this.SetMeshColors(i);
            this.SetTriangles(i);
            this.meshes[i].RecalculateBounds();
            this.meshes[i].RecalculateNormals();

            // It is important not to call Mesh.Optimize() here, because it will re-order the mesh vertices! 
            // Only Mesh.OptimizeIndexBuffers is uncritical -
            // Mesh.Optimize() will basically also call Mesh.OptimizeReorderVertexBuffer() which will mess up the order and f*ck things up I depend on later.
            this.meshes[i].OptimizeIndexBuffers();
        }

        // Generate Frequency legend
        SpectrumFreqLegend freqLegend = GameObject.Find("FreqLegend").GetComponent<SpectrumFreqLegend>();
        freqLegend.SetFreqLegend(this.audioEngine.fftFrequencies, this.edgeLengthOfRaster);

        SpectrumDeformer deformer = GameObject.Find("SpectrumMesh").GetComponent<SpectrumDeformer>();
        deformer.MeshGenerated();
    }

    /*
    // Visualize vertices for Scene view debugging
    // (huge performance killer for lots of vertices, will freeze unity if used for the whole fft data set)
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

    /// <summary>
    /// Sets the vertices of ground raster and FFT data peaks
    /// </summary>
    /// <param name="meshIdx">index of mesh to fill with vertices (= index of corresponding FFT chunk)</param>
    private void SetVertices(int meshIdx)
    {
        this.countOfRasterVertices = 2 * (this.audioEngine.fftBinCount + 1);
        this.countOfPeakVertices = this.audioEngine.fftBinCount;

        this.vertices[meshIdx] = new Vector3[this.countOfRasterVertices + this.countOfPeakVertices];

        Vector3 center = transform.parent.position;

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
            p.y = center.y + (float)this.audioEngine.fftDataMagnitudes[meshIdx][i] * 0.01f;
            //p.y = center.y + (float)this.audioEngine.fftDataPhases[meshIdx][i] * 0.02f;
            p.z = center.z + meshIdx * this.edgeLengthOfRaster + this.edgeLengthOfRaster / 2;

            this.vertices[meshIdx][this.countOfRasterVertices+i] = p;
        }
        this.meshes[meshIdx].vertices = this.vertices[meshIdx];
    }

    /// <summary>
    /// Resets all meshes materials to default
    /// </summary>
    public void ResetMeshColors()
    {
        for (int i = 0; i < this.audioEngine.fftData.Length; i++)
        {
            this.mRenderers[i].material = Resources.Load("Materials/SpectrumMat") as Material;
        }
    }

    /// <summary>
    /// Creates color lerp for mesh peaks
    /// </summary>
    /// <param name="meshIdx">index of mesh to colorize (= index of corresponding FFT chunk)</param>
    public void SetMeshColors(int meshIdx)
    {
        this.colors[meshIdx] = new Color32[this.vertices[meshIdx].Length];

        float max = 0;
        for (int i = 0; i < this.vertices[meshIdx].Length; i++)
        {
            if (this.vertices[meshIdx][i].y > max)
                max = this.vertices[meshIdx][i].y;
        }

        for (int i = 0; i < this.vertices[meshIdx].Length; i++)
            this.colors[meshIdx][i] = Color.Lerp(Color.green, Color.red, this.vertices[meshIdx][i].y / max);

        this.meshes[meshIdx].colors32 = this.colors[meshIdx];
    }

    /// <summary>
    /// Creates triangles for the chosen mesh
    /// </summary>
    /// <param name="meshIdx">index of mesh to create triangles for (= index of corresponding FFT chunk)</param>
    private void SetTriangles(int meshIdx)
    {
        // 4 pyramid sides per fft bin (without base), 3 vertices indices per side
        this.triangles[meshIdx] = new int[this.audioEngine.fftBinCount * 4 * 3]; 

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

    /// <summary>
    /// Scales the whole mesh 
    /// </summary>
    /// <param name="offset">positive or negative offset applied to y-scale</param>
    public void ScaleMeshY(float offset)
    {
        Vector3 scale = gameObject.transform.localScale;
        Debug.Log(scale.ToString());
        gameObject.transform.localScale.Set(scale.x, scale.y + offset, scale.z);
    }
}

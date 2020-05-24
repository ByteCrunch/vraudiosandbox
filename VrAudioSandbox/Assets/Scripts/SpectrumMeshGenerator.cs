using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class SpectrumMeshGenerator : MonoBehaviour
{
    public float edgeLengthOfRaster;
    public float fftScalingFactor;

    private AudioEngine audioEngine;
    private SpectrumDeformer deformer;

    private GameObject[] meshObj;
    public MeshFilter[] mFilters;
    private MeshRenderer[] mRenderers;
    private MeshCollider[] mColliders;
    public Mesh[] meshes;

    public float maxPeakValue;

    public Vector3[][] vertices;
    private int[][] triangles;
    public Color32[][] colors;

    private int countOfRasterVertices;
    public int startIndexOfPeakVertices { get { return this.countOfRasterVertices; } }

    private int countOfPeakVertices;

    public void Awake()
    {
        this.audioEngine = GameObject.Find("Audio").GetComponent<AudioEngine>();
        this.deformer = GameObject.Find("SpectrumMesh").GetComponent<SpectrumDeformer>();
    }

    private void Start()
    {

    }

    private void Update()
    {
        /*if (Input.GetButtonDown("ScaleMeshYDec"))
        {
            this.ScaleMeshY(-0.05f);
        }

        if (Input.GetButtonUp("ScaleMeshYInc"))
        {
            this.ScaleMeshY(0.05f);
        }*/

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
            for (int i = posIdx; i < this.audioEngine.fftDataMagnitudes.Length; i++)
            {
                this.mRenderers[i].material = Resources.Load("Materials/SpectrumMat") as Material;
            }
        }
    }

    /// <summary>
    /// Generates Meshes from FFT data
    /// </summary>
    public void GenerateMeshFromAudioData()
    {
        this.meshObj = new GameObject[this.audioEngine.fftDataMagnitudes.Length];
        this.mFilters = new MeshFilter[this.audioEngine.fftDataMagnitudes.Length];
        this.mRenderers = new MeshRenderer[this.audioEngine.fftDataMagnitudes.Length];
        this.mColliders = new MeshCollider[this.audioEngine.fftDataMagnitudes.Length];

        this.meshes = new Mesh[this.audioEngine.fftDataMagnitudes.Length];
        this.vertices = new Vector3[this.audioEngine.fftDataMagnitudes.Length][];
        this.triangles = new int[this.audioEngine.fftDataMagnitudes.Length][];
        this.colors = new Color32[this.audioEngine.fftDataMagnitudes.Length][];

        Stopwatch timer = Stopwatch.StartNew();
        this.FindMaxPeakValue();

        for (int i=0; i < this.audioEngine.fftDataMagnitudes.Length; i++)
        {
            // Add GOs, MFs and MRs
            this.meshObj[i] = new GameObject("spectrumMesh" + i.ToString());
            this.meshObj[i].transform.parent = this.transform;
            
            this.mFilters[i] = meshObj[i].AddComponent<MeshFilter>();
            this.mFilters[i].name = "FFTData" + i.ToString();

            this.mRenderers[i] = meshObj[i].AddComponent<MeshRenderer>();
            this.mRenderers[i].material = Resources.Load("Materials/SpectrumMat") as Material;

            this.meshes[i] = new Mesh();

            // Create Spectrum polygons
            this.meshes[i].MarkDynamic(); // to get better performance when continually updating the Mesh
            this.SetVertices(i);
            this.SetMeshColors(i);
            this.SetTriangles(i);
            this.meshes[i].RecalculateBounds();
            this.meshes[i].RecalculateNormals();

            // It is important not to call Mesh.Optimize() here, because it will re-order the mesh vertices! 
            // Only Mesh.OptimizeIndexBuffers is uncritical -
            // Mesh.Optimize() will basically also call Mesh.OptimizeReorderVertexBuffer() which will mess up the order and f*ck things up I depend on later.
            this.meshes[i].OptimizeIndexBuffers();

            this.mFilters[i].mesh = this.meshes[i];

            // Add mesh colliders
            this.mColliders[i] = this.meshObj[i].AddComponent<MeshCollider>();
            this.mColliders[i].sharedMesh = this.mFilters[i].mesh;
            this.mColliders[i].cookingOptions = MeshColliderCookingOptions.WeldColocatedVertices;
        }

        timer.Stop();
        UnityEngine.Debug.Log("<SpectrumMeshGenerator> Generated mesh in " + (timer.Elapsed.Seconds + timer.Elapsed.Minutes * 60).ToString() + " seconds.");

        // Generate Frequency legend
        SpectrumFreqLegend freqLegend = GameObject.Find("FreqLegend").GetComponent<SpectrumFreqLegend>();
        freqLegend.SetFreqLegend(this.audioEngine.fftFrequencies, this.edgeLengthOfRaster);

        // Init deformer instance
        this.deformer.MeshGenerated();
    }

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

            float peakValue = (float)this.audioEngine.fftDataMagnitudes[meshIdx][i] * this.fftScalingFactor;

            Vector3 p;
            p.x = center.x + i * this.edgeLengthOfRaster + this.edgeLengthOfRaster / 2;
            p.y = center.y + peakValue;
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
        for (int i = 0; i < this.audioEngine.fftDataMagnitudes.Length; i++)
        {
            this.mRenderers[i].material = Resources.Load("Materials/SpectrumMat") as Material;
        }
    }

    /// <summary>
    /// Find max value for Color lerp
    /// </summary>
    /// <returns>maximum of fftData peak values</returns>
    private void FindMaxPeakValue()
    {
        for (int i = 0; i < this.audioEngine.fftDataMagnitudes.Length; i++)
        {
            for (int j = 0; j < this.audioEngine.fftDataMagnitudes[i].Length; j++)
            {
                float value = (float)this.audioEngine.fftDataMagnitudes[i][j] * this.fftScalingFactor;
                if (value > this.maxPeakValue)
                    this.maxPeakValue = value;
            }
        }
    }
    /// <summary>
    /// Check if provided peak value is higher than current peak value and sets accordingly
    /// </summary>
    /// <param name="value">peak value to check against</param>
    public void SetMaxPeakValue(float value)
    {
        if (value > this.maxPeakValue)
            this.maxPeakValue = value;
    }

    /// <summary>
    /// Creates color lerp for mesh peaks
    /// </summary>
    /// <param name="meshIdx">index of mesh to colorize (= index of corresponding FFT chunk)</param>
    public void SetMeshColors(int meshIdx)
    {
        this.colors[meshIdx] = new Color32[this.vertices[meshIdx].Length];

        for (int i = 0; i < this.vertices[meshIdx].Length; i++)
            this.colors[meshIdx][i] = Color.Lerp(Color.green, Color.red, this.vertices[meshIdx][i].y / this.maxPeakValue);

        this.meshes[meshIdx].colors32 = this.colors[meshIdx];
    }

    /// <summary>
    /// Creates triangles for the chosen mesh
    /// </summary>
    /// <param name="meshIdx">index of mesh to create triangles for (= index of corresponding FFT chunk)</param>
    public void SetTriangles(int meshIdx)
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
        this.transform.localScale = new Vector3(this.transform.localScale.x, this.transform.localScale.y + offset, this.transform.localScale.z);
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
}

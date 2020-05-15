using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpectrumMeshGenerator))]
public class SpectrumDeformer : MonoBehaviour
{
    //public List<Vector3>[] origVertices; // for storing a copy of unmodified values
    public List<Vector3>[] modifiedVertices;
    public List<DeformJob> jobQueue;

    public float deformFactor;

    private SpectrumMeshGenerator spectrum;
    private AudioEngine audioEngine;

    /// <summary>
    /// Clones the mesh vertices and preparation for deforming mesh
    /// </summary>
    public void MeshGenerated()
    {
        //this.origVertices = new List<Vector3>[this.spectrum.meshes.Length]; // TODO use for revert function
        //(removed for performance reasons for now)

        this.modifiedVertices = new List<Vector3>[this.spectrum.meshes.Length];

        for (int i=0; i < this.spectrum.meshes.Length; i++)
        {
            //this.origVertices[i] = new List<Vector3>();
            this.modifiedVertices[i] = new List<Vector3>();

            //this.spectrum.meshes[i].GetVertices(this.origVertices[i]);
            this.spectrum.meshes[i].GetVertices(this.modifiedVertices[i]);
        }

    }
    public void DeformMeshPoint(Vector3 point, Vector3 direction, float radius, float absoluteOffset = 0)
    {
        List<Vector3> points = new List<Vector3>();
        points.Add(point);
        this.jobQueue.Add(new DeformJob(points, direction, radius, absoluteOffset));
    }

    public void DeformMeshMultiplePoints(List<Vector3> points, Vector3 direction, float radius, float absoluteOffset = 0)
    {
        this.jobQueue.Add(new DeformJob(points, direction, radius, absoluteOffset));
    }

    /// <summary>
    /// Moves FFT vertices to the chosen direction for the given points and radius
    /// </summary>
    /// <param name="point">point for vertices to be affected</param>
    /// <param name="direction">direction of movement</param>
    /// <param name="radius">radius for vertices to be affected</param>
    /// <param name="absoluteOffset">(optional) add given absolute value</param>
    private IEnumerator DeformMeshMultiplePointsWorker(DeformJob job, List<Vector3> points, Vector3 direction, float radius, float absoluteOffset = 0)
    {
        // Collect all changes to be made first
        Dictionary<int, List<VertexChange>> collectedChanges = new Dictionary<int, List<VertexChange>>();
        for (int p = 0; p < points.Count; p++)
        {
            for (int j = 0; j < this.modifiedVertices.Length; j++)
            {
                // only modify vertices corresponding to fft values, omit raster vertices indexes
                for (int i = this.spectrum.startIndexOfPeakVertices; i < this.modifiedVertices[j].Count; i++)
                {
                    var distance = (points[p] - this.modifiedVertices[j][i]).magnitude;
                    if (distance < radius)
                    {
                        Vector3 newVert;
                        // TODO Limit new position according y-scale of mesh, don't allow positions less than 0
                        if (absoluteOffset == 0)
                            newVert = this.modifiedVertices[j][i] + direction * this.deformFactor;
                        else
                            newVert = new Vector3(this.modifiedVertices[j][i].x, this.modifiedVertices[j][i].y + absoluteOffset, this.modifiedVertices[j][i].z);

                        if (!collectedChanges.ContainsKey(j))
                            collectedChanges.Add(j, new List<VertexChange>() { new VertexChange(i, newVert) });
                        else 
                            collectedChanges[j].Add(new VertexChange(i, newVert));
                    }
                }
            }       
        }

        yield return null;

        // Process consolidated vertex changes
        foreach (KeyValuePair<int, List<VertexChange>> changeList in collectedChanges)
        {
            foreach (VertexChange v in changeList.Value)
            {
                this.modifiedVertices[changeList.Key].RemoveAt(v.index);
                this.modifiedVertices[changeList.Key].Insert(v.index, v.newVert);
            }
        }

        yield return null;

        // Update meshes & colliders
        foreach (KeyValuePair<int, List<VertexChange>> changeList in collectedChanges)
        {
            this.spectrum.meshes[changeList.Key].SetVertices(this.modifiedVertices[changeList.Key]);
            this.spectrum.mFilters[changeList.Key].mesh = this.spectrum.meshes[changeList.Key];

            MeshCollider c = GameObject.Find("FFTData" + changeList.Key).GetComponent<MeshCollider>();
            c.sharedMesh = this.spectrum.meshes[changeList.Key];
        }

        yield return null;

        // Process fft data changes
        foreach (KeyValuePair<int, List<VertexChange>> changeList in collectedChanges)
        {
            foreach (VertexChange v in changeList.Value)
            {
                // Update FFT magnitude data for affected peak vertex
                this.audioEngine.fftDataMagnitudes[changeList.Key][v.index - this.spectrum.startIndexOfPeakVertices] = (double)v.newVert.y / this.spectrum.fftScalingFactor;
            }
        }

        this.audioEngine.fftDataEdited = true;
        job.isFinished = true;
    }

    /*
    /// <summary>
    /// Moves FFT vertices to the chosen direction for the given points and radius
    /// </summary>
    /// <param name="point">point for vertices to be affected</param>
    /// <param name="direction">direction of movement</param>
    /// <param name="radius">radius for vertices to be affected</param>
    /// <param name="absoluteOffset">(optional) add given absolute value</param>
    private IEnumerator DeformMeshMultiplePointsWorker(DeformJob job, List<Vector3> points, Vector3 direction, float radius, float absoluteOffset = 0)
    {
        for (int p = 0; p < points.Count; p++)
        {
            for (int j = 0; j < this.modifiedVertices.Length; j++)
            {
                bool changed = false;
                // only modify vertices corresponding to fft values, omit raster vertices indexes
                for (int i = this.spectrum.startIndexOfPeakVertices; i < this.modifiedVertices[j].Count; i++)
                {
                    var distance = (points[p] - this.modifiedVertices[j][i]).magnitude;
                    if (distance < radius)
                    {
                        Vector3 newVert;
                        // TODO Limit new position according y-scale of mesh, don't allow positions less than 0
                        if (absoluteOffset == 0)
                            newVert = this.modifiedVertices[j][i] + direction * this.deformFactor;
                        else
                            newVert = new Vector3(this.modifiedVertices[j][i].x, this.modifiedVertices[j][i].y + absoluteOffset, this.modifiedVertices[j][i].z);

                        this.modifiedVertices[j].RemoveAt(i);
                        this.modifiedVertices[j].Insert(i, newVert);

                        // Update FFT magnitude data for affected peak vertex
                        this.audioEngine.fftDataMagnitudes[j][i - this.spectrum.startIndexOfPeakVertices] = (double)newVert.y / this.spectrum.fftScalingFactor;

                        // Update Color lerp
                        this.spectrum.SetMaxPeakValue(newVert.y / this.spectrum.fftScalingFactor);
                        this.spectrum.colors[j][i] = Color.Lerp(Color.green, Color.red, this.spectrum.vertices[j][i].y / this.spectrum.maxPeakValue);

                        changed = true;
                    }
                }

                if (changed)
                {
                    Debug.Log("<SpectrumDeformer> mesh #" + j.ToString() + " modified");

                    // Update mesh
                    this.spectrum.mFilters[j].mesh.Clear(false);
                    this.spectrum.meshes[j].SetVertices(this.modifiedVertices[j]);
                    this.spectrum.SetTriangles(j);
                    this.spectrum.meshes[j].colors32 = this.spectrum.colors[j];
                    this.spectrum.mFilters[j].mesh = this.spectrum.meshes[j];

                    // Update mesh collider
                    MeshCollider c = GameObject.Find("FFTData" + j).GetComponent<MeshCollider>();
                    c.sharedMesh = this.spectrum.meshes[j];

                    this.audioEngine.fftDataEdited = true;
                }
            }
            yield return null;
        }
        job.isFinished = true;
    }*/

    void Awake()
    {
        this.spectrum = GetComponent<SpectrumMeshGenerator>();
        this.audioEngine = GameObject.Find("Audio").GetComponent<AudioEngine>();

        this.jobQueue = new List<DeformJob>();
    }

    void Update()
    {
        for (int i = 0; i < this.jobQueue.Count; i++)
        {
            if (!this.jobQueue[i].isFinished)
                StartCoroutine(this.DeformMeshMultiplePointsWorker(this.jobQueue[i], this.jobQueue[i].points, this.jobQueue[i].direction, this.jobQueue[i].radius, this.jobQueue[i].absoluteOffset));
            else
                this.jobQueue.RemoveAt(i);
        }
    }
}

public class DeformJob
{
    public List<Vector3> points;
    public Vector3 direction;
    public float radius;
    public float absoluteOffset = 0;

    public bool isFinished = false;

    public DeformJob(List<Vector3> points, Vector3 direction, float radius, float absoluteOffset = 0)
    {
        this.points = new List<Vector3>(points);
        this.direction = direction;
        this.radius = radius;
        this.absoluteOffset = absoluteOffset;
    }
}

public class VertexChange
{
    public int index;
    public Vector3 newVert;

    public VertexChange(int index, Vector3 newVert)
    {
        this.index = index;
        this.newVert = newVert;
    }
}

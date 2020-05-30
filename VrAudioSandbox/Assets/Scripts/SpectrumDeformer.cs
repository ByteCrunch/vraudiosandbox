using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[RequireComponent(typeof(SpectrumMeshGenerator))]
public class SpectrumDeformer : MonoBehaviour
{
    public Vector3[][] modifiedVertices;
    public List<DeformJob> routineQueue;
    public float deformFactor;

    private SpectrumMeshGenerator spectrum;
    private AudioEngine audioEngine;

    /// <summary>
    /// Clones the mesh vertices and preparation for deforming mesh
    /// </summary>
    public void MeshGenerated()
    {
        this.modifiedVertices = new Vector3[this.spectrum.meshes.Length][];

        for (int i = 0; i < this.spectrum.meshes.Length; i++)
        {
            this.modifiedVertices[i] = new Vector3[this.audioEngine.fftBinCount];
            this.modifiedVertices[i] = this.spectrum.meshes[i].vertices;
        }

    }
    public void DeformMeshPoint(Vector3 point, Vector3 direction, float radius, float absoluteValue = -1)
    {
        List<Vector3> points = new List<Vector3>();
        points.Add(point);
        this.routineQueue.Add(new DeformJob(points, direction, radius, absoluteValue));
    }

    public void DeformMeshMultiplePoints(List<Vector3> points, Vector3 direction, float radius, float absoluteValue = -1)
    {
        this.routineQueue.Add(new DeformJob(points, direction, radius, absoluteValue));
    }

    /// <summary>
    /// Moves FFT vertices to the chosen direction for the given points and radius
    /// </summary>
    /// <param name="point">point for vertices to be affected</param>
    /// <param name="direction">direction of movement</param>
    /// <param name="radius">radius for vertices to be affected</param>
    /// <param name="absoluteValue">(optional) add given absolute value</param>
    private IEnumerator DeformMeshMultiplePointsWorker(DeformJob routine, List<Vector3> points, Vector3 direction, float radius, float absoluteValue)
    {
        routine.isRunning = true;
        int frames = 0;
        // Interpolate additional points if there are gaps
        int i = 0;
        while (i < points.Count - 1)
        {
            float distance = (points[i] - points[i + 1]).magnitude;
            if (distance > radius)
            {
                int n = (int)(distance / radius);
                for (int j = 1; j <= n; j++)
                {
                    points.Insert(i, Vector3.Lerp(points[i], points[i + 1], (float)1 / n * j));
                    i++;
                }
            }
            i++;

            if (frames % 20 == 0)
                yield return null;
        }

        // Collect all changes to be made
        NativeArray<Vector3>[] vertices = new NativeArray<Vector3>[this.modifiedVertices.Length];
        NativeQueue<VertexChange> vertexChanges = new NativeQueue<VertexChange>(Allocator.Persistent);
        FindPointsToUpdateJob[] jobData = new FindPointsToUpdateJob[this.modifiedVertices.Length];
        JobHandle[] jobHandles = new JobHandle[this.modifiedVertices.Length];

        for (int j = 0; j < this.modifiedVertices.Length; j++)
        {
            if (j > 0)
                jobHandles[j - 1].Complete(); //Ensure that previous job is complete before preparing next one to prevent access on the NativeQueue while previous job is writing

            // only modify vertices corresponding to fft values, omit raster vertices indexes
            Vector3[] peakVertices = new Vector3[this.modifiedVertices[j].Length - this.spectrum.startIndexOfPeakVertices];
            Array.Copy(this.modifiedVertices[j], this.spectrum.startIndexOfPeakVertices, peakVertices, 0, peakVertices.Length);
            vertices[j] = new NativeArray<Vector3>(peakVertices, Allocator.TempJob);

            jobData[j] = new FindPointsToUpdateJob();
            jobData[j].meshIdx = j;
            jobData[j].vertices = vertices[j];
            jobData[j].points = new NativeArray<Vector3>(points.ToArray(), Allocator.TempJob);
            jobData[j].direction = direction;
            jobData[j].deformFactor = this.deformFactor;
            jobData[j].absoluteValue = absoluteValue;
            jobData[j].radius = radius;
            jobData[j].vertexChanges = vertexChanges.AsParallelWriter();

            if (j < 1)
                jobHandles[j] = jobData[j].Schedule(peakVertices.Length, 256);
            else
                jobHandles[j] = jobData[j].Schedule(peakVertices.Length, 256, jobHandles[j - 1]);
        }

        // wait for last job
        jobHandles[this.modifiedVertices.Length - 1].Complete();

        List<int> meshColorsToUpdate = new List<int>();
        // Update meshes, colliders & fftDataMagnitudes
        while (vertexChanges.TryDequeue(out VertexChange vc))
        {
            // Update vertices
            this.modifiedVertices[vc.meshIdx][vc.vertexIdx + this.spectrum.startIndexOfPeakVertices] = new Vector3(vc.x, vc.y, vc.z);
            this.spectrum.mFilters[vc.meshIdx].mesh.vertices = this.modifiedVertices[vc.meshIdx];

            if (frames % 30 == 0)
                yield return null;

            // Note for color update
            if (!meshColorsToUpdate.Contains(vc.meshIdx))
                meshColorsToUpdate.Add(vc.meshIdx);
            this.spectrum.SetMaxPeakValue(this.modifiedVertices[vc.meshIdx][vc.vertexIdx].y);

            // Update colliders
            MeshCollider c = GameObject.Find("FFTData" + vc.meshIdx.ToString()).GetComponent<MeshCollider>();
            c.sharedMesh = this.spectrum.mFilters[vc.meshIdx].mesh;

            // Update fft Data
            this.audioEngine.fftDataMagnitudes[vc.meshIdx][vc.vertexIdx] = (double)vc.y / this.spectrum.fftScalingFactor;

            frames++;
        }

        vertexChanges.Dispose();

        // Update mesh colors
        foreach (int m in meshColorsToUpdate)
        {
            for (int v = 0; v < this.spectrum.colors[m].Length; v++)
            {
                this.spectrum.colors[m][v] = Color.Lerp(Color.green, Color.red, this.modifiedVertices[m][v].y / this.spectrum.maxPeakValue);
            }
            this.spectrum.mFilters[m].mesh.SetColors(this.spectrum.colors[m]);
        }

        this.audioEngine.fftDataEdited = true;
        routine.isFinished = true;
    }

    void Awake()
    {
        this.spectrum = GetComponent<SpectrumMeshGenerator>();
        this.audioEngine = GameObject.Find("Audio").GetComponent<AudioEngine>();

        this.routineQueue = new List<DeformJob>();
    }

    void Update()
    {
        for (int i = 0; i < this.routineQueue.Count; i++)
        {
            if (!this.routineQueue[i].isFinished && !this.routineQueue[i].isRunning)
                StartCoroutine(this.DeformMeshMultiplePointsWorker(this.routineQueue[i], this.routineQueue[i].points, this.routineQueue[i].direction, this.routineQueue[i].radius, this.routineQueue[i].absoluteValue));
            else
                this.routineQueue.RemoveAt(i);
        }
    }
}
public class DeformJob
{
    public List<Vector3> points;
    public Vector3 direction;
    public float radius;
    public float absoluteValue = 0;

    public bool isFinished = false;
    public bool isRunning = false;

    public DeformJob(List<Vector3> points, Vector3 direction, float radius, float absoluteValue)
    {
        this.points = new List<Vector3>(points);
        this.direction = direction;
        this.radius = radius;
        this.absoluteValue = absoluteValue;
    }
}

public struct VertexChange
{
    public int meshIdx;
    public int vertexIdx;
    public float x;
    public float y;
    public float z;

    public VertexChange(int meshIdx, int vertexIdx, float x, float y, float z)
    {
        this.meshIdx = meshIdx;
        this.vertexIdx = vertexIdx;
        this.x = x;
        this.y = y;
        this.z = z;
    }
}

[BurstCompile]
public struct FindPointsToUpdateJob : IJobParallelFor
{
    [ReadOnly] public int meshIdx;
    [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Vector3> vertices;
    [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Vector3> points;
    [ReadOnly] public Vector3 direction;
    [ReadOnly] public float deformFactor;
    [ReadOnly] public float absoluteValue;
    [ReadOnly] public float radius;
    public NativeQueue<VertexChange>.ParallelWriter vertexChanges;
    public void Execute(int i)
    {
        for (int p = 0; p < points.Length; p++)
        {
            // Ignore y-axis when determing points to be affected
            var distance = (new Vector3(points[p].x, 0, points[p].z) - new Vector3(vertices[i].x, 0, vertices[i].z)).magnitude;
            if (distance < radius)
            {
                if (absoluteValue == -1)
                    // use factor for changing of y-value
                    vertexChanges.Enqueue(new VertexChange(
                        meshIdx,
                        i,
                        vertices[i].x + direction.x * deformFactor,
                        (vertices[i].y + direction.y * deformFactor >= 0) ? vertices[i].y + direction.y * deformFactor : 0f,
                        vertices[i].z + direction.z * deformFactor));
                else
                    // use absolute value
                    vertexChanges.Enqueue(new VertexChange(meshIdx, i, vertices[i].x, absoluteValue, vertices[i].z));
            }
        }
    }
}
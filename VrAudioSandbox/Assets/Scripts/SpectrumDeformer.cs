﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpectrumMeshGenerator))]
public class SpectrumDeformer : MonoBehaviour
{
    //public List<Vector3>[] origVertices; // for storing a copy of unmodified values
    public List<Vector3>[] modifiedVertices;

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

    /// <summary>
    /// Moves FFT vertices to the chosen direction for the given point and radius
    /// </summary>
    /// <param name="point">point for vertices to be affected</param>
    /// <param name="direction">direction of movement</param>
    /// <param name="radius">radius for vertices to be affected</param>
    /// <param name="absoluteOffset">(optional) add given absolute value</param>
    public void DeformMesh(Vector3 point, Vector3 direction, float radius, float absoluteOffset = 0)
    {
        for (int j = 0; j < this.modifiedVertices.Length; j++)
        {
            bool changed = false;
            // only modify vertices corresponding to fft values, omit raster vertices indexes
            for (int i = this.spectrum.startIndexOfPeakVertices; i < this.modifiedVertices[j].Count; i++)
            {
                var distance = (point - this.modifiedVertices[j][i]).magnitude;
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
                Debug.Log("<SpectrumDeformer> mesh #"+ j.ToString() + " modified");

                // Update mesh
                this.spectrum.mFilters[j].mesh.Clear(false);
                this.spectrum.meshes[j].SetVertices(this.modifiedVertices[j]);
                this.spectrum.SetTriangles(j);
                this.spectrum.meshes[j].colors32 = this.spectrum.colors[j];
                this.spectrum.mFilters[j].mesh = this.spectrum.meshes[j];

                // Update mesh collider
                MeshCollider c = GameObject.Find("FFTData"+j).GetComponent<MeshCollider>();
                c.sharedMesh = this.spectrum.meshes[j];

                this.audioEngine.fftDataEdited = true;
            }
        }
    }

    void Awake()
    {
        this.spectrum = GetComponent<SpectrumMeshGenerator>();
        this.audioEngine = GameObject.Find("Audio").GetComponent<AudioEngine>();
    }

    void Update()
    {

    }
}

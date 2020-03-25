using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpectrumMeshGenerator))]
public class SpectrumDeformer : MonoBehaviour
{
    public List<Vector3>[] origVertices; // for storing a copy of unmodified values
    public List<Vector3>[] modifiedVertices;

    public float deformFactor;

    private SpectrumMeshGenerator spectrum;

    public void Init()
    {
        
    }

    public void MeshGenerated()
    {
        this.spectrum = GetComponent<SpectrumMeshGenerator>();

        this.origVertices = new List<Vector3>[this.spectrum.meshes.Length];
        this.modifiedVertices = new List<Vector3>[this.spectrum.meshes.Length];

        for (int i=0; i < this.spectrum.meshes.Length; i++)
        {
            // to get better performance when continually updating the Mesh
            this.spectrum.meshes[i].MarkDynamic();

            this.origVertices[i] = new List<Vector3>();
            this.modifiedVertices[i] = new List<Vector3>();

            this.spectrum.meshes[i].GetVertices(this.origVertices[i]);
            this.spectrum.meshes[i].GetVertices(this.modifiedVertices[i]);
        }

    }

    public void DeformMesh(Vector3 point, Vector3 direction, float radius)
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
                    var newVert = this.modifiedVertices[j][i] + direction * this.deformFactor;
                    this.modifiedVertices[j].RemoveAt(i);
                    this.modifiedVertices[j].Insert(i, newVert);
                    changed = true;
                }
            }

            if (changed)
            {
                Debug.Log("<SpectrumDeformer> mesh #"+ j.ToString() + " modified");
                this.spectrum.meshes[j].SetVertices(this.modifiedVertices[j]);
            }
        }

        /*for (int j = 0; j < this.modifiedVertices.Length; j++)
        {
            for (int i = 0; i < this.modifiedVertices[j].Count; i++)
            {
                if (this.modifiedVertices[j][i] != this.origVertices[j][i])
                    Debug.Log("!= at index " + j.ToString() + ":" + i.ToString());
            }
        }*/
    }

    void Start()
    {
        
    }

    void Update()
    {

    }
}

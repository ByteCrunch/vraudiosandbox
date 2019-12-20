using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]

public class SpectrumMesh : MonoBehaviour
{
    public float ringRadius;
    public float dataScale;

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
        //mesh.RecalculateBounds();
        //mesh.RecalculateNormals();
        //mesh.Optimize();
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
        
        var transform = this.transform;
        int n = 0;
        foreach (var vert in m.vertices)
        {
            if (n % 4 == 0)
                Gizmos.color = Color.white;
            else
                Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.TransformPoint(vert), 0.002f);
            n++;
        }
            
    }

    private void SetVertices()
    {
        vertices = new Vector3[this.audioEngine.fftBinCount * 4];
        float angleStep = 360f / (float)this.audioEngine.fftBinCount;
        Vector3 center = transform.position;

        //for (int f = 0; f < this.audioEngine.fftData.Length; f++)
        for (int f = 0; f < 1; f++)
        {
            for (int i = 3; i < vertices.Length; i += 4)
            //for (int i = 3; i < 248; i += 120)
            {
                // Add vertices for tetrahedron
                float angle = (i-3) / 4 * angleStep;
                float cosAngle = Mathf.Cos(angle * Mathf.Deg2Rad);
                float sinAngle = Mathf.Sin(angle * Mathf.Deg2Rad);

                Vector3 posCenterOfBase;
                posCenterOfBase.x = center.x;
                posCenterOfBase.y = center.y + this.ringRadius * cosAngle;
                posCenterOfBase.z = center.z + this.ringRadius * sinAngle;

                Vector3 posFft;
                posFft.x = center.x;
                posFft.y = center.y + (this.ringRadius + (float)this.audioEngine.fftData[f][i / 4] * this.dataScale) * cosAngle;
                posFft.z = center.z + this.ringRadius * sinAngle;

                vertices[i - 3] = posFft;

                Vector3 normalized = (center - posFft).normalized;

                float dotProduct1 = Vector3.Dot(normalized, Vector3.left);
                float dotProduct2 = Vector3.Dot(normalized, Vector3.forward);
                float dotProduct3 = Vector3.Dot(normalized, Vector3.up);

                Vector3 dotVector = ((1.0f - Mathf.Abs(dotProduct1)) * Vector3.right) +
                            ((1.0f - Mathf.Abs(dotProduct2)) * Vector3.forward) +
                            ((1.0f - Mathf.Abs(dotProduct3)) * Vector3.up);

                Vector3 A = Vector3.Cross(normalized, dotVector.normalized);
                Vector3 B = Vector3.Cross(A, normalized);

                for (int j = 0; j < 3; j++)
                {
                    float angleBase = j * 120;
                    float radius = 0.015f;
                    Vector3 pos;
                    pos.x = posCenterOfBase.x + radius * (A.x * Mathf.Cos(angleBase * Mathf.Deg2Rad) + B.x * Mathf.Sin(angleBase * Mathf.Deg2Rad));
                    pos.y = posCenterOfBase.y + radius * (A.y * Mathf.Cos(angleBase * Mathf.Deg2Rad) + B.y * Mathf.Sin(angleBase * Mathf.Deg2Rad));
                    pos.z = posCenterOfBase.z + radius * (A.z * Mathf.Cos(angleBase * Mathf.Deg2Rad) + B.z * Mathf.Sin(angleBase * Mathf.Deg2Rad));

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
            triangles[i] = i + 1;
            triangles[i + 1] = i;
            triangles[i + 2] = i + 2;

            triangles[i + 3] = i + 1;
            triangles[i + 4] = i + 3;
            triangles[i + 5] = i;

            triangles[i + 6] = i + 3;
            triangles[i + 7] = i + 1;
            triangles[i + 8] = i + 2;

            triangles[i + 9] = i + 3;
            triangles[i + 10] = i + 2;
            triangles[i + 11] = i;
        }
        mesh.triangles = triangles;
    }
}

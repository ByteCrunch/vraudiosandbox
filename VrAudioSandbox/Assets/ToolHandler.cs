using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolHandler : MonoBehaviour
{
    private SpectrumDeformer deformer;
    private Renderer r;

    void Start()
    {
        this.deformer = GameObject.Find("SpectrumMesh").GetComponent<SpectrumDeformer>();
        this.r = GetComponent<Renderer>();
    }


    void Update()
    {
        if (Input.GetButtonUp("DeformTest"))
        {
            deformer.DeformMesh(transform.position, Vector3.up, this.r.bounds.extents.magnitude);
        }
    }
}

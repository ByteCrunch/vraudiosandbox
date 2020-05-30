using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpectrumHelper : MonoBehaviour
{

    private GameObject toolValueIndicator;
    private int toolValueIndicatorFramesToShow;

    private void Awake()
    {
        this.toolValueIndicator = GameObject.Find("ToolValueIndicator");
    }
    private void Start()
    {
        // Set size & position of tool value plane to spectrum dimensions
        Vector3 tScale = this.toolValueIndicator.transform.localScale;
        Bounds tBounds = this.toolValueIndicator.GetComponent<MeshFilter>().mesh.bounds;
        Bounds spectrumBounds = this.GetSpectrumBounds();
        tScale.x = (spectrumBounds.size.x * tScale.x / tBounds.size.x);
        tScale.z = (spectrumBounds.size.z * tScale.z / tBounds.size.z);

        this.toolValueIndicator.transform.localScale = tScale;
        this.toolValueIndicator.transform.position = spectrumBounds.center;
        this.toolValueIndicator.SetActive(false);
    }

    private void Update()
    {
        if (transform.hasChanged)
        {
            // dirty hack for now as GameObject.Find() doesn't find inactive objects.
            // Due to the teleportation steam VR script, floor is only enabled during teleportation phase
            FloorHelper floor = Resources.FindObjectsOfTypeAll<FloorHelper>()[0]; 

            floor.FitFloorToBounds(this.GetSpectrumBounds());
            transform.hasChanged = false;
        }

        // Show indicator plane for tool value change
        if (this.toolValueIndicatorFramesToShow > 0)
        {
            if (this.toolValueIndicatorFramesToShow == 1)
                this.toolValueIndicator.SetActive(false);
            this.toolValueIndicatorFramesToShow--;
        }
            
    }

    public void ShowToolValueIndicator(int framesCount, float y)
    {
        this.toolValueIndicator.transform.SetPositionAndRotation(new Vector3(this.toolValueIndicator.transform.position.x, y, this.toolValueIndicator.transform.position.z), this.toolValueIndicator.transform.rotation);
        this.toolValueIndicator.SetActive(true);
        this.toolValueIndicatorFramesToShow = framesCount;
    }

    /// <summary>
    /// Calculates combined bounds of all mesh renderers combined
    /// </summary>
    /// <returns>Combined bounds of all mesh renderers</returns>
    private Bounds GetSpectrumBounds()
    {
        Renderer[] renderers = GameObject.Find("SpectrumMesh").GetComponentsInChildren<Renderer>();
        Bounds combinedBounds = new Bounds();

        for (int i = 0; i < renderers.Length; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }

        return combinedBounds;
    }
}
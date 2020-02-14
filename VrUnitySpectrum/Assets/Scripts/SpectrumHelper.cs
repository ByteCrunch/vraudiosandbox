using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpectrumHelper : MonoBehaviour
{
    private void Start()
    {

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
            
    }

    private Bounds GetSpectrumBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Bounds combinedBounds = new Bounds();

        for (int i = 0; i < renderers.Length; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }

        return combinedBounds;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorHelper : MonoBehaviour
{
    void Start()
    {

    }

    void Update()
    {
        
    }

    public void FitFloorToBounds(Bounds refBounds)
    {
        if (refBounds != null)
        {
            Renderer floorRenderer = (Renderer)GetComponent("MeshRenderer");
            
            // Scale floor to spectrum dimensions
            Vector3 a = refBounds.size;
            Vector3 b = floorRenderer.bounds.size;
            gameObject.transform.localScale.Set(a.x / b.x, 1, a.z / b.z);

            // Re-position floor
            gameObject.transform.position.Set(refBounds.center.x, 0, refBounds.center.z);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class Spectrum : MonoBehaviour
{
    // Start is called before the first frame update
    private IEnumerator Start()
    {

        var rect = new Valve.VR.HmdQuad_t();

        while (!SteamVR_PlayArea.GetBounds(SteamVR_PlayArea.Size.Calibrated, ref rect))
            yield return new WaitForSeconds(0.1f);

        Vector3 newScale = new Vector3(Mathf.Abs(rect.vCorners0.v0 - rect.vCorners2.v0), this.transform.localScale.y, Mathf.Abs(rect.vCorners0.v2 - rect.vCorners2.v2));

        Debug.Log(newScale.x.ToString() + " " + newScale.z.ToString());
        //this.transform.localScale = newScale;
    }


    // Update is called once per frame
    private void Update()
    {
        
    }
}
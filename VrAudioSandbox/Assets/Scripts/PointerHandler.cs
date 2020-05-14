using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Valve.VR;
using Valve.VR.Extras;

public class PointerHandler : MonoBehaviour
{
    public SteamVR_LaserPointer laserPointer;
    public SteamVR_Action_Single action;

    private SpectrumDeformer deformer;

    private void Awake()
    {
        laserPointer.PointerClick += this.PointerClick;
    }

    private void Start()
    {
        this.deformer = GameObject.Find("SpectrumMesh").GetComponent<SpectrumDeformer>();
    }

    public void PointerClick(object sender, PointerEventArgs e)
    {
        Debug.Log("Click! " + e.target.name.ToString());

        if (e.target.name.StartsWith("FFTData"))
        {
            //deformer.DeformMesh(e.point, Vector3.up, 0.01f);
            deformer.DeformMesh(e.point, Vector3.up, 0.1f, 0.8f);

            return;
        }
        
        if (e.target.name.StartsWith("UI"))
        {

            return;
        }
    }
}
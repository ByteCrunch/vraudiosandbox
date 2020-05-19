using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Valve.VR;
using Valve.VR.Extras;

public class ToolHandler : MonoBehaviour
{
    public SteamVR_LaserPointer laserPointer;
    public SteamVR_Action_Single action;

    private bool triggerDown;
    private ToolType selectedTool;
    private List<Vector3> pointsToDraw;
    private LineRenderer lr;
    
    public ToolType SelectedTool { get => selectedTool; set => selectedTool = value; }
    public bool TriggerDown { get => triggerDown; set => triggerDown = value; }
    
    private SpectrumDeformer deformer;

    public enum ToolType
    {
        SpectrumPainterAddOffset,
        SpectrumPainterSubOffset,
        SpectrumPencilAddOffset,
        SpectrumPencilSubOffset,
    }

    private void Awake()
    {
        laserPointer.PointerClick += this.PointerClick;
        laserPointer.PointerIn += this.PointerIn;

        this.lr = GetComponent<LineRenderer>();

        this.selectedTool = ToolType.SpectrumPainterAddOffset;

        this.triggerDown = false;
    }

    private void Start()
    {
        this.deformer = GameObject.Find("SpectrumMesh").GetComponent<SpectrumDeformer>();
        this.pointsToDraw = new List<Vector3>();
    }

    public void PointerClick(object sender, PointerEventArgs e)
    {
        if (this.selectedTool == ToolType.SpectrumPencilAddOffset && e.target.name.StartsWith("FFTData"))
        {
            //deformer.DeformMeshPoint(e.point, Vector3.up, 0.01f);
            deformer.DeformMeshPoint(e.point, Vector3.up, 0.1f, 0.8f);

            return;
        }
        
        if (e.target.name.StartsWith("UI"))
        {

            return;
        }
    }

    public void PointerIn(object sender, PointerEventArgs e)
    {
        if (this.selectedTool == ToolType.SpectrumPainterAddOffset && this.triggerDown && e.target.name.StartsWith("FFTData"))
        {
            this.pointsToDraw.Add(e.point);

            // Visualize trail
            this.lr.enabled = true;
            this.lr.positionCount = this.pointsToDraw.Count;
            this.lr.SetPositions(this.pointsToDraw.ToArray());

            return;
        }
    }

    public void TriggerUp()
    {
        if (this.pointsToDraw.Count > 0)
        {
            // Remove duplicates
            this.pointsToDraw = this.pointsToDraw.Distinct().ToList();

            if (this.selectedTool == ToolType.SpectrumPainterAddOffset)
                deformer.DeformMeshMultiplePoints(this.pointsToDraw, Vector3.up, 0.01f, 0.8f);
            else if (this.selectedTool == ToolType.SpectrumPainterSubOffset)
                deformer.DeformMeshMultiplePoints(this.pointsToDraw, Vector3.down, 0.01f, 0.8f);

            this.lr.positionCount = 0;
            this.lr.enabled = false;

            this.pointsToDraw.Clear();
        }
    }
}
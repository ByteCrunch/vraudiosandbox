﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Valve.VR;
using Valve.VR.Extras;

public enum ToolType
{
    SpectrumPainter,
    SpectrumPencil,
    SpectrumEraser
}

public class ToolHandler : MonoBehaviour
{
    public SteamVR_LaserPointer laserPointer;

    private bool triggerDown;
    private Tool selectedTool;
    private List<Vector3> pointsToDraw;
    private LineRenderer lr;

    public float toolRadius;
    public float toolabsoluteValue;

    public Tool[] tools;
    public ToolType SelectedToolType { get => selectedTool.type; }
    public bool TriggerDown { get => triggerDown; set => triggerDown = value; }
    
    private SpectrumDeformer deformer;
    private SpectrumHelper helper;

    private void Awake()
    {
        this.lr = GetComponent<LineRenderer>();
        this.helper = GameObject.Find("Spectrum").GetComponent<SpectrumHelper>();

        this.laserPointer.PointerClick += this.PointerClick;
        this.laserPointer.PointerIn += this.PointerIn;
        
        this.toolRadius = 0.002f;
        this.laserPointer.thickness = this.toolRadius;
        this.lr.startWidth = this.toolRadius;
        this.lr.endWidth = this.toolRadius;

        this.toolabsoluteValue = 0.05f;
        this.helper.ShowToolValueIndicator(1, this.toolabsoluteValue);

        // Init Tools
        this.tools = new Tool[Enum.GetNames(typeof(ToolType)).Length];
        this.tools[(int)ToolType.SpectrumPainter] = new Tool(ToolType.SpectrumPainter, new Color32(20, 20, 255, 100), GameObject.Find("IconSpectrumPainter").GetComponent<Image>());
        this.tools[(int)ToolType.SpectrumPencil] = new Tool(ToolType.SpectrumPencil, new Color32(255, 185, 255, 100), GameObject.Find("IconSpectrumPencil").GetComponent<Image>());
        this.tools[(int)ToolType.SpectrumEraser] = new Tool(ToolType.SpectrumEraser, new Color32(255, 50, 50, 100), GameObject.Find("IconSpectrumEraser").GetComponent<Image>());
        this.changeTool(ToolType.SpectrumPainter);

        this.triggerDown = false;
    }

    private void Start()
    {
        this.deformer = GameObject.Find("SpectrumMesh").GetComponent<SpectrumDeformer>();
        this.pointsToDraw = new List<Vector3>();
    }

    public void changeTool(ToolType tool)
    {
        for (int i=0; i < this.tools.Length; i++)
        {
            if (i == (int)tool)
            {
                this.selectedTool = this.tools[i];
                this.selectedTool.image.color = this.tools[i].color;
                this.laserPointer.color = this.tools[i].color;
                Debug.Log("<ToolHandler> Changed tool to " + Enum.GetName(typeof(ToolType), i));
            } else {
                this.tools[i].image.color = new Color(255f, 255f, 255f);
            }
        }
    }

    public void nextTool()
    {
        if ((int)this.selectedTool.type < this.tools.Length - 1)
            this.changeTool((ToolType)Enum.Parse(typeof(ToolType), Enum.GetName(typeof(ToolType), (int)this.selectedTool.type + 1)));
        else
            this.changeTool((ToolType)Enum.Parse(typeof(ToolType), Enum.GetName(typeof(ToolType), 0)));
    }

    public void prevTool()
    {
        if ((int)this.selectedTool.type > 0)
            this.changeTool((ToolType)Enum.Parse(typeof(ToolType), Enum.GetName(typeof(ToolType), (int)this.selectedTool.type - 1)));
        else
            this.changeTool((ToolType)Enum.Parse(typeof(ToolType), Enum.GetName(typeof(ToolType), this.tools.Length - 1)));
    }

    public void PointerClick(object sender, PointerEventArgs e)
    {
        if (this.SelectedToolType == ToolType.SpectrumPencil && e.target.name.StartsWith("FFTData"))
        {
            deformer.DeformMeshPoint(e.point, Vector3.up, this.toolRadius, this.toolabsoluteValue);

            return;
        }
    }

    public void PointerIn(object sender, PointerEventArgs e)
    {
        if ((this.SelectedToolType == ToolType.SpectrumPainter || this.SelectedToolType == ToolType.SpectrumEraser) && this.triggerDown && e.target.name.StartsWith("FFTData"))
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
        if ((this.SelectedToolType == ToolType.SpectrumPainter || this.SelectedToolType == ToolType.SpectrumEraser) && this.pointsToDraw.Count > 0) { 
            // Remove duplicates
            this.pointsToDraw = this.pointsToDraw.Distinct().ToList();

            if (this.SelectedToolType == ToolType.SpectrumPainter)
                deformer.DeformMeshMultiplePoints(this.pointsToDraw, Vector3.up, this.toolRadius, this.toolabsoluteValue);
            else if (this.SelectedToolType == ToolType.SpectrumEraser)
                deformer.DeformMeshMultiplePoints(this.pointsToDraw, Vector3.up, this.toolRadius, 0f);

            this.lr.positionCount = 0;
            this.lr.enabled = false;

            this.pointsToDraw.Clear();
            
            return;
        }
    }

    public void SetToolRadiusWithOffset(float offset)
    {
        if (this.toolRadius + offset >= 0.002f && this.toolRadius + offset < 8f)
        {
            this.toolRadius += offset;
            this.laserPointer.thickness = this.toolRadius;
            this.lr.startWidth = this.toolRadius;
            this.lr.endWidth = this.toolRadius;
        }
        
    }

    public void SetToolAbsoluteValueOffset(float offset)
    {
        if (this.toolabsoluteValue + offset >= 0 && this.toolabsoluteValue + offset < 6) //TODO check for maximum value according to bit depth
        {
            this.toolabsoluteValue += offset;
            this.helper.ShowToolValueIndicator(120, this.toolabsoluteValue);
        }
    }
}

public class Tool
{
    public ToolType type;
    public Color32 color;
    public Image image;

    public Tool(ToolType type, Color color, Image image)
    {
        this.type = type;
        this.color = color;
        this.image = image;
    }
}
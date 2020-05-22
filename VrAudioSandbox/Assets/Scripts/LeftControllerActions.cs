﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class LeftControllerActions : MonoBehaviour
{
    public SteamVR_Action_Boolean LeftMenu;
    public SteamVR_Input_Sources handType;
    private GameObject leftHand;
    private float yRotation;

    public bool LeftMenuActive = false;
    private Vector3 UIworldPos;
    private Vector3 UIlocalPos;
    private Quaternion UIlocalRotation;

    private AudioEngine audioEngine;
    private SpectrumMeshGenerator spectrum;
    private ToolHandler tool;
    private GameObject ui;


    private void Awake()
    {
        this.leftHand = GameObject.Find("LeftHand");
    }

    void Start()
    {
        this.audioEngine = GameObject.Find("Audio").GetComponent<AudioEngine>();
        this.spectrum = GameObject.Find("SpectrumMesh").GetComponent<SpectrumMeshGenerator>();
        this.tool = this.GetComponent<ToolHandler>();

        this.ui = GameObject.Find("UI");
        this.ui.SetActive(false);

        this.LeftMenu.AddOnStateDownListener(this.LeftMenuActivate, handType);
        this.LeftMenu.AddOnStateUpListener(this.LeftMenuDeactivate, handType);
    }

    private void LeftMenuActivate (SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        this.LeftMenuActive = true;
        this.ui.SetActive(true);

        this.UIworldPos = this.ui.transform.position;
        this.UIlocalPos = this.ui.transform.localPosition;
        this.UIlocalRotation = this.ui.transform.localRotation;

        this.ui.transform.SetParent(null, true);
        this.ui.transform.position = this.UIworldPos;

        this.yRotation = this.leftHand.transform.rotation.y;
    }

    private void LeftMenuDeactivate(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        this.LeftMenuActive = false;
        this.ui.SetActive(false);
        this.ui.transform.SetParent(GameObject.Find("HoverPoint").transform);
        this.ui.transform.localPosition = this.UIlocalPos;
        this.ui.transform.localRotation = this.UIlocalRotation;
    }

    void Update()
    {
        // Handle tool selection menu
        if (this.LeftMenuActive)
        {
            if (this.leftHand.transform.rotation.y - this.yRotation > 0.05)
            {
                this.tool.nextTool();
                this.yRotation = this.leftHand.transform.rotation.y;
            }
                
            else if (this.yRotation - this.leftHand.transform.rotation.y > 0.05) { 
                this.tool.prevTool();
                this.yRotation = this.leftHand.transform.rotation.y;
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.Extras;

public class RightControllerActions : MonoBehaviour
{
    public SteamVR_Action_Boolean DecToolRadius;
    public SteamVR_Action_Boolean IncToolRadius;
    public SteamVR_Action_Boolean hold;

    public SteamVR_Input_Sources handType;

    private SteamVR_LaserPointer laserPointer;
    private AudioEngine audioEngine;
    private SpectrumMeshGenerator spectrum;
    private ToolHandler tool;

    void Start()
    {
        this.audioEngine = GameObject.Find("Audio").GetComponent<AudioEngine>();
        this.spectrum = GameObject.Find("SpectrumMesh").GetComponent<SpectrumMeshGenerator>();
        this.tool = this.GetComponent<ToolHandler>();
        this.laserPointer = GameObject.Find("RightHand").GetComponent<SteamVR_LaserPointer>();

        this.DecToolRadius.AddOnStateDownListener(this.DecToolRadiusDown, handType);
        this.IncToolRadius.AddOnStateDownListener(this.IncToolRadiusDown, handType);
        this.hold.AddOnStateDownListener(this.TriggerDown, handType);
        this.hold.AddOnStateUpListener(this.TriggerUp, handType);
    }

    private void DecToolRadiusDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        this.tool.SetToolRadiusWithOffset(-0.005f);
    }
    
    private void IncToolRadiusDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        this.tool.SetToolRadiusWithOffset(0.005f);
    }

    private void TriggerDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        this.tool.TriggerDown = true;
    }

    private void TriggerUp(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        this.tool.TriggerDown = false;
        this.tool.TriggerUp();

    }

    void Update()
    {
        
    }
}

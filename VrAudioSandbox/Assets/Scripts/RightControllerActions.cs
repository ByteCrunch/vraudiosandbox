using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class RightControllerActions : MonoBehaviour
{
    public SteamVR_Action_Boolean PlayStop;
    public SteamVR_Action_Boolean Rewind;
    public SteamVR_Action_Boolean ScaleMeshYDec;
    public SteamVR_Action_Boolean ScaleMeshYInc;
    public SteamVR_Action_Boolean hold;

    public SteamVR_Input_Sources handType;

    private AudioEngine audioEngine;
    private SpectrumMeshGenerator spectrum;
    private ToolHandler tool;

    void Start()
    {
        this.audioEngine = GameObject.Find("Audio").GetComponent<AudioEngine>();
        this.spectrum = GameObject.Find("SpectrumMesh").GetComponent<SpectrumMeshGenerator>();
        this.tool = this.GetComponent<ToolHandler>();

        this.PlayStop.AddOnStateDownListener(this.PlayStopDown, handType);
        this.Rewind.AddOnStateDownListener(this.RewindDown, handType);
        this.hold.AddOnStateDownListener(this.TriggerDown, handType);
        this.hold.AddOnStateUpListener(this.TriggerUp, handType);
    }

    private void PlayStopDown (SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (!this.audioEngine.isPlaying)
        {
            this.audioEngine.Play();
        }
        else
        {
            this.audioEngine.Stop();
        }
    }

    private void RewindDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        this.audioEngine.Rewind();
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

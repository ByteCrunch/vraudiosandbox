using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class HandController : MonoBehaviour
{
    public SteamVR_Action_Boolean PlayStop;
    public SteamVR_Action_Boolean Rewind;
    public SteamVR_Action_Boolean ScaleMeshYDec;
    public SteamVR_Action_Boolean ScaleMeshYInc;

    public SteamVR_Input_Sources handType;

    private AudioEngine audioEngine;
    private SpectrumMeshGenerator spectrum; 

    void Start()
    {
        this.audioEngine = GameObject.Find("Audio").GetComponent<AudioEngine>();
        this.spectrum = GameObject.Find("SpectrumMesh").GetComponent<SpectrumMeshGenerator>();

        this.PlayStop.AddOnStateDownListener(PlayStopDown, handType);
        this.Rewind.AddOnStateDownListener(RewindDown, handType);
        this.ScaleMeshYDec.AddOnStateDownListener(ScaleMeshYDecDown, handType);
        this.ScaleMeshYInc.AddOnStateDownListener(ScaleMeshYIncDown, handType);
    }

    private void PlayStopDown (SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources formSource)
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

    private void RewindDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources formSource)
    {
        this.audioEngine.Rewind();
    }

    private void ScaleMeshYDecDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources formSource)
    {
        this.spectrum.ScaleMeshY(-0.05f);
    }

    private void ScaleMeshYIncDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources formSource)
    {
        this.spectrum.ScaleMeshY(0.05f);
    }


    void Update()
    {
        
    }
}

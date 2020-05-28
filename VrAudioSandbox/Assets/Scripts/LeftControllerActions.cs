using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class LeftControllerActions : MonoBehaviour
{
    public SteamVR_Action_Boolean PlayStop;
    public SteamVR_Action_Boolean Rewind;
    public SteamVR_Action_Boolean ToolMenu;
    public SteamVR_Action_Boolean ToggleMainMenu;
    //public SteamVR_Action_Vector2 Scroll;

    public SteamVR_Input_Sources handType;
    private GameObject leftHand;
    private float yRotation;

    public bool toolMenuActive = false;
    public bool mainMenuActive = false;
    private Vector3 ToolUIworldPos;
    private Vector3 ToolUIlocalPos;
    private Quaternion UIlocalRotation;
    private Vector3 mainMenuworldPos;
    private Vector3 mainMenulocalPos;
    private Quaternion mainMenulocalRotation;

    private AudioEngine audioEngine;
    private SpectrumMeshGenerator spectrum;
    private ToolHandler tool;
    private GameObject uiTool;
    private GameObject uiMain;


    private void Awake()
    {
        this.leftHand = GameObject.Find("LeftHand");
    }

    void Start()
    {
        this.audioEngine = GameObject.Find("Audio").GetComponent<AudioEngine>();
        this.spectrum = GameObject.Find("SpectrumMesh").GetComponent<SpectrumMeshGenerator>();
        this.tool = this.GetComponent<ToolHandler>();

        this.uiTool = GameObject.Find("UITool");
        this.uiTool.SetActive(false);
        
        this.uiMain = GameObject.Find("UIMainMenu");
        this.uiMain.SetActive(false);

        this.PlayStop.AddOnStateDownListener(this.PlayStopDown, handType);
        this.Rewind.AddOnStateDownListener(this.RewindDown, handType);
        this.ToolMenu.AddOnStateDownListener(this.ToolMenuActivate, handType);
        this.ToolMenu.AddOnStateUpListener(this.ToolMenuDeactivate, handType);
        this.ToggleMainMenu.AddOnStateDownListener(this.MainMenuToggle, handType);
        //this.Scroll.AddOnAxisListener(this.ScaleMesh, handType);
    }
    private void PlayStopDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
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

    private void ToolMenuActivate(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        this.toolMenuActive = true;
        this.uiTool.SetActive(true);

        this.ToolUIworldPos = this.uiTool.transform.position;
        this.ToolUIlocalPos = this.uiTool.transform.localPosition;
        this.UIlocalRotation = this.uiTool.transform.localRotation;

        this.uiTool.transform.SetParent(null, true);
        this.uiTool.transform.position = this.ToolUIworldPos;

        this.yRotation = this.leftHand.transform.rotation.y;
    }

    private void ToolMenuDeactivate(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        this.toolMenuActive = false;
        this.uiTool.SetActive(false);
        
        this.uiTool.transform.SetParent(GameObject.Find("HoverPoint").transform);
        this.uiTool.transform.localPosition = this.ToolUIlocalPos;
        this.uiTool.transform.localRotation = this.UIlocalRotation;
    }

    private void MainMenuToggle(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (this.mainMenuActive)
        {
            this.mainMenuActive = false;
            this.uiMain.SetActive(false);

            this.uiMain.transform.SetParent(GameObject.Find("VRCamera").transform, true);
            this.uiMain.transform.localPosition = this.mainMenulocalPos;
            this.uiMain.transform.localRotation = this.mainMenulocalRotation;

        } else {

            this.mainMenuActive = true;
            this.uiMain.SetActive(true);

            this.mainMenuworldPos = this.uiMain.transform.position;
            this.mainMenulocalPos = this.uiMain.transform.localPosition;
            this.mainMenulocalRotation = this.uiMain.transform.localRotation;

            this.uiMain.transform.SetParent(null, true);
            this.uiMain.transform.position = this.mainMenuworldPos;
        }
    }

    /*public void ScaleMesh(SteamVR_Action_Vector2 fromAction, SteamVR_Input_Sources fromSource, Vector2 axis, Vector2 delta)
    {
        this.spectrum.ScaleMeshY(delta.y / 10);
    }*/

    void Update()
    {
        // Handle tool selection menu
        if (this.toolMenuActive)
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

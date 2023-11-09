// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2023) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using MixedReality.Toolkit.SpatialManipulation;
using MixedReality.Toolkit.UX;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SharedExperienceScript : MonoBehaviour
{
    private string DebugTAG = "SEWDS-SES";

    /// <summary>
    /// Shared event types used for command strings when communicating between devices
    /// </summary>
    private enum SEvents
    {
        Reset,
        Rotate,
        Parts,
        Fullsize,
        Move,
        Orientation,
        Scale
    };

    /// <summary>
    /// Flag to track when to pass interactions on to other devices
    /// </summary>
    [HideInInspector]
    public bool ShareWithConnections { get; set; }

    private bool isBeingManipulated = false;

    private Vector3 lastPosition;

    //private ModelController modelController;

    #region Inspector variables

    [Space(10.0f)]
    [SerializeField]
    [Tooltip("3D model being shared across devices")]
    public GameObject TargetObj;

    [SerializeField]
    [Tooltip("Main presentation stage for shared 3D object that is used as common reference origin between devices")]
    public GameObject Stage;

    [SerializeField]
    [Tooltip("WiFi Direct Manager used for communicating with WiFi Direct plugin")]
    private WifiDirectPluginScript WiFiDirectManager;

    [Header("Shared functionality between devices")]

    [SerializeField]
    [Tooltip("UI Action Button that triggers Reset behavior")]
    private PressableButton resetButton;

    [SerializeField]
    [Tooltip("UI Action Toggle Button that toggles on & off the model rotate behavior")]
    private PressableButton rotateButton;

    [SerializeField]
    [Tooltip("UI Action Toggle Button that toggles on & off the hideable elements of the 3D model")]
    private PressableButton partsButton;

    [SerializeField]
    [Tooltip("UI Action Button that triggers FullSize behavior")]
    private PressableButton fullsizeButton;

    #endregion Inspector variables

    // Start is called before the first frame update
    void Start()
    {
        //if (TargetObj != null)
        //{
        //    modelController = TargetObj.GetComponent<ModelController>();
        //}
    }

    // Update is called once per frame
    void Update()
    {
        if (isBeingManipulated)
        {
            ShareManipulation();
            lastPosition = TargetObj.transform.position;
        }
    }

    private void ShareCommand(string command)
    {
        if (WiFiDirectManager != null && ShareWithConnections)
        {
            WiFiDirectManager.GetComponent<WifiDirectPluginScript>().SendCommandToPeers(command);
        }
    }
    private void ShareManipulation()
    {
        if (ShareWithConnections)
        {
            //Send position of target
            //Target Postion relative to common stage

            Vector3 deltaPosition = TargetObj.transform.position - Stage.transform.position;
            deltaPosition = Quaternion.Euler(0, -Stage.transform.rotation.eulerAngles.y, 0) * deltaPosition;

            ShareCommand($"{nameof(SEvents.Move)},{deltaPosition.x.ToString()},{deltaPosition.y.ToString()},{deltaPosition.z.ToString()},");

            Quaternion eRelativeRotation = Quaternion.Euler(0, -Stage.transform.rotation.eulerAngles.y, 0) * TargetObj.transform.rotation;

            ShareCommand($"{nameof(SEvents.Orientation)},{eRelativeRotation.x.ToString()},{eRelativeRotation.y.ToString()},{eRelativeRotation.z.ToString()},{eRelativeRotation.w.ToString()},");

            //Send scale, should be same in all shared worlds, no need to adjust
            ShareCommand($"{nameof(SEvents.Scale)},{TargetObj.transform.localScale.x.ToString()},{TargetObj.transform.localScale.y.ToString()},{TargetObj.transform.localScale.z.ToString()},");

        }
    }

    /// <summary>
    /// Parse shared incomming commands from other devices and execute those commands on the local device.
    /// </summary>
    /// <param name="command">Comma delimited command string that begins with a shared event type of SEvents.</param>
    public void HandleSharedCommands(string command)
    {
        BoundsControl boundsControl = TargetObj.GetComponent<BoundsControl>();

        bool state;
        string[] cmdStrs;
        cmdStrs = command.Split(new char[] { ',' });
        bool parsedFailed = false;

        switch (cmdStrs[0])
        {
            case nameof(SEvents.Reset):
                resetButton.OnClicked.Invoke();
                break;

            case nameof(SEvents.Rotate):
                bool.TryParse(cmdStrs[1], out state);
                rotateButton.ForceSetToggled(state);
                break;

            case nameof(SEvents.Parts):
                bool.TryParse(cmdStrs[1], out state);
                partsButton.ForceSetToggled(state);
                break;

            case nameof(SEvents.Fullsize):
                fullsizeButton.OnClicked.Invoke();
                break;

            case nameof(SEvents.Move):
                float newX;
                float newY;
                float newZ;
                if (!float.TryParse(cmdStrs[1], out newX)) parsedFailed = true;
                if (!float.TryParse(cmdStrs[2], out newY)) parsedFailed = true;
                if (!float.TryParse(cmdStrs[3], out newZ)) parsedFailed = true;

                if (cmdStrs.Length > 5)
                {
                    Debug.LogWarning($"{DebugTAG}-PARSE Move command, more parts than expected: {command}");
                }

                if (float.NaN == newX || parsedFailed)
                {
                    Debug.LogError($"{DebugTAG}-PARSE Move X NaN Original Command: {command}");
                }
                else
                {
                    Vector3 newVector = new Vector3(newX, newY, newZ);

                    Vector3 deltaVector = Quaternion.Euler(0, Stage.transform.rotation.eulerAngles.y, 0) * newVector;

                    if (TargetObj != null)
                    {
                        TargetObj.transform.position = deltaVector + Stage.transform.position;
                        Debug.LogWarning($"{DebugTAG}-MOVE got set");
                    }
                }
                break;

            case nameof(SEvents.Orientation):
                float rotX;
                float rotY;
                float rotZ;
                float rotW;
                if (!float.TryParse(cmdStrs[1], out rotX)) parsedFailed = true;
                if (!float.TryParse(cmdStrs[2], out rotY)) parsedFailed = true;
                if (!float.TryParse(cmdStrs[3], out rotZ)) parsedFailed = true;
                if (!float.TryParse(cmdStrs[4], out rotW)) parsedFailed = true;

                if (cmdStrs.Length > 6)
                {
                    Debug.LogWarning($"{DebugTAG}-PARSE Rot command, more parts than expected: {command}");
                }

                if (float.NaN == rotX || parsedFailed)
                {
                    Debug.LogError($"{DebugTAG}-PARSE Rot X NaN Original Command: {command}");
                }
                else
                {
                    Quaternion newRot = new Quaternion(rotX, rotY, rotZ, rotW);

                    if (TargetObj != null)
                    {
                        TargetObj.transform.rotation = Quaternion.AngleAxis(Stage.transform.rotation.eulerAngles.y, Vector3.up) * newRot;
                        Debug.LogWarning($"{DebugTAG}-ROTATION got set");
                    }
                }
                break;

            case nameof(SEvents.Scale):

                float scaleX;
                float scaleY;
                float scaleZ;

                if (!float.TryParse(cmdStrs[1], out scaleX)) parsedFailed = true;
                if (!float.TryParse(cmdStrs[2], out scaleY)) parsedFailed = true;
                if (!float.TryParse(cmdStrs[3], out scaleZ)) parsedFailed = true;

                if (cmdStrs.Length > 5)
                {
                    Debug.LogWarning($"{DebugTAG}-PARSE Scale command, more parts than expected: {command}");
                }

                if (float.NaN == scaleX || parsedFailed)
                {
                    Debug.LogError($"{DebugTAG}-PARSE Scale X NaN Original Command: {command}");
                }
                else
                {
                    if (TargetObj != null)
                    {
                        TargetObj.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
                        Debug.LogWarning($"{DebugTAG}-SCALE got set");
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Called by 3DModel's Object Manipulator & Bounds Control components to mark the begging of a sharable interaction 
    /// </summary>
    /// <param name="selectEnterEventArgs">Select Enter event returns a SelectEnterEventArgs obj</param>
    public void TargetEnterManipulation(SelectEnterEventArgs selectEnterEventArgs)
    {
        lastPosition = TargetObj.transform.position;
        isBeingManipulated = true;
    }

    /// <summary>
    /// /// Called by 3DModel's Object Manipulator & Bounds Control components to mark the end of a sharable interaction 
    /// </summary>
    /// <param name="selectExitEventArgs">Select Exited event returns a SelecteExitEventArgs obj></param>
    public void TargetExitManipulation(SelectExitEventArgs selectExitEventArgs)
    {
        isBeingManipulated = false;
        ShareManipulation();
    }

    /// <summary>
    /// Called by Action Buttons that trigger shared functionality between devices when clicked.  
    /// </summary>
    /// <remarks>
    /// NOTE: Associating it to the button's XRI Interactable Event: Select Exited event will ensure
    /// the command is shared only for user interactions and avoid infinite message looping
    /// </remarks>
    /// <param name="args">Select Exited event returns a SelecteExitEventArgs obj</param>
    public void ShareOnClickSelectExited(SelectExitEventArgs args)
    {
        string command = "";

        if (WiFiDirectManager != null && ShareWithConnections)
        {
            if ((object)args.interactableObject == resetButton)
            {
                command = nameof(SEvents.Reset);
            }
            else if ((object)args.interactableObject == fullsizeButton)
            {
                command = nameof(SEvents.Fullsize);
            }
            else
            {
                Debug.LogError($"{DebugTAG}-SOCSE unrecognized interactableObj clicked button: {args.interactableObject}");
            }

            WiFiDirectManager.GetComponent<WifiDirectPluginScript>().SendCommandToPeers(command.ToString());
        }
    }

    /// <summary>
    /// Called by Toggle Buttons that trigger shared state based functionality between devices when clicked
    /// </summary>
    /// <remarks>
    /// NOTE: Associating it to the button's XRI Interactable Event: Select Exited event will ensure
    /// the command is shared only for user interactions and avoid infinite message looping
    /// </remarks>
    /// <param name="args">Select Exited event returns a SelecteExitEventArgs obj</param>
    public void ShareToggleSelectExited(SelectExitEventArgs args)
    {
        string command = "";
        bool state = false;

        if (WiFiDirectManager != null && ShareWithConnections)
        {
            if ((object)args.interactableObject == partsButton)
            {
                command = nameof(SEvents.Parts);
                state = partsButton.IsToggled;
            }
            else if ((object)args.interactableObject == rotateButton)
            {
                command = nameof(SEvents.Rotate);
                state = rotateButton.IsToggled;
            }
            else
            {
                Debug.LogError($"{DebugTAG}-STSE unrecognized interactableObj toggled button: {args.interactableObject}");
            }

            WiFiDirectManager.GetComponent<WifiDirectPluginScript>().SendCommandToPeers($"{command.ToString()},{state}");
        }
    }


}

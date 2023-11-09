// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2023) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using MixedReality.Toolkit.UX;
using System;
using TMPro;
using UnityEngine;


public class WifiDirectPluginScript : MonoBehaviour
{
    private string DebugTAG = "SEWDS-WDPS";

    private AndroidJavaClass unityClass;
    private AndroidJavaObject unityActivity;

    private TextMeshProUGUI outputLabel;

    private string[] serviceList;
    private string[] deviceList;

    private Color systemMsgColor;

    private bool isConnected = false;
    private CanvasGroup instructionPopupCanvasGroup;

    #region Inspector variables

    [Header("******* WiFi Direct Menu items *******")]
    [Space(10.0f)]

    [SerializeField]
    [Tooltip("Action Button used to initiate WiFi Direct Service Discovery")]
    private PressableButton discoverServicesButton;

    [Header("Dynamic lists in UI")]

    [SerializeField]
    [Tooltip("WiFi Direct UI scrollable content container for populating list of discovered WiFi Direct Services")]
    private GameObject serviceButtonList;

    [SerializeField]
    [Tooltip("WiFi Direct UI scrollable content container for populating list of connected WiFi Direct peer devices")]
    private GameObject deviceButtonList;

    [SerializeField]
    [Tooltip("Action button prefab used for elements in WiFi Direct UI lists")]
    private GameObject actionButtonPrefab;

    [Header("Dynamic labels")]

    [SerializeField]
    [Tooltip("WiFi Direct UI TextMeshPro label used to display name of this device")]
    private TextMeshProUGUI deviceNameLabel;

    [SerializeField]
    [Tooltip("WiFi Direct UI TextMeshPro label used to display name when hosting WiFi Direct Service")]
    private TextMeshProUGUI serviceNameLabel;

    [SerializeField]
    [Tooltip("WiFi Direct UI TextMeshPro label used to display information about name of this device")]
    private TextMeshProUGUI discoveryStatusLabel;

    [SerializeField]
    [Tooltip("WiFi Direct UI TextMeshPro label used to display incomming text messages from connected devices")]
    private TextMeshProUGUI incommingMessageBox;

    [SerializeField]
    [Tooltip("WiFi Direct UI TextMeshPro label used to temporarily display system text messages")]
    private TextMeshProUGUI systemMsgNotificationBox;
    
    [SerializeField]
    [Tooltip("Speed factor in seconds of lenght of time the system message will display")]
    private float systemMsgDisplaySpeed = 1.0f;

    [Header("******* Secondary Menu items *******")]
    [Header("Dynamic labels")]

    [SerializeField]
    [Tooltip("Secondary UI TextMeshPro label used to display information about current WiFi Direct Service status")]
    private TextMeshProUGUI serviceStatusLabel;

    [SerializeField]
    [Tooltip("Secondary UI TextMeshPro label used to display information about available WiFi Direct Services")]
    private TextMeshProUGUI servicesAvailableLabel;

    [SerializeField]
    [Tooltip("Secondary UI TextMeshPro scrollable content label used to display connection status")]
    private TextMeshProUGUI connectionStatusLabel;

    [Space(10.0f)]

    [SerializeField]
    [Tooltip("Secondary UI element to be displayed when connected over WiFi Direct")]
    private GameObject ConnectedIndicator;

    [SerializeField]
    [Tooltip("Secondary Popup UI element to be displayed when connected over WiFi Direct to instruct all users to align their Stage.")]
    private GameObject instructionPopup;

    [SerializeField]
    [Tooltip("Speed factor in seconds of lenght of time the instruction popup will display")]
    private float popupDisplaySpeed = 5.0f;

    [Header("Related components")]

    [SerializeField]
    [Tooltip("Shared Experience Manager for sharing and processing shared commands and experience")]
    private SharedExperienceScript sharedExperienceManager;

    #endregion Inspector variables

    // Start is called before the first frame update
    void Start()
    {        
        unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        unityActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");     
        unityActivity.Call("SetServiceName", Application.productName);
        deviceNameLabel.text = GetDeviceName();
        systemMsgColor = systemMsgNotificationBox.color;
        instructionPopupCanvasGroup = instructionPopup.GetComponent<CanvasGroup>();
    }

    // Update is called once per frame
    void Update()
    {
        if (systemMsgColor.a > 0)
        {
            systemMsgColor.a = systemMsgColor.a - Time.deltaTime / systemMsgDisplaySpeed;
            systemMsgNotificationBox.color = systemMsgColor;
        }
        if (instructionPopupCanvasGroup.alpha > 0)
        {
            instructionPopupCanvasGroup.alpha = instructionPopupCanvasGroup.alpha - Time.deltaTime / popupDisplaySpeed;
            if (instructionPopupCanvasGroup.alpha <= 0 )
            {
                instructionPopup.gameObject.SetActive(false);
            }
        }        
    }

    #region Calls to WiFi Direct Plugin

    /// <summary>
    /// Call to WDPluginActivity : Get name of this Android device
    /// </summary>
    /// <returns>device name</returns>
    private string GetDeviceName()
    {
        string dName = unityActivity.Call<string>("GetDeviceName");
        return dName;
    }

    /// <summary>
    /// Call to WDPluginActivity : Start hosting a WiFi Direct Service
    /// </summary>
    /// <remarks>Invoked from UI</remarks>
    public void StartHosting()
    {
        if (unityActivity != null)
        {
            unityActivity.Call("StartHosting");
        }
    }

    /// <summary>
    /// Call to WDPluginActivity : Stop hosting WiFi Direct Service
    /// </summary>
    /// <remarks>Invoked from UI</remarks>
    public void StopHosting()
    {
        if (unityActivity != null)
        {
            unityActivity.Call("StopHosting");
            connectionStatusLabel.text = $"Not connected";
        }
    }

    /// <summary>
    /// Call to WDPluginActivity : Start WiFi Direct Service Discovery
    /// </summary>
    /// <remarks>Invoked from UI</remarks>
    public void StartDiscovery()
    {
        if (unityActivity != null)
        {
            //Clear ServiceButtonList
            foreach (Transform child in serviceButtonList.transform)
            {
                Destroy(child.gameObject);
            }
            serviceButtonList.transform.DetachChildren();

            unityActivity.Call("StartDiscovering");
        }
    }

    /// <summary>
    /// Call to WDPluginActivity : Stop WiFi Direct Service Discovery
    /// </summary>
    /// <remarks>Invoked from UI</remarks>
    public void StopDiscovery()
    {
        if (unityActivity != null)
        {
            unityActivity.Call("StopDiscovering");
            servicesAvailableLabel.text = "";
        }
    }

    /// <summary>
    /// Call to WDPluginActivity : Connect to WiFi Direct Service
    /// </summary>
    /// <remarks>Invoked from UI code generated buttons</remarks>
    /// <param name="index">position of service in serviceList</param>
    public void ConnectToService(int index)
    {
        if (unityActivity != null & serviceList[index] != "")
        {
            unityActivity.Call("ConnectToService", serviceList[index]);
        }
    }

    /// <summary>
    /// Call to WDPluginActivity : Send incremented hardcoded test message to connected peer devices
    /// </summary>
    /// <remarks>Invoked from UI</remarks>
    public void SendMsgToPeer()
    {
        if (unityActivity != null)
        {
            unityActivity.Call("SendMsgToPeer");
        }
    }

    /// <summary>
    /// Call to WDPluginActivity : Send interaction command to connected peer devices
    /// </summary>
    /// <param name="command">command name followed by comma delimeted command arguments</param>
    public void SendCommandToPeers(string command)
    {
        if (unityActivity != null)
        {
            unityActivity.Call("SendCommandToPeers", command);
        }
    }

    #endregion Calls to WiFi Direct Plugin

    #region Handlers for WiFi Direct Plugin

    /// <summary>
    ///  WDPluginActivity Callback Handler: Handle the processing of incomming interaction commands from other connected devices
    /// </summary>
    /// <param name="command">command name followed by comma delimeted command arguments</param>
    public void HandleIncommingCmd(string command)
    {
        if (sharedExperienceManager != null)
        {
            sharedExperienceManager.GetComponent<SharedExperienceScript>().HandleSharedCommands(command);
        }
    }

    /// <summary>
    /// WDPluginActivity Callback Handler: Display name of WiFi Direct broadcast service
    /// </summary>
    /// <param name="serviceFriendlyName">Name of service</param>
    public void DisplayHostServiceFriendlyName(string serviceFriendlyName)
    {
        if (serviceFriendlyName == "")
        {
            serviceNameLabel.text = "Not hosting";
            serviceStatusLabel.text = "";
        }
        else
        {
            serviceNameLabel.text = serviceFriendlyName;
            serviceStatusLabel.text = $"Hosting service: {serviceNameLabel.text}";
        }
    }

    /// <summary>
    /// WDPluginActivity Callback Handler: Display list of WiFi Direct connected peers
    /// </summary>
    /// <param name="devices">comma dilimited string list of device names</param>
    public void DisplayConnectedDevices(string devices)
    {
        deviceList = devices.Split(char.Parse(","));

        int previousDeviceCount = 0;

        //Clear DeviceButtonList
        foreach (Transform child in deviceButtonList.transform)
        {
            previousDeviceCount++;
            Destroy(child.gameObject);
        }
        deviceButtonList.transform.DetachChildren();

        for (int i = 0; i < deviceList.Length; i++)
        {
            if (deviceList[i] != "")
            {
                GameObject newButton = Instantiate(actionButtonPrefab, deviceButtonList.transform);
                PressableButton pressableButton = newButton.GetComponent<PressableButton>();

                var label = newButton.transform.Find("Frontplate/AnimatedContent/Icon/Label");

                outputLabel = label.GetComponentInChildren<TextMeshProUGUI>();
                outputLabel.text = deviceList[i].ToString();

                Transform icon = newButton.transform.Find("Frontplate/AnimatedContent/Icon/UIButtonFontIcon");
                icon.gameObject.SetActive(true);
                pressableButton.enabled = false;
                IsConnected = true;
            }
        }

        if (deviceList.Length != 1)
        {
            connectionStatusLabel.text = $"{deviceList.Length} devices connected";
        }
        else
        {
            if (deviceList[0] == "")
            {
                //Peer connections always report back empty list in Android 10
                //But if Host and the only connected peer drops their connection, this will also be an empty list and should set isConnected to false and label to Not connected                                 
                if (previousDeviceCount > 0)
                {
                    IsConnected = false;
                    connectionStatusLabel.text = $"Not connected";
                }
            }
            else
            {
                connectionStatusLabel.text = $"{deviceList.Length} device connected";
            }
        }
    }

    /// <summary>
    /// WDPluginActivity Callback Handler: Display WiFi Direct Discovery status
    /// </summary>
    /// <param name="status">status description</param>
    public void DisplayDiscoveryStatus(string status)
    {
        if (status == "")
        {
            discoveryStatusLabel.text = "Discovery off";
            serviceStatusLabel.text = "";
        }
        else
        {
            discoveryStatusLabel.text = status;
            serviceStatusLabel.text = status;
        }
    }

    /// <summary>
    /// WDPluginActivity Callback Handler: Display name of WiFi Direct broadcast service currently connected to
    /// </summary>
    /// <param name="serviceFriendlyName">Name of service</param>
    public void DisplayConnectedServiceFriendlyName(string serviceFriendlyName)
    {

        if (serviceFriendlyName == "")
        {
            connectionStatusLabel.text = "Not connected";
            IsConnected = false;
        }

        int i = 0;
        foreach (Transform child in serviceButtonList.transform)
        {
            Transform icon = child.transform.Find("Frontplate/AnimatedContent/Icon/UIButtonFontIcon");

            if (serviceList[i] == serviceFriendlyName && serviceFriendlyName != "")
            {
                icon.gameObject.SetActive(true);
                IsConnected = true;
                connectionStatusLabel.text = $"Connected to: {serviceFriendlyName}";

                discoveryStatusLabel.text = "Connected to Host";

                foreach (Transform sibling in serviceButtonList.transform)
                {
                    if (sibling != child)
                    {
                        sibling.gameObject.SetActive(false);
                    }
                }

                child.gameObject.GetComponent<PressableButton>().enabled = false;
                discoverServicesButton.ForceSetToggled(false);
                servicesAvailableLabel.text = "";

            }
            else
            {
                icon.gameObject.SetActive(false);
                if (serviceList[i] == serviceFriendlyName && serviceFriendlyName == "")
                {
                    child.gameObject.SetActive(false);
                }
            }
            i++;
        }
    }

    /// <summary>
    /// WDPluginActivity Callback Handler: Display list of discovered WiFi Direct Services
    /// </summary>
    /// <param name="services">comma dilimited string list of recently discovered services</param>
    public void DisplayAvailableServices(string services)
    {
        serviceList = services.Split(char.Parse(","));

        if (serviceList.Length == 1 && serviceList[0] == "")
        {
            return;
        }

        //Clear ServiceButtonList
        foreach (Transform child in serviceButtonList.transform)
        {
            Destroy(child.gameObject);
        }
        serviceButtonList.transform.DetachChildren();

        for (int i = 0; i < serviceList.Length; i++)
        {
            if (serviceList[i] != "")
            {
                GameObject newButton = Instantiate(actionButtonPrefab, serviceButtonList.transform);
                PressableButton pressableButton = newButton.GetComponent<PressableButton>();
                int serviceIndex = i;
                pressableButton.OnClicked.AddListener(() => ConnectToService(serviceIndex));

                var label = newButton.transform.Find("Frontplate/AnimatedContent/Icon/Label");

                outputLabel = label.GetComponentInChildren<TextMeshProUGUI>();
                outputLabel.text = serviceList[i].ToString();
            }
        }

        if (serviceList.Length != 1)
        {
            servicesAvailableLabel.text = $"{serviceList.Length} services discovered";
        }
        else
        {
            if (serviceList[0] == "")
            {
                servicesAvailableLabel.text = $"";
            }
            else
            {
                servicesAvailableLabel.text = $"{serviceList.Length} service discovered";
            }
        }
    }

    /// <summary>
    /// WDPluginActivity Callback Handler: Display incomming user or text message from other connected devices
    /// </summary>
    /// <param name="msg">message from other peer device</param>
    public void DisplayIncomingMsg(string msg)
    {
        incommingMessageBox.text = $"{incommingMessageBox.text}{msg}{Environment.NewLine}";
    }

    /// <summary>
    /// WDPluginActivity Callback Handler: Display incomming system information message
    /// </summary>
    /// <param name="msg">system message</param>
    public void DisplaySystemMsg(string msg)
    {
        systemMsgColor.a = 1;
        systemMsgNotificationBox.text = msg;
    }

    #endregion Handlers for WiFi Direct Plugin

    /// <summary>
    /// Tracks if device is currently connected over WiFi Direct to one or more other devices
    /// </summary>
    /// <remarks>Bound to UI</remarks>
    public bool IsConnected
    {
        get
        {
            return isConnected;
        }
        set
        {
            isConnected = value;
            ConnectedIndicator.SetActive(isConnected);
            sharedExperienceManager.ShareWithConnections = isConnected;
            Debug.Log($"{DebugTAG} Connection set to: {isConnected}");
            if (isConnected)
            {
                //remind user to make sure that their stage is placed and aligned to the agreed position in the room
                instructionPopupCanvasGroup.alpha = 1;
                instructionPopup.SetActive(true);
            }
        }
    }

}

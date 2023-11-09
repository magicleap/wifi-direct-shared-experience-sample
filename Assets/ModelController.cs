// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2023) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System.Linq;
using Unity.XR.CoreUtils;
using UnityEngine;

public class ModelController : MonoBehaviour
{
    private string DebugTAG = "SEWDS-MC";

    private Vector3 origPosition;
    private Vector3 origScale;
    private Vector3 stageOffset = new Vector3(0, 0.01F, 0);
    private Quaternion rotationOffset = Quaternion.Euler(0, 180, 0);
    private float averageUserHeight; //World origin is placed at headset on app start height, floor therefore will be about (Y = 0 - average user height)? Until floor detection logic is added.

    #region Inspector variables

    [Space(10.0f)]
    [SerializeField]
    [Tooltip("Gameobject used to place model on top of when in the Reset position.\nPosition of stage also serves as shared origin in shared experiences.")]
    private GameObject stage;

    [Header("Rotation")]
    [SerializeField]
    [Tooltip("Rotations per minute in the counter-clockwise direction")]
    private float rotateSpeed;

    [SerializeField]
    [Tooltip("When true the model will rotate")]
    private bool rotateModel;
    [Space(10.0f)]

    [SerializeField]
    [Tooltip("List of parts or sub elements of the model which can be toggled to show or hide")]
    private GameObject[] hideablePartsList;

    #endregion Inspector variables

    // Start is called before the first frame update
    void Start()
    {
        XROrigin xrOrigin = Camera.main.GetComponentInParent<XROrigin>();
        averageUserHeight = -xrOrigin.transform.localPosition.y;

        if (stage != null)
        {
            transform.position = stage.transform.position + stageOffset;
        }

        origPosition = transform.position;
        origScale = transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        if (rotateModel)
        {
            this.transform.Rotate(new Vector3(0, (-360 / 60) * rotateSpeed * Time.deltaTime, 0));
        }
    }


    /// <summary>
    /// Reposition model on stage in the original orientation and scale, pointing towards the front of the stage
    /// </summary>
    public void ResetPosition()
    {
        if (stage != null)
        {
            transform.position = stage.transform.position + stageOffset;
            transform.rotation = stage.transform.rotation * rotationOffset;
        }
        else
        {
            transform.position = origPosition;
        }

        transform.localScale = origScale;
    }

    /// <summary>
    /// Expand model to its full original scale and position it approximately on the floor 
    /// </summary>
    public void MakeFullSized()
    {
        transform.localScale = new Vector3(1, 1, 1);
        transform.position = new Vector3(transform.position.x, averageUserHeight, transform.position.z);
        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
    }

    /// <summary>
    /// Iterate parts list and sets all of their active (show/hide) states 
    /// </summary>
    /// <param name="show">Display when true</param>
    public void ToggleParts(bool show)
    {
        for (int i = 0; i < hideablePartsList.Count(); i++)
        {
            if (hideablePartsList[i] != null)
            {
                hideablePartsList[i].SetActive(show);
            }
        }
    }

    /// <summary>
    /// Set model's rotation state
    /// </summary>
    /// <param name="on">Rotate model when true</param>
    public void RotateModel(bool on)
    {
        rotateModel = on;
    }
}

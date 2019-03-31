////////////////////////////////////////////////////////////////////////
// ProText.cs
// Copyright (C) 2017 by Don Hopkins, Ground Up Software.


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace UnityJS {


public class ProText : BridgeObject {


    public enum TrackPosition {
        Hidden,
        Passive,
        Transform,
    };


    public enum TrackRotation {
        Passive,
        TransformRotation,
        TransformYaw,
        CameraRotation,
        CameraYaw,
    };


    ////////////////////////////////////////////////////////////////////////
    // ProText properties


    public TextMeshPro textMesh;
    public TrackPosition trackPosition = TrackPosition.Passive;
    public Transform transformPosition;
    public TrackRotation trackRotation = TrackRotation.Passive;
    public Transform transformRotation;
    public Vector3 extraOffset = Vector3.zero;
    public Quaternion extraRotation = Quaternion.identity;
    public float cameraDistanceMin = 0.22f;
    public Camera trackCamera;


    ////////////////////////////////////////////////////////////////////////
    // Instance Methods


    void Update()
    {
        bool active = false;
        bool updatePosition = false;

        switch (trackPosition) {

            case TrackPosition.Hidden:
                break;

            case TrackPosition.Passive:
                active = true;
                break;

            case TrackPosition.Transform:
                active = true;
                if (transformPosition != null) {
                    updatePosition = true;
                }
                break;

        }

        if (active != textMesh.enabled) {
            textMesh.enabled = active;
        }

        if (!active) {
            return;
        }

        if (updatePosition) {
            transform.position =
                transformPosition.position +
                extraOffset;
        }

        Camera cam;

        switch (trackRotation) {

            case TrackRotation.Passive:
                break;

            case TrackRotation.TransformRotation:
                if (transformRotation != null) {

                    transform.rotation =
                        extraRotation *
                        transformRotation.rotation;

                }
                break;

            case TrackRotation.TransformYaw:
                if (transformRotation != null) {

                    Vector3 forward =
                        transformRotation.rotation * Vector3.forward;

                    float currentDirection =
                        Mathf.Atan2(forward.x, forward.z) *
                        Mathf.Rad2Deg;

                    transform.rotation =
                        extraRotation *
                        Quaternion.Euler(0.0f, currentDirection, 0.0f);

                }
                break;

            case TrackRotation.CameraRotation:

                cam = GetTrackCamera();

                if (cam != null) {
                    transform.rotation =
                        extraRotation *
                        cam.transform.rotation;
                }

                break;

            case TrackRotation.CameraYaw:

                // TODO: This uses the camera position but should use the position of the center of the screen (CameraHolder).

                cam = GetTrackCamera();

                if (cam != null) {

                    float distance = (transform.position - cam.transform.position).magnitude;
                    float goalDX = transform.position.x - cam.transform.position.x;
                    float goalDZ = transform.position.z - cam.transform.position.z;

                    float goalDirection =
                        (distance <= cameraDistanceMin)
                            ? cam.transform.rotation.eulerAngles.y
                            : (Mathf.Atan2(goalDX, goalDZ) *
                               Mathf.Rad2Deg);

                    transform.rotation =
                        Quaternion.Euler(0.0f, goalDirection, 0.0f);

                }

                break;

        }

    }


    public Camera GetTrackCamera()
    {
        if (trackCamera != null) {
            return trackCamera;
        } else {
            return Camera.main;
        }
    }


}


}

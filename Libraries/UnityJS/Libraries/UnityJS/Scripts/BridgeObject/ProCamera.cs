////////////////////////////////////////////////////////////////////////
// ProCamera.cs
// Copyright (C) 2017 by Don Hopkins, Ground Up Software.


using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace UnityJS {


public enum ProCameraTracking {
    None,
    Drag,
    Orbit,
    Approach,
    Interpolate,
    Tilt,
    Pedestal,
    Zoom,
};


public class ProCamera : BridgeObject {


    ////////////////////////////////////////////////////////////////////////
    // ProCamera properties


    public Camera proCamera;
    public TrackerProxy trackerProxy;
    public float moveSpeed = 60.0f;
    public float yawSpeed = 60.0f;
    public float pitchSpeed = 60.0f;
    public float orbitYawSpeed = 60.0f;
    public float orbitPitchSpeed = 60.0f;
    public float wheelZoomSpeed = 30.0f;
    public float wheelPanSpeed = -30.0f;
    public Vector3 moveVelocity = Vector3.zero;
    public float yawVelocity = 0.0f;
    public float pitchVelocity = 0.0f;
    public float orbitYawVelocity = 0.0f;
    public float orbitPitchVelocity = 0.0f;
    public float wheelZoomVelocity = 0.0f;
    public float wheelPanVelocity = 0.0f;
    public float mouseScrollDeltaMax = 5.0f;
    public Vector3 positionMin = new Vector3(-1000.0f, 1.0f, -1000.0f);
    public Vector3 positionMax = new Vector3(1000.0f, 200.0f, 1000.0f);
    public float pitchMin = -90f;
    public float pitchMax = 90f;
    public bool initialized = false;
    public Vector3 forward;
    public Vector3 initialPosition;
    public Quaternion initialRotation;
    public Vector3 initialEulers;
    public Vector3 zoomDeltaRotated;
    public Vector3 orbitLocation;
    public bool dragging = false;
    public bool wasDragging = false;
    public ProCameraTracking tracking = ProCameraTracking.None;
    public Plane dragPlane = new Plane(Vector3.up, Vector3.zero);
    public Vector3 dragMousePosition;
    public Vector3 dragStartMousePosition;
    public Vector3 dragLastMousePosition;
    public Vector3 dragPlaneNormal = Vector3.up;
    public Vector3 dragPlanePosition = Vector3.zero;
    public float dragStartDistance;
    public Vector3 dragStartPosition;
    public Vector3 dragScreenDistance;
    public float dragDistance;
    public Vector3 dragPosition;
    public float fieldOfViewMax = 120.0f;
    public float fieldOfViewMin = 1.0f;
    public float fieldOfViewScale = -0.2f;
    public float pedestalScale = -0.5f;
    public float orbitScale = 1.0f;
    public Ray approachRay;
    public float approachDistance;
    public float approachScale = 1.0f;
    public float approachMin = 1.0f;
    public float approachMax = 10000.0f;
    public Vector3 orbitOffset;
    public float orbitYaw;
    public float orbitPitch;
    public float tiltMin = 0;
    public float tiltMax = 90;
    public float tiltPitchScale = 0.5f;
    public float tiltYawScale = -0.5f;
    public float tiltPitch;
    public float tiltYaw;
    public int interpolateRows;
    public int interpolateColumns;
    public Vector3[] interpolatePositions;
    public Quaternion[] interpolateRotations;


    ////////////////////////////////////////////////////////////////////////
    // Instance Methods


    void Update()
    {
        if (!initialized) {

            initialized = true;

            if (proCamera == null) {
                proCamera = gameObject.GetComponent<Camera>();
            }

            initialPosition = transform.position;
            initialRotation = transform.rotation;
            initialEulers = transform.rotation.eulerAngles;

        }

        if (dragging) {

            dragMousePosition = Input.mousePosition;

            if (!wasDragging) {

                // Start dragging.
                //Debug.Log("ProCamera: Update: StartDragging: tracking: " + tracking);

                dragStartMousePosition = dragMousePosition;
                dragLastMousePosition = dragMousePosition;

                dragPlane.SetNormalAndPosition(
                    dragPlaneNormal,
                    dragPlanePosition);

                Ray startRay =
                    proCamera.ScreenPointToRay(
                        dragMousePosition);
                dragStartDistance = 0.0f;
                if (!dragPlane.Raycast(startRay, out dragStartDistance)) {
                    Debug.Log("ProCamera: Update: startRay doesn't hit");
                }
                dragStartPosition = startRay.GetPoint(dragStartDistance);
                //Debug.Log("ProCamera: Update: dragStartDistance: " + dragStartDistance + " dragStartPosition: " + dragStartPosition.x + " " + dragStartPosition.y + " " + dragStartPosition.z);

                dragScreenDistance = Vector3.zero;
                dragDistance = 0.0f;
                dragPosition = Vector3.zero;

                switch (tracking) {

                    case ProCameraTracking.Drag:
                        break;

                    case ProCameraTracking.Orbit:

                        Vector3 orbitCameraDirection =
                            transform.rotation * Vector3.forward;

                        orbitYaw =
                            Mathf.Rad2Deg *
                            Mathf.Atan2(
                                orbitCameraDirection.x,
                                orbitCameraDirection.z);

                        Vector3 unrotatedOrbitCameraDirection =
                            Quaternion.Euler(0.0f, -orbitYaw, 0.0f) *
                            orbitCameraDirection;

                        orbitPitch =
                            Mathf.Rad2Deg *
                            Mathf.Atan2(
                                -unrotatedOrbitCameraDirection.y,
                                unrotatedOrbitCameraDirection.z);

                        Vector3 offset = 
                            dragStartPosition - transform.position;
                        orbitOffset =
                            Quaternion.Euler(0.0f, -orbitYaw, 0.0f) *
                            offset;

                        //Debug.Log("ProCamera: Update: Start Orbit: orbitCameraDirection: " + orbitCameraDirection.x + " " + orbitCameraDirection.y + " " + orbitCameraDirection.z + " orbitYaw: " + orbitYaw + " orbitPitch: " + orbitPitch + " dragMousePosition: " + dragMousePosition.x + " " + dragMousePosition.y + " " + dragMousePosition.z + " dragStartPosition: " + dragStartPosition.x + " " + dragStartPosition.y + " " + dragStartPosition.z + " transform.position: " + transform.position.x + " " + transform.position.y + " " + transform.position.z + " offset: " + offset.x + " " + offset.y + " " + offset.z + " orbitOffset: " + orbitOffset.x + " " + orbitOffset.y + " " + orbitOffset.z);

                        break;

                    case ProCameraTracking.Approach:
                        approachRay = new Ray(dragStartPosition, -startRay.direction);
                        approachDistance = dragStartDistance;
                        break;

                    case ProCameraTracking.Interpolate:
                        break;

                    case ProCameraTracking.Pedestal:
                        break;

                    case ProCameraTracking.Tilt:

                        Vector3 rotatedDirection =
                            transform.rotation * Vector3.forward;

                        tiltYaw =
                            Mathf.Rad2Deg *
                            Mathf.Atan2(
                                rotatedDirection.x,
                                rotatedDirection.z);

                        Vector3 unrotatedDirection =
                            Quaternion.Euler(0.0f, -tiltYaw, 0.0f) *
                            rotatedDirection;

                        tiltPitch =
                            Mathf.Rad2Deg *
                            Mathf.Atan2(
                                -unrotatedDirection.y,
                                unrotatedDirection.z);

                        //Debug.Log("ProCamera: Update: Start Tilt: rotatedDirection: " + rotatedDirection.x + " " + rotatedDirection.y + " " + rotatedDirection.z + " tiltYaw: " + tiltYaw + " unrotatedDirection: " + unrotatedDirection.x + " " + unrotatedDirection.y + " " + unrotatedDirection.z + " tiltPitch: " + tiltPitch);

                        break;

                    case ProCameraTracking.Zoom:
                        break;

                }

            } else {

                // Keep dragging.
                //Debug.Log("ProCamera: Update: KeepDragging");

                dragScreenDistance =
                    dragMousePosition - dragLastMousePosition;

                if ((dragScreenDistance.x != 0.0f) ||
                    (dragScreenDistance.y != 0.0f)) {

                    //Debug.Log("ProCamera: Update: dragScreenDistance: " + dragScreenDistance.x + " " + dragScreenDistance.y);

                    switch (tracking) {

                        case ProCameraTracking.Drag:

                            Ray lastDragRay =
                                proCamera.ScreenPointToRay(
                                    dragLastMousePosition);
                            float lastDragDistance = 0.0f;
                            if (!dragPlane.Raycast(lastDragRay, out lastDragDistance)) {
                                Debug.Log("ProCamera: Update: lastDragRay doesn't hit");
                            }
                            Vector3 lastDragPosition = lastDragRay.GetPoint(lastDragDistance);
                            //Debug.Log("ProCamera: Update: Drag: lastDragDistance: " + lastDragDistance + " lastDragPosition: " + lastDragPosition.x + " " + lastDragPosition.y + " " + lastDragPosition.z);

                            Ray dragRay =
                                proCamera.ScreenPointToRay(
                                    dragMousePosition);
                            dragDistance = 0.0f;
                            if (!dragPlane.Raycast(dragRay, out dragDistance)) {
                                Debug.Log("ProCamera: Update: dragRay doesn't hit");
                            }
                            dragPosition = dragRay.GetPoint(dragDistance);
                            //Debug.Log("ProCamera: Update: Drag: dragDistance: " + dragDistance + " dragPosition: " + dragPosition.x + " " + dragPosition.y + " " + dragPosition.z);

                            Vector3 offset = dragPosition - lastDragPosition;
                            //Debug.Log("ProCamera: Update: Drag: offset: " + offset.x + " " + offset.y + " " + offset.z);

                            transform.position -= offset;

                            break;

                        case ProCameraTracking.Orbit:

                            float turn =
                                dragScreenDistance.x * orbitScale;

                            orbitYaw += turn;

                            transform.rotation =
                                Quaternion.Euler(orbitPitch, orbitYaw, 0.0f);
                            
                            Vector3 rotatedOffset =
                                Quaternion.Euler(0.0f, orbitYaw, 0.0f) *
                                orbitOffset;

                            Vector3 orbitPosition =
                                dragStartPosition -
                                rotatedOffset;

                            //Debug.Log("ProCamera: Update: Orbit: turn: " + turn + " orbitPitch: " + orbitPitch + " orbitYaw: " + orbitYaw + " dragStartPosition: " + dragStartPosition.x + " " + dragStartPosition.y + " "  + dragStartPosition.z + " orbitOffset: " + orbitOffset.x + " " + orbitOffset.y + " " + orbitOffset.z + " rotatedOffset: " + rotatedOffset.x + " " + rotatedOffset.y + " " + rotatedOffset.z + " orbitPosition: " + orbitPosition.x + " " + orbitPosition.y + " " + orbitPosition.z);

                            transform.position =
                                orbitPosition;

                            break;

                        case ProCameraTracking.Approach:

                            float change =
                                approachScale *
                                (dragMousePosition.y - dragStartMousePosition.y);

                            approachDistance =
                                Mathf.Max(
                                    approachMin,
                                    Mathf.Min(
                                        approachMax,
                                        (approachDistance +
                                         change)));

                            Vector3 position =
                                approachRay.GetPoint(approachDistance);

                            //Debug.Log("ProCamera: Update: Approach: approachDistance: " + approachDistance + " position: " + position.x + " " + position.y + " " + position.z);

                            transform.position = position;

                            break;

                        case ProCameraTracking.Tilt:

                            float pitchChange =
                                tiltPitchScale *
                                (dragMousePosition.y - dragStartMousePosition.y);

                            float pitch =
                                Mathf.Max(
                                    tiltMin,
                                    Mathf.Min(
                                        tiltMax,
                                        (tiltPitch +
                                         pitchChange)));

                            float yawChange =
                                tiltYawScale *
                                (dragMousePosition.x - dragStartMousePosition.x);

                            float yaw =
                                tiltYaw + yawChange;

                            //Debug.Log("ProCamera: Update: Tilt: pitch: " + pitch + " pitchChange: " + pitchChange + " tiltYaw: " + tiltYaw + " yaw: " + yaw);

                            transform.rotation =
                                Quaternion.Euler(pitch, yaw, 0.0f);

                            break;

                        case ProCameraTracking.Interpolate:

                            //Debug.Log("ProCamera: Update: Interpolate: interpolateRows: " + interpolateRows + " interpolateColumns: " + interpolateColumns + " mouse: " + dragMousePosition.x + " " + dragMousePosition.y);

                            float x = (float)dragMousePosition.x / (float)Screen.width;
                            float xColumn = x * interpolateColumns;
                            int column0 = (int)Mathf.Floor(xColumn);
                            float columnFactor = xColumn - column0;
                            if (column0 < 0) column0 = 0;
                            if (column0 >= interpolateColumns) column0 = interpolateColumns - 1;
                            int column1 = column0 + 1;
                            if (column1 >= interpolateColumns) column1 = interpolateColumns - 1;

                            float y = (float)dragMousePosition.y / (float)Screen.height;
                            float yRow = y * interpolateRows;
                            int row0 = (int)Mathf.Floor(yRow);
                            float rowFactor = yRow - row0;
                            if (row0 < 0) row0 = 0;
                            if (row0 >= interpolateRows) row0 = interpolateRows - 1;
                            int row1 = row0 + 1;
                            if (row1 >= interpolateRows) row1 = interpolateRows - 1;

                            //Debug.Log("ProCamera: Update: Interpolate: x: " + x + " columns: " + column0 + " " + column1 + " factor: " + columnFactor);
                            //Debug.Log("ProCamera: Update: Interpolate: y: " + y + " rows: " + row0 + " " + row1 + " factor: " + rowFactor);

                            Vector3 position0 = 
                                Vector3.Lerp(
                                    interpolatePositions[column0 + (row0 * interpolateColumns)],
                                    interpolatePositions[column1 + (row0 * interpolateColumns)],
                                    columnFactor);
                            Vector3 position1 = 
                                Vector3.Lerp(
                                    interpolatePositions[column0 + (row1 * interpolateColumns)],
                                    interpolatePositions[column1 + (row1 * interpolateColumns)],
                                    columnFactor);
                            Vector3 pos =
                                Vector3.Lerp(
                                    position0,
                                    position1,
                                    rowFactor);

                            transform.position = pos;

                            Quaternion rotation0 = 
                                Quaternion.Slerp(
                                    interpolateRotations[column0 + (row0 * interpolateColumns)],
                                    interpolateRotations[column1 + (row0 * interpolateColumns)],
                                    columnFactor);
                            Quaternion rotation1 = 
                                Quaternion.Slerp(
                                    interpolateRotations[column0 + (row1 * interpolateColumns)],
                                    interpolateRotations[column1 + (row1 * interpolateColumns)],
                                    columnFactor);
                            Quaternion rot =
                                Quaternion.Slerp(
                                    rotation0,
                                    rotation1,
                                    rowFactor);

                            transform.rotation = rot;

                            break;

                        case ProCameraTracking.Pedestal:

                            float height = transform.position.y;
                            height =
                                Mathf.Max(
                                    positionMin.y,
                                    Mathf.Min(
                                        positionMax.y,
                                        (height +
                                         (dragScreenDistance.y * pedestalScale))));
                            transform.position =
                                new Vector3(
                                    transform.position.x,
                                    height,
                                    transform.position.z);

                            break;

                        case ProCameraTracking.Zoom:

                            float fov = proCamera.fieldOfView;
                            fov = 
                                Mathf.Max(
                                    fieldOfViewMin,
                                    Mathf.Min(
                                        fieldOfViewMax,
                                        (fov +
                                         (dragScreenDistance.y * fieldOfViewScale))));
                            proCamera.fieldOfView = fov;

                            break;

                    }

                    dragLastMousePosition = dragMousePosition;

                }

            }

        } else {

            if (wasDragging) {

                // Stop dragging.
                //Debug.Log("ProCamera: Update: StopDragging");

            } else {

                // Not dragging.

            }

        }

        wasDragging = dragging;

        //float deltaTime = Time.deltaTime;
        float deltaTime = Time.smoothDeltaTime; // Try smoothing!
        Vector3 moveDelta = moveVelocity * deltaTime;
        float yawDelta = yawVelocity * deltaTime;
        float pitchDelta = pitchVelocity * deltaTime;
        float orbitYawDelta = orbitYawVelocity * deltaTime;
        float orbitPitchDelta = orbitPitchVelocity * deltaTime;
        float wheelZoomDelta = wheelZoomVelocity * deltaTime;
        float wheelPanDelta = wheelPanVelocity * deltaTime;

        if (Input.GetKey("w")) {
            moveDelta.z += moveSpeed * deltaTime;
        }
        if (Input.GetKey("s")) {
            moveDelta.z -= moveSpeed * deltaTime;
        }
        if (Input.GetKey("a")) {
            moveDelta.x -= moveSpeed * deltaTime;
        }
        if (Input.GetKey("d")) {
            moveDelta.x += moveSpeed * deltaTime;
        }
        if (Input.GetKey("z")) {
            moveDelta.y -= moveSpeed * deltaTime;
        }
        if (Input.GetKey("x")) {
            moveDelta.y += moveSpeed * deltaTime;
        }
        if (Input.GetKey("q")) {
            yawDelta -= yawSpeed * deltaTime;
        }
        if (Input.GetKey("e")) {
            yawDelta += yawSpeed * deltaTime;
        }
        if (Input.GetKey("r")) {
            pitchDelta -= pitchSpeed * deltaTime;
        }
        if (Input.GetKey("f")) {
            pitchDelta += pitchSpeed * deltaTime;
        }
        if (Input.GetKey("i")) {
            orbitYawDelta += orbitYawSpeed * deltaTime;
        }
        if (Input.GetKey("m")) {
            orbitYawDelta -= orbitYawSpeed * deltaTime;
        }
        if (Input.GetKey("j")) {
            orbitPitchDelta += orbitPitchSpeed * deltaTime;
        }
        if (Input.GetKey("k")) {
            orbitPitchDelta -= orbitPitchSpeed * deltaTime;
        }

        float scrollX =
            Mathf.Clamp(Input.mouseScrollDelta.x, -mouseScrollDeltaMax, mouseScrollDeltaMax);
        float scrollY = 
            Mathf.Clamp(Input.mouseScrollDelta.y, -mouseScrollDeltaMax, mouseScrollDeltaMax);

#if false
        if (scrollX != 0.0f) {
            Debug.Log("scrollX: " + scrollX + " deltaTime: " + Time.deltaTime + " smoothDeltaTime: " + Time.smoothDeltaTime);
        }
        if (scrollY != 0.0f) {
            Debug.Log("scrollY: " + scrollY + " deltaTime: " + Time.deltaTime + " smoothDeltaTime: " + Time.smoothDeltaTime);
        }
#endif

        wheelZoomDelta += scrollY * wheelZoomSpeed * deltaTime;
        wheelPanDelta += scrollX * wheelPanSpeed * deltaTime;

        if ((yawDelta != 0.0f) || 
            (pitchDelta != 0.0f)) {

            Vector3 forward = 
                transform.rotation * Vector3.forward;

            float yaw =
                Mathf.Atan2(forward.x, forward.z) *
                Mathf.Rad2Deg;

            Quaternion q = Quaternion.identity;

            if (pitchDelta != 0.0f) {
                q *= Quaternion.AngleAxis(
                    pitchDelta, 
                    Quaternion.AngleAxis(yaw, Vector3.up) * Vector3.right);
            }

            if (yawDelta != 0.0f) {
                q *= Quaternion.AngleAxis(
                    yawDelta, 
                    Vector3.up);
            }

            transform.rotation = 
                q *
                transform.rotation;
        }

        if (moveDelta != Vector3.zero) {

            Vector3 forward = 
                transform.rotation * Vector3.forward;
            float yaw =
                Mathf.Atan2(forward.x, forward.z) *
                Mathf.Rad2Deg;
            Vector3 moveDeltaRotated =
                Quaternion.Euler(0.0f, yaw, 0.0f) *
                moveDelta;
            Vector3 pos =
                transform.position + moveDeltaRotated;

            pos = 
                new Vector3(
                    Mathf.Clamp(pos.x, positionMin.x, positionMax.x),
                    Mathf.Clamp(pos.y, positionMin.y, positionMax.y),
                    Mathf.Clamp(pos.z, positionMin.z, positionMax.z));

            transform.position = pos;
        }


        if (wheelZoomDelta != 0.0f) {
            zoomDeltaRotated =
                transform.rotation *
                (Vector3.forward * wheelZoomDelta * wheelZoomSpeed);

            Vector3 pos =
                transform.position + zoomDeltaRotated;

            pos = 
                new Vector3(
                    Mathf.Clamp(pos.x, positionMin.x, positionMax.x),
                    Mathf.Clamp(pos.y, positionMin.y, positionMax.y),
                    Mathf.Clamp(pos.z, positionMin.z, positionMax.z));

            transform.position = pos;

        }

    }

    
}


}

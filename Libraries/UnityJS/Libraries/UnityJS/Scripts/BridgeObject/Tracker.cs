////////////////////////////////////////////////////////////////////////
// Tracker.cs
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace UnityJS {


public class Tracker : BridgeObject {


    ////////////////////////////////////////////////////////////////////////
    // Class Variables


    static public Tracker grabber;


    ////////////////////////////////////////////////////////////////////////
    // Instance Variables


    public bool mouseTracking = true;
    public bool mouseEntered = false;
    public float mouseEnteredTime = 0.0f;
    public bool mouseEnteredChanged = false;
    public bool triggerTracking = false;
    public bool triggerEntered = false;
    public float triggerEnteredTime = 0.0f;
    public bool triggerEnteredChanged = false;
    public bool mouseDown = false;
    public float mouseDownTime = 0.0f;
    public bool mouseDownChanged = false;
    public bool ignoringMouseClick = false;
    public bool isPointerOverUIObject = false;
    public bool mouseTrackingPosition = true;
    public bool mouseTrackingPositionHover = false;
    public float mouseTrackingPositionHoverDelay = 0.1f;
    public float mouseTrackingPositionDriftDelay = 0.1f;
    public bool mouseTrackingPositionHovering = false;
    public float mouseTrackingPositionDriftTime = -1.0f;
    public Vector2 screenSize = Vector2.zero;
    public Vector3 mousePosition = Vector3.zero;
    public Vector3 mousePositionLast = Vector3.zero;
    public bool mousePositionChanged = false;
    public Vector2 screenPosition = Vector2.zero;
    public Vector3 mousePositionToCameraOffset;
    public bool mouseTrackingRaycast = true;
    public float mouseRayMaxDistance = Mathf.Infinity;
    public int mouseRayLayerMask = Physics.DefaultRaycastLayers;
    public QueryTriggerInteraction mouseRayQueryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    public Ray mouseRay;
    public bool mouseRaycastResult = false;
    public RaycastHit mouseRaycastHit;
    public Quaternion mouseRaycastHitPointFaceCameraRotation;
    public BridgeObject mouseRaycastHitBridgeObject;
    public string mouseRaycastHitBridgeObjectID;
    public BridgeObject mouseRaycastHitColliderBridgeObject;
    public string mouseRaycastHitColliderBridgeObjectID;
    public bool dragTracking = false;
    public bool dragging = false;
    public bool draggingSetsIsKinematic = true;
    public bool draggingLastIsKinematic = false;
    public Plane dragPlane = new Plane(Vector3.up, Vector3.zero);
    public float dragStartDistance;
    public Vector3 dragLastPosition;
    public Vector3 dragLastPlanePosition;
    public Vector3 dragPlanePosition;
    public Vector3 dragLastMousePosition;
    public Vector3 dragScreenDistance;
    public Vector3 dragPlaneDistance;
    public float rotateAmount;
    public bool collisionTracking = true;
    public bool collisionEntered = false;
    public float collisionEnteredTime = 0.0f;
    public bool collisionEnteredChanged = false;
    public Collision collision;
    public bool shiftKey = false;
    public bool controlKey = false;
    public bool altKey = false;


    ////////////////////////////////////////////////////////////////////////
    // Instance Methods


     public bool IsPointerOverUIObject()
     {
         if (!EventSystem.current) {
             return false;
         }

         PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
         eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
         List<RaycastResult> results = new List<RaycastResult>();
         EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
#if false
         foreach (RaycastResult result in results) {
             Debug.Log("Tracker: IsPointerOverUIObject: " + result);
         }
#endif
         return results.Count > 0;
     }


    public Vector3 NearestPointOnLine(Vector3 linePnt, Vector3 lineDir, Vector3 pnt)
    {
        //lineDir.Normalize(); // This needs to be a unit vector.
        var v = pnt - linePnt;
        var d = Vector3.Dot(v, lineDir);
        return linePnt + lineDir * d;
    }


    public virtual void TrackKeyModifiers()
    {
        shiftKey = 
            Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);

        controlKey = 
            Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl);

        altKey = 
            Input.GetKey(KeyCode.LeftAlt) ||
            Input.GetKey(KeyCode.RightAlt);
    }


    public virtual void TrackMousePosition()
    {
        mouseRaycastHitBridgeObject = null;
        mouseRaycastHitBridgeObjectID = null;
        mouseRaycastHitColliderBridgeObject = null;
        mouseRaycastHitColliderBridgeObjectID = null;

        TrackKeyModifiers();

        isPointerOverUIObject = IsPointerOverUIObject();

        if (!mouseTrackingPosition) {
            return;
        }

        mousePosition = Input.mousePosition;
        mousePositionChanged = mousePosition != mousePositionLast;

        screenSize = 
            new Vector2(
                Screen.width, 
                Screen.height);
        screenPosition = 
            new Vector2(
                mousePosition.x - (screenSize.x * 0.5f),
                mousePosition.y - (screenSize.y * 0.5f));

        if (mouseTrackingPositionHover) {

            if (mouseTrackingPositionHovering) {

                if (mousePositionChanged) {
                    mouseTrackingPositionHovering = false;
                    mouseTrackingPositionDriftTime = Time.time;
                    SendEventName("Drift");
                }

            } else {

                if (mousePositionChanged) {
                    mouseTrackingPositionDriftTime = Time.time;
                }
                if ((Time.time - mouseTrackingPositionDriftTime) >= mouseTrackingPositionDriftDelay) {
                    mouseTrackingPositionHovering = true;
                    SendEventName("Hover");
                }

            }
        }

        mousePositionLast = mousePosition;

        if (Camera.main == null) {
            return;
        }

        mouseRay =
            Camera.main.ScreenPointToRay(
                mousePosition);

        if (!mouseTrackingRaycast) {

            mouseRaycastResult = false;

        } else {

            mouseRaycastResult =
                Physics.Raycast(
                    mouseRay, 
                    out mouseRaycastHit, 
                    mouseRayMaxDistance, 
                    mouseRayLayerMask, 
                    mouseRayQueryTriggerInteraction);

            //Debug.Log("Tracker: TrackMousePosition: mouseRaycastResult: " + mouseRaycastResult + " mouseRaycastHitPoint: " + mouseRaycastHit.point.x + " " + mouseRaycastHit.point.y + " " + mouseRaycastHit.point.z);

            if (!mouseRaycastResult) {

            } else {

                Vector3 cameraPosition = Camera.main.transform.position;
                Vector3 offset = cameraPosition - mouseRaycastHit.point;
                offset.y = 0.0f;
                float direction = 
                    (offset == Vector3.zero)
                        ? 0.0f
                        : (180.0f + (Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg));
                mouseRaycastHitPointFaceCameraRotation =
                    Quaternion.Euler(0.0f, direction, 0.0f);

                mouseRaycastHitBridgeObject = null;
                Transform xform = mouseRaycastHit.transform;
                while (xform != null) {
                    mouseRaycastHitBridgeObject = xform.gameObject.GetComponent<BridgeObject>();
                    if (mouseRaycastHitBridgeObject != null) {
                        break;
                    }

                    xform = xform.parent;
                }

                mouseRaycastHitBridgeObjectID =
                    (mouseRaycastHitBridgeObject == null)
                        ? null
                        : mouseRaycastHitBridgeObject.id;

                mouseRaycastHitColliderBridgeObject = null;
                xform = mouseRaycastHit.collider.transform;
                while (xform != null) {
                    mouseRaycastHitColliderBridgeObject = xform.gameObject.GetComponent<BridgeObject>();
                    if (mouseRaycastHitColliderBridgeObject != null) {
                        break;
                    }

                    xform = xform.parent;
                }

                mouseRaycastHitColliderBridgeObjectID =
                    (mouseRaycastHitColliderBridgeObject == null)
                        ? null
                        : mouseRaycastHitColliderBridgeObject.id;

            }

            //Debug.Log("Tracker: TrackMousePosition: cameraPosition: " + cameraPosition.x + " " + cameraPosition.y + " " + cameraPosition.z + " point: " + mouseRaycastHit.point.x + " " + mouseRaycastHit.point.y + " " + mouseRaycastHit.point.z + " offset: " + offset.x + " " + offset.y + " " + offset.z + " direction: " + direction);

        }

        if (dragging) {

            float horizontalScale = -2.0f;
            float verticalScale = 0.5f;

            dragScreenDistance = 
                mousePosition - dragLastMousePosition;
            dragLastMousePosition = mousePosition;

            float enter = 0.0f;
            if (dragPlane.Raycast(mouseRay, out enter)) {
                dragPlanePosition = mouseRay.GetPoint(enter);
                dragPlaneDistance = dragPlanePosition - dragLastPlanePosition;
                dragLastPlanePosition = dragPlanePosition;

                if (Input.GetKey("left shift") ||
                    Input.GetKey("right shift")) {
                    rotateAmount = dragScreenDistance.x * horizontalScale;
                    dragPlaneDistance = new Vector3(0.0f, dragScreenDistance.y * verticalScale, 0.0f);
                } else {
                    rotateAmount = 0.0f;
                }


            } else {
                dragPlaneDistance = Vector3.zero;
                rotateAmount = 0.0f;
            }

            if ((dragPlaneDistance != Vector3.zero) || 
                (rotateAmount != 0.0f)) {

                Vector3 newPosition = 
                    gameObject.transform.position + dragPlaneDistance;

                Quaternion newRotation =
                    Quaternion.Euler(0.0f, rotateAmount, 0.0f) *
                    gameObject.transform.rotation;

                Rigidbody rb =
                    gameObject.GetComponent<Rigidbody>();
                if (rb != null) {

                    rb.MovePosition(newPosition);

                    if (rotateAmount != 0.0f) {
                        rb.MoveRotation(newRotation);
                    }

                } else {

                    transform.position = newPosition;

                    if (rotateAmount != 0.0f) {
                        transform.rotation = newRotation;
                    }

                }

                SendEventName("DragMove");

            }

        }

        HandleMouseMove();
    }


    public virtual void SetMouseEntered(bool mouseEntered0)
    {
        //Debug.Log("Tracker: SetMouseEntered: mouseEntered0: " + mouseEntered0, this);
        if (mouseEntered != mouseEntered0) {
            mouseEnteredChanged = true;
            mouseEntered = mouseEntered0;
        }
    }


    public virtual void OnMouseEnter()
    {
        if (((grabber != null) && (grabber != this)) ||
            !mouseTracking) {
            return;
        }

        //Debug.Log("Tracker: OnMouseEnter", this);

        TrackMousePosition();

        SetMouseEntered(true);

        mouseEnteredTime = Time.time;

        HandleMouseEnter();
    }


    public virtual void HandleMouseEnter()
    {
        //Debug.Log("Tracker: HandleMouseEnter", this);
        SendEventName("MouseEnter");
    }
    

    public virtual void OnMouseExit()
    {
        if (((grabber != null) && (grabber != this)) ||
            !mouseTracking) {
            return;
        }

        //Debug.Log("Tracker: OnMouseExit", this);

        TrackMousePosition();

        SetMouseEntered(false);

        HandleMouseExit();
    }


    public virtual void HandleMouseExit()
    {
        //Debug.Log("Tracker: HandleMouseExit", this);
        SendEventName("MouseExit");
    }
    

    public virtual void SetTriggerEntered(bool triggerEntered0)
    {
        //Debug.Log("Tracker: SetTriggerEntered: triggerEntered0: " + triggerEntered0, this);
        if (triggerEntered != triggerEntered0) {
            triggerEnteredChanged = true;
            triggerEntered = triggerEntered0;
        }
    }


    public virtual void OnTriggerEnter()
    {
        if (((grabber != null) && (grabber != this)) ||
            !triggerTracking) {
            return;
        }

        //Debug.Log("Tracker: OnTriggerEnter", this);

        TrackMousePosition();

        SetTriggerEntered(true);

        triggerEnteredTime = Time.time;

        HandleTriggerEnter();
    }


    public virtual void HandleTriggerEnter()
    {
        //Debug.Log("Tracker: HandleTriggerEnter", this);
        SendEventName("TriggerEnter");
    }
    

    public virtual void OnTriggerExit()
    {
        if (((grabber != null) && (grabber != this)) ||
            !triggerTracking) {
            return;
        }

        //Debug.Log("Tracker: OnTriggerExit", this);

        TrackMousePosition();

        SetTriggerEntered(false);

        HandleTriggerExit();
    }


    public virtual void HandleTriggerExit()
    {
        //Debug.Log("Tracker: HandleTriggerExit", this);
        SendEventName("TriggerExit");
    }
    

    public virtual void SetMouseDown(bool mouseDown0)
    {
        //Debug.Log("Tracker: SetMouseDown: mouseDown0: " + mouseDown0, this);
        if (mouseDown != mouseDown0) {
            mouseDownChanged = true;
            mouseDown = mouseDown0;
        }

        if (dragTracking) {
            if (mouseDown) {

                dragging = true;

                if (draggingSetsIsKinematic) {
                    Rigidbody rb = gameObject.GetComponent<Rigidbody>();
                    if (rb != null) {
                        draggingLastIsKinematic = rb.isKinematic;
                        rb.isKinematic = true;
                    }
                }

                dragPlane.SetNormalAndPosition(
                    Vector3.up,
                    transform.position);

                if (!dragPlane.Raycast(mouseRay, out dragStartDistance)) {
                    dragging = false;
                    return;
                }

                dragLastPlanePosition = mouseRay.GetPoint(dragStartDistance);
                dragLastPosition = transform.position;
                dragLastMousePosition = mousePosition;

                HandleDragStart();

            } else {

                dragging = false;

                if (draggingSetsIsKinematic) {
                    Rigidbody rb = gameObject.GetComponent<Rigidbody>();
                    if (rb != null) {
                        rb.isKinematic = draggingLastIsKinematic;
                    }
                }

                HandleDragStop();

            }
        }
    }


    public virtual void OnMouseDown()
    {
        if (((grabber != null) && (grabber != this)) ||
            !mouseTracking) {
            return;
        }

        //Debug.Log("Tracker: OnMouseDown", this);

        TrackMousePosition();

        SetMouseDown(true);

        HandleMouseDown();
    }


    public virtual void HandleMouseDown()
    {
        //Debug.Log("Tracker: HandleMouseDown", this);
        SendEventName("MouseDown");
    }


    public virtual void OnMouseUp()
    {
        if (((grabber != null) && (grabber != this)) ||
            !mouseTracking) {
            return;
        }

        //Debug.Log("Tracker: OnMouseUp", this);

        TrackMousePosition();

        SetMouseDown(false);

        HandleMouseUp();
    }


    public virtual void HandleMouseUp()
    {
        //Debug.Log("Tracker: HandleMouseUp", this);
        SendEventName("MouseUp");
    }


    public virtual void OnMouseUpAsButton()
    {
        if (((grabber != null) && (grabber != this)) ||
            !mouseTracking) {
            return;
        }

        //Debug.Log("Tracker: OnMouseUpAsButton", this);

        TrackMousePosition();

        SetMouseDown(false);

        HandleMouseUpAsButton();
    }


    public virtual void HandleMouseUpAsButton()
    {
        //Debug.Log("Tracker: HandleMouseUpAsButton", this);
        SendEventName("MouseUpAsButton");
    }


    public virtual void HandleMouseMove()
    {
        //Debug.Log("Tracker: HandleMouseMove", this);
        SendEventName("MouseMove");

    }


    public virtual void HandleDragStart()
    {
        //Debug.Log("Tracker: HandleDragStart", this);
        SendEventName("DragStart");

    }


    public virtual void HandleDragStop()
    {
        //Debug.Log("Tracker: HandleDragStop", this);
        SendEventName("DragStop");

    }


    public virtual void OnMouseDrag()
    {
        if (((grabber != null) && (grabber != this)) ||
            !mouseTracking) {
            return;
        }

        //Debug.Log("Tracker: OnMouseDrag", this);

        TrackMousePosition();

        HandleMouseDrag();
    }


    public virtual void HandleMouseDrag()
    {
        //Debug.Log("Tracker: HandleMouseDrag", this);
        SendEventName("MouseDrag");
    }


    public virtual void OnMouseOver()
    {
        if ((grabber != null) && (grabber != this)) {
            return;
        }
            
        //Debug.Log("Tracker: OnMouseOver", this);

        TrackMousePosition();

        HandleMouseOver();
    }


    public virtual void HandleMouseOver()
    {
        //Debug.Log("Tracker: HandleMouseOver", this);
        SendEventName("MouseOver");
    }


    public virtual void SetCollisionEntered(bool collisionEntered0)
    {
        //Debug.Log("Tracker: SetCollisionEntered: collisionEntered0: " + collisionEntered0, this);
        if (collisionEntered != collisionEntered0) {
            collisionEnteredChanged = true;
            collisionEntered = collisionEntered0;
        }
    }


    public virtual void OnCollisionEnter(Collision other)
    {
        if (!collisionTracking) {
            return;
        }

        collision = other;

        //Debug.Log("Tracker: OnCollisionEnter", this);

        SetCollisionEntered(true);

        collisionEnteredTime = Time.time;

        HandleCollisionEnter();
    }


    public virtual void HandleCollisionEnter()
    {
        //Debug.Log("Tracker: HandleCollisionEnter", this);
        SendEventName("CollisionEnter");
    }
    

    public virtual void OnCollisionExit(Collision other)
    {
        if (!collisionTracking) {
            return;
        }

        collision = other;

        //Debug.Log("Tracker: OnCollisionExit", this);

        SetCollisionEntered(false);

        HandleCollisionExit();
    }


    public virtual void HandleCollisionExit()
    {
        //Debug.Log("Tracker: HandleCollisionExit", this);
        SendEventName("CollisionExit");
    }
    

}


}

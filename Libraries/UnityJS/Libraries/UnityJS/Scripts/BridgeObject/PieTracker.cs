////////////////////////////////////////////////////////////////////////
// PieTracker.cs
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;


namespace UnityJS {


public class PieTracker : Tracker {


    public bool tracking = true;

    public bool trackingMouseButton = false;
    public bool mouseButtonChanged = true;
    public bool mouseButton = false;
    public bool mouseButtonLast = false;
    public bool trackingMousePosition = false;
    public Vector3 mousePositionStart = Vector3.zero;
    public Vector3 mousePositionDelta = Vector3.zero;
    public float distance = 0.0f;
    public float direction = 0.0f;
    public int sliceIndex = -1;
    public int itemIndex = -1;
    public int slices = 8;
    public float initialDirection = 0.5f * Mathf.PI;
    public float subtend = 0.0f;
    public bool clockwise = true;
    public float inactiveDistance = 30.0f;
    public float itemDistance = 30.0f;
    public bool pinned = false;
    public bool pinToCursor = true;
    public JObject _pie;
    public bool pieChanged = true;


    public JObject pie {
        get {
            return _pie;
        }
        set {
            _pie = value;
            pieChanged = true;
            //Debug.Log("PieTracker: set pie: " + pie);
        }
    }


    public void Awake()
    {
        Refresh();
    }


    public void Refresh()
    {
        mousePositionChanged = true;
        pieChanged = true;
    }


    void Update()
    {
        if (!tracking) {
            return;
        }

        UpdatePie();

        TrackMousePosition();

        if (ignoringMouseClick) {
            if (Input.GetMouseButtonUp(0)) {
                //Debug.Log("PieTracker: Update: ignoringMouseClick: Up: isPointerOverUIObject: " + isPointerOverUIObject);
                SendEventName("MouseButtonUpUI");
                ignoringMouseClick = false;
            }
        } else {
            TrackMouseButton();
        }
    }


    public void UpdatePie()
    {
        if (!pieChanged || (_pie == null)) {
            return;
        }

        pieChanged = false;

        LayoutPie();
    }
    

    public void LayoutPie()
    {
        //Debug.Log("LayoutPie: _pie: " + _pie);

        if (_pie == null) {
            //Debug.Log("LayoutPie: null _pie");
            slices = 0;
            return;
        }

        JArray pieSlices = _pie.GetArray("slices");
        if (pieSlices == null) {
            //Debug.Log("LayoutPie: no slices");
            slices = 0;
            return;
        }

        slices = pieSlices.ArrayLength();
        //Debug.Log("LayoutPie: slices: " + slices);

        initialDirection = _pie.GetFloat("initialDirection", 0.5f * Mathf.PI);
        subtend = _pie.GetFloat("subtend", 0.0f);
        clockwise = _pie.GetBoolean("clockwise", true);
        inactiveDistance = _pie.GetFloat("inactiveDistance", 10.0f);
        itemDistance = _pie.GetFloat("itemDistance", 10.0f);

        float sliceSizeTotal = 0.0f;
        foreach (JObject slice in pieSlices) {
            float sliceSize = slice.GetFloat("sliceSize", 1.0f);
            sliceSizeTotal += sliceSize;
        }

        //Debug.Log("slices: " + slices + " sliceSizeTotal: " + sliceSizeTotal);

        float pieSubtend = 
            (subtend == 0.0f) 
                ? (2.0f * Mathf.PI)
                : subtend;
        float sliceSizeScale =
            (sliceSizeTotal == 0)
                ? 1.0f
                : (pieSubtend / sliceSizeTotal);
        float clockSign = clockwise ? -1 : 1;
        float sliceDirection = initialDirection;
        bool firstSlice = true;
        //Debug.Log("pieSubtend: " + pieSubtend + " sliceSizeScale: " + sliceSizeScale + " clockSign: " + clockSign + " sliceDirection: " + sliceDirection);

        foreach (JObject slice in pieSlices) {
            float sliceSize = slice.GetFloat("sliceSize", 1.0f);
            float sliceSubtend = sliceSize * sliceSizeScale;
            float halfTurn = 0.5f * clockSign * sliceSubtend;
            //Debug.Log("start sliceDirection: " + sliceDirection + " sliceSize: " + sliceSize + " sliceSubtend: " + sliceSubtend + " halfTurn: " + halfTurn);

            if (firstSlice) {
                firstSlice = false;
                // If the subtend was zero, use the whole pie, but start the first slice centered no the initial direction.
                if (subtend == 0.0f) {
                    sliceDirection -= halfTurn;
                    //Debug.Log("firstSlice and zero subtend, turning back halfTurn: " + halfTurn + " to sliceDirection: " + sliceDirection);
                }
            }

            sliceDirection += halfTurn;
            //Debug.Log("center sliceDirection: " + sliceDirection);

            float dx = Mathf.Cos(sliceDirection);
            float dy = Mathf.Sin(sliceDirection);
            //Debug.Log("slice: " + slice + " sliceSize: " + sliceSize + " sliceDirection: " + sliceDirection + " sliceSubtend: " + sliceSubtend + " dx: " + dx + " dy: " + dy);

            slice["sliceDirection"] = sliceDirection;
            slice["sliceSubtend"] = sliceSubtend;
            slice["dx"] = dx;
            slice["dy"] = dy;

            sliceDirection += halfTurn;
            //Debug.Log("end sliceDirection: " + sliceDirection);
        }
    }


    public bool AngleBetween(float n, float a, float b)
    {
        return true;
/*
        const float twoPI = 2.0f * Mathf.PI;
        const float manyPI = twoPI * 1000.0f;
    	n = (twoPI + (n % twoPI)) % twoPI;
        a = (manyPI + a) % twoPI;
        b = (manyPI + b) % twoPI;

        if (a < b) {
            bool between1 = (a <= n) && (n <= b);
            //Debug.Log("AngleBetween n: " + n + " a: " + a + " b: " + b + " between1: " + between1);
            return between1;
        }

        bool between2 = (a <= n) || (n <= b);
        //Debug.Log("AngleBetween n: " + n + " a: " + a + " b: " + b + " between2: " + between2);
        return between2;
*/
    }
    

    public override void TrackMousePosition()
    {
        base.TrackMousePosition();

        if (!trackingMousePosition) {
            return;
        }

        mousePositionChanged |= (mousePosition != mousePositionLast);

        if (!mousePositionChanged) {
            return;
        }

        if (pinned &&
            pinToCursor &&
            !mouseDown) {
            mousePositionStart = mousePosition;
        }

        mousePositionDelta = mousePosition - mousePositionStart;

        distance = mousePositionDelta.magnitude;

        direction =
            (distance == 0)
                ? 0.0f
                : NormalRad(
                      Mathf.Atan2(
                          mousePositionDelta.y,
                          mousePositionDelta.x));

        bool inactive = 
            (distance < inactiveDistance) ||
            (slices == 0);

        //Debug.Log("TrackMousePosition: mouse dx: " + mousePositionDelta.x + " dy: " + mousePositionDelta.y + " direction: " + direction + " distance: " + distance + " inactive: " + inactive + " clockwise: " + clockwise + " subtend: " + subtend + " initialDirection: " + initialDirection);

        if (!inactive && 
            (subtend != 0.0f) &&
            !AngleBetween(
                direction, 
                clockwise 
                    ? (initialDirection + subtend)
                    : initialDirection,
                clockwise 
                    ? initialDirection
                    : (initialDirection + subtend))) {
            inactive = true;
        }

        if (inactive) {

            sliceIndex = -1;
            itemIndex = -1;

        } else {

            sliceIndex = -1;

            JArray pieSlices = _pie.GetArray("slices");
            if (pieSlices != null) {

                float mouseDX = Mathf.Cos(direction);
                float mouseDY = Mathf.Sin(direction);

                //Debug.Log("pieSlices direction: " + direction + " mouseDX: " + mouseDX + " mouseDY: " + mouseDY);

                float bestDot = -1.0e+6f;
                JObject bestSlice = null;
                int i = 0;
                foreach (JObject slice in pieSlices) {
                    float dx = slice.GetFloat("dx");
                    float dy = slice.GetFloat("dy");
                    float sliceDirection = slice.GetFloat("sliceDirection");
                    float dot = 
                        ((mouseDX * dx) + 
                         (mouseDY * dy));
                    //Debug.Log("i: " + i + " sliceDirection: " + sliceDirection + " dx: " + dx + " dy: " + dy + " dot: " + dot + " best: " + (dot > bestDot) + " slice: " + slice);
                    if (dot > bestDot) {
                        bestDot = dot;
                        bestSlice = slice;
                        sliceIndex = i;
                    }
                    i++;
                }

                if (bestSlice != null) {
                    JArray items = bestSlice.GetArray("items");
                    if ((items != null) && 
                        (items.Count > 0)) {
                        
                        itemIndex =
                            (int)Mathf.Min(
                                items.Count - 1,
                                Mathf.Floor(
                                    (distance - inactiveDistance) /
                                    itemDistance));

                    }
                }

                //Debug.Log("finally sliceIndex: " + sliceIndex + " i: " + i);
            }

        }


        SendEventName("MousePositionChanged");

        mousePositionChanged = false;
    }


    public float NormalRad(float rad)
    {
        const float twoPI = 2.0f * Mathf.PI;

        while (rad < 0.0f) {
            rad += twoPI;
        }

        while (rad >= twoPI) {
            rad -= twoPI;
        }

        return rad;
    }


    public void TrackMouseButton()
    {
        if (!trackingMouseButton) {
            return;
        }

        if (Input.GetMouseButtonDown(0)) {

            isPointerOverUIObject = IsPointerOverUIObject();

            //Debug.Log("PieTracker: TrackMouseButton: Down: isPointerOverUIObject: "+ isPointerOverUIObject);

            if (isPointerOverUIObject) {
                SendEventName("MouseButtonDownUI");
                ignoringMouseClick = true;
                return;
            }

            SendEventName("MouseButtonDown");
        }

        if (Input.GetMouseButtonUp(0)) {
            SendEventName("MouseButtonUp");
        }

        mouseButton = Input.GetMouseButton(0);
        mouseButtonChanged |= mouseButton != mouseButtonLast;
        mouseButtonLast = mouseButton;

        if (mouseButtonChanged) {
            SendEventName("MouseButtonChanged");
            mouseButtonChanged = false;
        }

    }


}


}

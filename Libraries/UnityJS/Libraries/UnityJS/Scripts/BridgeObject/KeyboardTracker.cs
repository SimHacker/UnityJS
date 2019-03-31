////////////////////////////////////////////////////////////////////////
// KeyboardTracker.cs
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;


namespace UnityJS {


public class KeyboardTracker : Tracker {


    public bool tracking = true;
    public bool inputStringTracking = true;
    public string inputString = "";
    public bool keyEventTracking = true;
    public Event keyEvent;


    void Update()
    {
        TrackKeyboard();
    }


    public void TrackKeyboard()
    {
        if (tracking && inputStringTracking) {
            inputString = Input.inputString;
            if (inputString != "") {
                SendEventName("InputString");
            }
        }
    }


    void OnGUI()
    {
        if (tracking && keyEventTracking) {
            keyEvent = Event.current;
            if (keyEvent.isKey) {
                //Debug.Log("KeyboardTracker: OnGUI: Detected keyCode: " + keyEvent.keyCode + " keyEvent: " + keyEvent);
                SendEventName("KeyEvent");
            }
        }
    }


}


}

////////////////////////////////////////////////////////////////////////
// LeanTweenBridge.cs
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// move moveX moveY moveZ (object, destination, duration)
//   setEase
//   setDelay
//   setOnComplete
// pause(object)
// resume(object)
// cancel(object)
// cancelAll

// Use curves defined in LeanTween editor.

// Don't use LTRect or LTEvent stuff, that's for the old GUI.

/*

LeanTween

Applies to GameObject, RectTransform

init

addListener
removeListener
delayedCall
dispatchEvent

descr
descriptions
isPaused
isTweening
tweensRunning

play
cancel
cancelAll
pause
pauseAll
resume
resumeAll

value
  Color, float, Vector3, Vector3

sequence

alpha
color
colorText

move
moveLocal
moveSpline
moveSplineLocal
moveX
moveY
moveZ
rotate
rotateLocal
rotateX
rotateY
rotateZ
rotateAround
rotateAroundLocal
scale
scaleX
scaleY
scaleZ
size

LTDescr

  pause
  resume
  setAxis
  setDelay
  setDirection
  setEase
  setFrom
  setIgnoreTimeScale
  setLoopClamp
  setLoopOnce
  setLoopPingPong
  setOnComplete
  setOnCompleteOnRepeat
  setOnCompleteOnStart
  setOnCompleteParam
  setOnStart
  setOnUpdate
  setOnUpdateParam
  setOrientToPath
  setOrientToPath2d
  setOvershoot
  setPassed
  setPeriod
  setPoint
  setRecursive
  setRepeat
  setScale
  setSpeed
  setTime
  setTo
  setUseFrames
  updateNow

*/

namespace UnityJS {


public class LeanTweenBridge : BridgeObject {


    ////////////////////////////////////////////////////////////////////////
    // Instance Variables


    public int maxTweens = 2500;


    ////////////////////////////////////////////////////////////////////////
    // Static Variables


    public static LeanTweenBridge leanTweenBridge;


    ////////////////////////////////////////////////////////////////////////
    // Static Methods


    public static void AnimateData(BridgeObject bridgeObject, JArray dataArray)
    {
        GameObject go = bridgeObject.gameObject;

        if (leanTweenBridge == null) {
            Debug.LogError("LeanTweenBridge: AnimateData: no leanTweenBridge has been created.");
            return;
        }

        foreach (JObject data in dataArray) {

            LTDescr result = null;
            GameObject target = go;

            float time = 1.0f;
            if (data.ContainsKey("time")) {
                time = data.GetFloat("time");
                if (time <= 0.0f) {
                    Debug.LogError("LeanTweenBridge: AnimateData: time must be > 0. data: " + data);
                    return;
                }
            }

            if (data.ContainsKey("target")) {
                string targetPath = data.GetString("target");

                if (targetPath == "") {
                    Debug.LogError("LeanTweenBridge: AnimateData: empty targetPath");
                    return;
                }

                Accessor accessor = null;
                if (!Accessor.FindAccessor(
                        bridgeObject,
                        targetPath,
                        ref accessor)) {

                    Debug.LogError("LeanTweenBridge: AnimateData: target: can't find accessor for targetPath: " + targetPath);
                    return;

                }

                object obj = null;
                if (!accessor.Get(ref obj)) {

                    if (!accessor.conditional) {
                        Debug.LogError("LeanTweenBridge: AnimateData: target: can't get accessor: " + accessor + " targetPath: " + targetPath);
                        return;
                    }

                }

                GameObject goTarget = obj as GameObject;
                if (goTarget == null) {
                    Transform xform = obj as Transform;
                    if (xform != null) {
                        goTarget = xform.gameObject;
                    } else {
                        Component component = obj as Component;
                        if (component != null) {
                            goTarget = component.gameObject;
                        }
                    }
                }

                if (goTarget == null) {
                    Debug.LogError("LeanTweenBridge: AnimateData: null targetPath: " + targetPath + " obj: " + obj);
                    return;
                }

                //Debug.Log("LeanTweenBridge: AnimateData: targetPath: " + targetPath + " goTarget: " + goTarget);
                target = goTarget;
            }

            var command = (string)data.GetString("command");
            if (string.IsNullOrEmpty(command)) {
                Debug.LogError("LeanTweenBridge: AnimateData: missing command from data: " + data);
                return;
            }

            switch (command) {

                case "init":
                    Debug.Log("LeanTweenBridge: AnimateData: TODO: command: " + command + " data: " + data);
                    break;

                case "play":
                    Debug.Log("LeanTweenBridge: AnimateData: TODO: command: " + command + " data: " + data);
                    break;

                case "cancel":
                    Debug.Log("LeanTweenBridge: AnimateData: TODO: command: " + command + " data: " + data);
                    break;

                case "cancelAll":
                    Debug.Log("LeanTweenBridge: AnimateData: TODO: command: " + command + " data: " + data);
                    break;

                case "pause":
                    Debug.Log("LeanTweenBridge: AnimateData: TODO: command: " + command + " data: " + data);
                    break;

                case "pauseAll":
                    Debug.Log("LeanTweenBridge: AnimateData: TODO: command: " + command + " data: " + data);
                    break;

                case "resume":
                    Debug.Log("LeanTweenBridge: AnimateData: TODO: command: " + command + " data: " + data);
                    break;

                case "resumeAll":
                    Debug.Log("LeanTweenBridge: AnimateData: TODO: command: " + command + " data: " + data);
                    break;

                case "sequence":
                    Debug.Log("LeanTweenBridge: AnimateData: TODO: command: " + command + " data: " + data);
                    break;

                case "value":
                    Debug.Log("LeanTweenBridge: AnimateData: TODO: command: " + command + " data: " + data);
                    break;

                case "move":
                case "moveLocal":
                case "moveSpline":
                case "moveSplineLocal":

                    //Debug.Log("LeanTweenBridge: AnimateData: command: " + command + " data: " + data);

                    // TODO: handle RectTransform instead of GameObject

                    if (data.ContainsKey("position")) {

                        Vector3 position = Vector3.zero;
                        if (!Bridge.bridge.ConvertToType<Vector3>(data["position"], ref position)) {
                            Debug.LogError("LeanTweenBridge: AnimateData: position must be a Vector3. data: " + data);
                            return;
                        }

                        switch (command) {

                            case "move":
                                result = LeanTween.move(target, position, time);
                                break;

                            case "moveLocal":
                                result = LeanTween.moveLocal(target, position, time);
                                break;

                            case "moveSpline":
                            case "moveSplineLocal":
                                Debug.LogError("LeanTweenBridge: AnimateData: position not supported for " + command + ". data: " + data);
                                return;

                        }

                    } else if (data.ContainsKey("transform")) {

                        Transform xform = null;

                        string path = data.GetString("transform");

                        Accessor accessor = null;
                        if (!Accessor.FindAccessor(
                                bridgeObject,
                                path,
                                ref accessor)) {
                            Debug.LogError("LeanTweenBridge: AnimateData: transform must be a path. path: " + path + " data: " + data);
                            return;
                        }
                        object val = null;
                        if (!accessor.Get(ref val) ||
                            (val == null)) {
                            Debug.LogError("LeanTweenBridge: AnimateData: error getting path to transform. path: " + path + " data: " + data);
                            return;
                        }

                        xform = val as Transform;

                        if (xform == null) {
                            Debug.LogError("LeanTweenBridge: AnimateData: path does not point to transform. path: " + path + " data: " + data);
                            return;
                        }

                        switch (command) {

                            case "move":
                                result = LeanTween.move(target, xform, time);
                                break;

                            case "moveLocal":
                            case "moveSpline":
                            case "moveSplineLocal":
                                Debug.LogError("LeanTweenBridge: AnimateData: transform not supported for " + command + ". data: " + data);
                                return;

                        }

                    } else if (data.ContainsKey("path")) {

                        Vector3[] path = null;
                        if (!Bridge.bridge.ConvertToType<Vector3[]>(data["path"], ref path)) {
                            Debug.LogError("LeanTweenBridge: AnimateData: path must be an Vector3[]. data: " + data);
                            return;
                        }

                        switch (command) {

                            case "move":
                                result = LeanTween.move(target, path, time);
                                break;

                            case "moveLocal":
                                result = LeanTween.moveLocal(target, path, time);
                                break;

                            case "moveSpline":
                                result = LeanTween.moveSpline(target, path, time);
                                break;

                            case "moveSplineLocal":
                                result = LeanTween.moveSplineLocal(target, path, time);
                                break;

                        }

                    } else if (data.ContainsKey("spline")) {

                        LTSpline spline = null;
                        if (!Bridge.bridge.ConvertToType<LTSpline>(data["spline"], ref spline)) {
                            Debug.LogError("LeanTweenBridge: AnimateData: spline must be a LTSpline. data: " + data);
                            return;
                        }

                        switch (command) {

                            case "moveSpline":
                                result = LeanTween.moveSpline(target, spline, time);
                                break;

                            case "move":
                            case "moveLocal":
                            case "moveSplineLocal":
                                Debug.LogError("LeanTweenBridge: AnimateData: spline not supported for " + command + ". data: " + data);
                                break;

                        }

                    } else {
                        Debug.LogError("LeanTweenBridge: AnimateData: command: " + command + " should contain position, transform, path or spline. data: " + data);
                        return;
                    }

                    break;

                case "moveX":
                case "moveY":
                case "moveZ":

                    // TODO: handle RectTransform instead of GameObject

                    if (!data.ContainsKey("to")) {
                        Debug.LogError("LeanTweenBridge: AnimateData: missing to parameter. data: " + data);
                        return;
                    }

                    float moveTo = data.GetFloat("to");

                    switch (command) {

                        case "moveX":
                            result = LeanTween.moveX(target, moveTo, time);
                            break;

                        case "moveY":
                            result = LeanTween.moveY(target, moveTo, time);
                            break;

                        case "moveZ":
                            result = LeanTween.moveZ(target, moveTo, time);
                            break;

                    }

                    break;

                case "rotate":
                case "rotateLocal":

                    //Debug.Log("LeanTweenBridge: AnimateData: TODO: command: " + command + " data: " + data);

                    // TODO: handle RectTransform instead of GameObject

                    if (!data.ContainsKey("to")) {
                        Debug.LogError("LeanTweenBridge: AnimateData: missing to. data: " + data);
                        return;
                    }

                    Vector3 rotateTo = Vector3.zero;
                    if (!Bridge.bridge.ConvertToType<Vector3>(data["to"], ref rotateTo)) {
                        Debug.LogError("LeanTweenBridge: AnimateData: to must be an Vector3. data: " + data);
                        return;
                    }

                    switch (command) {

                        case "rotate":
                            result = LeanTween.rotate(target, rotateTo, time);
                            break;

                        case "rotateLocal":
                            result = LeanTween.rotateLocal(target, rotateTo, time);
                            break;

                    }

                    break;

                case "rotateX":
                case "rotateY":
                case "rotateZ":

                    //Debug.Log("LeanTweenBridge: AnimateData: TODO: command: " + command + " data: " + data);

                    // TODO: handle RectTransform instead of GameObject

                    if (!data.ContainsKey("to")) {
                        Debug.LogError("LeanTweenBridge: AnimateData: missing to. data: " + data);
                        return;
                    }

                    float rotateToValue = data.GetFloat("to");

                    switch (command) {

                        case "rotateX":
                            result = LeanTween.rotateX(target, rotateToValue, time);
                            break;

                        case "rotateY":
                            result = LeanTween.rotateY(target, rotateToValue, time);
                            break;

                        case "rotateZ":
                            result = LeanTween.rotateZ(target, rotateToValue, time);
                            break;

                    }

                    break;

                case "rotateAround":
                case "rotateAroundLocal":

                    //Debug.Log("LeanTweenBridge: AnimateData: command: " + command + " data: " + data);

                    // TODO: handle RectTransform instead of GameObject

                    if (!data.ContainsKey("axis")) {
                        Debug.LogError("LeanTweenBridge: AnimateData: missing axis. data: " + data);
                        return;
                    }

                    if (!data.ContainsKey("to")) {
                        Debug.LogError("LeanTweenBridge: AnimateData: missing to. data: " + data);
                        return;
                    }

                    Vector3 rotateAroundAxis = Vector3.zero;
                    if (!Bridge.bridge.ConvertToType<Vector3>(data["axis"], ref rotateAroundAxis)) {
                        Debug.LogError("LeanTweenBridge: AnimateData: to must be an Vector3. data: " + data);
                        return;
                    }

                    float rotateAroundTo = data.GetFloat("to");

                    switch (command) {

                        case "rotateAround":
                            result = LeanTween.rotateAround(target, rotateAroundAxis, rotateAroundTo, time);
                            break;

                        case "rotateLocalLocal":
                            result = LeanTween.rotateAroundLocal(target, rotateAroundAxis, rotateAroundTo, time);
                            break;

                    }

                    break;

                case "scale":

                    //Debug.Log("LeanTweenBridge: AnimateData: command: " + command + " data: " + data);

                    // TODO: handle RectTransform instead of GameObject

                    if (!data.ContainsKey("to")) {
                        Debug.LogError("LeanTweenBridge: AnimateData: missing to. data: " + data);
                        return;
                    }

                    Vector3 to = Vector3.one;
                    JToken toToken = data["to"];
                    if (toToken.IsNumber()) {
                        float s = (float)toToken;
                        to = new Vector3(s, s, s);
                    } else if (toToken.IsObject()) {
                        if (!Bridge.bridge.ConvertToType<Vector3>(toToken, ref to)) {
                            Debug.LogError("LeanTweenBridge: AnimateData: error converting to object to Vector3. data: " + data);
                            return;
                        }
                    } else {
                        Debug.LogError("LeanTweenBridge: AnimateData: to should be number or object. toToken: " + toToken + " " + toToken.Type + " data: " + data);
                        return;
                    }

                    result = LeanTween.scale(target, to, time);

                    break;

                case "scaleX":
                case "scaleY":
                case "scaleZ":

                    //Debug.Log("LeanTweenBridge: AnimateData: command: " + command + " data: " + data);

                    if (!data.ContainsKey("to")) {
                        Debug.LogError("LeanTweenBridge: AnimateData: missing to. data: " + data);
                        return;
                    }

                    float scaleTo = data.GetFloat("to");

                    switch (command) {

                        case "scaleX":
                            result = LeanTween.scaleX(target, scaleTo, time);
                            break;

                        case "scaleY":
                            result = LeanTween.scaleY(target, scaleTo, time);
                            break;

                        case "scaleZ":
                            result = LeanTween.scaleZ(target, scaleTo, time);
                            break;

                    }

                    break;

                case "size":
                    Debug.Log("LeanTweenBridge: AnimateData: TODO: command: " + command + " data: " + data);
                    // TODO: handle RectTransform but not GameObject
                    break;

                case "alpha":
                    Debug.Log("LeanTweenBridge: AnimateData: TODO: command: " + command + " data: " + data);
                    break;

                case "color":
                    Debug.Log("LeanTweenBridge: AnimateData: TODO: command: " + command + " data: " + data);
                    break;

                case "colorText":
                    Debug.Log("LeanTweenBridge: AnimateData: TODO: command: " + command + " data: " + data);
                    break;

                default:
                    Debug.LogError("LeanTweenBridge: AnimateData: unknown command: " + command + " in data: " + data);
                    return;

            }

            if (result != null) {

/*
                pause
                resume
                axis
                delay
                direction
                ease
                from
                ignoreTimeScale
                loopClamp
                loopOnce
                loopPingPong
                onComplete
                onCompleteOnRepeat
                onCompleteOnStart
                onCompleteParam
                onStart
                onUpdate
                onUpdateParam
                orientToPath
                orientToPath2d
                overshoot
                passed
                period
                point
                recursive
                repeat
                scale
                speed
                time
                to
                useFrames
                updateNow
*/

            }

        }

    }


    //////////////////////////////////////////////////////////////////////////////////
    // Instance Methods
    

    public void Awake()
    {
        //Debug.Log("LeanTweenBridge: Awake: this: " + this + " bridge: " +  ((bridge == null) ? "null" : ("" + bridge)));

        if (leanTweenBridge == null) {
            leanTweenBridge = this;
        } else {
            Debug.LogError("LeanTweenBridge: Awake: There should only be one leanTweenBridge!");
        }

        LeanTween.init(maxTweens);
    }


    public override void OnDestroy()
    {
        //Debug.Log("LeanTweenBridge: OnDestroy: this: " + this + " leanTweenBridge: " +  ((leanTweenBridge == null) ? "null" : ("" + leanTweenBridge)));

        base.OnDestroy();

        if (leanTweenBridge == this) {
            leanTweenBridge = null;
        } else {
            if (leanTweenBridge != null) {
                Debug.LogError("LeanTweenBridge: OnDestroy: the global leanTweenBridge: " + ((leanTweenBridge == null) ? "null" : ("" + leanTweenBridge)) + " isn't me!");
            }
        }
    }


    public override void HandleEvent(JObject ev)
    {
        base.HandleEvent(ev);

        //Debug.Log("LeanTweenBridge: HandleEvent: this: " + this + " ev: " + ev, this);

        string eventName = (string)ev["event"];
        //Debug.Log("LeanTweenBridge: HandleEvent: eventName: " + eventName, this);
        if (string.IsNullOrEmpty(eventName)) {
            Debug.LogError("LeanTweenBridge: HandleEvent: missing event name in ev: " + ev);
            return;
        }

        JObject data = (JObject)ev["data"];
        //Debug.Log("LeanTweenBridge: HandleEvent: eventName: " + eventName, this);

        switch (eventName) {

            case "Tween": {
                Debug.Log("LeanTweenBridge: HandleEvent: Tween: ev: " + ev);
                break;
            }

        }
    }


}


}

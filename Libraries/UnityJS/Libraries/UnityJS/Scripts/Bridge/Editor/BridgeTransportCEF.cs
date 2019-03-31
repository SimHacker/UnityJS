////////////////////////////////////////////////////////////////////////
// BridgeTransportCEF.cs
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.


#if UNITY_EDITOR && USE_CEF


using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;


public class BridgeTransportCEF : BridgeTransport
{


    public UnityJSWindow unityJSWindow;


    public override void HandleInit()
    {
        //Debug.Log("BridgeTransportCEF: HandleInit");

        base.HandleInit();

        driver = "CEF";
        MakeUnityJSWindow();
    }


    public void MakeUnityJSWindow()
    {
        if (unityJSWindow != null) {
            Debug.Log("BridgeTransportCEF: MakeUnityJSWindow: called multiple times!");
            return;
        }
        
        Debug.Log("BridgeTransportCEF: MakeUnityJSWindow: calling CreateInstance<UnityJSWindow>");
        unityJSWindow = ScriptableObject.CreateInstance<UnityJSWindow>();

        unityJSWindow.titleContent = new GUIContent("Unity-JS WebView");

        Debug.Log("BridgeTransportCEF: MakeUnityJSWindow: calling unityJSWindow: " + unityJSWindow + " Show");
        unityJSWindow.Show(true);

        unityJSWindow.startedEvent += HandleUnityJSWindowStarted;
        unityJSWindow.sendEvent += SendJSToUnityEvents;

        Debug.Log("BridgeTransportCEF: MakeUnityJSWindow: calling unityJSWindow: " + unityJSWindow + " Init");
        unityJSWindow.Init();

        Debug.Log("BridgeTransportCEF: MakeUnityJSWindow: unityJSWindow: " + unityJSWindow);
    }


    public void HandleUnityJSWindowStarted()
    {
        Debug.Log("BridgeTransportCEF: HandleUnityJSWindowStarted: unityJSWindow: " + unityJSWindow);

        startedJS = true;

        bridge.HandleTransportStarted();
    }


    public override void HandleDestroy()
    {
        Debug.Log("BridgeTransportCEF: HandleDestroy: unityJSWindow: " + unityJSWindow);

        base.HandleDestroy();
        DestroyUnityJSWindow();
    }


    public void DestroyUnityJSWindow()
    {
        if (unityJSWindow == null) {
            Debug.Log("BridgeTransportCEF: DestroyUnityJSWindow: called multiple times!");
            return;
        }

        unityJSWindow.Close();
        UnityEngine.Object.DestroyImmediate(unityJSWindow);
        unityJSWindow = null;
    }


    public override void EvaluateJS(string js)
    {
        Debug.Log("BridgeTransportCEF: EvaluateJS: js: " + js);

        if (unityJSWindow == null) {
            Debug.LogError("BridgeTransportCEF: EvaluateJS: unityJSWindow not defined!");
            return;
        }

        unityJSWindow.EvaluateJS(js);
    }


}


#endif

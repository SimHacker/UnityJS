////////////////////////////////////////////////////////////////////////
// BridgeTransport.cs
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.


using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


using StringCallback = System.Action<string>;


namespace UnityJS {


public class BridgeTransport : MonoBehaviour
{


    public string driver = "Unknown";
    public Bridge bridge;
    public List<string> jsToUnityEventQueue = new List<string>();
    public List<string> unityToJSEventQueue = new List<string>();
    public bool startedJS = false;
    public int jsToUnityEventMaxCount = 100;
    public int unityToJSEventMaxCount = 100;
    

    public void Init(Bridge bridge0)
    {
        bridge = bridge0;
        Debug.Log("BridgeTransport: Init: bridge: " + bridge + " spreadsheetID: " + bridge.spreadsheetID);
        HandleInit();
    }
    

    public virtual void HandleInit()
    {
        Debug.Log("BridgeTransport: HandleInit: this: " + this + " bridge: " + bridge + " spreadsheetID: " + bridge.spreadsheetID);
    }


    void Awake()
    {
        Debug.Log("BridgeTransport: Awake: this: " + this + " bridge: " + bridge);
        HandleAwake();
    }
    

    public virtual void HandleAwake()
    {
        Debug.Log("BridgeTransport: HandleAwake: this: " + this + " bridge: " + bridge);
    }


    void Start()
    {
        Debug.Log("BridgeTransport: Start: this: " + this + " bridge: " + bridge);
        HandleStart();
    }
    

    public virtual void HandleStart()
    {
        Debug.Log("BridgeTransport: HandleStart: this: " + this + " bridge: " + bridge);
    }


    void OnDestroy()
    {
        HandleDestroy();

        if (bridge != null) {
            bridge.HandleTransportStopped();
            bridge = null;
        }
    }
    

    public virtual void HandleDestroy()
    {
        //Debug.Log("BridgeTransport: HandleDestroy: this: " + this + " bridge: " + bridge);
    }
    

    public virtual void StartTransport()
    {
    }
    

    public virtual void StopTransport()
    {
    }
    

    public virtual void SendJSToUnityEvents(string evListString)
    {
        //Debug.Log("BridgeTransport: SendJSToUnityEvents: evListString: " + evListString);

        jsToUnityEventQueue.Add(evListString);
    }


    public virtual string ReceiveJSToUnityEvents()
    {
        int eventCount = jsToUnityEventQueue.Count;

        if (eventCount == 0) {
            return null;
        }

        string evListString;

        if (eventCount <= jsToUnityEventMaxCount) {

            evListString =
                string.Join(",", jsToUnityEventQueue.ToArray());
            jsToUnityEventQueue.Clear();

        } else {

            List<string> firstEvents = 
                jsToUnityEventQueue.GetRange(0, jsToUnityEventMaxCount);
            jsToUnityEventQueue.RemoveRange(0, jsToUnityEventMaxCount);
            evListString =
                string.Join(",", firstEvents.ToArray());
        }

        //Debug.Log("BridgeTransport: ReceiveJSToUnityEvents: eventCount: " + eventCount + " evListString: " + evListString.Length + " " + evListString);

        return evListString;
    }


    public virtual void SendUnityToJSEvents(string evListString)
    {
        //Debug.Log("BridgeTransport: SendUnityToJSEvents: evListString: " + evListString);

        unityToJSEventQueue.Add(evListString);
    }


    public virtual string ReceiveUnityToJSEvents()
    {
        int eventCount = unityToJSEventQueue.Count;

        if (eventCount == 0) {
            return null;
        }

        string evListString;

        if (eventCount <= unityToJSEventMaxCount) {

            evListString =
                string.Join(",", unityToJSEventQueue.ToArray());
            unityToJSEventQueue.Clear();

        } else {

            List<string> firstEvents = 
                unityToJSEventQueue.GetRange(0, unityToJSEventMaxCount);
            unityToJSEventQueue.RemoveRange(0, unityToJSEventMaxCount);
            evListString =
                string.Join(",", firstEvents.ToArray());
        }

        //Debug.Log("BridgeTransport: ReceiveUnityToJSEvents: eventCount: " + eventCount + " evListString: " + evListString.Length + " " + evListString);

        return evListString;
    }


    public virtual void DistributeJSEvents()
    {
        if (!startedJS) {
            return;
        }

        string evListString = ReceiveUnityToJSEvents();
        int evListStringLength = 0;
        if (string.IsNullOrEmpty(evListString)) {
            //evListString = "null";
            return;
        } else {
            evListString = "[" + evListString + "]";
            evListStringLength = evListString.Length;
        }

        string js =
            "bridge.distributeEvents(" +
                evListString + "," +
                evListStringLength + ");";

        //Debug.Log("BridgeTransport: DistributeJSEvents: js: " + js.Length + " " + js);

        EvaluateJS(js);
    }


    public virtual void EvaluateJS(string js)
    {
        Debug.LogError("BridgeTransport: TODO: EvaluateJS: js: " + js.Length + " " + js);
    }


    public virtual bool HasSharedTexture()
    {
        return false;
    }


    public virtual bool HasSharedData()
    {
        return false;
    }


    public virtual Texture2D GetSharedTexture(int id)
    {
        Debug.LogError("BridgeTransport: TODO: GetSharedTexture: id: " + id);
        return null;
    }


    public virtual byte[] GetSharedData(int id)
    {
        Debug.LogError("BridgeTransport: TODO: GetSharedData: id: " + id);
        return null;
    }


}


}

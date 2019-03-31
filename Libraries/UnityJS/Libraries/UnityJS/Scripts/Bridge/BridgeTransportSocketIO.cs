////////////////////////////////////////////////////////////////////////
// BridgeTransportSocketIO.cs
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.


#if USE_SOCKETIO && UNITY_EDITOR


using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Dpoch.SocketIO;


namespace UnityJS {


public class BridgeTransportSocketIO : BridgeTransport
{

    public SocketIO socket;
    public string scriptingEngineID = "";
    public string scriptingEngineType = "";
    public static Dictionary<int, byte[]> blobs = new Dictionary<int, byte[]>();


    public override void HandleInit()
    {
        Debug.Log("BridgeTransportSocketIO: HandleStart: this: " + this + " bridge: " + bridge);

        driver = "SocketIO";

        socket = new SocketIO(bridge.socketIOAddress);
        
        socket.OnOpen += () => {
            Debug.Log("Socket open!");
            startedJS = true;

            JObject message = new JObject();
            message.Add("engineType", "DisplayEngine");
            socket.Emit("Hello", message);

            Debug.Log("BridgeTransportSocketIO: HandleStart: OnOpen: sent Hello message: " + message);

            //bridge.HandleTransportStarted();
        };

        socket.OnConnectFailed += () => {
            Debug.Log("Socket failed to connect!");
        };

        socket.OnClose += () => {
            Debug.Log("Socket closed!");
        };
        
        socket.OnError += (err) => {
            Debug.Log("Socket Error: " + err);
        };

        socket.On("AddFriend", (ev) => {
            Debug.Log("BridgeTransportSocketIO: HandleStart: On AddFriend: ev: " + ev + " Name: " + ev.Name + " Data: " + ev.Data);
            JObject message = (JObject)ev.Data[0];
            string id = (string)message["id"];
            string engineType = (string)message["engineType"];
            switch (engineType) {

                case "ScriptingEngine":
                    scriptingEngineID = id;
                    scriptingEngineType = engineType;
                    Debug.Log("BridgeTransportSocketIO: HandleStart: On AddFriend: ScriptingEngine: Starting ScriptingEngine SocketIO transport.");
                    bridge.HandleTransportStarted();
                    break;

                default:
                    Debug.Log("BridgeTransportSocketIO: HandleStart: On AddFriend: unknown engineType: " + engineType);
                    break;

            }
        });

        socket.On("RemoveFriend", (ev) => {
            Debug.Log("BridgeTransportSocketIO: HandleStart: On RemoveFriend: ev: " + ev + " Name: " + ev.Name + " Data: " + ev.Data);
            JObject message = (JObject)ev.Data[0];
            string id = (string)message["id"];
            string engineType = (string)message["engineType"];
            switch (engineType) {

                case "ScriptingEngine":
                    scriptingEngineID = "";
                    scriptingEngineType = "";
                    Debug.Log("BridgeTransportSocketIO: HandleStart: On RemoveFriend: ScriptingEngine: Starting ScriptingEngine SocketIO transport.");
                    bridge.HandleTransportStopped();
                    break;

                default:
                    Debug.Log("BridgeTransportSocketIO: HandleStart: On RemoveFriend: unknown engineType: " + engineType);
                    break;

            }
        });

        socket.On("SendEventList", (ev) => {
            Debug.Log("BridgeTransportSocketIO: HandleStart: On SendEventList: ev: " + ev + " Name: " + ev.Name + " Data: " + ev.Data);
            string evListString = (string)ev.Data[0];
            SendJSToUnityEvents(evListString);
        });

        socket.On("SendBlob", (ev) => {
            Debug.Log("BridgeTransportSocketIO: HandleStart: On SendBlob: ev: " + ev + " Name: " + ev.Name + " Data: " + ev.Data);
            int blobID = (int)ev.Data[0];
            JToken blobToken = ev.Data[1];
            Debug.Log("BridgeTransportSocketIO: HandleStart: blobToken.Type: " + blobToken.Type + " blobToken: " + blobToken);
            byte[] blob = blobToken.ToObject<byte[]>();
            Debug.Log("BridgeTransportSocketIO: HandleStart: blob: " + blob);
            SendBlob(blobID, blob);
        });

        socket.Connect();

    }


    public static void SendBlob(int blobID, byte[] blob)
    {
        Debug.Log("BridgeTransportSocketIO: SendBlob: blobID: " + blobID + " length: " + blob.Length + " data: " + blob[0] + " " + blob[1] + " " + blob[2] + " " + blob[3]);
        blobs[blobID] = blob;
    }


    public static byte[] GetBlob(int blobID)
    {
        Debug.Log("BridgeTransportSocketIO: GetBlob: blobID: " + blobID + " contains: " + blobs.ContainsKey(blobID));

        if (!blobs.ContainsKey(blobID)) {
            return null;
        }

        byte[] blob = blobs[blobID];
        blobs.Remove(blobID);

        Debug.Log("BridgeTransportSocketIO: GetBlob: blob length: " + blob.Length + " data: " + blob[0] + " " + blob[1] + " " + blob[2] + " " + blob[3]);

        return blob;
    }


    public override void HandleStart()
    {
    }


    public override void HandleDestroy()
    {
        //Debug.Log("BridgeTransportSocketIO: HandleDestroy: this: " + this + " bridge: " + bridge);
        if (socket != null) {
            socket.Close();
            socket = null;
        }
    }
    

    public override void EvaluateJS(string js)
    {
        //Debug.Log("BridgeTransportSocketIO: EvaluateJS: js: " + js.Length + " " + js);
        socket.Emit("EvaluateJS", js);
    }


    public override bool HasSharedTexture()
    {
        return false;
    }


    public override bool HasSharedData()
    {
        return false;
    }


    public override Texture2D GetSharedTexture(int id)
    {
        Debug.LogError("BridgeTransportSocketIO: TODO: GetSharedTexture: id: " + id);
        return null;
    }


    public override byte[] GetSharedData(int id)
    {
        Debug.LogError("BridgeTransportSocketIO: TODO: GetSharedData: id: " + id);
        return null;
    }


}


}


#endif

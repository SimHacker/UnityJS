////////////////////////////////////////////////////////////////////////
// BridgeTransportWebGL.cs
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.


#if UNITY_WEBGL && !UNITY_EDITOR


using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;


namespace UnityJS {


public class BridgeTransportWebGL : BridgeTransport
{


    // https://forum.unity.com/threads/monopinvokecallback-in-unity.132510/

    public class TextureInfo {
        public int id;
        public int width;
        public int height;
        public Texture2D texture;
        public bool locked;
        public byte[] data;
        public GCHandle handle;
        public IntPtr pointer;
    };


    public class DataInfo {
        public int id;
        public int size;
        public bool locked;
        public byte[] data;
        public GCHandle handle;
        public IntPtr pointer;
    };


    public delegate int AllocateTextureDelegate(int width, int height);
    public delegate void FreeTextureDelegate(int id);
    public delegate int LockTextureDelegate(int id);
    public delegate void UnlockTextureDelegate(int id);
    public delegate int AllocateDataDelegate(int size);
    public delegate void FreeDataDelegate(int id);
    public delegate int LockDataDelegate(int id);
    public delegate void UnlockDataDelegate(int id);


    public static Dictionary<int, TextureInfo> textureInfos = new Dictionary<int, TextureInfo>();
    public static int nextTextureID = 1;
    public static Dictionary<int, DataInfo> dataInfos = new Dictionary<int, DataInfo>();
    public static int nextDataID = 1;


    private const string PLUGIN_DLL = "__Internal";


    [DllImport(PLUGIN_DLL)]
    public static extern void _UnityJS_HandleAwake(AllocateTextureDelegate allocateTextureCallback, FreeTextureDelegate freeTextureCallback, LockTextureDelegate lockTextureCallback, UnlockTextureDelegate unlockTextureCallback, AllocateDataDelegate allocateDataCallback, FreeDataDelegate freeDataCallback, LockDataDelegate lockDataCallback, UnlockDataDelegate unlockDataCallback);


    [DllImport(PLUGIN_DLL)]
    public static extern void _UnityJS_HandleDestroy();


    [DllImport(PLUGIN_DLL)]
    public static extern void _UnityJS_EvaluateJS(string js);


    [DllImport(PLUGIN_DLL)]
    public static extern string _UnityJS_ReceiveJSToUnityEvents();


    [DllImport(PLUGIN_DLL)]
    public static extern void _UnityJS_SendUnityToJSEvents(string evListString);


    [DllImport(PLUGIN_DLL)]
    public static extern void _UnityJS_DistributeJSEvents();


    public override void HandleInit()
    {
        Debug.Log("BridgeTransportWebGL: HandleInit");

        driver = "WebGL";

        base.HandleInit();

        _UnityJS_HandleAwake(
            AllocateTexture,
            FreeTexture,
            LockTexture,
            UnlockTexture,
            AllocateData,
            FreeData,
            LockData,
            UnlockData);

        startedJS = true;

        Debug.Log("BridgeTransportWebGL: HandleAwake: calling HandleTransportStarted: bridge: " + bridge);

        bridge.HandleTransportStarted();
    }


    [MonoPInvokeCallback(typeof(AllocateTextureDelegate))]
    public static int AllocateTexture(int width, int height)
    {
        //Debug.Log("BridgeTransportWebGL: AllocateTexture: width: " + width + " height: " + height + " id: " + nextTextureID);

        TextureInfo textureInfo = new TextureInfo();
        textureInfo.id = nextTextureID++;
        textureInfo.width = width;
        textureInfo.height = height;
        textureInfo.texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        textureInfo.locked = false;
        textureInfo.data = null;
        textureInfos[textureInfo.id] = textureInfo;

        return textureInfo.id;
    }


    [MonoPInvokeCallback(typeof(FreeTextureDelegate))]
    public static void FreeTexture(int id)
    {
        //Debug.Log("BridgeTransportWebGL: FreeTexture: id: " + id);
        if (!textureInfos.ContainsKey(id)) {
            Debug.LogError("BridgeTransportWebGL: FreeTexture: invalid id: " + id);
            return;
        }

        TextureInfo textureInfo = textureInfos[id];

        if (textureInfo.locked) {
            Debug.LogError("BridgeTransportWebGL: FreeTexture: free while locked! id: " + id);
            UnlockTexture(id);
        }

        textureInfos.Remove(id);
    }
    

    [MonoPInvokeCallback(typeof(LockTextureDelegate))]
    public static int LockTexture(int id)
    {
        if (!textureInfos.ContainsKey(id)) {
            Debug.LogError("BridgeTransportWebGL: LockTexture: invalid id: " + id);
            return 0;
        }

        TextureInfo textureInfo = textureInfos[id];

        if (textureInfo.locked) {
            Debug.LogError("BridgeTransportWebGL: LockTexture: already locked: " + id);
            return 0;
        }

        textureInfo.locked = true;
        if (textureInfo.data == null) {
            textureInfo.data = new byte[textureInfo.width * textureInfo.height * 4];
        }
        textureInfo.handle = GCHandle.Alloc(textureInfo.data, GCHandleType.Pinned);
        textureInfo.pointer = textureInfo.handle.AddrOfPinnedObject();

        //Debug.Log("BridgeTransportWebGL: LockTexture: locked. pointer: " + textureInfo.pointer);

        return (int)textureInfo.pointer;
    }


    [MonoPInvokeCallback(typeof(UnlockTextureDelegate))]
    public static void UnlockTexture(int id)
    {
        //Debug.Log("BridgeTransportWebGL: UnlockTexture: id: " + id);

        if (!textureInfos.ContainsKey(id)) {
            Debug.LogError("BridgeTransportWebGL: UnlockTexture: invalid id: " + id);
            return;
        }

        TextureInfo textureInfo = textureInfos[id];

        if (!textureInfo.locked) {
            Debug.LogError("BridgeTransportWebGL: UnlockTexture: not locked: " + id);
            return;
        }

        textureInfo.texture.LoadRawTextureData(textureInfo.data);
        textureInfo.texture.Apply();
        textureInfo.handle.Free();
        textureInfo.locked = false;
        //textureInfo.data = null;
        textureInfo.pointer = (IntPtr)0;
        //Debug.Log("BridgeTransportWebGL: UnlockTexture: unlocked.");
    }
    

    [MonoPInvokeCallback(typeof(AllocateDataDelegate))]
    public static int AllocateData(int size)
    {
        //Debug.Log("BridgeTransportWebGL: AllocateData: size: " + size + " id: " + nextDataID);

        DataInfo dataInfo = new DataInfo();
        dataInfo.id = nextDataID++;
        dataInfo.size = size;
        dataInfo.data = new byte[size];
        dataInfo.locked = false;
        dataInfo.data = null;
        dataInfos[dataInfo.id] = dataInfo;

        return dataInfo.id;
    }


    [MonoPInvokeCallback(typeof(FreeDataDelegate))]
    public static void FreeData(int id)
    {
        //Debug.Log("BridgeTransportWebGL: FreeData: id: " + id);
        if (!dataInfos.ContainsKey(id)) {
            Debug.LogError("BridgeTransportWebGL: FreeData: invalid id: " + id);
            return;
        }

        DataInfo dataInfo = dataInfos[id];

        if (dataInfo.locked) {
            Debug.LogError("BridgeTransportWebGL: FreeData: free while locked! id: " + id);
            UnlockData(id);
        }

        dataInfos.Remove(id);
    }


    [MonoPInvokeCallback(typeof(LockDataDelegate))]
    public static int LockData(int id)
    {
        if (!dataInfos.ContainsKey(id)) {
            Debug.LogError("BridgeTransportWebGL: LockData: invalid id: " + id);
            return 0;
        }

        DataInfo dataInfo = dataInfos[id];

        if (dataInfo.locked) {
            Debug.LogError("BridgeTransportWebGL: LockData: already locked: " + id);
            return 0;
        }

        dataInfo.locked = true;
        if (dataInfo.data == null) {
            dataInfo.data = new byte[dataInfo.size];
        }
        dataInfo.handle = GCHandle.Alloc(dataInfo.data, GCHandleType.Pinned);
        dataInfo.pointer = dataInfo.handle.AddrOfPinnedObject();

        //Debug.Log("BridgeTransportWebGL: LockData: locked. pointer: " + dataInfo.pointer);

        return (int)dataInfo.pointer;
    }


    [MonoPInvokeCallback(typeof(UnlockDataDelegate))]
    public static void UnlockData(int id)
    {
        //Debug.Log("BridgeTransportWebGL: UnlockData: id: " + id);

        if (!dataInfos.ContainsKey(id)) {
            Debug.LogError("BridgeTransportWebGL: UnlockData: invalid id: " + id);
            return;
        }

        DataInfo dataInfo = dataInfos[id];

        if (!dataInfo.locked) {
            Debug.LogError("BridgeTransportWebGL: UnlockData: not locked: " + id);
            return;
        }

        // TODO ...
        dataInfo.handle.Free();
        dataInfo.locked = false;
        dataInfo.pointer = (IntPtr)0;
        //Debug.Log("BridgeTransportWebGL: UnlockData: unlocked.");
    }
    

    public override void HandleAwake()
    {
        Debug.Log("BridgeTransportWebGL: HandleAwake: this: " + this + " bridge: " + bridge);
    }


    public override void HandleDestroy()
    {
        //Debug.Log("BridgeTransportWebGL: HandleDestroy: this: " + this + " bridge: " + bridge);
        _UnityJS_HandleDestroy();
    }


    public override void SendJSToUnityEvents(string evListString)
    {
        Debug.LogError("BridgeTransportWebGL: SendJSToUnityEvents: should not be called!");
    }


    public override string ReceiveJSToUnityEvents()
    {
        return _UnityJS_ReceiveJSToUnityEvents();
    }
    

    public override void SendUnityToJSEvents(string evListString)
    {
        //Debug.Log("BridgeTransportWebGL: SendUnityToJSEvents: evListString: " + evListString);

        _UnityJS_SendUnityToJSEvents(evListString);
    }


    public override string ReceiveUnityToJSEvents()
    {
        Debug.LogError("BridgeTransportWebGL: DistributeJSEvents: should not be called!");
        return null;
    }


    public override void DistributeJSEvents()
    {
        _UnityJS_DistributeJSEvents();
    }
    

    public override void EvaluateJS(string js)
    {
        //Debug.Log("BridgeTransportWebGL: EvaluateJS: js: " + js);
        _UnityJS_EvaluateJS(js);
    }


    public override bool HasSharedTexture()
    {
        return true;
    }


    public override bool HasSharedData()
    {
        return true;
    }


    public override Texture2D GetSharedTexture(int id)
    {
        //Debug.Log("BridgeTransportWebGL: GetSharedTexture: id: " + id);

        if (!textureInfos.ContainsKey(id)) {
            Debug.LogError("BridgeTransport: GetSharedTexture: invalid id: " + id);
            return null;
        }

        TextureInfo textureInfo = textureInfos[id];

        return textureInfo.texture;
    }


    public override byte[] GetSharedData(int id)
    {
        //Debug.Log("BridgeTransportWebGL: GetSharedData: id: " + id);

        if (!dataInfos.ContainsKey(id)) {
            Debug.LogError("BridgeTransport: GetSharedData: invalid id: " + id);
            return null;
        }

        DataInfo dataInfo = dataInfos[id];

        return dataInfo.data;
    }


}


}


#endif

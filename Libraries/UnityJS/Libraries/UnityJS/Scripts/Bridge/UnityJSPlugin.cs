/*
 * Copyright (C) 2011 Keijiro Takahashi
 * Copyright (C) 2012 GREE, Inc.
 * Copyright (C) 2017 by Don Hopkins, Ground Up Software.
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty.  In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would be
 *    appreciated but is not required.
 * 2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 * 3. This notice may not be removed or altered from any source distribution.
 */


#if !(UNITY_EDITOR && USE_CEF) && !(UNITY_WEBGL && !UNITY_EDITOR)


using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Text.RegularExpressions;


using StringCallback = System.Action<string>;
using TextureCallback = System.Action<UnityEngine.Texture2D>;


namespace UnityJS {


public class UnityJSPlugin : MonoBehaviour {

    // This is an efficient, simplified direct alternative to UnitySendMessage, which is extremely slow.
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_ANDROID
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)] // Enables getting a C callable function pointer.
    private delegate void UnitySendMessageDelegate(string target, string method, string message);
    static UnitySendMessageDelegate unitySendMessageDelegate;
#endif

    static IntPtr renderEventFunc;
    static Dictionary<string, UnityJSPlugin> plugins = new Dictionary<string, UnityJSPlugin>();

    public event StringCallback onJS;
    public event StringCallback onResult;
    public event StringCallback onError;
    public event StringCallback onLoaded;
    public event StringCallback onConsoleMessage;
    public event TextureCallback onTexture;

    public string pluginID;
    public bool visibility;
    public long textureHandle = -1;
    public int textureWidth = 0;
    public int textureHeight = 0;
    public Texture2D texture;
    public bool issuePluginRenderEvents = true;
    public bool pluginRenderEventIssued;
    public List<string> messageQueue = new List<string>();

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE
    IntPtr plugin;
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    // TODO
#elif UNITY_ANDROID
    AndroidJavaObject plugin;
#endif

#if !UNITY_IOS
    public bool isKeyboardVisible = false;
#endif


    public bool IsKeyboardVisible {
        get {
#if UNITY_ANDROID
            return isKeyboardVisible;
#elif UNITY_IPHONE
            return TouchScreenKeyboard.visible;
#else
            return false;
#endif
        }
    }


    /// Called from Java native plugin to set when the keyboard is opened
    public void SetKeyboardVisible(string isVisible)
    {
#if UNITY_ANDROID
        isKeyboardVisible = (isVisible == "true");
#endif
    }


#if UNITY_EDITOR_OSX
    private const string PLUGIN_DLL = "UnityJS_Editor";
#elif UNITY_STANDALONE_OSX
    private const string PLUGIN_DLL = "UnityJS";
#elif UNITY_EDITOR_WIN
    private const string PLUGIN_DLL = "UnityJS_Editor";
#elif UNITY_STANDALONE_WIN
    private const string PLUGIN_DLL = "UnityJS";
#elif UNITY_IPHONE
    private const string PLUGIN_DLL = "__Internal";
#endif

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE

    [DllImport(PLUGIN_DLL)]
    private static extern IntPtr _CUnityJSPlugin_SetUnitySendMessageCallback(IntPtr unitySendMessageCallback);

    [DllImport(PLUGIN_DLL)]
    private static extern IntPtr _CUnityJSPlugin_Init(bool transparent);

    [DllImport(PLUGIN_DLL)]
    private static extern int _CUnityJSPlugin_Destroy(IntPtr instance);

    [DllImport(PLUGIN_DLL)]
    private static extern void _CUnityJSPlugin_SetRect(IntPtr instance, int width, int height);

    [DllImport(PLUGIN_DLL)]
    private static extern void _CUnityJSPlugin_SetVisibility(IntPtr instance, bool visibility);

    [DllImport(PLUGIN_DLL)]
    private static extern void _CUnityJSPlugin_LoadURL(IntPtr instance, string url);

    [DllImport(PLUGIN_DLL)]
    private static extern void _CUnityJSPlugin_EvaluateJS(IntPtr instance, string js);

    [DllImport(PLUGIN_DLL)]
    private static extern void _CUnityJSPlugin_EvaluateJSReturnResult(IntPtr instance, string js);

    [DllImport(PLUGIN_DLL)]
    private static extern bool _CUnityJSPlugin_CanGoBack(IntPtr instance);

    [DllImport(PLUGIN_DLL)]
    private static extern bool _CUnityJSPlugin_CanGoForward(IntPtr instance);

    [DllImport(PLUGIN_DLL)]
    private static extern void _CUnityJSPlugin_GoBack(IntPtr instance);

    [DllImport(PLUGIN_DLL)]
    private static extern void _CUnityJSPlugin_GoForward(IntPtr instance);

    [DllImport(PLUGIN_DLL)]
    private static extern string _CUnityJSPlugin_GetPluginID(IntPtr instance);

    [DllImport(PLUGIN_DLL)]
    private static extern void _CUnityJSPlugin_RenderIntoTextureSetup(IntPtr instance, int width, int height);

    [DllImport(PLUGIN_DLL)]
    private static extern long _CUnityJSPlugin_GetRenderTextureHandle(IntPtr instance);

    [DllImport(PLUGIN_DLL)]
    private static extern int _CUnityJSPlugin_GetRenderTextureWidth(IntPtr instance);

    [DllImport(PLUGIN_DLL)]
    private static extern int _CUnityJSPlugin_GetRenderTextureHeight(IntPtr instance);

    [DllImport(PLUGIN_DLL)]
    private static extern IntPtr _CUnityJSPlugin_GetRenderEventFunc();

    [DllImport(PLUGIN_DLL)]
    private static extern void _CUnityJSPlugin_FlushCaches(IntPtr instance);

#endif


    public static void DestroyPlugins()
    {
        //Debug.Log("UnityJSPlugin: DestroyPlugins");

        List<UnityJSPlugin> pluginsToDestroy = new List<UnityJSPlugin>();
        foreach (UnityJSPlugin plugin in plugins.Values) {
            pluginsToDestroy.Add(plugin);
        }
        foreach (UnityJSPlugin plugin in pluginsToDestroy) {
            //Debug.Log("UnityJSPlugin: DestroyPlugins: destroying plugin id " + plugin.pluginID + " " + plugin);
            plugin.OnDestroy();
        }
    }


    public void Init(bool transparent=false)
    {
        //Debug.Log("UnityJSPlugin: Init: transparent: " + transparent);

        if (unitySendMessageDelegate == null) {

            unitySendMessageDelegate =
                new UnitySendMessageDelegate(
                    HandleUnitySendMessageDispatch);
            IntPtr unitySendMessageCallback =
                Marshal.GetFunctionPointerForDelegate(
                    unitySendMessageDelegate);

            //Debug.Log("UnityJSPlugin: Init: unitySendMessageDelegate: " + unitySendMessageDelegate);

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IOS

            _CUnityJSPlugin_SetUnitySendMessageCallback(
                unitySendMessageCallback);

#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

            // TODO

#elif UNITY_ANDROID

            AndroidJavaClass pluginClass =
                new AndroidJavaClass(
                    "com.groundupsoftware.unityjs.CUnityJSPlugin");

            pluginClass.CallStatic(
                "SetUnitySendMessageCallback",
                (long)unitySendMessageCallback);

#endif

        }

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IOS

        plugin =
            _CUnityJSPlugin_Init(
                transparent);

        pluginID =
            _CUnityJSPlugin_GetPluginID(plugin);


        // TODO

#elif UNITY_ANDROID

        plugin =
            new AndroidJavaObject(
                "com.groundupsoftware.unityjs.CUnityJSPlugin");

        plugin.Call(
            "Init", 
            transparent);

        pluginID = 
            plugin.Call<string>("GetPluginID");

#endif

        plugins[pluginID] = this;
    }


    protected virtual void OnDestroy()
    {

        //Debug.Log("UnityJSPlugin: OnDestroy: pluginID: " + pluginID);

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE

        if (plugin == IntPtr.Zero) {
            //Debug.Log("UnityJSPlugin: OnDestroy: pluginID: " + pluginID + " plugin was null");
            return;
        }

        //Debug.Log("UnityJSPlugin: OnDestroy: pluginID: " + pluginID + " destroying plugin " + plugin);
        _CUnityJSPlugin_Destroy(plugin);

        plugin = IntPtr.Zero;

#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

        // TODO

#elif UNITY_ANDROID

        if (plugin == null) {
            return;
        }

        plugin.Call("Destroy");

        plugin = null;

#endif

        if (!string.IsNullOrEmpty(pluginID) &&
            plugins.ContainsKey(pluginID)) {
            plugins.Remove(pluginID);
        }

    }


    [MonoPInvokeCallback(typeof(UnitySendMessageDelegate))]
    public static void HandleUnitySendMessageDispatch(string target, string method, string message)
    {
        //Debug.Log("UnityJSPlugin: HandleUnitySendMessageDispatch: target: " + target + " method: " + method + " message: " + message);

        if (!plugins.ContainsKey(target)) {
            //Debug.Log("UnityJSPlugin: HandleUnitySendMessageDispatch: missing target: " + target + " method: " + method + " message: " + message);
            return;
        }

        UnityJSPlugin obj = plugins[target];
        //Debug.Log("UnityJSPlugin: HandleUnitySendMessageDispatch: sending to obj: " + ((obj == null) ? "null" : "OBJ"));

        obj.HandleUnitySendMessage(method, message);
    }


    public void HandleUnitySendMessage(string method, string message)
    {
        //Debug.Log("UnityJSPlugin: HandleUnitySendMessage: BEGIN: pluginID: " + pluginID + " method: " + method + " message: " + message);

        lock (messageQueue) {
            messageQueue.Add(method);
            messageQueue.Add(message);
        }

        //Debug.Log("UnityJSPlugin: HandleUnitySendMessage: DONE: pluginID: " + pluginID + " method: " + method + " message: " + message);
    }


    public void PumpMessageQueue()
    {
        lock (messageQueue) {

            int messageQueueCount = messageQueue.Count & ~1;

            if (messageQueueCount < 2) {
                return;
            }

            //Debug.Log("UnityJSPlugin: PumpMessageQueue: BEGIN: pluginID: " + pluginID + " messageQueue.Count: " + messageQueue.Count + " messageQueueCount: " + messageQueueCount);

            for (int i = 0; i < messageQueueCount; i += 2) {

                string method = messageQueue[i];
                string message = messageQueue[i + 1];

                //Debug.Log("UnityJSPlugin: PumpMessageQueue: pluginID: " + pluginID + " i: " + i + " messageQueueCount: " + messageQueueCount + " method: " + method + " message: " + message + " messageQueue.Count: " + ((messageQueue == null) ? "NULL" : ("" + messageQueue.Count)));

                switch (method) {

                    case "Loaded":
                        CallOnLoaded(message);
                        break;

                    case "Error":
                        CallOnError(message);
                        break;

                    case "CallFromJS":
                        CallFromJS(message);
                        break;

                    case "ResultFromJS":
                        ReturnResultFromJS(message);
                        break;

                    case "MessageFromJS":
                        ReturnResultFromJS(message);
                        break;

                    case "ConsoleMessage":
                        CallOnConsoleMessage(message);
                        break;

                    case "Texture":
                        CallOnTexture();
                        break;

                    case "SetKeyboardVisible":
                        SetKeyboardVisible(message);
                        break;

                    default:
                        Debug.LogError("UnityJSPlugin: PumpMessageQueue: pluginID: " + pluginID + " undefined method: " + method + " message: " + message);
                        break;

                }

            }

            messageQueue.RemoveRange(0, messageQueueCount);

            //Debug.Log("UnityJSPlugin: PumpMessageQueue: END: pluginID: " + pluginID + " messageQueue.Count: " + messageQueue.Count + " messageQueueCount: " + messageQueueCount);

        }

    }


    public void SetRect(int width, int height)
    {

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE

        if (plugin == IntPtr.Zero) {
            return;
        }

        _CUnityJSPlugin_SetRect(plugin, width, height);

#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

        // TODO

#elif UNITY_ANDROID

        if (plugin == null) {
            return;
        }

        plugin.Call("SetRect", width, height);

#endif

    }


    public void SetVisibility(bool v)
    {

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE

        if (plugin == IntPtr.Zero) {
            return;
        }

        _CUnityJSPlugin_SetVisibility(plugin, v);

#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

        // TODO

#elif UNITY_ANDROID

        if (plugin == null) {
            return;
        }

        plugin.Call("SetVisibility", v);

#endif

        visibility = v;
    }


    public bool GetVisibility()
    {
        return visibility;
    }


    public void LoadURL(string url)
    {
        //Debug.Log("UnityJSPlugin: LoadURL: url: " + url);

        if (string.IsNullOrEmpty(url)) {
            return;
        }

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE

        if (plugin == IntPtr.Zero) {
            return;
        }

        _CUnityJSPlugin_LoadURL(plugin, url);

#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

        // TODO

#elif UNITY_ANDROID

        if (plugin == null) {
            return;
        }

        plugin.Call("LoadURL", url);

#endif

    }


    public void EvaluateJS(string js)
    {
        //Debug.Log("UnityJSPlugin: EvaluateJS: js: " + js, this);

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE

        if (plugin == IntPtr.Zero) {
            //Debug.Log("UnityJSPlugin: EvaluateJS: no plugin", this);
            return;
        }

        //Debug.Log("UnityJSPlugin: EvaluateJS: CUnityJSPlugin EvaluateJS: js: " + js, this); 

        _CUnityJSPlugin_EvaluateJS(plugin, js);

#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

        // TODO

#elif UNITY_ANDROID

        if (plugin == null) {
            //Debug.Log("UnityJSPlugin: EvaluateJS: no plugin", this);
            return;
        }

        plugin.Call("EvaluateJS", js);

#endif

    }


    public void EvaluateJSReturnResult(string js)
    {
        //Debug.Log("UnityJSPlugin: EvaluateJSReturnResult: js: " + js, this);

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE

        if (plugin == IntPtr.Zero) {
            //Debug.Log("UnityJSPlugin: EvaluateJSReturnResult: no plugin", this);
            return;
        }

        _CUnityJSPlugin_EvaluateJSReturnResult(plugin, js);

#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

        // TODO

#elif UNITY_ANDROID

        if (plugin == null) {
            //Debug.Log("UnityJSPlugin: EvaluateJSReturnResult: no plugin", this);
            return;
        }

        plugin.Call("EvaluateJSReturnResult", js);

#endif

    }


    public bool CanGoBack()
    {

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE

        if (plugin == IntPtr.Zero) {
            return false;
        }

        return _CUnityJSPlugin_CanGoBack(plugin);

#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

        // TODO
        return false;

#elif UNITY_ANDROID

        if (plugin == null) {
            return false;
        }

        return plugin.Get<bool>("canGoBack");

#endif

    }


    public bool CanGoForward()
    {

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE

        if (plugin == IntPtr.Zero) {
            return false;
        }

        return _CUnityJSPlugin_CanGoForward(plugin);

#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

        // TODO
        return false;

#elif UNITY_ANDROID

        if (plugin == null) {
            return false;
        }

        return plugin.Get<bool>("canGoForward");

#endif

    }


    public void GoBack()
    {

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE

        if (plugin == IntPtr.Zero) {
            return;
        }

        _CUnityJSPlugin_GoBack(plugin);

#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

        // TODO

#elif UNITY_ANDROID

        if (plugin == null) {
            return;
        }

        plugin.Call("GoBack");

#endif

    }


    public void GoForward()
    {

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE
        if (plugin == IntPtr.Zero) {
            return;
        }

        _CUnityJSPlugin_GoForward(plugin);

#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

        // TODO

#elif UNITY_ANDROID

        if (plugin == null) {
            return;
        }

        plugin.Call("GoForward");

#endif
    }


    public void CallOnError(string errorMessage)
    {

        //Debug.Log("UnityJSPlugin: CallOnError: errorMessage: " + errorMessage, this);

        if (onError != null) {
            onError(errorMessage);
        }

    }


    public void CallOnLoaded(string loadedUrl)
    {
        //Debug.Log("UnityJSPlugin: CallOnLoaded: loadedUrl: " + loadedUrl, this);

        if (onLoaded != null) {
            onLoaded(loadedUrl);
        }
    }


    public void CallFromJS(string data)
    {
        //Debug.Log("UnityJSPlugin: CallFromJS: data: " + data);

        if (onJS != null) {
            onJS(data);
        }
    }


    public void ReturnResultFromJS(string data)
    {
        //Debug.Log("UnityJSPlugin: ReturnResultFromJS: data: " + data);

        if (onResult != null) {
            onResult(data);
        }
    }


    public void CallOnConsoleMessage(string consoleMessage)
    {
        //Debug.Log("UnityJSPlugin: CallOnConsoleMessage: consoleMessage: " + consoleMessage, this);

        if (onConsoleMessage != null) {
            onConsoleMessage(consoleMessage);
        }
    }


    public void CallOnTexture()
    {

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE
        long newTextureHandle = _CUnityJSPlugin_GetRenderTextureHandle(plugin);
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        // TODO
        long newTextureHandle = 0;
#elif UNITY_ANDROID
        long newTextureHandle = plugin.Call<long>("GetRenderTextureHandle");
#endif

        //Debug.Log("UnityJSPlugin: CallOnTexture: newTextureHandle: " + newTextureHandle + " textureHandle: " + textureHandle);
        if (newTextureHandle != textureHandle) {
            //Debug.Log("UnityJSPlugin: CallOnTexture: textureHandle changed from: " + textureHandle + " to: " + newTextureHandle);
            textureHandle = newTextureHandle;
            texture = null;
        }

        if ((texture == null) && (textureHandle != 0)) {

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE
            textureWidth = _CUnityJSPlugin_GetRenderTextureWidth(plugin);
            textureHeight = _CUnityJSPlugin_GetRenderTextureHeight(plugin);
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            // TODO
            textureWidth = 0;
            textureHeight = 0;
#elif UNITY_ANDROID
            textureWidth = plugin.Call<int>("GetRenderTextureWidth");
            textureHeight = plugin.Call<int>("GetRenderTextureHeight");
#endif

            texture = Texture2D.CreateExternalTexture(textureWidth, textureHeight, TextureFormat.RGBA32, false, true, (IntPtr)textureHandle);

            //Debug.Log("UnityJSPlugin: CallOnTexture: CreateExternalTexture width: " + textureWidth + " height: " + textureHeight + " textureHandle: " + textureHandle + " texture: " + texture);
        }

        //Debug.Log("UnityJSPlugin: CallOnTexture texture: " + texture + " onTexture: " + onTexture, this);

        if (onTexture != null) {
            onTexture(texture);
        }

    }


    private IntPtr GetRenderEventFunc()
    {
        if (renderEventFunc == (IntPtr)0) {
            renderEventFunc =
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE
                (IntPtr)_CUnityJSPlugin_GetRenderEventFunc();
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                (IntPtr)0; // TODO
#elif UNITY_ANDROID
                (IntPtr)plugin.CallStatic<long>("GetRenderEventFunc");
#endif
            //Debug.Log("UnityJSPlugin: GetRenderEventFunc: Got renderEventFunc: " + renderEventFunc);
        }

        return renderEventFunc;
    }


    public void RenderIntoTexture(int width, int height)
    {
        //Debug.Log("UnityJSPlugin: RenderIntoTexture: time: " + Time.time + " width: " + width + " height: " + height);

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE
        _CUnityJSPlugin_RenderIntoTextureSetup(plugin, width, height);
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        // TODO
#elif UNITY_ANDROID
        plugin.Call("RenderIntoTextureSetup", width, height);
#endif
    }


    public void FlushCaches()
    {

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE

        if (plugin == IntPtr.Zero) {
            return;
        }

        _CUnityJSPlugin_FlushCaches(plugin);

#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

        // TODO

#elif UNITY_ANDROID

        if (plugin == null) {
            return;
        }

        plugin.Call("FlushCaches");
#endif

    }


    void Update()
    {
        pluginRenderEventIssued = false;
    }


    void FixedUpdate()
    {
        PumpMessageQueue();
    }


    void LateUpdate()
    {
        // This must only happen once per render frame!
        if (issuePluginRenderEvents &&
            !pluginRenderEventIssued) {
            pluginRenderEventIssued = true;
            IssuePluginRenderEvent();
        }
    }


    public void IssuePluginRenderEvent()
    {
        //Debug.Log("UnityJSPlugin: IssuePluginRenderEvent: time: " + Time.time + " pluginID: " + pluginID + " this: " + this);

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE || UNITY_ANDROID
        GL.IssuePluginEvent(GetRenderEventFunc(), 2);
#endif
    }


}


}


#endif

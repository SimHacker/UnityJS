////////////////////////////////////////////////////////////////////////
// BridgeTransportWebView.cs
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.


#if !(UNITY_EDITOR && USE_CEF) && !(UNITY_WEBGL && !UNITY_EDITOR)


using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


using StringCallback = System.Action<string>;


namespace UnityJS {


public class BridgeTransportWebView : BridgeTransport
{

    public Renderer webViewRenderer;
    public UnityJSPlugin unityJSPlugin;
    public bool visibility = false;
    public bool transparent = true;
    public int initialScale = 50;
    public int webViewWidth = 1024;
    public int webViewHeight = 1024;
    public string textureChannel = "WebView";
    public Texture2D webViewTexture;
    public bool sendPoll = false;
    public bool sentPoll = false;
    public float startTime = 0.0f;
    public float webViewTextureUpdateStartDelay = 0.0f;
    public float webViewTextureUpdateTime = 0.0f;
    public float webViewTextureUpdateDelay = 0.1f;
    public bool initialFlushCaches = true;


    public override void HandleInit()
    {
        driver = "WebView";

        StartCoroutine(StartWebView());
    }
    

    public IEnumerator StartWebView()
    {
        //Debug.Log("BridgeTransportWebView: StartWebView: url: " + url);

#if UNITY_ANDROID && !UNITY_EDITOR
        //Debug.Log("BridgeTransportWebView: StartWebView: Android");

        string sourceDir = Application.streamingAssetsPath;
        string destDir = Application.persistentDataPath;

        string filesPath = sourceDir + "/files.txt";
        string filesData = null;
        if (filesPath.Contains("://")) { // Android jar: URLs
            //Debug.Log("BridgeTransportWebView: StartWebView: www reading filesPath: " + filesPath);
            var www = new WWW(filesPath);
            yield return www;
            filesData = www.text;
        } else {
            //Debug.Log("BridgeTransportWebView: StartWebView: reading filesPath: " + filesPath);
            filesData = File.ReadAllText(filesPath);
        }

        //Debug.Log("BridgeTransportWebView: StartWebView: filesData: " + filesData);

        string[] fileNames = filesData.Split(new char[] { '\n' });

        foreach (string fileName in fileNames) {

            //Debug.Log("BridgeTransportWebView: StartWebView: fileName: " + fileName);

            if (fileName == "" ||
                fileName.StartsWith(".") ||
                fileName.EndsWith(".meta")) {
                continue;
            }

            string sourceFile = sourceDir + "/" + fileName;
            string destFile = destDir + "/" + fileName;

            //Debug.Log("BridgeTransportWebView: StartWebView: Copying sourceFile: " + sourceFile + " to destFile: " + destFile);

            if (File.Exists(destFile)) {
                File.Delete(destFile);
            }

            byte[] data = null;
            if (sourceFile.Contains("://")) { // Android jar: URLs
                //Debug.Log("BridgeTransportWebView: www reading: " + sourceFile);
                var www = new WWW(sourceFile);
                yield return www;
                data = www.bytes;
            } else {
                data = System.IO.File.ReadAllBytes(sourceFile);
            }
            //Debug.Log("BridgeTransportWebView: read " + data.Length + " bytes from: " + sourceFile);

            System.IO.File.WriteAllBytes(destFile, data);
            //Debug.Log("BridgeTransportWebView: wrote " + data.Length + " bytes to: " + destFile);

        }
#endif

        startTime = Time.time;

        unityJSPlugin = gameObject.AddComponent<UnityJSPlugin>();

        unityJSPlugin.onJS += HandleJS;
        unityJSPlugin.onResult += HandleResult;
        unityJSPlugin.onError += HandleError;
        unityJSPlugin.onLoaded += HandleLoaded;
        unityJSPlugin.onConsoleMessage += HandleConsoleMessage;
        unityJSPlugin.onTexture += HandleTexture;

        unityJSPlugin.Init(transparent: transparent);

        if (initialFlushCaches) {
            unityJSPlugin.FlushCaches();
        }

        unityJSPlugin.SetRect(webViewWidth, webViewHeight);
        unityJSPlugin.SetVisibility(visibility);

        string cleanURL = CleanURL(bridge.url);
        unityJSPlugin.LoadURL(cleanURL);

        yield break;
    }


    public override void HandleDestroy()
    {
        //Debug.Log("BridgeTransportWebView: HandleDestroy");

        base.HandleDestroy();

        if (unityJSPlugin != null) {
            //UnityEngine.Object.DestroyImmediate(unityJSPlugin);
            UnityEngine.Object.Destroy(unityJSPlugin);
            unityJSPlugin = null;
        }
    }


    private void OnApplicationQuit()
    {
        //Debug.Log("BridgeTransportWebView: OnApplicationQuit");
        UnityJSPlugin.DestroyPlugins();
    }


    public string CleanURL(string url)
    {
        string cleanURL = url;

        if (!cleanURL.StartsWith("http")) {

            cleanURL =
                "file://" + 
#if UNITY_ANDROID && !UNITY_EDITOR
                Application.persistentDataPath + 
#else
                Application.streamingAssetsPath +
#endif
                "/" + cleanURL;

        }

        cleanURL = cleanURL.Replace(" ", "%20");

        //Debug.Log("BridgeTransportWebView: CleanURL: url: " + url + " cleanURL: " + cleanURL);

        return cleanURL;
    }
    

    public void Update()
    {
        if ((webViewTextureUpdateDelay > 0.0f) &&
            (Time.time >= (startTime + webViewTextureUpdateStartDelay)) &&
            (Time.time >= (webViewTextureUpdateTime + webViewTextureUpdateDelay))) {
            webViewTextureUpdateTime = Time.time;
            //Debug.Log("BridgeTransportWebView: Update: BEFORE UpdateWebViewTexture ================================");
            UpdateWebViewTexture();
            //Debug.Log("BridgeTransportWebView: Update: AFTER UpdateWebViewTexture ================================");
        }
    }


    public void FixedUpdate()
    {
        if ((bridge != null) && !sentPoll && sendPoll) {
            sendPoll = false;
            sentPoll = true;
            PollForMessages();
        }
    }


    public void UpdateWebViewTexture()
    {
        //Debug.Log("BridgeTransportWebView: UpdateWebViewTexture pluginID: " + unityJSPlugin.pluginID + " unityJSPlugin: " +  unityJSPlugin + " width: " + webViewWidth + " height: " + webViewHeight);
        if (unityJSPlugin != null) {
            unityJSPlugin.RenderIntoTexture(webViewWidth, webViewHeight);
        }
    }


    public void PollForMessages()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        EvaluateJSReturnResult("bridge.pollForEventsAndroid()");
#else
        EvaluateJSReturnResult("bridge.pollForEvents()");
#endif
    }


    public void UpdateVisibility()
    {
        unityJSPlugin.SetVisibility(visibility);
    }


    public void HandleJS(string message)
    {
        //Debug.Log("BridgeTransportWebView: HandleJS: message: " + message, this);

        if (bridge == null) {
            return;
        }

        if (message == "poll") {
            sendPoll = true;
            return;
        }
    }


    public void HandleResult(string result)
    {
        //Debug.Log("BridgeTransportWebView: HandleResult: result: " + result.Length + " " + result);

        if (bridge == null) {
            return;
        }

        sentPoll = false;

        SendJSToUnityEvents(result);
    }


    public void HandleError(string message)
    {
        //Debug.Log("BridgeTransportWebView: HandleError: message: " + message, this);
    }


    public void HandleLoaded(string url)
    {
        //Debug.Log("BridgeTransportWebView: HandleLoaded: url: " + url, this);

        startedJS = true;

        bridge.HandleTransportStarted();
    }


    public void HandleConsoleMessage(string message)
    {
        Debug.Log("BridgeTransportWebView: HandleConsoleMessage: **** " + message, this);
    }


    public void HandleTexture(Texture2D texture)
    {
        //Debug.Log("BridgeTransportWebView: HandleTexture: BEGIN: texture: " + texture, this);

        if (webViewRenderer != null) {
            webViewRenderer.material.mainTexture = texture;
        }

        bridge.DistributeTexture(textureChannel, texture, this);

        //Debug.Log("BridgeTransportWebView: HandleTexture: DONE", this);
    }


    public void ToggleVisibility()
    {
        visibility = !visibility;
        UpdateVisibility();
    }


    public override void EvaluateJS(string js)
    {
        //Debug.Log("BridgeTransportWebView: EvaluateJS: js: " + js.Length + " " + js);
        unityJSPlugin.EvaluateJS(js);
    }


    public void EvaluateJSReturnResult(string js)
    {
        //Debug.Log("BridgeTransportWebView: EvaluateJSReturnResult: js: " + js.Length + " " + js);
        unityJSPlugin.EvaluateJSReturnResult(js);
    }


}


}


#endif

////////////////////////////////////////////////////////////////////////
// TextureViewer.cs
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace UnityJS {


public class TextureViewer : Tracker {


    ////////////////////////////////////////////////////////////////////////
    // Instance variables


    public string textureChannel;
    public string textureChannelSubscribed;
    public Texture2D texture;
    public Renderer textureRenderer;


    ////////////////////////////////////////////////////////////////////////
    // TextureViewer properties


    public bool needsUpdate;


    ////////////////////////////////////////////////////////////////////////
    // Instance Methods


    public override void HandleEvent(JObject ev)
    {
        base.HandleEvent(ev);

        Debug.Log("TextureViewer: HandleEvent: this: " + this + " ev: " + ev, this);

        string eventName = (string)ev["event"];
        //Debug.Log("TextureViewer: HandleEvent: eventName: " + eventName, this);
        if (string.IsNullOrEmpty(eventName)) {
            Debug.LogError("TextureViewer: HandleEvent: missing event name in ev: " + ev);
            return;
        }

        JObject data = (JObject)ev["data"];
        //Debug.Log("TextureViewer: HandleEvent: eventName: " + eventName, this);

        switch (eventName) {

            case "Foo": {
                break;
            }

        }
    }


    void Start()
    {
        needsUpdate = true;
        SetMouseEntered(false);
        SetMouseDown(false);
        UpdateState();
    }


    public override void OnDestroy()
    {
        //Debug.Log("TextureViewer: OnDestroy: clearing textureChannelSubscribed: " + textureChannelSubscribed + " textureChannel: " + textureChannel);
        
        textureChannel = null;
        UpdateTextureChannelSubscription();

        base.OnDestroy();
    }


    void Update()
    {
        UpdateTextureChannelSubscription();
        UpdateState();
    }


    public void UpdateState()
    {
        if (!needsUpdate) {
            return;
        }

        needsUpdate = false;
    }

    void UpdateTextureChannelSubscription()
    {

        if (string.IsNullOrEmpty(textureChannel) &&
            (!string.IsNullOrEmpty(textureChannelSubscribed))) {
            //Debug.Log("TextureViewer: UpdateTextureChannelSubscription: unsubscribing textureChannelSubscribed: " + textureChannelSubscribed);
            //Bridge.TextureChannelUnsubscribe(textureChannelSubscribed, HandleTextureChannel);
            textureChannelSubscribed = null;
        }

        if (!string.IsNullOrEmpty(textureChannel) &&
            string.IsNullOrEmpty(textureChannelSubscribed)) {
            //Debug.Log("TextureViewer: UpdateTextureChannelSubscription: subscribing textureChannelSubscribed: " + textureChannelSubscribed);
            textureChannelSubscribed = textureChannel;
            //Bridge.TextureChannelSubscribe(textureChannelSubscribed, HandleTextureChannel);
        }

    }


    public void HandleTextureChannel(Texture2D texture0, string channel, object data)
    {
        //Debug.Log("TextureViewer: HandleTextureChannel: texture0: " + texture0 + " channel: " + channel + " data: " + data + " textureRenderer: " + textureRenderer, this);

        texture = texture0;

        if ((textureRenderer != null)  &&
            (textureRenderer.material != null)) {
            textureRenderer.material.mainTexture = texture0;
        }

    }


}


}

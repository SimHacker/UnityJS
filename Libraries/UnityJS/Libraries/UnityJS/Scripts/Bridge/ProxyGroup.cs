////////////////////////////////////////////////////////////////////////
// ProxyGroup.cs
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace UnityJS {


public class ProxyGroup : MonoBehaviour {


    ////////////////////////////////////////////////////////////////////////
    // Static Variables


    public static Dictionary<ProxyGroup, string> proxyGroupToHandle = new Dictionary<ProxyGroup, string>();
    public static Dictionary<string, ProxyGroup> handleToProxyGroup = new Dictionary<string, ProxyGroup>();
    public static int nextProxyGroupHandle = 0;


    ////////////////////////////////////////////////////////////////////////
    // Instance Variables


    public string proxyGroupHandle;
    public Dictionary<object, string> partToHandle = new Dictionary<object, string>();
    public Dictionary<string, object> handleToPart = new Dictionary<string, object>();
    public int nextPartHandle = 0;


    ////////////////////////////////////////////////////////////////////////
    // Static Methods


    public static ProxyGroup FindProxyGroupForProxied(object proxied)
    {
        if (proxied == null) {
            Debug.LogError("ProxyGroup: GetProxyGroup: null proxied: " + proxied);
            return null;
        }

        if (proxied is ProxyGroup) {
            return (ProxyGroup)proxied;
        }

        GameObject go = null;

        if (proxied is GameObject) {
            go = (GameObject)proxied;
        } else if (proxied is Component) {
            Component comp = (Component)proxied;
            go = comp.gameObject;
        }

        if (go == null) {
            Debug.LogError("ProxyGroup: GetProxyGroup: can't find GameObject for proxied: " + proxied + " type: " + proxied.GetType());
            return null;
        }

        ProxyGroup proxyGroup = go.GetComponent<ProxyGroup>();

        if (proxyGroup != null) {
            return proxyGroup;
        }

        proxyGroup = go.AddComponent<ProxyGroup>();
        proxyGroup.Initialize();

        return proxyGroup;
    }


    public static ProxyGroup FindProxyGroup(string proxyGroupHandle)
    {
        if (handleToProxyGroup.ContainsKey(proxyGroupHandle)) {
            return handleToProxyGroup[proxyGroupHandle];
        }

        return null;
    }


    public static object FindProxied(string handle)
    {
        string[] parts = handle.Split(new char[] {'.'}, 2);

        if (parts.Length != 2) {
            Debug.LogError("ProxyGroup: FindProxied: invalid handle: " + handle);
            return null;
        }

        string proxyGroupHandle = parts[0];

        ProxyGroup proxyGroup = FindProxyGroup(proxyGroupHandle);

        if (proxyGroup == null) {
            Debug.LogError("ProxyGroup: FindProxied: undefined proxyGroupHandle: " + proxyGroupHandle + " in handle: " + handle);
            return null;
        }
        
        return proxyGroup.FindPart(handle);
    }


    public static string FindHandle(object obj)
    {
        ProxyGroup proxyGroup = FindProxyGroupForProxied(obj);

        if (proxyGroup == null) {
            Debug.LogError("ProxyGroup: FindHandle: can't find ProxyGroup for obj: " + obj + " type: " + obj.GetType());
            return null;
        }

        return proxyGroup.FindPartHandle(obj);
    }


    ////////////////////////////////////////////////////////////////////////
    // Instance Methods


    public void Initialize()
    {
        proxyGroupHandle = gameObject.name + "#" + nextProxyGroupHandle;
        proxyGroupToHandle[this] = proxyGroupHandle;
        handleToProxyGroup[proxyGroupHandle] = this;
        //Debug.Log("ProxyGroup: Initialize: this: " + this + " proxyGroupHandle: " + proxyGroupHandle);
    }


    public void OnDestroy()
    {
        //Debug.Log("ProxyGroup: OnDestroy: this: " + this + " proxyGroupHandle: " + proxyGroupHandle);

        if (proxyGroupToHandle.ContainsKey(this)) {
            proxyGroupToHandle.Remove(this);
        }

        if (handleToProxyGroup.ContainsKey(proxyGroupHandle)) {
            handleToProxyGroup.Remove(proxyGroupHandle);
        }
    }


    public string FindPartHandle(object part)
    {
        if (partToHandle.ContainsKey(part)) {
            return partToHandle[part];
        }

        string partHandle = part.GetType().Name + "#" + nextPartHandle++;
        string handle = proxyGroupHandle + ":" + partHandle;

        partToHandle[part] = handle;
        handleToPart[handle] = part;

        return handle;
    }


    public object FindPart(string handle)
    {
        if (handleToPart.ContainsKey(handle)) {
            return handleToPart[handle];
        }

        return null;
    }


}


}

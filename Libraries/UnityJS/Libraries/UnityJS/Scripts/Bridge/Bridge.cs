/////////////////////////////////////////////////////////////////////////
// Bridge.cs
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;


namespace UnityJS {


public class Bridge : BridgeObject {


    ////////////////////////////////////////////////////////////////////////
    // Delegates


    public delegate bool ConvertToDelegate(JToken obj, System.Type systemType, ref object result);
    public delegate bool ConvertFromDelegate(object obj, System.Type systemType, ref JToken result);
    public delegate void TextureChannelDelegate(Texture2D texture, string channel, object data);


    ////////////////////////////////////////////////////////////////////////
    // Static Variables


    public static Bridge mainBridge;
    public static JsonSerializer jsonSerializer;


    //////////////////////////////////////////////////////////////////////////////////
    // Instance Variables
    

    public Dictionary<string, object> idToObject = new Dictionary<string, object>();
    public Dictionary<object, string> objectToID = new Dictionary<object, string>();
    public Dictionary<string, TextureChannelDelegate> textureChannels = new Dictionary<string, TextureChannelDelegate>();
    public Booter booter;
    public string gameID = "";
    public string deployment = "";
    public string title = "";
    public string url = "bridge.html";
    public string spreadsheetID = "1nh8tlnanRaTmY8amABggxc0emaXCukCYR18EGddiC4w";
    public string configuration = "world";
#if USE_SOCKETIO && UNITY_EDITOR
    public bool useSocketIO = false;
    public string socketIOAddress;
#endif
    public bool startedJS = false;
    public bool restarting = false;
    public BridgeTransport transport;
    public string handleStartedScript = "";
    public string handleLoadedScript = "";
    public string handleLoadFailedScript = "";


    ////////////////////////////////////////////////////////////////////////
    // Static Methods
    

    public static string GetStringDefault(JObject obj, string key, string def = null)
    {
        var valueToken = obj[key];
        if (valueToken == null) {
            return def;
        }

        string str = (string)valueToken;
        if (str == null) {
            return def;
        }

        return str;
    }


    public static JObject GetJObjectDefault(JObject obj, string key, JObject def = null)
    {
        var valueToken = obj[key];
        if (valueToken == null) {
            return def;
        }

        JObject jobj = valueToken as JObject;
        if (jobj == null) {
            return def;
        }

        return jobj;
    }


    public static JArray GetJArrayDefault(JObject obj, string key, JArray def = null)
    {
        var valueToken = obj[key];
        if (valueToken == null) {
            return def;
        }

        JArray jarr = valueToken as JArray;
        if (jarr == null) {
            return def;
        }

        return jarr;
    }


    public static bool ConvertToEnum<EnumType>(object obj, ref EnumType result)
    {
        int i = 0;

        if (obj != null) {

            if (obj is JToken) {
                JToken token = (JToken)obj;
                switch (token.Type) {
                    case JTokenType.String:
                        obj = (string)token.ToString();
                        break;
                }
            }

            if (obj is string) {

                var str = (string)obj;

                result =
                    (EnumType)Enum.Parse(
                        typeof(EnumType), 
                        str);

                //Debug.Log("BridgeManager: ConvertToEnum: EnumType: " + typeof(EnumType) + " str: " + str + " result: " + result);

                return true;
            }

            if (obj is JValue) {
                i = ((JValue)obj).ToObject<int>();
            } else {
                return false;
            }

        }

        result = 
            (EnumType)Enum.ToObject(
                typeof(EnumType), 
                i);

        //Debug.Log("BridgeManager: ConvertToEnum: EnumType: " + typeof(EnumType) + " i: " + i + " result: " + result);

        return true;
    }


    public static EnumType ToEnum<EnumType>(object obj)
    {
        EnumType result = default(EnumType);
        ConvertToEnum<EnumType>(obj, ref result);
        return result;
    }


    public static string ConvertFromEnum<EnumType>(EnumType value)
    {
        string result =
            Enum.Format(
                typeof(EnumType), 
                value, 
                "g");

        //Debug.Log("BridgeManager: ConvertFromEnum: EnumType: " + typeof(EnumType) + " value: " + value + " result: " + result);

        return result;
    }


    public static bool ConvertToEnumMask<EnumType>(object obj, ref EnumType result)
    {
        int val = 0;

        if (obj != null) {

            if (obj is JArray) {
                foreach (JToken value in (JArray)obj) {
                    EnumType enumVal = ToEnum<EnumType>(value);
                    int intVal = Convert.ToInt32(enumVal);
                    //Debug.Log("val: " + val + " value: " + value + " intVal: " + intVal + " val now: " + (val | intVal));
                    val |= intVal;
                }
            } else {
                EnumType enumVal = ToEnum<EnumType>(obj);
                val = Convert.ToInt32(enumVal);
            }

        }

        result = (EnumType)Enum.ToObject(typeof(EnumType), val);

        return true;
    }


    public static EnumType ToEnumMask<EnumType>(object obj)
    {
        EnumType result = default(EnumType);
        ConvertToEnumMask<EnumType>(obj, ref result);
        //Debug.Log("Bridge: ToEnumMask: EnumType: " + typeof(EnumType).Name + " obj: " + obj.GetType().Name + " " + obj + " result: " + result);
        return result;
    }


    //////////////////////////////////////////////////////////////////////////////////
    // Instance Methods
    

    public void Awake()
    {
        //Debug.Log("Bridge: Awake: this: " + this + " bridge: " +  ((bridge == null) ? "null" : ("" + bridge)) + " enabled: " + this.enabled + " spreadsheetID: " + this.spreadsheetID);

        if (mainBridge == null) {
            mainBridge = this;
        } else {
            Debug.LogError("Bridge: Awake: There should only be one mainBridge!");
        }

        bridge = this;

        if (jsonSerializer == null) {
            jsonSerializer = new JsonSerializer();
            jsonSerializer.Converters.Add(new BridgeJsonConverter());
            jsonSerializer.Converters.Add(new StringEnumConverter());
        }
    }


    public void Start()
    {
        //Debug.Log("Bridge: Start: this: " + this + " bridge: " +  ((bridge == null) ? "null" : ("" + bridge)) + " enabled: " + this.enabled + " spreadsheetID: " + this.spreadsheetID);

        StartBridge();
    }


    public void OnDestroy()
    {
        //Debug.Log("Bridge: OnDestroy: this: " + this + " bridge: " +  ((bridge == null) ? "null" : ("" + bridge)) + " enabled: " + this.enabled);

        if (bridge == this) {
            bridge = null;
        } else {
            if (bridge != null) {
                Debug.LogError("Bridge: OnDestroy: the global bridge: " + ((bridge == null) ? "null" : ("" + bridge)) + " isn't me!");
            }
        }
    }


    public void StartBridge()
    {
        //Debug.Log("Bridge: StartBridge: creating maps");
        idToObject = new Dictionary<string, object>();
        objectToID = new Dictionary<object, string>();
        idToObject["bridge"] = this;
        objectToID[this] = "bridge";

        textureChannels = new Dictionary<string, TextureChannelDelegate>();

        //Debug.Log("Bridge: StartBridge: creating transport");
        CreateTransport();
        //Debug.Log("Bridge: StartBridge: created transport");
    }
    

    public void StopBridge()
    {
        DestroyTransport();
        idToObject = null;
        objectToID = null;
        textureChannels = null;
    }


    public void CreateTransport()
    {
        //Debug.Log("Bridge: CreateTransport: spreadsheetID: " + this.spreadsheetID);

        if (transport != null) {
            Debug.LogError("Bridge: CreateTransport: called multiple times!");
            return;
        }

        #if UNITY_EDITOR
            #if USE_SOCKETIO
                if (useSocketIO) {
                    transport = gameObject.AddComponent<BridgeTransportSocketIO>();
                } else {
            #endif
                    #if USE_CEF
                        transport = gameObject.AddComponent<BridgeTransportCEF>();
                    #else
                        transport = gameObject.AddComponent<BridgeTransportWebView>();
                    #endif
            #if USE_SOCKETIO
                }
            #endif
        #else
            #if UNITY_WEBGL
                transport = gameObject.AddComponent<BridgeTransportWebGL>();
            #else
                #if USE_SOCKETIO && UNITY_EDITOR
                    if (useSocketIO) {
                        transport = gameObject.AddComponent<BridgeTransportSocketIO>();
                    } else {
                #endif
                        transport = gameObject.AddComponent<BridgeTransportWebView>();
                #if USE_SOCKETIO && UNITY_EDITOR
                    }
                #endif
            #endif
        #endif

        //Debug.Log("Bridge: CreateTransport: created transport: " + transport + " spreadsheetID: " + this.spreadsheetID);
        
        //Debug.Log("Bridge: CreateTransport: initializing transport: this: " + this + " spreadsheetID: " + this.spreadsheetID);
        transport.Init(this);

        //Debug.Log("Bridge: CreateTransport: starting transport: spreadsheetID: " + this.spreadsheetID);
        transport.StartTransport();
        //Debug.Log("Bridge: CreateTransport: started transport: spreadsheetID: " + this.spreadsheetID);
    }


    public void DestroyTransport()
    {
        transport.StopTransport();
        Destroy(transport);
        transport = null;
    }


    public void HandleTransportStarted()
    {
        //Debug.Log("Bridge: HandleTransportStarted: this: " + this + " spreadsheetID: " + this.spreadsheetID);

        string js = "";

        if (!string.IsNullOrEmpty(handleStartedScript)) {
            js += 
                "bridge.handleStartedScript = " + 
                JsonConvert.ToString(handleStartedScript) +
                "; ";
        }

        if (!string.IsNullOrEmpty(handleLoadedScript)) {
            js += 
                "bridge.handleLoadedScript = " + 
                JsonConvert.ToString(handleLoadedScript) +
                "; ";
        }

        if (!string.IsNullOrEmpty(handleLoadFailedScript)) {
            js += 
                "bridge.handleLoadFailedScript = " + 
                JsonConvert.ToString(handleLoadFailedScript) +
                "; ";
        }

        js +=
          "bridge.start(" + 
          JsonConvert.ToString(transport.driver) +
          ", " +
          JsonConvert.ToString(spreadsheetID) +
          ", " + 
          JsonConvert.ToString(configuration) + 
          "); ";

        //Debug.Log("Bridge: HandleTransportStarted: EvaluateJS: " + js);

        transport.EvaluateJS(js);

        JObject ev = new JObject();
        ev.Add("event", "StartedUnity");

        //Debug.Log("Bridge: HandleStart: sending StartedUnity ev: " + ev);
        SendEvent(ev);
    }


    public void HandleTransportStopped()
    {
        //Debug.Log("Bridge: HandleTransportStopped: this: " + this);
    }


    public void SendEvent(JObject ev)
    {
        //Debug.Log("Bridge: SendEvent: ev: " + ev);

        string evString = ev.ToString();

        transport.SendUnityToJSEvents(evString);
    }


    void FixedUpdate()
    {
        DistributeUnityEvents();
        transport.DistributeJSEvents();
    }


    void DistributeUnityEvents()
    {
        string evListString = transport.ReceiveJSToUnityEvents();

        if (string.IsNullOrEmpty(evListString)) {
            return;
        }

        string json = "[" + evListString + "]";
        //Debug.Log("Bridge: DistributeUnityEvents: json:\n" + json);

        JArray evList = JArray.Parse(json);
        //Debug.Log("Bridge: DistributeUnityEvents: evList: " + evList);

        //Debug.Log("Bridge: DistributeUnityEvents: evList.Count: " + evList.Count + " json.Length: " + json.Length);

        foreach (JObject ev in evList) {
            DistributeUnityEvent(ev);
        }
    }


    void DistributeUnityEvent(JObject ev)
    {
        string eventName = (string)ev["event"];

        //Debug.Log("Bridge: DistributeUnityEvent: eventName: " + eventName + " ev: " + ev);

        JObject data = ev["data"] as JObject;

        switch (eventName) {

            case "StartedJS": {

                //Debug.Log("Bridge: DistributeUnityEvent: StartedJS: " + ev);
                startedJS = true;

                break;

            }

            case "Log": {

                string line = (string)data["line"];

                Debug.Log ("Bridge: DistributeUnityEvent: Log: line: " + line);

                break;

            }

            case "Create": {

                string id = data.GetString("id");
                string prefab = data.GetString("prefab");
                string component = data.GetString("component");
                JArray preEvents = data.GetArray("preEvents");
                string parent = data.GetString("parent");
                bool worldPositionStays = data.GetBoolean("worldPositionStays", true);
                JObject update = data.GetObject("update");
                JObject interests = data.GetObject("interests");
                JArray postEvents = data.GetArray("postEvents");

                //Debug.Log("Bridge: DistributeUnityEvent: Create: id: " + id + " prefab: " + prefab + " component: " + component + " preEvents: " + preEvents + " parent: " + parent + " worldPositionStay: " + worldPositionStays + " update: " + update + " interests: " + interests + " postEvents: " + postEvents);

                GameObject instance = null;
                if (string.IsNullOrEmpty(prefab)) {
                    instance = new GameObject();
                } else {
                    GameObject prefabObject = Resources.Load<GameObject>(prefab);
                    //Debug.Log("Bridge: DistributeUnityEvent: Create: prefab: " + prefab + " prefabObject: " + prefabObject);
                    if (prefabObject == null) {
                        Debug.LogError("Bridge: DistributeUnityEvent: Create: Can't find prefab: " + prefab);
                        return;
                    }
                    instance = Instantiate(prefabObject);
                    //Debug.Log("Bridge: DistributeUnityEvent: Create: instance: " + instance);
                    if (instance == null) {
                        Debug.LogError("Bridge: DistributeUnityEvent: Create: Can't instantiate prefab: " + prefab + " prefabObject: " + prefabObject);
                        return;
                    }
                }

                BridgeObject bridgeObject;

                if (string.IsNullOrEmpty(component)) {

                    bridgeObject = instance.GetComponent<BridgeObject>();
                    //Debug.Log("Bridge: DistributeUnityEvent: Create: bridgeObject: " + bridgeObject);

                    if (bridgeObject == null) {
                        bridgeObject = instance.AddComponent<BridgeObject>();
                    }

                } else {

                    Type componentType = Type.GetType(component);

                    if (componentType == null) {
                        componentType = Type.GetType("UnityJS." + component);
                    }

                    if (componentType == null) {
                        componentType = Type.GetType("UnityEngine." + component);
                    }

                    if (componentType == null) {
                        Debug.LogError("Bridge: DistributeUnityEvent: Create: undefined component class: " + component);
                        return;
                    }

                    if ((componentType != typeof(BridgeObject)) &&
                        (!componentType.IsSubclassOf(typeof(BridgeObject)))) {
                        Debug.LogError("Bridge: DistributeUnityEvent: Create: component class is not subclass of BridgeObject: " + component);
                        return;
                    }

                    bridgeObject = (BridgeObject)instance.AddComponent(componentType);
                }

                instance.name = id;
                bridgeObject.id = id;
                bridgeObject.bridge = this;
                bridgeObject.AddInterests(interests);
                objectToID[bridgeObject] = id;
                idToObject[id] = bridgeObject;

                //Debug.Log("Bridge: DistributeUnityEvent: Create: created, position: " + bridgeObject.transform.position.x + " " + bridgeObject.transform.position.y + " " + bridgeObject.transform.position.z + " bridgeObject: " + bridgeObject, bridgeObject);

                if (preEvents != null) {
                    bridgeObject.HandleEvents(preEvents);
                }

                if (!String.IsNullOrEmpty(parent)) {
                    //Debug.Log("BridgeObject: DistributeUnityEvent: Create: parent: bridgeObject: " + bridgeObject + " parent: " + parent);

                    Accessor accessor = null;
                    if (!Accessor.FindAccessor(
                            bridgeObject,
                            parent,
                            ref accessor)) {

                        Debug.LogError("Bridge: DistributeUnityEvent: Create: parent: can't find accessor for bridgeObject: " + bridgeObject + " parent: " + parent);

                    } else {

                        object obj = null;
                        if (!accessor.Get(ref obj)) {

                            if (!accessor.conditional) {
                                Debug.LogError("Bridge: DistributeUnityEvent: Create: parent: can't get accessor: " + accessor + " bridgeObject: " + bridgeObject + " parent: " + parent);
                            }

                        } else {

                            Component comp = obj as Component;
                            if (comp == null) {

                                if (!accessor.conditional) {
                                    Debug.LogError("Bridge: DistributeUnityEvent: Create: parent: expected Component obj: " + obj + " this: " + this + " parent: " + parent);
                                }

                            } else {

                                GameObject go = comp.gameObject;
                                Transform xform = go.transform;
                                //Debug.Log("Bridge: DistributeUnityEvent: Create: parent: xform: " + xform + " parent: " + parent + " worldPositionStays: " + worldPositionStays);

                                bridgeObject.transform.SetParent(xform, worldPositionStays);

                            }

                        }

                    }

                }

                if (update != null) {
                    bridgeObject.LoadUpdate(update);
                }

                bridgeObject.SendEventName("Created");

                if (postEvents != null) {
                    bridgeObject.HandleEvents(postEvents);
                }

                //Debug.Log("Bridge: DistributeUnityEvent: Create: done, position: " + bridgeObject.transform.position.x + " " + bridgeObject.transform.position.y + " " + bridgeObject.transform.position.z + " bridgeObject: " + bridgeObject);

                break;

            }

            default: {

                string id = (string)ev["id"];
                //Debug.Log("Bridge: DistributeUnityEvent: id: " + id + " ev: " + ev);

                if (string.IsNullOrEmpty(id)) {
                    Debug.LogError("Bridge: DistributeUnityEvent: undefined id on eventName: " + eventName + " ev: " + ev);
                    return;
                }

                if (!idToObject.ContainsKey(id)) {
                    Debug.LogWarning("Bridge: DistributeUnityEvent: missing id: " + id + " ev: " + ev);
                    return;
                }

                object obj = idToObject[id];
                //Debug.Log("Bridge: DistributeUnityEvent: obj: " + obj);

                BridgeObject bridgeObject = obj as BridgeObject;

                if (bridgeObject == null) {
                    Debug.LogError("Bridge: DistributeUnityEvent: tried to send eventName: " + eventName + " to non-BridgeObject obj: " + obj + " id: " + id + " ev: " + ev);
                    return;
                }

                bridgeObject.HandleEvent(ev);

                break;

            }

        }

    }


    public void Boot()
    {
        //Debug.Log("Bridge: Boot: calling JS: bridge.boot();");

        transport.EvaluateJS("bridge.boot();");

        restarting = true;

        string[] keys = new string[idToObject.Keys.Count];
        idToObject.Keys.CopyTo(keys, 0);
        foreach (string objectID in keys) {
            //Debug.Log("Bridge: DistributeUnityEvent: HardBoot: destroying object: " + objectID);
            DestroyObject(idToObject[objectID]);
        }

        idToObject = new Dictionary<string, object>();
        objectToID = new Dictionary<object, string>();
        idToObject["bridge"] = this;
        objectToID[this] = "bridge";

        textureChannels = new Dictionary<string, TextureChannelDelegate>();

        startedJS = false;
        restarting = false;

        idToObject = new Dictionary<string, object>();
        objectToID = new Dictionary<object, string>();
        startedJS = false;

        //Debug.Log("Bridge: HardBoot: calling HandleTranportStarted");
        HandleTransportStarted();
    }


    public object GetObject(string id)
    {
        if (!idToObject.ContainsKey(id)) {
            return null;
        }

        object obj = idToObject[id];

        return obj;
    }


    public void DestroyObject(object obj)
    {
        BridgeObject bridgeObject = obj as BridgeObject;

        string id = null;

        if (bridgeObject != null) {

            if (bridgeObject.destroyed) {
                return;
            }

            id = bridgeObject.id;
            bridgeObject.destroyed = true;

            //Debug.Log("Bridge: DestroyObject: bridgeObject: " + bridgeObject);

            if (!restarting) {
                bridgeObject.SendEventName("Destroyed");
            }

            Destroy(bridgeObject.gameObject);
        }

        if (objectToID.ContainsKey(obj)) {
            id = objectToID[obj];
            objectToID.Remove(obj);
        } else {
            //Debug.Log("Bridge: DestroyObject: objectToID missing obj: " + obj, this);
        }

        if ((id != null) &&
            (id != "bridge") &&
            idToObject.ContainsKey(id)) {
            idToObject.Remove(bridgeObject.id);
        } else {
            //Debug.Log("Bridge: DestroyObject: idToObject missing id: " + id, this);
        }

    }


    public void SendCallbackData(string callbackID, JObject data)
    {
        //Debug.Log("Bridge: SendCallbackData: callbackID: " + callbackID + " results: " + results);
        JObject ev = new JObject();
        ev.Add("event", "Callback");
        ev.Add("id", callbackID);
        ev.Add("data", data);

        //Debug.Log("Bridge: SendCallbackData: sending ev: " + ev);

        SendEvent(ev);
    }


    public virtual void SendQueryData(object obj, JObject query, string callbackID)
    {
        //Debug.Log("Bridge: SendQueryData: obj: " + obj + " query: " + query + " callbackID: " + callbackID);

        JObject data = new JObject();
        AddQueryData(obj, query, data);

        //Debug.Log("Bridge: QueryData: data: " + data);

        if (!string.IsNullOrEmpty(callbackID)) {
            SendCallbackData(callbackID, data);
        }

    }


    public void AddQueryData(object obj, JObject query, JObject data)
    {
        //Debug.Log("Bridge: AddQueryData: query: " + query);

        foreach (var item in query) {
            string key = item.Key;
            string path = (string)item.Value;
            object value = null;
            JToken valueData = null;

            //Debug.Log("Bridge: AddQueryData: get property obj: " + obj + " path: " + path);

            if (!Accessor.GetProperty(obj, path, ref value)) {

                Debug.LogError("Bridge: AddQueryData: can't get property path: " + path);

            } else {

                //Debug.Log("Bridge: AddQueryData: got property value: " + ((value == null) ? "null" : ("" + value)));

                if (!ConvertFromType(value, ref valueData)) {

                    Debug.LogError("Bridge: AddQueryData: can't convert from JSON for type: " + ((valueData == null) ? "null" : ("" + valueData.GetType())) + " obj: " + obj + " key: " + key + " path: " + path + " value: " + value + " valueData: " + valueData);

                } else {

                    //Debug.Log("Bridge: AddQueryData: obj: " + obj + " key: " + key + " path: " + path + " value: " + value + " valueData: " + valueData);

                    data[key] = valueData;

                }

            }

            //data[key] = valueData;

        }

    }


    public override void HandleEvent(JObject ev)
    {
        base.HandleEvent(ev);

        //Debug.Log("Bridge: HandleEvent: this: " + this + " ev: " + ev, this);

        string eventName = (string)ev["event"];
        //Debug.Log("Bridge: HandleEvent: eventName: " + eventName, this);
        if (string.IsNullOrEmpty(eventName)) {
            Debug.LogError("Bridge: HandleEvent: missing event name in ev: " + ev);
            return;
        }

        JObject data = (JObject)ev["data"];
        //Debug.Log("Bridge: HandleEvent: eventName: " + eventName, this);

        switch (eventName) {

            case "ResetBootConfigurations": {
                //Debug.Log("Bridge: HandleEvent: ResetBootConfigurations: ev: " + ev);
                if (booter != null) {
                    booter.ResetBootConfigurations();
                }
                break;
            }

            case "ShowBootCanvas": {
                //Debug.Log("Bridge: HandleEvent: Boot: ev: " + ev);
                if (booter != null) {
                    booter.ShowBootCanvas();
                }
                break;
            }

            case "Boot": {
                //Debug.Log("Bridge: HandleEvent: Boot: ev: " + ev);
                if (booter != null) {
                    booter.BootNow();
                }
                break;
            }

        }
    }


    ////////////////////////////////////////////////////////////////////////
    // Texture channels.


    public void DistributeTexture(string channel, Texture2D texture, object data)
    {
        if (!textureChannels.ContainsKey(channel)) {
            //Debug.Log("Bridge: SendTexture: not sending to dead channel: " + channel + " texture: " + texture + " data: " + data);
            return;
        }

        TextureChannelDelegate handler = textureChannels[channel];
        //Debug.Log("Bridge: SendTexture: sending to live channel: " + channel + " texture: " + texture + " data: " + data + " handler: " + handler);

        handler(texture, channel, data);
    }


    public void TextureChannelSubscribe(string channel, TextureChannelDelegate handler)
    {
        //Debug.Log("Bridge: TextureChannelSubscribe: channel: " + channel + " handler: " + handler + " exists: " + textureChannels.ContainsKey(channel));

        if (!textureChannels.ContainsKey(channel)) {
            textureChannels.Add(channel, null);
        }

        textureChannels[channel] += handler;
    }


    public void TextureChannelUnsubscribe(string channel, TextureChannelDelegate handler)
    {
        //Debug.Log("Bridge: TextureChannelUnsubscribe: channel: " + channel + " handler: " + handler + " exists: " + textureChannels.ContainsKey(channel));

        if (!textureChannels.ContainsKey(channel)) {
            return;
        }

        textureChannels[channel] -= handler;
    }


    ////////////////////////////////////////////////////////////////////////
    // Type conversion.


    public bool ConvertToType<T>(JToken data, ref T result)
    {
        //Debug.Log("Bridge: ConvertToType: T: " + typeof(T) + " data: " + data);

        result = (T)data.ToObject(typeof(T), jsonSerializer);

        //Debug.Log("Bridge: ConvertToType: result: " + result);

        return true;
    }


    public bool ConvertToType(JToken data, System.Type objectType, ref object result)
    {
        //Debug.Log("Bridge: ConvertToType: objectType: " + objectType + " data: " + data);

        result = data.ToObject(objectType, jsonSerializer);

        //Debug.Log("Bridge: ConvertToType: result: " + result);

        return true;
    }


    public bool ConvertFromType(object value, ref JToken result)
    {
        //Debug.Log("Bridge: ConvertFromType: value: " + value + " type: " + ((value == null) ? "NULL" : ("" + value.GetType())));

        if (value == null) {
            result = null;
            return true;
        }

        result = JToken.FromObject(value, jsonSerializer);

        //Debug.Log("Bridge: ConvertFromType: result: " + result + " TokenType: " + result.Type);

        return true;
    }


    public Texture2D GetSharedTexture(int id)
    {
        return transport.GetSharedTexture(id);
    }
    

}


}

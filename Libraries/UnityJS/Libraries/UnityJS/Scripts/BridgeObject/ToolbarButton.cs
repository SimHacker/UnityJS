////////////////////////////////////////////////////////////////////////
// ToolbarButton.cs
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace UnityJS {


public class ToolbarButton : BridgeObject {


    public string message = "Click";
    public string param = "";
    public TextMeshProUGUI label;


    public void Click()
    {
        Debug.Log("ToolbarButton: Click: message: " + message + " param: " + param + " label: " + label.text);
        SendEventName(message);
    }


}


}

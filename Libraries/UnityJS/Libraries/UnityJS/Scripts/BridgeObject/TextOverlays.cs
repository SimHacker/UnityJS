////////////////////////////////////////////////////////////////////////
// TextOverlays.cs
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.


using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


namespace UnityJS {


public class TextOverlays: Tracker {


    ////////////////////////////////////////////////////////////////////////
    // Instance Variables


    public Canvas canvas;
    public RectTransform panel;
    public Image panelImage;
    public RectTransform infoPanel;
    public RectTransform infoPanelImage;
    public TextMeshProUGUI infoText;
    public RectTransform overlay;
    public TextMeshProUGUI centerText;


    ////////////////////////////////////////////////////////////////////////
    // Instance Methods


    public void HandleConsoleInputFieldValueChanged()
    {
        Debug.Log("TextOverlays: HandleConsoleInputFieldValueChanged");
    }
    

    public void HandleConsoleInputFieldEndEdit()
    {
        Debug.Log("TextOverlays: HandleConsoleInputFieldEndEdit");
    }
    

    public void HandleConsoleInputFieldSelect()
    {
        Debug.Log("TextOverlays: HandleConsoleInputFieldSelect");
    }
    

    public void HandleConsoleInputFieldDeselect()
    {
        Debug.Log("TextOverlays: HandleConsoleInputFieldDeselect");
    }
    

    public void HandleClickInfoPanel()
    {
        //Debug.Log("TextOverlays: HandleClickInfoPanel");
        SendEventName("ClickInfoPanel");
    }
    

}


}

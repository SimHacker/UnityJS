////////////////////////////////////////////////////////////////////////
// TrackerProxy.cs
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


namespace UnityJS {


public class TrackerProxy : MonoBehaviour {


    public Tracker target;


    public virtual void OnMouseEnter()
    {
        //Debug.Log("TrackerProxy: OnMouseEnter: target: " + target);
        target.OnMouseEnter();
    }


    public virtual void OnMouseExit()
    {
        //Debug.Log("TrackerProxy: OnMouseExit: target: " + target);
        target.OnMouseExit();
    }

    public virtual void OnMouseDown()
    {
        //Debug.Log("TrackerProxy: OnMouseDown: target: " + target);
        target.OnMouseDown();
    }


    public virtual void OnMouseUp()
    {
        //Debug.Log("TrackerProxy: OnMouseUp: target: " + target);
        target.OnMouseUp();
    }


    public virtual void OnMouseUpAsButton()
    {
        //Debug.Log("TrackerProxy: OnMouseUpAsButton: target: " + target);
        target.OnMouseUpAsButton();
    }


    public virtual void OnMouseDrag()
    {
        //Debug.Log("TrackerProxy: OnMouseDrag: target: " + target);
        target.OnMouseDrag();
    }


    public virtual void OnMouseOver()
    {
        //Debug.Log("TrackerProxy: OnMouseOver: target: " + target);
        target.OnMouseOver();
    }


}


}

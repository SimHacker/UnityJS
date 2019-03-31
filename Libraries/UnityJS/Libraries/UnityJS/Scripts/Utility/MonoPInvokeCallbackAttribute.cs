/////////////////////////////////////////////////////////////////////////
// MonoPInvokeCallbackAttribute.cs
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.
//
// Attribute that allows static functions to have callbacks (from C) generated AOT.


using System;


namespace UnityJS {


public class MonoPInvokeCallbackAttribute : System.Attribute
{


    public Type type;


    public MonoPInvokeCallbackAttribute(Type t) {
        type = t;
    }


}


}

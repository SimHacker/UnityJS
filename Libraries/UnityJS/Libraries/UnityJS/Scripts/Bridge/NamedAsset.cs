////////////////////////////////////////////////////////////////////////
// NamedAsset.cs
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


namespace UnityJS {


//public class NamedAsset : ScriptableObject
[System.Serializable]
public struct NamedAsset
{
    public string name;
    public UnityEngine.Object asset;
}


}

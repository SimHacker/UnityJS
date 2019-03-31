////////////////////////////////////////////////////////////////////////
// NamedAssetManager.cs
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


namespace UnityJS {


public class NamedAssetManager : MonoBehaviour {


    ////////////////////////////////////////////////////////////////////////
    // Instance Variables


    public NamedAsset[] namedAssetArray;
    public Dictionary<string, NamedAsset> namedAssets = new Dictionary<string, NamedAsset>();
    public bool changed = true;


    ////////////////////////////////////////////////////////////////////////
    // Instance Methods


    void Awake()
    {
        changed = true;
        UpdateNamedAssets();
    }


    void Update()
    {
        UpdateNamedAssets();
    }


    void UpdateNamedAssets()
    {
        if (!changed) {
            return;
        }

        changed = false;

        namedAssets.Clear();

        foreach (NamedAsset namedAsset in namedAssetArray) {
            string assetName = namedAsset.name;
            if (string.IsNullOrEmpty(assetName)) {
                Debug.Log("NamedAssetManager: UpdateNamedAssets: ignored empty asset name with asset: " + namedAsset.asset, this);
            } else if (namedAssets.ContainsKey(assetName)) {
                Debug.Log("NamedAssetManager: UpdateNamedAssets: ignored duplicate asset name: " + namedAsset.name + " with asset: " + namedAsset.asset, this);
            } else if (namedAsset.asset == null) {
                Debug.Log("NamedAssetManager: UpdateNamedAssets: ignored asset name: " + namedAsset.name + " with empty asset", this);
            } else {
                namedAssets[namedAsset.name] = namedAsset;
                //Debug.Log("NamedAssetManager: UpdateNamedAssets: got asset name: " + namedAsset.name + " with asset: " + namedAsset.asset, this);
            }
        }

        //Debug.Log("NamedAssetManager: UpdateNamedAssets: namedAssetArray length: " + namedAssetArray.Length + " namedAssets count: " + namedAssets.Count, this);
    }


}


}

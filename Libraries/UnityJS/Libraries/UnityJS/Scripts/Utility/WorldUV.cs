////////////////////////////////////////////////////////////////////////
// WorldUV.cs
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.


using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UnityJS {


public class WorldUV: MonoBehaviour {


    public enum UVMapping {
        XY,
        XZ,
        YZ,
    };

    ////////////////////////////////////////////////////////////////////////
    // Instance Variables


    public UVMapping uvMapping = UVMapping.XZ;
    public Vector2 uvScale = Vector2.one;
    public Vector2 uvOffset = Vector2.zero;
    public bool updateUV = true;
    public bool updateUVAlways = false;


    ////////////////////////////////////////////////////////////////////////
    // Instance Methods


    void Update()
    {
        if (!updateUV && !updateUVAlways) {
            return;
        }

        updateUV = updateUVAlways;

        MeshFilter mf = gameObject.GetComponent<MeshFilter>();
        Mesh normalMesh = mf.sharedMesh;
        Mesh mesh = mf.mesh;

        Vector3[] vertices = normalMesh.vertices;
        Vector2[] uv = normalMesh.uv;

        for (int i = 0; i < vertices.Length; i ++) {
            Vector2 coord = Vector2.zero;
            Vector3 v = transform.TransformPoint(vertices[i]);
            switch (uvMapping) {
                case UVMapping.XY:
                    coord.x = v.x;
                    coord.y = v.y;
                    break;
                case UVMapping.XZ:
                    coord.x = v.x;
                    coord.y = v.z;
                    break;
                case UVMapping.YZ:
                    coord.x = v.y;
                    coord.y = v.z;
                    break;
            }
            coord.x *= uvScale.x;
            coord.x += uvOffset.x;
            coord.y *= uvScale.y;
            coord.y += uvOffset.y;
            uv[i] = coord;
        }

        mesh.uv = uv;
        mf.mesh = mesh;
    }


}


}

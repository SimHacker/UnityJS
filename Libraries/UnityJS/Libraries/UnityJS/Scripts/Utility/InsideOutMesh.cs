////////////////////////////////////////////////////////////////////////
// InsideOutMesh.cs
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.


using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UnityJS {


public class InsideOutMesh: MonoBehaviour {


    ////////////////////////////////////////////////////////////////////////
    // Instance Methods


    void Awake()
    {
        MeshFilter mf = gameObject.GetComponent<MeshFilter>();
        Mesh normalMesh = mf.sharedMesh;
        Mesh insideOutMesh = mf.mesh;

        Vector3[] vertices = normalMesh.vertices;
        int[] triangles = normalMesh.triangles;
        
        for (int i = 0; i < triangles.Length; i += 3) {
            int tmp = triangles[i];
            triangles[i] = triangles[i + 2];
            triangles[i + 2] = tmp;
        }

        insideOutMesh.vertices = vertices;
        insideOutMesh.triangles = triangles;
        insideOutMesh.RecalculateNormals();

        mf.mesh = insideOutMesh;
    }    


}


}

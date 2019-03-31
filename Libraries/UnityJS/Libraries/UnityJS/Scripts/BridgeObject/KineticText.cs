////////////////////////////////////////////////////////////////////////
// KineticText.cs
// Copyright (C) 2017 by Don Hopkins, Ground Up Software.


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace UnityJS {


public class KineticText : Tracker {


    ////////////////////////////////////////////////////////////////////////
    // Component references


    public TextMeshPro textMesh;
    public List<BoxCollider> boxColliders = new List<BoxCollider>();
    public ConfigurableJoint joint;
    public PhysicMaterial physicMaterial;


    ////////////////////////////////////////////////////////////////////////
    // KineticText properties


    public bool textCollider = false;
    public bool characterColliders = true;
    public bool trackColor = false;
    public Color colorNormal = Color.gray;
    public Color faceColorNormal = Color.gray;
    public Color outlineColorNormal = Color.black;
    public Color colorMouseEntered = Color.yellow;
    public Color faceColorMouseEntered = Color.red;
    public Color outlineColorMouseEntered = Color.green;
    public Color colorMouseDown = Color.red;
    public Color faceColorMouseDown = Color.yellow;
    public Color outlineColorMouseDown = Color.green;
    public float colliderThickness = 0.1f;
    public float bumpMag = 3.0f;


    ////////////////////////////////////////////////////////////////////////
    // Instance Methods


    void Update()
    {
        if (textMesh == null) {
            return;
        }

        UpdateState();
        UpdateColliders();
    }


    public void UpdateState()
    {
        if (trackColor && 
            (mouseEnteredChanged || mouseDownChanged)) {

            mouseEnteredChanged = false;
            mouseDownChanged = false;

            Color colorNew = mouseDown ? colorMouseDown : (mouseEntered ? colorMouseEntered : colorNormal);
            if (colorNew != textMesh.color) {
                textMesh.color = colorNew;
            }

            Color faceColorNew = mouseDown ? faceColorMouseDown : (mouseEntered ? faceColorMouseEntered : faceColorNormal);
            if (faceColorNew != textMesh.faceColor) {
                textMesh.faceColor = faceColorNew;
            }
            
            Color outlineColorNew = mouseDown ? outlineColorMouseDown : (mouseEntered ? outlineColorMouseEntered : outlineColorNormal);
            if (outlineColorNew != textMesh.outlineColor) {
                textMesh.outlineColor = outlineColorNew;
            }
            
        }
    }


    public bool ShouldRenderCharacter(TMP_CharacterInfo info)
    {
        return (
            (info.character != ' ') &&
            (info.character != '\n') &&
            (info.bottomLeft.x < info.topRight.x) &&
            (info.bottomLeft.y < info.topRight.y));
    }
    

    public void UpdateColliders()
    {
        int colliderCount = 0;
        int colliderIndex = 0;

        if (textCollider) {
            colliderCount++;
        }

        // This must be called first before depending on textInfo being consistent. 
        textMesh.ForceMeshUpdate();

        TMP_TextInfo textInfo = textMesh.textInfo;
        int characterCount = textInfo.characterCount;
        TMP_CharacterInfo[] characterInfo = textInfo.characterInfo;

        if (characterColliders) {

            for (int i = 0;
                 i < characterCount;
                 i++) {

                TMP_CharacterInfo info = characterInfo[i];

                if (ShouldRenderCharacter(info)) {
                    colliderCount++;
                }

            }

        }

        SetColliderCount(colliderCount);

        if (textCollider) {

            Bounds bounds = textMesh.bounds;
            BoxCollider boxCollider = boxColliders[colliderIndex++];
            boxCollider.material = physicMaterial;

            if (boxCollider.center != Vector3.zero) {
                boxCollider.center = Vector3.zero;
            }

            Vector3 newSize =
                new Vector3(
                    bounds.size.x,
                    bounds.size.y,
                    colliderThickness);
            if (boxCollider.size != newSize) {
                boxCollider.size = newSize;
            }

        }

        if (characterColliders) {

            for (int i = 0;
                 i < characterCount;
                 i++) {

                TMP_CharacterInfo info = characterInfo[i];

                if (!ShouldRenderCharacter(info)) {
                    continue;
                }

                float left   = info.bottomLeft.x;
                float bottom = info.bottomLeft.y;
                float right  = info.topRight.x;
                float top    = info.topRight.y;

                BoxCollider boxCollider = boxColliders[colliderIndex++];

                Vector3 newCenter =
                    new Vector3(
                        (left + right) / 2.0f,
                        (top + bottom) / 2.0f,
                        0.0f);
                if (boxCollider.center != newCenter) {
                    boxCollider.center = newCenter;
                }

                Vector3 newSize =
                    new Vector3(
                        right - left,
                        top - bottom,
                        colliderThickness);
                if (boxCollider.size != newSize) {
                    boxCollider.size = newSize;
                }

            }

        }

    }


    public void SetColliderCount(int n)
    {
        if (boxColliders == null) {
            boxColliders = new List<BoxCollider>();
        }

        while (boxColliders.Count < n) {
            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            boxColliders.Add(collider);
            //Debug.Log("KineticText: SetColliderCount n: " + n + " Add count: " + boxColliders.Count);
        }

        int len;
        while ((len = boxColliders.Count) > n) {
            BoxCollider boxCollider = boxColliders[len - 1];
            boxColliders.RemoveAt(len - 1);
            Destroy(boxCollider);
            //Debug.Log("KineticText: SetColliderCount n: " + n + " Destroy count: " + boxColliders.Count + " boxCollider: " + boxCollider);
        }

    }
    

}


}

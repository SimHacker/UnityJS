////////////////////////////////////////////////////////////////////////
// Cuboid.cs
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


public enum CuboidFace {
    Top = 0,
    Bottom = 1,
    Front = 2,
    Back = 3,
    Right = 4,
    Left = 5,
};


namespace UnityJS {


public class Cuboid : Tracker {


    ////////////////////////////////////////////////////////////////////////
    // Instance variables


    public BoxCollider boxCollider;
    public Tile[] tiles = new Tile[6];
    public Vector3 cuboidSize = Vector3.one;
    public bool updateTiles;


    ////////////////////////////////////////////////////////////////////////
    // Instance Methods


    void Start()
    {
        updateTiles = true;
        UpdateTiles();
    }


    void Update()
    {
        UpdateTiles();
    }


    public void UpdateTiles()
    {
        if (!updateTiles) {
            return;
        }

        updateTiles = false;


        float cx = cuboidSize.x;
        float cy = cuboidSize.y;
        float cz = cuboidSize.z;

        if (tiles[(int)CuboidFace.Top   ] != null) {
            UpdateTile(tiles[(int)CuboidFace.Top   ], cx, cz, new Vector3( 0.0f,       0.5f * cy,  0.0f)      );
        }

        if (tiles[(int)CuboidFace.Bottom] != null) {
            UpdateTile(tiles[(int)CuboidFace.Bottom], cx, cz, new Vector3( 0.0f,      -0.5f * cy,  0.0f)      );
        }

        if (tiles[(int)CuboidFace.Front ] != null) {
            UpdateTile(tiles[(int)CuboidFace.Front ], cx, cy, new Vector3( 0.0f,       0.0f,       0.5f * cz) );
        }

        if (tiles[(int)CuboidFace.Back  ] != null) {
            UpdateTile(tiles[(int)CuboidFace.Back  ], cx, cy, new Vector3( 0.0f,       0.0f,      -0.5f * cz) );
        }

        if (tiles[(int)CuboidFace.Right ] != null) {
            UpdateTile(tiles[(int)CuboidFace.Right ], cz, cy, new Vector3( 0.5f * cx,  0.0f,       0.0f)      );
        }

        if (tiles[(int)CuboidFace.Left  ] != null) {
            UpdateTile(tiles[(int)CuboidFace.Left  ], cz, cy, new Vector3(-0.5f * cx,  0.0f,       0.0f)      );
        }

        boxCollider.size = cuboidSize;
    }


    public void UpdateTile(Tile tile, float x, float y, Vector3 zOffset)
    {
        tile.transform.localPosition =
            zOffset;
        tile.transform.localScale =
            new Vector3(
                x,
                y,
                1.0f);
    }


}


}

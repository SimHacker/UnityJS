////////////////////////////////////////////////////////////////////////
// arscript.js
// Don Hopkins, Ground Up Software.


////////////////////////////////////////////////////////////////////////
// Globals.


if (!window.globals) {
    window.globals = {};
}


////////////////////////////////////////////////////////////////////////
// Utilities.


////////////////////////////////////////////////////////////////////////
// Create everything.


function LoadObjects()
{
    console.log("arscript.js: LoadObjects");

    globals.ar = CreatePrefab({
        prefab: 'Prefabs/ARKitBridge',
        obj: {
        },
        update: {
        },
        interests: {
        }
    });

    globals.light = CreatePrefab({
        prefab: 'Prefabs/ARKitLight',
        obj: {
        },
        update: {
        },
        interests: {
        }
    });

    globals.pointCloud = CreatePrefab({
        prefab: 'Prefabs/ARKitPointCloud',
        obj: {
        },
        update: {
        },
        interests: {
        }
    });

}


////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////
// ARKitBridge.cs
// Copyright (C) 2019 by Don Hopkins, Ground Up Software.


#if USE_ARKIT


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.XR.iOS;


namespace UnityJS {


public enum PointerHitTest {
    None,
    Test3DCollider,
    TestARFeaturePoint,
    TestARHorizontalPlane,
    TestARExistingPlane,
    TestARExistingPlaneUsingExtent,
};


public class ARKitBridge : Tracker {


#if false


    ////////////////////////////////////////////////////////////////////////
    // Component references


    ////////////////////////////////////////////////////////////////////////
    // ARKitBridge properties


    public ARPlaneAnchor planeAnchor;
    public ARUserAnchor userAnchor;
    public Camera arCamera;
    public Transform arCameraBase;
    public Transform arCameraTranslation;
    public Transform arCameraRotation;
    public Transform arCameraSpace;
    public Transform arCameraHolder;
    public Rigidbody arCameraHolderRigidbody;
    public Transform arCameraBody;
    public Transform arCameraBackground;
    public Transform arCameraText;
    public Collider arCameraBackgroundCollider;
    public Transform pointer;
    public Collider pointerCollider;
    public Rigidbody pointerRigidbody;
    public FixedJoint fixedJoint;
    public UnityARSessionNativeInterface arSession;
    public ARTrackingState trackingState;
    public ARTrackingStateReason trackingReason;
    public float arCameraWorldScale = 1.0f;
    public string errorMessage = "";
	private Material savedClearMaterial;
    public bool cameraTracking = true;
    public Vector3 cameraPosition;
    public Quaternion cameraRotation;
    public Vector3 cameraDirection;
    public Vector3 cameraEulerAngles;
    public bool pointerTracking = true;
    public PointerHitTest pointerHitTest = PointerHitTest.None;
    public bool pointerHitTestResult = false;
    public RaycastHit pointerHitTestRaycastHit;
    public Collider pointerHitTestCollider;
    public ARHitTestResult pointerARHitTestResult;
    public float pointerScreenDistanceDefault = 2.0f;
    public float pointerScreenDistance = 2.0f;
    public Vector2 screenDimensions = new Vector3(0.24f, 0.186f, 0.0094f);
    public Vector2 pointerScreenSurfacePosition = new Vector2(0.5f, 0.5f);
    public Vector3 pointerScreenPoint;
    public Ray pointerScreenRay;
    public Vector3 pointerScreenPosition;
    public Quaternion pointerScreenRotation;
    public Vector3 pointerPosition;
    public Quaternion pointerRotation;
    public Quaternion pointerUprightRotation;
    public TextMeshPro topLeftText;
    public TextMeshPro topText;
    public TextMeshPro topRightText;
    public TextMeshPro leftText;
    public TextMeshPro centerText;
    public TextMeshPro rightText;
    public TextMeshPro bottomLeftText;
    public TextMeshPro bottomText;
    public TextMeshPro bottomRightText;


    ////////////////////////////////////////////////////////////////////////
    // Instance Methods


    public void Awake()
    {
        //Debug.Log("ARKitBridge: Awake: Setting Input.compensateSensors from " + Input.compensateSensors + " to false.");
        Input.compensateSensors = false;

        Quaternion quaternion = new Quaternion(
            0.00021143197955098f,
            -0.165264397859573f,
            -0.0001220703125f,
            -0.986249268054962f);
        Vector3 eulerAngles = quaternion.eulerAngles;
        Debug.Log("quaternion: " + quaternion.x + " " + quaternion.y + " " + quaternion.z + " " + quaternion.w + " eulerAngles: " + eulerAngles.x + " " + eulerAngles.y + " " + eulerAngles.z);
    }
    

    public void Start()
    {
        //Debug.Log("ARKitBridge: Start: Setting Input.compensateSensors from " + Input.compensateSensors + " to false.");
        Input.compensateSensors = false;

        UnityARSessionNativeInterface.ARFrameUpdatedEvent += FrameUpdate;
        UnityARSessionNativeInterface.ARAnchorAddedEvent += AddAnchor;
        UnityARSessionNativeInterface.ARAnchorUpdatedEvent += UpdateAnchor;
        UnityARSessionNativeInterface.ARAnchorRemovedEvent += RemoveAnchor;
        UnityARSessionNativeInterface.ARUserAnchorAddedEvent += AddUserAnchor;
        UnityARSessionNativeInterface.ARUserAnchorUpdatedEvent += UpdateUserAnchor;
        UnityARSessionNativeInterface.ARUserAnchorRemovedEvent += RemoveUserAnchor;
        UnityARSessionNativeInterface.ARSessionFailedEvent += SessionFailed;
        UnityARSessionNativeInterface.ARSessionInterruptedEvent += SessionInterrupted;
        UnityARSessionNativeInterface.ARSessioninterruptionEndedEvent += SessionInterruptionEnded;
        UnityARSessionNativeInterface.ARSessionTrackingChangedEvent += SessionTrackingChanged;

		arSession = UnityARSessionNativeInterface.GetARSessionNativeInterface();

#if !UNITY_EDITOR
		Application.targetFrameRate = 60;
        ARKitWorldTrackingSessionConfiguration config = new ARKitWorldTrackingSessionConfiguration();
        config.planeDetection = UnityARPlaneDetection.Horizontal;
        config.alignment = UnityARAlignment.UnityARAlignmentGravity;
        config.getPointCloudData = true;
        config.enableLightEstimation = true;
        arSession.RunWithConfig(config);
#endif

    }


    public void OnDestroy()
    {
        UnityARSessionNativeInterface.ARFrameUpdatedEvent -= FrameUpdate;
        UnityARSessionNativeInterface.ARAnchorAddedEvent -= AddAnchor;
        UnityARSessionNativeInterface.ARAnchorUpdatedEvent -= UpdateAnchor;
        UnityARSessionNativeInterface.ARAnchorRemovedEvent -= RemoveAnchor;
        UnityARSessionNativeInterface.ARUserAnchorAddedEvent -= AddUserAnchor;
        UnityARSessionNativeInterface.ARUserAnchorUpdatedEvent -= UpdateUserAnchor;
        UnityARSessionNativeInterface.ARUserAnchorRemovedEvent -= RemoveUserAnchor;
        UnityARSessionNativeInterface.ARSessionFailedEvent -= SessionFailed;
        UnityARSessionNativeInterface.ARSessionInterruptedEvent -= SessionInterrupted;
        UnityARSessionNativeInterface.ARSessioninterruptionEndedEvent -= SessionInterruptionEnded;
        UnityARSessionNativeInterface.ARSessionTrackingChangedEvent -= SessionTrackingChanged;
    }


    public void Update()
    {
        if (Input.compensateSensors) {
            //Debug.Log("ARKitBridge: Update: RE-Setting Input.compensateSensors from " + Input.compensateSensors + " to false.");
            Input.compensateSensors = false;
        }

        TrackCamera();
        TrackPointerPosition();
    }
    

    public void TrackCamera()
    {
        if (!cameraTracking) {
            return;
        }

        cameraPosition = arCameraHolder.position;
        cameraRotation = arCameraHolder.rotation;
        cameraEulerAngles = arCameraHolder.rotation.eulerAngles;
        cameraDirection = arCameraHolder.TransformDirection(Vector3.forward);
    }
    

    public virtual void TrackPointerPosition()
    {
        if (!pointerTracking) {
            return;
        }

        Vector2 mousePos = 
            Input.mousePosition;

        pointerScreenSurfacePosition = 
            new Vector2(
                mousePos.x / Screen.width,
                mousePos.y / Screen.height);

        pointerScreenPoint = 
            new Vector3(
                pointerScreenSurfacePosition.x * Screen.width,
                pointerScreenSurfacePosition.y * Screen.height,
                0.0f);

        pointerScreenRotation = 
            arCameraHolder.rotation;

        pointerScreenRay =
            arCamera.ScreenPointToRay(
                pointerScreenPoint);

        switch (pointerHitTest) {

            case PointerHitTest.None:
                pointerHitTestResult = false;
                pointerScreenDistance = pointerScreenDistanceDefault;
                break;

            case PointerHitTest.Test3DCollider:
                int layerMask =
                    ~(1<<2); // Exclude "Ignore Raycast" layer 2.
                pointerHitTestResult =
                    Physics.Raycast(
                        pointerScreenRay,
                        out pointerHitTestRaycastHit,
                        Mathf.Infinity,
                        layerMask,
                        QueryTriggerInteraction.Ignore);
                if (pointerHitTestResult) {
                    pointerScreenDistance = pointerHitTestRaycastHit.distance;
                } else {
                    pointerScreenDistance = pointerScreenDistanceDefault;
                }
                break;

            case PointerHitTest.TestARFeaturePoint:
                pointerHitTestResult = 
                    ARHitTest(
                        pointerScreenSurfacePosition, 
                        ARHitTestResultType.ARHitTestResultTypeFeaturePoint, 
                        pointerScreenDistanceDefault, 
                        out pointerScreenDistance);
                break;

            case PointerHitTest.TestARHorizontalPlane:
                pointerHitTestResult = 
                    ARHitTest(
                        pointerScreenSurfacePosition, 
                        ARHitTestResultType.ARHitTestResultTypeHorizontalPlane, 
                        pointerScreenDistanceDefault, 
                        out pointerScreenDistance);
                break;

            case PointerHitTest.TestARExistingPlane:
                pointerHitTestResult = 
                    ARHitTest(
                        pointerScreenSurfacePosition, 
                        ARHitTestResultType.ARHitTestResultTypeExistingPlane, 
                        pointerScreenDistanceDefault, 
                        out pointerScreenDistance);
                break;

            case PointerHitTest.TestARExistingPlaneUsingExtent:
                pointerHitTestResult = 
                    ARHitTest(
                        pointerScreenSurfacePosition, 
                        ARHitTestResultType.ARHitTestResultTypeExistingPlaneUsingExtent, 
                        pointerScreenDistanceDefault, 
                        out pointerScreenDistance);
                break;

            default:
                Debug.LogError("ARKitBridge: TrackPointerPosition unexpected PointerHitTest: " + pointerHitTest);
                pointerHitTestResult = false;
                pointerScreenDistance = pointerScreenDistanceDefault;
                break;
        }

        pointerPosition =
            pointerScreenRay.GetPoint(
                pointerScreenDistance);
        pointerRotation = 
            pointerScreenRotation;
        pointerUprightRotation =
            Quaternion.Euler(
                0, 
                pointerRotation.eulerAngles.y, 
                0);
#if false
        pointer.position =
            pointerPosition;
#else
        pointerRigidbody.MovePosition(pointerPosition);
#endif

        //Debug.Log("ARKitBridge: TrackPointerPosition: mousePosition: " + mousePosition.x + " " + mousePosition.y + " pointerScreenSurfacePosition: " + pointerScreenSurfacePosition.x + " " + pointerScreenSurfacePosition.y + " pointerScreenPoint: " + pointerScreenPoint.x + " " + pointerScreenPoint.y + " " + pointerScreenPoint.z + " pointerScreenDistance: " + pointerScreenDistance + " pointerPosition: " + pointerPosition.x + " " + pointerPosition.y + " " + pointerPosition.z);

        Debug.DrawLine(pointerScreenPosition, pointerPosition, Color.blue, 0.1f, false);
    }


    public bool ARHitTest(Vector2 screenPosition, ARHitTestResultType hitTestResultType, float defaultDistance, out float distance)
    {
        ARPoint arPoint = new ARPoint();
        arPoint.x = screenPosition.x;
        arPoint.y = screenPosition.y;

        List<ARHitTestResult> hitResults =
            UnityARSessionNativeInterface.GetARSessionNativeInterface().HitTest(arPoint, hitTestResultType);
        int hitResultsCount = hitResults.Count;
        if (hitResultsCount == 0) {
            distance = defaultDistance;
            return false;
        }

        distance = 1.0e+6f;
        for (int index = 0; index < hitResultsCount; index++) {
            ARHitTestResult hitResult = hitResults[index];
            if (hitResult.distance < distance) {
                distance = (float)hitResult.distance;
                pointerARHitTestResult = hitResult;
            }
        }

        return true;
    }
    

	public void SetCamera(Camera newCamera)
	{
		if (arCamera != null) {
			UnityARVideo oldARVideo = arCamera.gameObject.GetComponent<UnityARVideo>();
			if (oldARVideo != null) {
				savedClearMaterial = oldARVideo.m_ClearMaterial;
				Destroy(oldARVideo);
			}
		}
		SetupNewCamera (newCamera);
	}


	private void SetupNewCamera(Camera newCamera)
	{
		arCamera = newCamera;

        if (arCamera != null) {

            UnityARVideo unityARVideo = arCamera.gameObject.GetComponent<UnityARVideo>();

            if (unityARVideo != null) {
                savedClearMaterial = unityARVideo.m_ClearMaterial;
                Destroy (unityARVideo);
            }

            unityARVideo = arCamera.gameObject.AddComponent<UnityARVideo>();
            unityARVideo.m_ClearMaterial = savedClearMaterial;

        }
	}

	// Update is called once per frame

	void FixedUpdate() {
		
#if !UNITY_EDITOR
        Matrix4x4 matrix = arSession.GetCameraPose();

        Vector3 pos = UnityARMatrixOps.GetPosition(matrix);
        Vector3 worldPos = arCameraSpace.TransformPoint(pos);
        Quaternion rot = UnityARMatrixOps.GetRotation(matrix);
        Quaternion direction = Quaternion.Euler(0, arCameraSpace.eulerAngles.y, 0);
        Quaternion worldRot = direction * rot;

        worldPos *= arCameraWorldScale;

        //Debug.Log("WovenARCameraManager: FixedUpdate: pos: " + pos.x + " " + pos.y + " " + pos.z + " rot: " + rot.x + " " + rot.y + " " + rot.z + " worldPos: " + worldPos.x + " " + worldPos.y + " " + worldPos.x + " worldRot: " + worldRot.x + " " + worldRot.y + " " + worldRot.z);

        arCameraHolderRigidbody.MovePosition(worldPos);
        arCameraHolderRigidbody.MoveRotation(worldRot);

        arCamera.projectionMatrix = arSession.GetCameraProjection();
#endif

	}


    public void FrameUpdate(UnityARCamera unityARCamera)
    {
        //Debug.Log("ARKitBridge: FrameUpdate: unityARCamera: " + unityARCamera);
    }


    public override void HandleMessage(fsData messageData)
    {
        base.HandleMessage(messageData);

        var messageDict = messageData.AsDictionary;
        string message = messageDict["message"].AsString;

        //Debug.Log("ARKitBridge: HandleMessage: message: " + message, this);

        switch (message) {

            case "HitTest":
                HitTest(messageData);
                break;

        }

    }


    public void HitTest(fsData messageData)
    {
        //Debug.Log("ARKitBridge: HitTest: messageData: " + messageData);

        if (!messageData.IsDictionary) {
            return;
        }

        var messageDict = messageData.AsDictionary;
        Vector2 screenPoint = BridgeManager.GetVector2(messageDict, "screenPoint");
        ARHitTestResultType resultTypes = BridgeManager.GetEnum<ARHitTestResultType>(messageDict, "resultTypes");
        string callbackID = BridgeManager.GetString(messageDict, "callbackID");

        fsData results = fsData.CreateDictionary();
        var resultsDict = results.AsDictionary;

        resultsDict["resultTypes"] = messageDict["resultTypes"];

        ARPoint arPoint = new ARPoint();
        arPoint.x = screenPoint.x;
        arPoint.y = screenPoint.y;

        List<ARHitTestResult> hitResults =
            UnityARSessionNativeInterface.GetARSessionNativeInterface().HitTest(arPoint, resultTypes);

        fsData hitTestResults = fsData.Null;
        if (!BridgeManager.ConvertFromList(hitResults, ref hitTestResults)) {
            Debug.LogError("ARKitBridge: HitTest: error converting List<ARHitTestResult> to json.");
        }
        resultsDict["hitTestResults"] = hitTestResults;

        //Debug.Log("ARKitBridge: HitTest: results: " + results);

        bridgeManager.SendCallbackDataToJS(callbackID, results);
    }


    public void AddAnchor(ARPlaneAnchor arPlaneAnchor)
    {
        //Debug.Log("ARKitBridge: AddAnchor: arPlaneAnchor: " + arPlaneAnchor);

        planeAnchor = arPlaneAnchor;
        SendEventData("AddAnchor");
    }

    public void UpdateAnchor(ARPlaneAnchor arPlaneAnchor)
    {
        //Debug.Log("ARKitBridge: UpdateAnchor: arPlaneAnchor: " + arPlaneAnchor);

        planeAnchor = arPlaneAnchor;
        SendEventData("UpdateAnchor");
    }


    public void RemoveAnchor(ARPlaneAnchor arPlaneAnchor)
    {
        //Debug.Log("ARKitBridge: RemoveAnchor: arPlaneAnchor: " + arPlaneAnchor);

        planeAnchor = arPlaneAnchor;
        SendEventData("RemoveAnchor");
    }


    public void AddUserAnchor(ARUserAnchor arUserAnchor)
    {
        //Debug.Log("ARKitBridge: AddUserAnchor: arUserAnchor: " + arUserAnchor);

        userAnchor = arUserAnchor;
        SendEventData("AddUserAnchor");
    }


    public void UpdateUserAnchor(ARUserAnchor arUserAnchor)
    {
        //Debug.Log("ARKitBridge: UpdateUserAnchor: arUserAnchor: " + arUserAnchor);

        userAnchor = arUserAnchor;
        SendEventData("UpdateUserAnchor");
    }


    public void RemoveUserAnchor(ARUserAnchor arUserAnchor)
    {
        //Debug.Log("ARKitBridge: RemoveUserAnchor: arUserAnchor: " + arUserAnchor);

        userAnchor = arUserAnchor;
        SendEventData("RemoveUserAnchor");
    }


    public void SessionFailed(string error)
    {
        //Debug.Log("ARKitBridge: SessionFailed: error: " + error);
        errorMessage = error;
        SendEventData("SessionFailed");
    }
    

    public void SessionInterrupted()
    {
        //Debug.Log("ARKitBridge: SessionInterrupted");
        SendEventData("SessionInterrupted");
    }
    

    public void SessionInterruptionEnded()
    {
        //Debug.Log("ARKitBridge: SessionInterruptionEnded");
        SendEventData("SessionInterruptionEnded");
    }
    

    public void SessionTrackingChanged(UnityARCamera unityARCamera)
    {
        //Debug.Log("ARKitBridge: SessionTrackingChanged: unityARCamera: " + unityARCamera + " trackingState: " + unityARCamera.trackingState + " trackingReason: " + unityARCamera.trackingReason);

        trackingState = unityARCamera.trackingState;
        trackingReason = unityARCamera.trackingReason;

        SendEventData("SessionTrackingStateChanged");
    }
    

#endif


}


}


#endif

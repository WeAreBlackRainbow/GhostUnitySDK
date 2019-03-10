using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;

public class GhostCameraManager : MonoBehaviour {

	public Camera m_camera;
	private UnityARSessionNativeInterface m_session;
	private Material savedClearMaterial;

	[Header("AR Config Options")]
	public UnityARAlignment startAlignment = UnityARAlignment.UnityARAlignmentGravity;
	public UnityARPlaneDetection planeDetection = UnityARPlaneDetection.Horizontal;
	public bool getPointCloud = true;
	public bool enableLightEstimation = true;
	public bool enableAutoFocus = true;
	public UnityAREnvironmentTexturing environmentTexturing = UnityAREnvironmentTexturing.UnityAREnvironmentTexturingNone;

	[Header("Image Tracking")]
	public ARReferenceImagesSet detectionImages = null;
	public int maximumNumberOfTrackedImages = 0;

	[Header("Object Tracking")]
	public ARReferenceObjectsSetAsset detectionObjects = null;
	private bool sessionStarted = false;

	[Header("Ghost Improved Tracking (Beta)")]
	public bool GhostTrackingEnabled = false;

	private Vector3 prevRotation = new Vector3 ();
	private Vector3 prevLocation = new Vector3 ();

	public ARKitWorldTrackingSessionConfiguration sessionConfiguration
	{
		get
		{
			ARKitWorldTrackingSessionConfiguration config = new ARKitWorldTrackingSessionConfiguration ();
			config.planeDetection = planeDetection;
			config.alignment = startAlignment;
			config.getPointCloudData = getPointCloud;
			config.enableLightEstimation = enableLightEstimation;
			config.enableAutoFocus = enableAutoFocus;
			config.maximumNumberOfTrackedImages = maximumNumberOfTrackedImages;
			config.environmentTexturing = environmentTexturing;
			if (detectionImages != null)
				config.referenceImagesGroupName = detectionImages.resourceGroupName;

			if (detectionObjects != null) 
			{
				config.referenceObjectsGroupName = "";  //lets not read from XCode asset catalog right now
				config.dynamicReferenceObjectsPtr = m_session.CreateNativeReferenceObjectsSet(detectionObjects.LoadReferenceObjectsInSet());
			}

			return config;
		}
	}

	// Use this for initialization
	void Start () {

		m_session = UnityARSessionNativeInterface.GetARSessionNativeInterface();

		Application.targetFrameRate = 60;

		var config = sessionConfiguration;
		if (config.IsSupported) {
			m_session.RunWithConfig (config);
			UnityARSessionNativeInterface.ARFrameUpdatedEvent += FirstFrameUpdate;
		}

		if (m_camera == null) {
			m_camera = Camera.main;
		}
	}

	void OnDestroy()
	{
		m_session.Pause();
	}

	void FirstFrameUpdate(UnityARCamera cam)
	{
		sessionStarted = true;
		UnityARSessionNativeInterface.ARFrameUpdatedEvent -= FirstFrameUpdate;
	}

	public void SetCamera(Camera newCamera)
	{
		if (m_camera != null) {
			UnityARVideo oldARVideo = m_camera.gameObject.GetComponent<UnityARVideo> ();
			if (oldARVideo != null) {
				savedClearMaterial = oldARVideo.m_ClearMaterial;
				Destroy (oldARVideo);
			}
		}
		SetupNewCamera (newCamera);
	}

	private void SetupNewCamera(Camera newCamera)
	{
		m_camera = newCamera;

		if (m_camera != null) {
			UnityARVideo unityARVideo = m_camera.gameObject.GetComponent<UnityARVideo> ();
			if (unityARVideo != null) {
				savedClearMaterial = unityARVideo.m_ClearMaterial;
				Destroy (unityARVideo);
			}
			unityARVideo = m_camera.gameObject.AddComponent<UnityARVideo> ();
			unityARVideo.m_ClearMaterial = savedClearMaterial;
		}
	}

	// Update is called once per frame

	void Update () {

		if (m_camera != null && sessionStarted)
		{
			// JUST WORKS!

			if (GhostTrackingEnabled) {
				Matrix4x4 matrix = m_session.GetCameraPose();

				Vector3 pos = UnityARMatrixOps.GetPosition (matrix);
				Vector3 pos2 = pos;
				pos2.x += (pos.x - prevLocation.x)*4.0f;
				pos2.y += (pos.y - prevLocation.y)*4.0f;
				pos2.z += (pos.z - prevLocation.z)*4.0f;
				Vector3 pos3 = m_camera.transform.localPosition;
				pos3.x = m_camera.transform.localPosition.x + (pos2.x - m_camera.transform.localPosition.x)*0.8f;
				pos3.y = m_camera.transform.localPosition.y + (pos2.y - m_camera.transform.localPosition.y)*0.8f;
				pos3.z = m_camera.transform.localPosition.z + (pos2.z - m_camera.transform.localPosition.z)*0.8f;
				m_camera.transform.localPosition = pos;
				prevLocation = pos;

				Vector3 rot = UnityARMatrixOps.GetRotation (matrix).eulerAngles;
				Vector3 rot2 = rot;
				rot2.x += (rot.x - prevRotation.x)*4.0f;
				rot2.y += (rot.y - prevRotation.y)*4.0f;
				rot2.z += (rot.z - prevRotation.z)*4.0f;
				Vector3 rot3 = m_camera.transform.localEulerAngles;
				rot3.x = m_camera.transform.localEulerAngles.x + (rot2.x - m_camera.transform.localEulerAngles.x)*0.8f;
				rot3.y = m_camera.transform.localEulerAngles.y + (rot2.y - m_camera.transform.localEulerAngles.y)*0.8f;
				rot3.z = m_camera.transform.localEulerAngles.z + (rot2.z - m_camera.transform.localEulerAngles.z)*0.8f;
				m_camera.transform.localEulerAngles = rot3;
				prevRotation = rot;

				m_camera.projectionMatrix = m_session.GetCameraProjection ();
			} else {
				Matrix4x4 matrix = m_session.GetCameraPose ();
				m_camera.transform.localPosition = UnityARMatrixOps.GetPosition (matrix);
				m_camera.transform.localRotation = UnityARMatrixOps.GetRotation (matrix);

				m_camera.projectionMatrix = m_session.GetCameraProjection ();
			}
		}

	}

}

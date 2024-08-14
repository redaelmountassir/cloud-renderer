/*
 * Copyright (c) Hubbahu
 */

using UnityEngine;
using UnityEditor;

[AddComponentMenu("Player/Player Camera/FPS Camera")]
[DisallowMultipleComponent]
public class FPSCamController : CameraComponent
{
	[Tooltip("Recommended that both values are the same")]
	public float rotSnappiness = 5;
	[Range(-180f, 180f)]
	public float minTiltDegrees = -90, maxTiltDegrees = 90;
	float pitch, yaw;
	float desiredPitch, desiredYaw;

	private void Start()
	{
		//setting variables default values
		yaw = transform.eulerAngles.y;
		desiredYaw = yaw;
	}

	public void RotateCharacter(Vector2 moveDelta)
	{
		desiredPitch -= moveDelta.y;
		desiredYaw += moveDelta.x;
		//clamp head tilt
		desiredPitch = Mathf.Clamp(desiredPitch, minTiltDegrees, maxTiltDegrees);

		//rotation smoothing
		pitch = Mathf.Lerp(pitch, desiredPitch, rotSnappiness * Time.deltaTime);
		yaw = Mathf.Lerp(yaw, desiredYaw, rotSnappiness * Time.deltaTime);

		//set rotation
		playerCam.player.transform.eulerAngles = new Vector3(0f, yaw, 0f);
		transform.localEulerAngles = new Vector3(pitch, 0f, 0f);
	}
} 

[CustomEditor(typeof(FPSCamController))]
public class FPSCameraEditor : Editor
{
	public override void OnInspectorGUI()
	{
		FPSCamController camController = (FPSCamController)target;

		camController.playerCam = (PlayerCamera)EditorGUILayout.ObjectField("Player Camera", camController.playerCam, typeof(PlayerCamera), true);

		EditorGUILayout.Space();

		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Rotation Snappiness", EditorStyles.boldLabel);
		camController.rotSnappiness = EditorGUILayout.FloatField(camController.rotSnappiness);
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space();

		EditorGUILayout.LabelField("Min and Max Tilt Degrees", EditorStyles.boldLabel);
		EditorGUILayout.BeginHorizontal();
		camController.minTiltDegrees = EditorGUILayout.FloatField(camController.minTiltDegrees);
		EditorGUILayout.MinMaxSlider(ref camController.minTiltDegrees, ref camController.maxTiltDegrees, -180, 180);
		camController.maxTiltDegrees = EditorGUILayout.FloatField(camController.maxTiltDegrees);
		EditorGUILayout.EndHorizontal();
	}
}
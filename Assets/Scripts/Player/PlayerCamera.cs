/*
 * Copyright (c) Hubbahu
 */

using UnityEngine;
using UnityEditor;
using System;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Player/Player Camera/Player Camera")]
public class PlayerCamera : MonoBehaviour
{
	public Player player; 
	public new Camera camera;
    public CameraComponents components;

	[Serializable]
	public struct CameraComponents
	{
		public CameraComponent[] components;

		public FPSCamController fpsCamController;

		public void UpdateCamComponents(PlayerCamera playerCam)
		{
			if (playerCam.camera)
				Debug.LogWarning("No Camera Provided!");

			components = playerCam.GetComponents<CameraComponent>();

			foreach (CameraComponent component in components)
			{
				component.playerCam = playerCam;
				switch (component)
				{
					case FPSCamController fpsCamController:
						this.fpsCamController = fpsCamController;
						break;
					default:
						break;
				}
			}
		}
	}
}

[RequireComponent(typeof(PlayerCamera))]
public class CameraComponent : MonoBehaviour
{
	[Tooltip("The playerCamera asscociated with this component. Can be assigned manually or through the player component.")]
	public PlayerCamera playerCam;

	private void Awake()
	{
		if (playerCam)
			return;
		Debug.LogWarning("No PlayerCam was found on CameraComponent of type: " + GetType());
		Destroy(this);
	}
}

[CustomEditor(typeof(PlayerCamera))]
class PlayerCamEditor : Editor
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		GUILayout.Space(25);
		PlayerCamera playerCamera = (PlayerCamera)target;
		if (GUILayout.Button("Find the Player with Player tag"))
			playerCamera.player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
		if (GUILayout.Button("Find the Main Camera"))
			playerCamera.camera = Camera.main;
		if (GUILayout.Button("Find All Camera Components"))
			playerCamera.components.UpdateCamComponents(playerCamera);
	}
}
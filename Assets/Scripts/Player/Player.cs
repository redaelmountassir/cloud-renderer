/*
 * Copyright (c) Hubbahu
 */

using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;

[DisallowMultipleComponent]
[AddComponentMenu("Player/Player")]
public sealed class Player : MonoBehaviour
{
	static List<Player> players;
	public PlayerCamera playerCam;

	public PlayerComponents components;

	private void Awake()
	{
		//Ensure that there is a player cam component
		if (!playerCam)
			Debug.LogError("No Player Cam Found");

		//Add player to list of current players
		if (players == null)
			players = new List<Player>();
		players.Add(this);
	}

	[Serializable]
	public struct PlayerComponents
	{
		public PlayerComponent[] components;

		public LocomotionController movement;
		public InputManager input;

		public void UpdatePlayerComponents(Player player)
		{
			components = player.gameObject.GetComponents<PlayerComponent>();

			foreach (PlayerComponent component in components)
			{
				component.player = player;
				switch (component)
				{
					case LocomotionController movement:
						this.movement = movement;
						break;
					case InputManager input:
						this.input = input;
						break;
				}
			}
		}
	}
}

[RequireComponent(typeof(Player))]
public class PlayerComponent : MonoBehaviour
{
	[Tooltip("The player asscociated with this component. Can be assigned manually or through the player component.")]
	public Player player;

	private void Awake()
	{
		if (player)
			return;
		Debug.LogWarning("No Player was found on PlayerComponent of type: " + GetType());
		Destroy(this);
	}
}

[CustomEditor(typeof(Player))]
class PlayerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		GUILayout.Space(25);
		Player player = (Player)target;
		if(GUILayout.Button("Find PlayerCam on Main Camera"))
			player.playerCam = Camera.main.GetComponent<PlayerCamera>();
		if (GUILayout.Button("Find All Player Components"))
			player.components.UpdatePlayerComponents(player);
	}
}
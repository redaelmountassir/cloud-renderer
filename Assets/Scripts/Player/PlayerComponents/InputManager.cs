/*
 * Copyright (c) Hubbahu
 */

using UnityEditor;
using UnityEngine;
using System;

[AddComponentMenu("Player/Input")]
[DisallowMultipleComponent]
public class InputManager : PlayerComponent
{
    #region Variables
    [Header("Toggle Modes")]
    public bool SprintToggleMode;
    public bool CrouchToggleMode;
    public bool ZoomToggleMode;

    #region Presets

    [Header("Input Presets")]
    public InputPreset keyboardAndMouse;
    public InputPreset controller;
    public InputDevice currentInputDevice;
    public enum InputDevice { Keyboard, Controller }
    InputPreset CurrentPreset
	{
		get
		{
			switch (currentInputDevice)
			{
				case InputDevice.Keyboard:
					return keyboardAndMouse;
				case InputDevice.Controller:
					return controller;
			}
            return keyboardAndMouse;
		}
	}
	#endregion

	[Header("Extra Input Data")]
    public bool ignoreViewInput;
    Vector2 viewMovement;
    [HideInInspector]
    public Vector2 rawMovementInput;
    [HideInInspector]
    public bool jump;
    public bool reverseScroll;
    bool uiMode;
    public bool UIMode {
        get => uiMode;
        set
        {
            if (value) UnlockCursor();
            else LockCursor();
            uiMode = value;
        }
    }
    int selectedSlot = 0;
    bool blockGamePlayInput;
    #endregion

    void Start()
    {
        if (!keyboardAndMouse || !controller)
        {
            Debug.LogError("Presets Unassigned");
            Destroy(this);
        }
        /*        PlayerInfo.Player.Stats.OnDeath += UnlockCursor;
                PlayerInfo.Player.Stats.OnDeath += BlockGameplayInput;
                PlayerInfo.Player.Stats.OnSleep += UnlockCursor;
                PlayerInfo.Player.Stats.OnSleep += BlockGameplayInput;
                PlayerInfo.Player.Stats.OnRespawn += LockCursor;
                PlayerInfo.Player.Stats.OnRespawn += AllowGameplayInput;
                PlayerInfo.Player.Stats.OnWake += LockCursor;
                PlayerInfo.Player.Stats.OnWake += AllowGameplayInput;*/
        ChangeLockState(true);
    }
    void Update()
    {
        if (!blockGamePlayInput)
        {
            if (player.components.movement)
                GetMovementInput();
            GetInventoryAndInteractionInput();
        }

        if (player.components.movement)
            //player.components.movement.ApplyMovement(rawMovementInput, jump);
        if (player.playerCam.components.fpsCamController)
            player.playerCam.components.fpsCamController.RotateCharacter(viewMovement);
    }

    #region Movement
    void GetMovementInput()
    {
        if (!UIMode)
        {
            rawMovementInput = CurrentPreset.MoveDirection;

            viewMovement = ignoreViewInput ? Vector2.zero : CurrentPreset.ViewDelta;

            TestForZoomInput();

            TestForCrouchInput();
            TestForSprintInput();

            TestForJumpInput();
        }
        else
        {
            rawMovementInput = Vector3.zero;
            viewMovement = Vector2.zero;
        }
    }
    void TestForCrouchInput()
    {
        //crouch & sprint
        if (CrouchToggleMode)
        {
            //if (InputPreset.CheckAllDown(CurrentPreset.crouch))
                //player.components.movement.TryChangeCrouch();
        }
        //else
            //player.components.movement.TrySetCrouch(InputPreset.CheckAll(CurrentPreset.crouch));
    }
    void TestForSprintInput()
    {
        if (SprintToggleMode)
        {
            //if (InputPreset.CheckAllDown(CurrentPreset.sprint))
                //player.components.movement.ChangeSprint();
        }
        //else
            //player.components.movement.SetSprint(InputPreset.CheckAll(CurrentPreset.sprint));
    }
    void TestForJumpInput()
    {
        rawMovementInput.y = Input.GetKeyDown(CurrentPreset.jump) ? 1f : 0f;
    }

    void TestForZoomInput()
    {
        /*        //zooms
                PlayerInfo.Player.PlayerCam.Zoom = ZoomToggleMode && Input.GetKeyDown(currentPreset.zoomButton)
                    ? !PlayerInfo.Player.PlayerCam.Zoom
                    : Input.GetKey(currentPreset.zoomButton);*/
    }
    #endregion

    void GetInventoryAndInteractionInput()
    {/*
        if (Input.GetKeyDown(currentPreset.inventoryButton))
            player.Player.Interaction.TryOpenOrCloseInventory();
        else if(!UIMode)
        {
            if (Input.GetKey(currentPreset.use))
                PlayerInfo.Player.Inventory.Use(Input.GetKeyDown(currentPreset.use) ? Slot.UseType.Press : Slot.UseType.Hold);
            else if(Input.GetKeyUp(currentPreset.use))
                PlayerInfo.Player.Inventory.Use(Slot.UseType.Release);
            else if (CheckForSlotInput())
                PlayerInfo.Player.Inventory.SetSelectedSlot(selectedSlot);

            if (Input.GetKey(currentPreset.throwItem))
                PlayerInfo.Player.Inventory.ChargeThrow(Input.GetKeyDown(currentPreset.throwItem));
            else if (Input.GetKeyUp(currentPreset.throwItem))
                PlayerInfo.Player.Inventory.Throw();

            if (Input.GetKeyDown(currentPreset.interact))
                PlayerInfo.Player.Interaction.Interact();
        }
        if (Input.GetKeyDown(currentPreset.exitMenu))
            PlayerInfo.Player.Interaction.CancelInteraction();*/
    }
    bool CheckForSlotInput()
    {
        if (Input.mouseScrollDelta.y > 0f)
        {
            if (reverseScroll) selectedSlot--; else selectedSlot++;
            if (selectedSlot < 0) selectedSlot = 6;
            else if (selectedSlot > 6) selectedSlot = 0;
        }
        else if (Input.mouseScrollDelta.y < 0f)
        {
            if (reverseScroll) selectedSlot++; else selectedSlot--;
            if (selectedSlot < 0) selectedSlot = 6;
            else if (selectedSlot > 6) selectedSlot = 0;
        }
        else if (InputPreset.CheckAllDown(CurrentPreset.inventorySlot1))
            selectedSlot = 0;
        else if (InputPreset.CheckAllDown(CurrentPreset.inventorySlot2))
            selectedSlot = 1;
        else if (InputPreset.CheckAllDown(CurrentPreset.inventorySlot3))
            selectedSlot = 2;
        else if (InputPreset.CheckAllDown(CurrentPreset.inventorySlot4))
            selectedSlot = 3;
        else if (InputPreset.CheckAllDown(CurrentPreset.inventorySlot5))
            selectedSlot = 4;
        else if (InputPreset.CheckAllDown(CurrentPreset.inventorySlot6))
            selectedSlot = 5;
        else if (InputPreset.CheckAllDown(CurrentPreset.inventorySlot7))
            selectedSlot = 6;
        else
            return false;
        return true;
    }

    #region Universal Input Funcs
    void ChangeLockState(bool state)
    {
        if (state)
            LockCursor();
        else
            UnlockCursor();
    }
    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }
    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
    }
    void BlockGameplayInput()
    {
        blockGamePlayInput = true;
    }
    void AllowGameplayInput()
    {
        blockGamePlayInput = false;
    }
    #endregion

    [CreateAssetMenu(fileName = "Input Preset", menuName = "InputPreset")]
    public class InputPreset : ScriptableObject
	{
        public enum MoveDirMode { FourButtons, Axis };
        [HideInInspector]
        public MoveDirMode moveDirMode;
        public Vector2 MoveDirection
		{
			get
			{
                Vector2 movementVector = Vector3.zero;
                switch (moveDirMode)
				{
					case MoveDirMode.FourButtons:
                        if (CheckAll(forward))
                            movementVector.y += 1;
                        if (CheckAll(backward))
                            movementVector.y -= 1;
                        if (CheckAll(strafeLeft))
                            movementVector.x -= 1f;
                        if (CheckAll(strafeRight))
                            movementVector.x += 1;
                        break;
					case MoveDirMode.Axis:
                        movementVector = new Vector2(Input.GetAxis(axisX), Input.GetAxis(axisY));
                        break;
				}
				return movementVector;
			}
		}

        [HideInInspector]
        public KeyCode[] 
            forward = new KeyCode[] { KeyCode.UpArrow, KeyCode.W },
            backward = new KeyCode[] { KeyCode.DownArrow, KeyCode.S },
            strafeLeft = new KeyCode[] { KeyCode.LeftArrow, KeyCode.A },
            strafeRight = new KeyCode[] { KeyCode.RightArrow, KeyCode.D };
        [HideInInspector]
        public string axisX, axisY;

        public float sensitivity = 100;
        public Vector2 ViewDelta => new Vector2(Input.GetAxis(viewAxisX), Input.GetAxis(viewAxisY)) * Time.deltaTime * sensitivity;
		public string viewAxisX = "Mouse X", viewAxisY = "Mouse Y";

		public KeyCode jump = KeyCode.Space;
        public KeyCode[] sprint = new KeyCode[] { KeyCode.RightShift, KeyCode.LeftShift };
        public KeyCode[] crouch = new KeyCode[] { KeyCode.RightControl, KeyCode.LeftControl };
        public KeyCode zoom = KeyCode.Z;
        public KeyCode settings = KeyCode.Tab;
        public KeyCode inventory = KeyCode.I;
        //The keycode for Inventory slots
        public KeyCode[] 
            inventorySlot1 = new KeyCode[] { KeyCode.Alpha1, KeyCode.Keypad1 },
            inventorySlot2 = new KeyCode[] { KeyCode.Alpha2, KeyCode.Keypad2 },
            inventorySlot3 = new KeyCode[] { KeyCode.Alpha3, KeyCode.Keypad3 },
            inventorySlot4 = new KeyCode[] { KeyCode.Alpha4, KeyCode.Keypad4 },
            inventorySlot5 = new KeyCode[] { KeyCode.Alpha5, KeyCode.Keypad5 },
            inventorySlot6 = new KeyCode[] { KeyCode.Alpha6, KeyCode.Keypad6 },
            inventorySlot7 = new KeyCode[] { KeyCode.Alpha7, KeyCode.Keypad7 };
        public KeyCode use = KeyCode.Mouse0;
        public KeyCode interact = KeyCode.Mouse1;
        public KeyCode throwItem = KeyCode.E;
        public KeyCode exitMenu = KeyCode.Escape;

        public static bool CheckAll(KeyCode[] buttons)
		{
            foreach (KeyCode button in buttons)
                if (Input.GetKey(button))
                    return true;
            return false;
		}
        public static bool CheckAllDown(KeyCode[] buttons)
        {
            foreach (KeyCode button in buttons)
                if (Input.GetKeyDown(button))
                    return true;
            return false;
        }
        public static bool CheckAllUp(KeyCode[] buttons)
        {
            foreach (KeyCode button in buttons)
                if (Input.GetKeyUp(button))
                    return true;
            return false;
        }
    }

    [CustomEditor(typeof(InputPreset))]
    public class InputPresetEditor : Editor
	{
        bool collapsed;
        bool collapsed1;
        bool collapsed2;
        bool collapsed3;

        public override void OnInspectorGUI()
		{
            InputPreset inputPreset = (InputPreset)target;
            inputPreset.moveDirMode = (InputPreset.MoveDirMode)EditorGUILayout.EnumPopup("Movement Mode:", inputPreset.moveDirMode);
            EditorGUI.indentLevel++;
			switch (inputPreset.moveDirMode)
			{
				case InputPreset.MoveDirMode.FourButtons:
                    DrawArray(ref inputPreset.forward, "Forward", ref collapsed);
                    DrawArray(ref inputPreset.backward, "Backward", ref collapsed1);
                    DrawArray(ref inputPreset.strafeLeft, "Strafe Left", ref collapsed2);
                    DrawArray(ref inputPreset.strafeRight, "Strafe Right", ref collapsed3);
                    break;
				case InputPreset.MoveDirMode.Axis:
                    inputPreset.axisX = EditorGUILayout.TextField("Axis X", inputPreset.axisX);
                    inputPreset.axisY = EditorGUILayout.TextField("Axis Y", inputPreset.axisY);
                    break;
			}
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(25);
            base.OnInspectorGUI();
		}
        public void DrawArray(ref KeyCode[] elements, string label, ref bool collapsed)
		{
            collapsed = EditorGUILayout.Foldout(collapsed, label);
            if (collapsed)
            {
                EditorGUI.indentLevel++;
                int arraySize = EditorGUILayout.DelayedIntField("Size", elements.Length);
                if (arraySize < 0)
                    arraySize = 0;
                if (arraySize != elements.Length)
                    Array.Resize(ref elements, arraySize);
				for (int i = 0; i < elements.Length; i++)
                    elements[i] = (KeyCode)EditorGUILayout.EnumPopup(elements[i]);
                EditorGUI.indentLevel--;
            }
        }
	}
}

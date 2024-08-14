/*
 * Copyright (c) Hubbahu
 */

using System;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Player/Locomotion Controller")]
[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public class LocomotionController : PlayerComponent
{
	#region Variables
	//references to nescessary objects
	public Rigidbody character;

	[Space()]

	//movement Controls
	[Header("Movement")]
	[Tooltip("Remember that this is an arbitrary value.")]
	public float speed = 3f;
	[Tooltip("The multiplier applied when in one of these states")]
	public float
	crouchMult = .6f,
	sprintMult = 7f,
	fastCrouchMult = 1.5f,
	backwardMult = .5f,
	sideMult = .75f,
	midairMult = .75f;
	//a low accelerationSpeed makes it difficult to change velocity || a low decelerationSpeed makes it difficult to stop an objects velocity
	public float accelerationSpeed, decelerationSpeed;
	public Dictionary<string, float> externalSpeedMults = new Dictionary<string, float>();
	public float ExternalSpeedMult
	{
		get
		{
			float multiplier = 1;
			foreach (float mult in externalSpeedMults.Values)
				multiplier *= mult;
			return multiplier;
		}
	}
	public float FinalMult
	{
		get
		{
			float multiplier = ExternalSpeedMult * speed;

			if (!Grounded)
				multiplier *= midairMult;
			if (BackTracking)
				multiplier *= backwardMult;
			else if (StrafingLeft || StrafingRight)
				multiplier *= sideMult;

			multiplier *= Sprinting ? (Crouching ? fastCrouchMult : sprintMult) : (Crouching ? crouchMult : 1f);

			return multiplier;
		}
	}
	Vector2 rawMovement;
	Vector3 smoothedMovement;

	[Header("Jumping")]
	public float jumpDistance = .75f;
	public float leapDistance = 1.5f;
	public float antiBunnyHopTime, coyoteTime;
	bool antiBunnyHop, jumped;
	public bool CanJump => (Grounded || (midairTime <= coyoteTime && !jumped)) && antiBunnyHop && !Sliding;
	public float midairTime;
	public event Action OnJump;
	public Dictionary<string, float> externalJumpMults = new Dictionary<string, float>();
	public float ExternalJumpMult
	{
		get
		{
			float multiplier = 1;
			foreach (float mult in externalJumpMults.Values)
				multiplier *= mult;
			return multiplier;
		}
	}

	//Physics Forces and Objects
	[Header("Physics")]
	[SerializeField]
	float terminalVelocity;
	public Vector3 GravVelocity { get; private set; }
	Vector3 velocity;
	public event Action<float> OnLand;
	public float groundCheckSize;
	public bool Grounded { get; private set; }
	public RaycastHit hit;

	//Slope and Enviroment Info
	[Header("Enviroment Data")]
	[SerializeField]
	LayerMask slidableMask;
	[SerializeField]
	LayerMask uncrouchCheckMask;
	[HideInInspector]
	public float slopeAngle, forwardAngle;
	RaycastHit ground;
	Vector3 collisionPoint;
	float slopeSpeedMulti;
	//direction in which controller should apply force parallel to ground
	[SerializeField]
	[Tooltip("Must be child of player.")]
	Transform groundDirection;
	//direction to apply gravity force
	Ray groundCheck;
	bool antiStick;
	Vector3 pastPos;

	//Extra
	[Header("Miscellaneous")]
	public bool lockSprint;
	public bool applyGrav = true;
	public bool BackTracking => rawMovement.y < 0f;
	public bool StrafingRight => rawMovement.x > 0f;
	public bool StrafingLeft => rawMovement.x < 0f;
	public bool Idle => rawMovement.x == 0f && rawMovement.y == 0f;
	public bool HitWall      { get; private set; }
	public bool Sliding      { get; private set; }
	public bool Falling      { get; private set; }
	public bool Crouching    { get; private set; }
	//This sprinting means sprinting is toggled
	public bool SprintToggled { get; private set; }
	//This sprinting means it is applied
	public bool Sprinting => SprintToggled && !HitWall && !lockSprint;
	#endregion

	#region Functions
	void Start()
	{
		if (!character)
		{
			Destroy(this);
			Debug.LogError("Missing Character Controller");
		}
		else if (!groundDirection)
		{
			Destroy(this);
			Debug.LogError("Missing Ground Direction Transform");
		}
	}
	void Update()
	{
		Move();
	}
	
	public bool Jump()
	{
		return true;
	}
	public void Move()
	{

	}
	public void TestGround()
	{
		//Updates ground related variables

	}

	/*
	#region MovementFunctions
	public bool TryChangeCrouch()
	{
		if (Crouching && CheckIfCanUncrouch())
		{
			Crouching = false;
			return true;
		}
		else if (!Crouching && characterController.isGrounded)
		{
			Crouching = true;
			return true;
		}
		return false;
	}
	public void ChangeSprint()
	{
		SprintToggled = !SprintToggled;
	}
	public bool TrySetCrouch(bool crouch)
	{
		if (crouch && !Crouching && characterController.isGrounded)
		{
			Crouching = true;
			return true;
		}
		else if(!crouch && CheckIfCanUncrouch() && Crouching)
		{
			Crouching = false;
			return true;
		}
		return false;
	}
	public void SetSprint(bool sprint)
	{
		if (LockPlayer) return;
		SprintToggled = sprint;
	}

	public void ApplyMovement(Vector2 movement, bool jump)
	{
		rawMovement = movement;

		ApplyEnviromentInfo();

		#region AntiStick 2
		//if the antistick is nescessary, it will cancel out the the canceling of input
		if (antiStick)
		{
			Sliding = false;
			if (Falling)
			{
				OnLand?.Invoke(GravVelocity.magnitude);
				Falling = false;
			}
		}
		#endregion

		#region Jump
		if (jump && CanJump)
		{
			bool uncrouched = TrySetCrouch(false);
			if (!Crouching)
			{
				jumped = true;
				GravVelocity = -Physics.gravity.normalized
					* Mathf.Sqrt((uncrouched ? leapDistance : jumpDistance) * ExternalJumpMult * Physics.gravity.y * -2f);
				OnJump?.Invoke();
			}
		}
		#endregion

		#region Apply Velocity
		//XZ Movement
		//applies input and other vars to velocity. Depends on whether you're Jumping, Sliding, freefalling, or none of the above
		if (Sliding)
		{
			//records last pos in case of getting stuck
			pastPos = transform.position;

			//applies velocity for when Sliding
			velocity = groundDirection.up * -GravVelocity.magnitude;
		}
		else if (characterController.isGrounded)
		{
			//normal way of applying velocity
			velocity = GravVelocity * slopeSpeedMulti + ComputeXZ(rawMovement);
		}
		else
		{
			//applies velocity for when Jumping or Falling
			velocity = GravVelocity + ComputeXZ(rawMovement);
		}
		#endregion

		ApplyVelocityAndForces();

		#region AntiStick 1
		//checks if in the case of character Sliding causes it to get stuck on object
		antiStick = (Sliding && transform.position == pastPos) || ((rawMovement.x + rawMovement.y) != 0f && transform.position == pastPos);
		#endregion
	}
	#endregion

	public void SetPosition(Vector3 pos)
	{
		characterController.enabled = false;
		transform.position = pos;
		characterController.enabled = true;
	}

	public bool Jump()
	{

	}

	IEnumerator AntiBunnyHop()
	{
		if (jumped)
		{
			antiBunnyHop = true;
			jumped = false;
			yield return new WaitForSecondsRealtime(antiBunnyHopTime);
			antiBunnyHop = false;
		}
	}

	void ApplyVelocityAndForces()
	{
		if (!characterController.isGrounded || Sliding)
		{
			//applies gravity on mid-air object
			if (applyGrav)
			{
				if (GravVelocity.magnitude < terminalVelocity)
					GravVelocity += Physics.gravity * Time.deltaTime;
				else
					GravVelocity = terminalVelocity * GravVelocity.normalized;
			}
			else
				GravVelocity = Vector3.zero;

			if (!Falling && GravVelocity.y < 0)
				Falling = true;

			if(!Sliding)
				midairTime += Time.deltaTime;
		}
		else if(GravVelocity.y <= 0)
		{
			#region Check If Landed
			//if you were Falling and/or Sliding and just Hit the ground set imapctVelocity to the velocity at which you were Falling
			if (Falling)
			{
				OnLand?.Invoke(GravVelocity.magnitude);
				StartCoroutine(AntiBunnyHop());
				Falling = false;
			}
			#endregion

			midairTime = 0f;

			//prevents compounding velocity when on ground & sets the impact velocity
			//forces velocityZ to -2f to reset || it is -2f to force character to ground (nescessary for .isGrounded)
			GravVelocity = Physics.gravity.normalized * 2f;
		}
		//applies velocity
		characterController.Move(velocity * Time.deltaTime);
	}

	//comprehends xz input for a first person controller
	Vector3 ComputeXZ(Vector2 input)
	{
		Vector3 xzMovement;
		//normalize the final direction and apply speed
		xzMovement = input.x * groundDirection.right + input.y * groundDirection.forward;
		xzMovement = xzMovement.normalized * FinalMult;

		//smooth the movement
		//if you want to move more then you are, you're accelerating, else decelerate
		if (smoothedMovement.sqrMagnitude < xzMovement.sqrMagnitude)
			smoothedMovement = Vector3.Lerp(smoothedMovement, xzMovement, accelerationSpeed * Time.deltaTime);
		else
			smoothedMovement = Vector3.Lerp(smoothedMovement, xzMovement, decelerationSpeed * Time.deltaTime);

		return smoothedMovement;
	}

	#region CheckEnviromentFunctions
	void ApplyEnviromentInfo()
	{
		CalcGroundData();
		CheckIfWall();
	}

	void OnControllerColliderHit(ControllerColliderHit hit)
	{
		collisionPoint = hit.point;
		collisionPoint -= transform.position;
	}

	public void CalcGroundData()
	{
		//Calculates Speed of controller based off of slope steepness
		groundCheck.origin = transform.position + collisionPoint + Vector3.up * .05f;
		groundCheck.direction = Vector3.down;

		//Reset groundDirection, slopeSpeedMulti and Sliding
		slopeSpeedMulti = 1f;
		Sliding = false;

		//Tests angle of ground
		if (Physics.Raycast(groundCheck, out ground, .55f, slidableMask))
		{
			groundDirection.localRotation = Quaternion.identity;
			slopeAngle = Vector3.Angle(Vector3.up, ground.normal);
			//similar to slopeAngle except the locoController direction is taken into account
			forwardAngle = Vector3.Angle(transform.forward, ground.normal) - 90f;

			if (forwardAngle < 0f && slopeAngle <= defaultSlopeLimit)
			{
				//increase speed accurately when going down slope with trigonometry;
				slopeSpeedMulti = 1 / Mathf.Cos(forwardAngle * Mathf.Deg2Rad);
				//setting ground direction based on forwardAngle
				groundDirection.localEulerAngles += new Vector3(-forwardAngle, 0f, 0f);
			}
			else if ((slopeAngle > defaultSlopeLimit))
			{
				Sliding = true;
				//sets fall direction to be in line with the slope (the up direction is what is in line)
				Vector3 groundCross = Vector3.Cross(ground.normal, Vector3.up);
				groundDirection.rotation = Quaternion.FromToRotation(Vector3.up, Vector3.Cross(groundCross, ground.normal));
			}
		}
	}

	void CheckIfWall()
	{
		if ((characterController.collisionFlags & CollisionFlags.Sides) != 0)
		{
			HitWall = true;
			//prevents weird jittering when skimming walls and Falling
			characterController.slopeLimit = 90f;
		}
		else if (HitWall)
		{
			HitWall = false;

			//resets slopelimit to normal value
			characterController.slopeLimit = defaultSlopeLimit;
		}
	}

	bool CheckIfCanUncrouch()
	{
		/*		//makes a capsule in the shape of the character when standing. This makes sure that when the character stands there is enough space for the full body
				Vector3 startPoint = transform.position, endPoint = transform.position;
				startPoint.y += characterController.radius;
				endPoint.y += defaultHeight - characterController.radius;

				//check the area with the variables
				return !Physics.CheckCapsule(startPoint, endPoint, characterController.radius, uncrouchCheckMask);
		return true;
	}
	#endregion
	*/
	#endregion
}
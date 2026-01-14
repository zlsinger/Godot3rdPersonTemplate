using Godot;
using System;
using System.Reflection.Metadata;

public partial class PlayerBaseController : CharacterBody3D
{
// Editor exposed variables
	[Export] public int PlayerID = 0;
	[Export] public float Speed = 5.0f;
	[Export] public float SlowDownSpeedGround = 5.0f;
	[Export] public float SlowDownSpeedAir = 0.01f;
	[Export] public float JumpVelocity = 15.0f;
	[Export] public float JumpDecrement = 0.5f;
	[Export] public float MaxFallSpeed = -100.0f;
	[Export] public float CameraSpeed = 0.07f; // Speed at which camera input moves camera
	[Export] public float CameraLerpSpeed = 0.5f; // Speed at which camera lerps to destination
	[Export] public double MaxCoyoteTime = 0.15f;

// Private variables
	private Node3D CameraPivot;
	private Camera3D PlayerCamera;
	private Node3D CameraDestination;
	private Node3D CameraTarget;
	private AnimationTree PlayerAnimationTree;
	private Node3D PlayerModel;
	private Sprite3D BlobShadow;
	private PhysicsDirectSpaceState3D SpaceState;
	private double CoyoteTime = 0.0f;
	private Vector2 movementInputVectorRaw;

// Input strings
	private string InputJump;
	private string InputMoveLeft;
	private string InputMoveRight;
	private string InputMoveUp;
	private string InputMoveDown;
	private string InputCameraDown;
	private string InputCameraUp;
	private string InputCameraRight;
	private string InputCameraLeft;
	private string InputOptionUp;
	private string InputOptionDown;
	private string InputOptionLeft;
	private string InputOptionRight;
	private string InputRecenterCamera;

// STATIC VARIABLES
	private static float CameraRotateToLookUpLimit = -70.0f;
	private static float CameraRotateToLookDownLimit = 20.0f;

	public enum PlayerMovementState
	{
		grounded,
		jumping,
		falling,
	}

	public PlayerMovementState CurrentPlayerMovementState = PlayerMovementState.grounded;

	public override void _Ready()
	{
		base._Ready();
		CameraPivot = GetNode<Node3D>("CameraPivot");
		PlayerModel = GetNode<Node3D>("Faye");
		BlobShadow = GetNode<Sprite3D>("Faye/BlobShadow");
		BlobShadow.Visible = false;
		PlayerAnimationTree = GetNode<AnimationTree>("Faye/AnimationTree");
		PlayerAnimationTree.Active = true;
		PlayerCamera = GetNode<Camera3D>("Camera3D");
		CameraDestination = GetNode<Node3D>("CameraPivot/CameraSpringArm/CameraDestination");
		CameraTarget = GetNode<Node3D>("CameraTarget");

		SpaceState = GetWorld3D().DirectSpaceState;

		SetupInputStrings();
	}

	void SetupInputStrings()
	{
		InputJump = "Jump_" + PlayerID.ToString();
		InputMoveLeft = "MoveLeft_" + PlayerID.ToString();
		InputMoveRight = "MoveRight_" + PlayerID.ToString();
		InputMoveUp = "MoveUp_" + PlayerID.ToString();
		InputMoveDown = "MoveDown_" + PlayerID.ToString();
		InputCameraDown = "CameraDown_" + PlayerID.ToString();
		InputCameraUp = "CameraUp_" + PlayerID.ToString();
		InputCameraRight = "CameraRight_" + PlayerID.ToString();
		InputCameraLeft = "CameraLeft_" + PlayerID.ToString();
		InputOptionUp = "OptionUp_" + PlayerID.ToString();
		InputOptionDown = "OptionDown_" + PlayerID.ToString();
		InputOptionLeft = "OptionLeft_" + PlayerID.ToString();
		InputOptionRight = "OptionRight_" + PlayerID.ToString();
		InputRecenterCamera = "RecenterCamera_" + PlayerID.ToString();
	}

    public override void _Process(double delta)
    {
        base._Process(delta);
		if (GameManager.CurrentLevelManager.KillHeightEnabled && GlobalPosition.Y <= GameManager.CurrentLevelManager.KillHeight)
		{
			ResetPlayer();
		}
    }

	public override void _PhysicsProcess(double delta)
	{
		Vector3 calculatedVelocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
		{
			calculatedVelocity += GetGravity() * (float)delta;
		}

		HandleJumpInput(ref calculatedVelocity, delta);
		HandleBlobShadow();
		HandleMovementInput(ref calculatedVelocity);
		HandleCameraInput();
		HandleCameraLerp();
		UpdateAnimParameters();

		// Maintain vertical velocity so jumps are instant.
		// For horizonal velocity, make that feel gradual so there's a feeling of momentum.
		Vector3 gradualVelocity = Velocity.MoveToward(calculatedVelocity, 0.7f);
		Velocity = new Vector3(gradualVelocity.X, calculatedVelocity.Y, gradualVelocity.Z);
		MoveAndSlide();
	}

	public void ResetPlayer()
	{
		GlobalPosition = GameManager.CurrentLevelManager.SpawnPoints[PlayerID].GlobalPosition;	
		Velocity = Vector3.Zero;
	}


	void HandleJumpInput(ref Vector3 calculatedVelocity, double delta)
	{
		switch (CurrentPlayerMovementState)
		{
			case PlayerMovementState.grounded:
				if (Input.IsActionJustPressed(InputJump) && IsOnFloor())
				{
					InitiateJump(ref calculatedVelocity);
				}
				if (Velocity.Y < 0)
				{
					CoyoteTime = 0.0f;
					CurrentPlayerMovementState = PlayerMovementState.falling;
					BlobShadow.Visible = true;
				}
				break;
			case PlayerMovementState.jumping:
				CoyoteTime = MaxCoyoteTime;
				if (Input.IsActionJustReleased(InputJump) || calculatedVelocity.Y <= 0)
				{
					CurrentPlayerMovementState = PlayerMovementState.falling;
				}
				break;
			case PlayerMovementState.falling:
				if (CoyoteTime < MaxCoyoteTime)
				{
					CoyoteTime += delta;
					if (Input.IsActionJustPressed(InputJump))
					{
						InitiateJump(ref calculatedVelocity);
					}
				}
				if (calculatedVelocity.Y >= MaxFallSpeed) {
					calculatedVelocity.Y -= JumpDecrement;
				}
				if (IsOnFloor())
				{
					CoyoteTime = 0.0f;
					CurrentPlayerMovementState = PlayerMovementState.grounded;
					BlobShadow.Visible = false;
				}
				break;
		}
	}

	void InitiateJump(ref Vector3 calculatedVelocity)
	{
		calculatedVelocity.Y = JumpVelocity;
		CurrentPlayerMovementState = PlayerMovementState.jumping;
		BlobShadow.Visible = true;
	}

	void HandleBlobShadow()
	{
		if (BlobShadow.Visible)
		{
			Vector3 origin = PlayerModel.GlobalPosition;
			Vector3 end = origin + Vector3.Down * 20.0f;
			PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(origin, end);
			query.CollideWithAreas = false;

			Godot.Collections.Dictionary intersection = SpaceState.IntersectRay(query);
			if (intersection.ContainsKey("position"))
			{
				BlobShadow.GlobalPosition = (Vector3)intersection["position"] + (Vector3.Up * 0.4f);
			}
		}
	}

	void HandleMovementInput(ref Vector3 calculatedVelocity)
	{
		// Get the input direction and handle the movement/deceleration.
		movementInputVectorRaw = new Vector2(Input.GetAxis(InputMoveLeft, InputMoveRight), Input.GetAxis(InputMoveUp, InputMoveDown));
		Vector3 normalizedDirection = new Vector3(movementInputVectorRaw.X, 0, movementInputVectorRaw.Y).Normalized();
		Vector3 relativeDirection = normalizedDirection.Rotated(Vector3.Up, CameraPivot.Rotation.Y);

		if (relativeDirection != Vector3.Zero)
		{
			// Allows the player to move slowly if they don't push the joystick all the way.
			float finalSpeed = Speed * movementInputVectorRaw.Length();
			calculatedVelocity.X = relativeDirection.X * finalSpeed;
			calculatedVelocity.Z = relativeDirection.Z * finalSpeed;

			Vector3 lookAtVector = PlayerModel.GlobalPosition + calculatedVelocity;
			Vector3 lookAtVectorSameHeight = new Vector3(lookAtVector.X, PlayerModel.GlobalPosition.Y, lookAtVector.Z);
			PlayerModel.LookAt(lookAtVectorSameHeight, Vector3.Up, true);
		}
		else if (Velocity != Vector3.Zero)
		{
			float slowDownSpeed = CurrentPlayerMovementState == PlayerMovementState.grounded ? SlowDownSpeedGround : SlowDownSpeedAir;
			calculatedVelocity.X = Mathf.MoveToward(Velocity.X, 0, slowDownSpeed);
			calculatedVelocity.Z = Mathf.MoveToward(Velocity.Z, 0, slowDownSpeed);
		}
	}

	void HandleCameraInput()
	{
		if (Input.IsActionJustPressed(InputRecenterCamera))
		{
			CameraPivot.Rotation = new Vector3(CameraPivot.Rotation.X, PlayerModel.Rotation.Y + 135.0f, CameraPivot.Rotation.Z);
			PlayerCamera.GlobalPosition = CameraDestination.GlobalPosition;
		}
		else
		{
			float cameraUpDown = Input.GetAxis(InputCameraDown, InputCameraUp);
			float cameraLeftRight = Input.GetAxis(InputCameraRight, InputCameraLeft);

			Vector3 newRotation = CameraPivot.Rotation;
			newRotation.X += cameraUpDown * CameraSpeed;
			newRotation.X = Mathf.Clamp(newRotation.X, Mathf.DegToRad(CameraRotateToLookUpLimit), Mathf.DegToRad(CameraRotateToLookDownLimit));
			newRotation.Y += cameraLeftRight * CameraSpeed;

			CameraPivot.Rotation = newRotation;
		}
	}

	void HandleCameraLerp()
	{
		if (PlayerCamera.GlobalPosition != CameraDestination.GlobalPosition) {
			float cameraDistanceToTarget = PlayerCamera.GlobalPosition.DistanceSquaredTo(CameraTarget.GlobalPosition);
			float cameraDestDistanceToTarget = CameraDestination.GlobalPosition.DistanceSquaredTo(CameraTarget.GlobalPosition);

			if (cameraDestDistanceToTarget > cameraDistanceToTarget) 
			{
				PlayerCamera.GlobalPosition = PlayerCamera.GlobalPosition.MoveToward(CameraDestination.GlobalPosition, CameraLerpSpeed);
			}
			else
			{
				PlayerCamera.GlobalPosition = CameraDestination.GlobalPosition;
			}
		}
		if (PlayerCamera.GlobalPosition != CameraTarget.GlobalPosition) {
			PlayerCamera.LookAt(CameraTarget.GlobalPosition);
		}
	}

	void UpdateAnimParameters()
	{
		switch (CurrentPlayerMovementState)
		{
			case PlayerMovementState.grounded:
				if (movementInputVectorRaw.Length() > 0.2f) 
				{
					PlayerAnimationTree.Set("parameters/conditions/isMovingOnGround", true);
					PlayerAnimationTree.Set("parameters/conditions/isIdle", false);
				}
				else
				{
					PlayerAnimationTree.Set("parameters/conditions/isMovingOnGround", false);
					PlayerAnimationTree.Set("parameters/conditions/isIdle", true);
				}
				PlayerAnimationTree.Set("parameters/conditions/isJumping", false);
				break;
			case PlayerMovementState.jumping:
				PlayerAnimationTree.Set("parameters/conditions/isMovingOnGround", false);
				PlayerAnimationTree.Set("parameters/conditions/isIdle", false);
				PlayerAnimationTree.Set("parameters/conditions/isJumping", true);
				break;
			case PlayerMovementState.falling:
				
				PlayerAnimationTree.Set("parameters/conditions/isMovingOnGround", false);
				PlayerAnimationTree.Set("parameters/conditions/isIdle", false);
				PlayerAnimationTree.Set("parameters/conditions/isJumping", true);
				break;
		}
	}
}

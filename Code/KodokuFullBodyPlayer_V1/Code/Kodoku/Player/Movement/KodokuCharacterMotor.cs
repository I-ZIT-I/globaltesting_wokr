using Sandbox;

namespace Kodoku;

/// <summary>
/// Rigidbody based character motor made specifically for Kodoku.
/// This component does not use Sandbox.PlayerController or Sandbox.CharacterController.
/// </summary>
[Title( "Kodoku Character Motor" )]
[Category( "Kodoku/Player" )]
[Icon( "directions_walk" )]
[EditorHandle( Icon = "directions_walk" )]
public sealed partial class KodokuCharacterMotor : Component, IScenePhysicsEvents, Component.ExecuteInEditor
{
	private const float Skin = 0.095f;

	[Property, Hide, RequireComponent]
	public Rigidbody Body { get; set; }

	[Property, Group( "References" )]
	public CapsuleCollider BodyCollider { get; set; }

	[Property, Group( "References" )]
	public BoxCollider FeetCollider { get; set; }

	[Property, Group( "References" )]
	public KodokuPlayerState PlayerState { get; set; }

	[Property, Group( "Body" ), Range( 1.0f, 64.0f )]
	public float BodyRadius { get; set; } = 16.0f;

	[Property, Group( "Body" ), Range( 1.0f, 128.0f )]
	public float StandingHeight { get; set; } = 72.0f;

	[Property, Group( "Body" ), Range( 1.0f, 128.0f )]
	public float DuckedHeight { get; set; } = 44.0f;

	[Property, Group( "Body" ), Range( 1.0f, 2000.0f )]
	public float BodyMass { get; set; } = 500.0f;

	[Property, Group( "Body" ), Range( 0.1f, 8.0f )]
	public float FeetColliderHeight { get; set; } = 2.0f;

	[Property, Group( "Body" ), Range( 0.0f, 4.0f )]
	public float FeetFriction { get; set; } = 1.0f;

	[Property, Group( "Movement" )]
	public float WalkSpeed { get; set; } = 110.0f;

	[Property, Group( "Movement" )]
	public float RunSpeed { get; set; } = 260.0f;

	[Property, Group( "Movement" )]
	public float DuckedSpeed { get; set; } = 70.0f;

	[Property, Group( "Movement" )]
	public float GroundAcceleration { get; set; } = 900.0f;

	[Property, Group( "Movement" )]
	public float AirAcceleration { get; set; } = 180.0f;

	[Property, Group( "Movement" )]
	public float GroundFriction { get; set; } = 8.0f;

	[Property, Group( "Movement" )]
	public float StopSpeed { get; set; } = 100.0f;

	[Property, Group( "Jump" )]
	public float JumpSpeed { get; set; } = 280.0f;

	[Property, Group( "Jump" )]
	public float CoyoteTime { get; set; } = 0.2f;

	[Property, Group( "Jump" )]
	public float JumpCooldown { get; set; } = 0.25f;

	[Property, Group( "Crouch" )]
	public float CrouchResizeSpeed { get; set; } = 180.0f;

	[Property, Group( "Ground" ), Range( 0.0f, 89.0f )]
	public float MaxGroundAngle { get; set; } = 45.0f;

	[Property, Group( "Ground" )]
	public float GroundProbeStart { get; set; } = 2.0f;

	[Property, Group( "Ground" )]
	public float GroundProbeDistance { get; set; } = 5.0f;

	[Property, Group( "Ground" ), Range( 0.1f, 1.0f )]
	public float GroundProbeRadiusScale { get; set; } = 0.45f;

	[Property, Group( "Step" )]
	public float StepHeight { get; set; } = 18.0f;

	[Property, Group( "Step" )]
	public float StepCheckDistance { get; set; } = 6.0f;

	[Sync]
	public Vector3 WishVelocity { get; private set; }

	[Sync]
	public bool IsGrounded { get; private set; }

	public bool IsAirborne => !IsGrounded;

	/// <summary>
	/// Velocity relative to the surface underneath the player.
	/// </summary>
	public Vector3 Velocity { get; private set; }

	public Vector3 GroundVelocity { get; private set; }

	public Vector3 GroundNormal { get; private set; } = Vector3.Up;

	public GameObject GroundObject { get; private set; }

	public Collider GroundCollider { get; private set; }

	public TimeSince TimeSinceGrounded { get; private set; } = 999.0f;

	public TimeSince TimeSinceUngrounded { get; private set; } = 999.0f;

	public float CurrentHeight { get; private set; }

	private Vector3 _moveInput;
	private Rotation _moveRotation = Rotation.Identity;
	private bool _wantsRun;
	private bool _wantsDuck;
	private bool _jumpRequested;
	private TimeSince _timeSinceJump = 999.0f;
	private TimeUntil _groundingPrevented;

	protected override void OnAwake()
	{
		base.OnAwake();

		ResolveReferences();

		CurrentHeight = StandingHeight;
		ConfigurePhysicsComponents();
	}

	protected override void OnEnabled()
	{
		base.OnEnabled();

		ResolveReferences();

		CurrentHeight = PlayerState.IsValid() && PlayerState.IsDucking
			? DuckedHeight
			: StandingHeight;

		ConfigurePhysicsComponents();
	}

	protected override void OnValidate()
	{
		ResolveReferences();

		if ( CurrentHeight <= 0.0f )
		{
			CurrentHeight = StandingHeight;
		}

		ConfigurePhysicsComponents();
	}

	private void ResolveReferences()
	{
		Body ??= GetComponent<Rigidbody>();
		BodyCollider ??= GetComponent<CapsuleCollider>();
		FeetCollider ??= GetComponent<BoxCollider>();
		PlayerState ??= GetComponent<KodokuPlayerState>();
	}

	private void ConfigurePhysicsComponents()
	{
		if ( Body.IsValid() )
		{
			Body.Gravity = true;
			Body.MotionEnabled = true;
			Body.MassOverride = BodyMass;
			Body.LinearDamping = 0.0f;
			Body.AngularDamping = 0.0f;
			Body.Locking = new PhysicsLock
			{
				Pitch = true,
				Roll = true,
				Yaw = true
			};
		}

		UpdateColliderGeometry();
	}

	private void UpdateColliderGeometry()
	{
		var radius = BodyRadius;
		var height = CurrentHeight.Clamp( radius * 2.0f, StandingHeight );

		if ( BodyCollider.IsValid() )
		{
			BodyCollider.Start = Vector3.Up * radius;
			BodyCollider.End = Vector3.Up * (height - radius);
			BodyCollider.Radius = radius;
			BodyCollider.Friction = 0.0f;
		}

		if ( FeetCollider.IsValid() )
		{
			FeetCollider.Scale = new Vector3(
				radius * 1.5f,
				radius * 1.5f,
				FeetColliderHeight
			);

			FeetCollider.Center = Vector3.Up * (FeetColliderHeight * 0.5f);
			FeetCollider.Friction = FeetFriction;
		}
	}

	public void SetMoveInput(
		Vector3 moveInput,
		Rotation viewRotation,
		bool wantsRun,
		bool wantsDuck
	)
	{
		if ( IsProxy )
			return;

		moveInput = moveInput.WithZ( 0.0f );

		if ( moveInput.Length > 1.0f )
		{
			moveInput = moveInput.Normal;
		}

		_moveInput = moveInput;
		_moveRotation = Rotation.FromYaw( viewRotation.Angles().yaw );
		_wantsRun = wantsRun;
		_wantsDuck = wantsDuck;
	}

	public void RequestJump()
	{
		if ( IsProxy )
			return;

		_jumpRequested = true;
	}

	void IScenePhysicsEvents.PrePhysicsStep()
	{
		if ( Scene.IsEditor || IsProxy || !Body.IsValid() )
			return;

		UpdateCrouching();
		UpdateWishVelocity();
		ApplyHorizontalMovement();
		TryJump();
		TryStepUp();
	}

	void IScenePhysicsEvents.PostPhysicsStep()
	{
		if ( Scene.IsEditor || !Body.IsValid() )
			return;

		CategorizeGround();
		Velocity = Body.Velocity - GroundVelocity;
	}
}

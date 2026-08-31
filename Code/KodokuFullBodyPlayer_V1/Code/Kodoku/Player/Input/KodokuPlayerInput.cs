using Sandbox;

namespace Kodoku;

/// <summary>
/// Reads local input and forwards intent to the Kodoku player components.
/// </summary>
[Title( "Kodoku Player Input" )]
[Category( "Kodoku/Player" )]
[Icon( "sports_esports" )]
public sealed class KodokuPlayerInput : Component
{
	[Property, Group( "References" )]
	public KodokuPlayerState PlayerState { get; set; }

	[Property, Group( "References" )]
	public KodokuCharacterMotor Motor { get; set; }

	[Property, Group( "References" )]
	public KodokuAnimatorDriver Animator { get; set; }

	[Property, Group( "Look" ), Range( 0.0f, 2.0f )]
	public float LookSensitivity { get; set; } = 1.0f;

	[Property, Group( "Look" ), Range( 0.0f, 180.0f )]
	public float PitchClamp { get; set; } = 89.0f;

	[Property, Group( "Actions" ), InputAction]
	public string RunAction { get; set; } = "run";

	[Property, Group( "Actions" ), InputAction]
	public string CrouchAction { get; set; } = "duck";

	[Property, Group( "Actions" ), InputAction]
	public string JumpAction { get; set; } = "Jump";

	[Property, Group( "Actions" ), InputAction]
	public string AimAction { get; set; } = "attack2";

	[Property, Group( "Actions" ), InputAction]
	public string AttackAction { get; set; } = "attack1";

	[Property, Group( "Actions" ), InputAction]
	public string ReloadAction { get; set; } = "reload";

	[Property, Group( "Movement" )]
	public bool RunByDefault { get; set; }

	[Property, Group( "Debug" )]
	public bool DebugScrollHoldType { get; set; } = true;

	[Property, Group( "Debug" )]
	public bool DebugWeaponInputs { get; set; } = true;

	protected override void OnAwake()
	{
		base.OnAwake();

		PlayerState ??= GetComponent<KodokuPlayerState>();
		Motor ??= GetComponent<KodokuCharacterMotor>();
		Animator ??= GetComponent<KodokuAnimatorDriver>();
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		if ( !PlayerState.IsValid() || !Motor.IsValid() )
			return;

		UpdateLook();
		UpdateMovementIntent();
		UpdateAiming();
		UpdateHoldTypeDebug();
		UpdateWeaponDebug();
	}

	private void UpdateLook()
	{
		var angles = PlayerState.EyeAngles;
		angles += Input.AnalogLook * LookSensitivity;

		PlayerState.SetEyeAngles( angles, PitchClamp );
	}

	private void UpdateMovementIntent()
	{
		var wantsRun = Input.Down( RunAction );

		if ( RunByDefault )
		{
			wantsRun = !wantsRun;
		}

		Motor.SetMoveInput(
			Input.AnalogMove,
			PlayerState.EyeRotation,
			wantsRun,
			Input.Down( CrouchAction )
		);

		if ( Input.Pressed( JumpAction ) )
		{
			Motor.RequestJump();
		}
	}

	private void UpdateAiming()
	{
		PlayerState.SetAiming( Input.Down( AimAction ) );
	}

	private void UpdateHoldTypeDebug()
	{
		if ( !DebugScrollHoldType )
			return;

		var scroll = Input.MouseWheel.y;

		if ( scroll > 0.0f )
		{
			PlayerState.CycleDebugHoldType( 1 );
		}
		else if ( scroll < 0.0f )
		{
			PlayerState.CycleDebugHoldType( -1 );
		}
	}

	private void UpdateWeaponDebug()
	{
		if ( !DebugWeaponInputs || !Animator.IsValid() )
			return;

		if ( Input.Pressed( AttackAction ) )
		{
			Animator.TriggerAttack();
		}

		if ( Input.Pressed( ReloadAction ) )
		{
			Animator.TriggerReload();
		}
	}
}

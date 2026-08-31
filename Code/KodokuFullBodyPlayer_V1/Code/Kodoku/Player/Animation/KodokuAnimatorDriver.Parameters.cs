using Sandbox;

namespace Kodoku;

public sealed partial class KodokuAnimatorDriver
{
	/// <summary>
	/// Complete list of parameters currently declared by kodokan(25).vanmgrph.
	/// Names are case-sensitive. Camera settings do not belong here.
	/// </summary>
	public static class Parameters
	{
		// Movement
		public const string MoveX = "move_x";
		public const string MoveY = "move_y";
		public const string Duck = "duck";

		// Character look
		public const string AimHead = "aim_head";

		// Weapon
		public const string Attack = "b_attack";
		public const string Reload = "b_reload";
		public const string Aim = "b_aim";
		public const string HoldType = "holdtype";

		// Emotion / mental state
		public const string Sanity = "Sanity";
		public const string Sadness = "Sadness";
	}

	// ---------------------------------------------------------------------
	// Current AnimGraph values
	// ---------------------------------------------------------------------

	[Property, ReadOnly, Group( "AnimGraph State/Movement" )]
	public float MoveX { get; private set; }

	[Property, ReadOnly, Group( "AnimGraph State/Movement" )]
	public float MoveY { get; private set; }

	[Property, ReadOnly, Group( "AnimGraph State/Movement" )]
	public float Duck { get; private set; }

	[Property, ReadOnly, Group( "AnimGraph State/Look" )]
	public Vector3 AimHeadDirection { get; private set; } = Vector3.Forward;

	[Property, ReadOnly, Group( "AnimGraph State/Weapon" )]
	public bool IsAiming { get; private set; }

	[Property, ReadOnly, Group( "AnimGraph State/Weapon" )]
	public KodokuPlayerState.HoldType CurrentHoldType { get; private set; }

	/// <summary>
	/// b_attack is an auto-reset pulse. Use TriggerAttack().
	/// </summary>
	public bool AttackIsPulse => true;

	/// <summary>
	/// b_reload is an auto-reset pulse. Use TriggerReload().
	/// </summary>
	public bool ReloadIsPulse => true;

	[Sync]
	[Property, Group( "AnimGraph State/Emotion" ), Range( 0.0f, 100.0f )]
	public float Sanity { get; private set; }

	[Sync]
	[Property, Group( "AnimGraph State/Emotion" ), Range( 0.0f, 100.0f )]
	public float Sadness { get; private set; }

	public void SetSanity( float value )
	{
		if ( IsProxy )
			return;

		Sanity = value.Clamp( 0.0f, 100.0f );
	}

	public void SetSadness( float value )
	{
		if ( IsProxy )
			return;

		Sadness = value.Clamp( 0.0f, 100.0f );
	}
}

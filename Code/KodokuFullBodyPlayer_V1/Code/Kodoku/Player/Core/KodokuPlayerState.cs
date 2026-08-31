using Sandbox;

namespace Kodoku;

/// <summary>
/// Shared state used by the Kodoku player components.
/// Only the owning player is allowed to change input-driven values.
/// </summary>
[Title( "Kodoku Player State" )]
[Category( "Kodoku/Player" )]
[Icon( "person" )]
public sealed class KodokuPlayerState : Component
{
	/// <summary>
	/// Numeric values must stay aligned with the enum options in kodokan.vanmgrph:
	/// none = 0, rifle = 1, pistol = 2.
	/// </summary>
	public enum HoldType
	{
		None = 0,
		Rifle = 1,
		Pistol = 2
	}

	[Sync( SyncFlags.Interpolate )]
	public Angles EyeAngles { get; private set; }

	[Sync]
	public bool IsAiming { get; private set; }

	[Sync]
	public bool IsDucking { get; private set; }

	[Sync]
	public bool IsRunning { get; private set; }

	[Sync]
	public HoldType CurrentHoldType { get; private set; } = HoldType.None;

	public Rotation EyeRotation => EyeAngles.ToRotation();

	protected override void OnEnabled()
	{
		base.OnEnabled();

		if ( Scene.IsEditor || IsProxy )
			return;

		EyeAngles = WorldRotation.Angles() with
		{
			pitch = 0.0f,
			roll = 0.0f
		};
	}

	public void SetEyeAngles( Angles angles, float pitchClamp = 89.0f )
	{
		if ( IsProxy )
			return;

		angles.roll = 0.0f;

		if ( pitchClamp > 0.0f )
		{
			angles.pitch = angles.pitch.Clamp( -pitchClamp, pitchClamp );
		}

		EyeAngles = angles;
	}

	public void SetAiming( bool aiming )
	{
		if ( IsProxy )
			return;

		IsAiming = aiming;
	}

	public void SetDucking( bool ducking )
	{
		if ( IsProxy )
			return;

		IsDucking = ducking;

		if ( ducking )
		{
			IsRunning = false;
		}
	}

	public void SetRunning( bool running )
	{
		if ( IsProxy )
			return;

		IsRunning = running && !IsDucking;
	}

	public void SetHoldType( HoldType holdType )
	{
		if ( IsProxy )
			return;

		CurrentHoldType = holdType;
	}

	/// <summary>
	/// Temporary debug order requested for the first milestone:
	/// None -> Pistol -> Rifle -> None.
	/// This order is independent from the AnimGraph numeric values.
	/// </summary>
	public void CycleDebugHoldType( int direction )
	{
		if ( IsProxy || direction == 0 )
			return;

		var order = new[]
		{
			HoldType.None,
			HoldType.Pistol,
			HoldType.Rifle
		};

		var currentIndex = System.Array.IndexOf( order, CurrentHoldType );
		if ( currentIndex < 0 )
		{
			currentIndex = 0;
		}

		var nextIndex = currentIndex + (direction > 0 ? 1 : -1);

		if ( nextIndex >= order.Length )
		{
			nextIndex = 0;
		}
		else if ( nextIndex < 0 )
		{
			nextIndex = order.Length - 1;
		}

		CurrentHoldType = order[nextIndex];
	}
}

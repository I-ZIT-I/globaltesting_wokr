using Sandbox;

namespace Kodoku;

/// <summary>
/// Owns every write made to the Kodokan animation graph.
/// </summary>
[Title( "Kodoku Animator Driver" )]
[Category( "Kodoku/Player" )]
[Icon( "sports_martial_arts" )]
public sealed partial class KodokuAnimatorDriver : Component
{
	[Property, Group( "References" )]
	public SkinnedModelRenderer BodyRenderer { get; set; }

	[Property, Group( "References" )]
	public KodokuCharacterMotor Motor { get; set; }

	[Property, Group( "References" )]
	public KodokuPlayerState PlayerState { get; set; }

	[Property, Group( "Body Rotation" )]
	public float BodyTurnSpeed { get; set; } = 12.0f;

	[Property, Group( "Body Rotation" )]
	public float StationaryTurnLimit { get; set; } = 45.0f;

	[Property, Group( "Body Rotation" )]
	public float AdsBodyTurnSpeed { get; set; } = 40.0f;

	[Property, Group( "Body Rotation" )]
	public float AdsStationaryTurnLimit { get; set; } = 3.0f;

	[Property, Group( "Blending" )]
	public float MovementBlendSpeed { get; set; } = 12.0f;

	[Property, Group( "Blending" )]
	public float DuckBlendSpeed { get; set; } = 10.0f;

	[Property, Group( "Look" ), Range( 0.0f, 1.0f )]
	public float AimHeadStrength { get; set; } = 1.0f;

	/// <summary>
	/// Animator belonging to the currently spawned weapon prefab.
	/// This reference is local and is not network state.
	/// </summary>
	public EquippedItemAnimator ActiveItemAnimator { get; private set; }

	protected override void OnAwake()
	{
		base.OnAwake();

		Motor ??= GetComponent<KodokuCharacterMotor>();
		PlayerState ??= GetComponent<KodokuPlayerState>();

		if ( !BodyRenderer.IsValid() )
		{
			Log.Error(
				$"{nameof( KodokuAnimatorDriver )}: "
				+ "BodyRenderer must be assigned."
			);
		}
	}

	protected override void OnUpdate()
	{
		if ( !BodyRenderer.IsValid() )
			return;

		UpdateBodyRotation();
		UpdateMovementParameters();
		UpdatePersistentParameters();
	}

	/// <summary>
	/// Called by KodokuWeaponVisualController whenever
	/// a new weapon prefab becomes active.
	/// </summary>
	public void SetEquippedItemAnimator(
		EquippedItemAnimator animator
	)
	{
		ActiveItemAnimator = animator;

		if ( ActiveItemAnimator.IsValid() )
		{
			ActiveItemAnimator.SetAiming( IsAiming );
		}
	}

	private void UpdatePersistentParameters()
	{
		var duckTarget =
			PlayerState.IsValid() && PlayerState.IsDucking
				? 1.0f
				: 0.0f;

		Duck = Duck.LerpTo(
			duckTarget,
			ExponentialBlend(
				DuckBlendSpeed,
				Time.Delta
			)
		);

		IsAiming =
			PlayerState.IsValid()
			&& PlayerState.IsAiming;

		CurrentHoldType =
			PlayerState.IsValid()
				? PlayerState.CurrentHoldType
				: KodokuPlayerState.HoldType.None;

		AimHeadDirection =
			PlayerState.IsValid()
				? PlayerState.EyeRotation.Forward
				: BodyRenderer.WorldRotation.Forward;

		BodyRenderer.Set(
			Parameters.Duck,
			Duck.Clamp( 0.0f, 1.0f )
		);

		BodyRenderer.Set(
			Parameters.Aim,
			IsAiming
		);

		BodyRenderer.Set(
			Parameters.HoldType,
			(int)CurrentHoldType
		);

		BodyRenderer.Set(
			Parameters.Sanity,
			Sanity
		);

		BodyRenderer.Set(
			Parameters.Sadness,
			Sadness
		);

		BodyRenderer.SetLookDirection(
			Parameters.AimHead,
			AimHeadDirection,
			AimHeadStrength
		);

		// Synchronise l'ADS du modèle de l'arme active.
		if ( ActiveItemAnimator.IsValid() )
		{
			ActiveItemAnimator.SetAiming( IsAiming );
		}
	}

	private void UpdateBodyRotation()
	{
		if ( !PlayerState.IsValid() )
			return;

		var targetRotation =
			Rotation.FromYaw(
				PlayerState.EyeAngles.yaw
			);

		// During ADS the torso follows the view much more closely. Keeping the
		// shoulders nearly aligned with the camera prevents the arm IK from
		// reaching across a large yaw gap while the weapon is camera-locked.
		if ( PlayerState.IsAiming )
		{
			var adsDifference =
				BodyRenderer.WorldRotation.Distance(
					targetRotation
				);

			if ( adsDifference <= AdsStationaryTurnLimit )
				return;

			BodyRenderer.WorldRotation =
				Rotation.Slerp(
					BodyRenderer.WorldRotation,
					targetRotation,
					ExponentialBlend(
						AdsBodyTurnSpeed,
						Time.Delta
					)
				);

			return;
		}

		var horizontalSpeed =
			Motor.IsValid()
				? Motor.WishVelocity
					.WithZ( 0.0f )
					.Length
				: 0.0f;

		if ( horizontalSpeed > 1.0f )
		{
			BodyRenderer.WorldRotation =
				Rotation.Slerp(
					BodyRenderer.WorldRotation,
					targetRotation,
					ExponentialBlend(
						BodyTurnSpeed,
						Time.Delta
					)
				);

			return;
		}

		var difference =
			BodyRenderer.WorldRotation.Distance(
				targetRotation
			);

		if ( difference <= StationaryTurnLimit )
			return;

		var correction =
			1.0f
			- StationaryTurnLimit / difference;

		BodyRenderer.WorldRotation =
			Rotation.Slerp(
				BodyRenderer.WorldRotation,
				targetRotation,
				correction
			);
	}

	private static float ExponentialBlend(
		float speed,
		float deltaTime
	)
	{
		if ( speed <= 0.0f )
			return 1.0f;

		return 1.0f
			- System.MathF.Exp(
				-speed * deltaTime
			);
	}
	public KodokuWeaponFireController ActiveWeaponFireController
	{
		get;
		private set;
	}

	public void SetEquippedWeaponFireController(
		KodokuWeaponFireController controller
	)
	{
		ActiveWeaponFireController = controller;
	}
}
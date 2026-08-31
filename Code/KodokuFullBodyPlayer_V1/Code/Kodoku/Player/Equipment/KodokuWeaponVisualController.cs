using Sandbox;

namespace Kodoku;

[Title( "Kodoku Weapon Visual Controller" )]
[Category( "Kodoku/Equipment" )]
[Icon( "sports_martial_arts" )]
public sealed class KodokuWeaponVisualController : Component
{
	[Property, Group( "References" )]
	public KodokuPlayerState PlayerState { get; set; }

	[Property, Group( "References" )]
	public KodokuAnimatorDriver Animator { get; set; }

	[Property, Group( "References" )]
	public KodokuEquipmentVisualController EquipmentVisuals { get; set; }

	[Property, Group( "References" )]
	public KodokuPlayerCamera PlayerCamera { get; set; }

	[Property, Group( "Weapon Prefabs" )]
	public GameObject PistolPrefab { get; set; }

	[Property, Group( "Weapon Prefabs" )]
	public GameObject RiflePrefab { get; set; }

	[Property, Group( "Weapon Follow" )]
	public string PlayerHandBoneName { get; set; } = "hand_r";

	[Property, Group( "Weapon Follow" )]
	public string WeaponHandBoneName { get; set; } = "hand_r";

	[Property, Group( "ADS Alignment" )]
	public bool AlignSightToAimEye { get; set; } = true;

	[Property, Group( "ADS Alignment" )]
	public string SightBoneName { get; set; } = "sight";

	[Property, Group( "ADS Alignment" )]
	public float AdsSightForwardDistance { get; set; } = 8.0f;

	[Property, Group( "ADS Alignment" )]
	public Vector3 AdsSightLocalOffset { get; set; }

	[Property, Group( "ADS Alignment" )]
	public Angles AdsSightRotationOffset { get; set; }

	[Property, Group( "ADS Hand Follow" )]
	public bool FollowAnimatedWeaponHand { get; set; } = true;

	[Property, Group( "Sway" )]
	public bool EnableSway { get; set; } = true;

	[Property, Group( "Sway" )]
	public float SwayFullSpeed { get; set; } = 180.0f;

	[Property, Group( "Sway" )]
	public float SwayResponseSpeed { get; set; } = 14.0f;

	[Property, Group( "Sway" )]
	public float SwayReturnSpeed { get; set; } = 8.0f;

	[Property, Group( "Sway" )]
	public float SwayXScale { get; set; } = 1.0f;

	[Property, Group( "Sway" )]
	public float SwayYScale { get; set; } = 1.0f;

	[Property, Group( "Sway" )]
	public float SwayHorizontalPosition { get; set; } = 0.75f;

	[Property, Group( "Sway" )]
	public float SwayVerticalPosition { get; set; } = 0.55f;

	[Property, Group( "Sway" )]
	public float SwayYawDegrees { get; set; } = 2.0f;

	[Property, Group( "Sway" )]
	public float SwayPitchDegrees { get; set; } = 1.5f;

	[Property, Group( "Sway" )]
	public float SwayRollDegrees { get; set; } = 1.0f;

	public GameObject ActiveWeaponInstance { get; private set; }

	private KodokuPlayerState.HoldType _lastHoldType =
		(KodokuPlayerState.HoldType)(-1);

	private SkinnedModelRenderer _weaponRenderer;
	private global::Transform _weaponHandLocal;
	private global::Transform _sightLocal;
	private Vector3 _weaponWorldScale = Vector3.One;

	private Angles _lastSwayEyeAngles;
	private float _swayX;
	private float _swayY;
	private bool _swayInitialized;

	private global::Transform _sharedAdsSightTarget;
	private global::Transform _sharedAdsWeaponWorld;
	private global::Transform _sharedAdsHandTarget;
	private bool _hasSharedAdsPose;
	private float _sharedAdsUpdateTime = float.NegativeInfinity;

	// Captured after the weapon AnimGraph has evaluated. This is stored in
	// weapon-local space so camera/root movement never gets baked into the IK
	// target. The next Update can combine this animation-only pose with the
	// current-frame shared ADS root.
	private global::Transform _animatedWeaponHandLocal;
	private bool _hasAnimatedWeaponHandLocal;

	private bool _hasWeaponHandLocal;
	private bool _hasSightLocal;
	private bool _warnedMissingBodyRenderer;
	private bool _warnedMissingWeaponRenderer;
	private bool _warnedMissingPlayerHand;
	private bool _warnedMissingWeaponHand;
	private bool _warnedMissingPlayerCamera;
	private bool _warnedMissingSight;

	private bool IsReloadOverrideActive =>
		Animator.IsValid()
		&& Animator.IsReloadAnimationActive;

	protected override void OnAwake()
	{
		base.OnAwake();

		PlayerState ??= GetComponent<KodokuPlayerState>();
		Animator ??= GetComponent<KodokuAnimatorDriver>();
		EquipmentVisuals ??= GetComponent<KodokuEquipmentVisualController>();
		PlayerCamera ??= GetComponent<KodokuPlayerCamera>();
	}

	protected override void OnUpdate()
	{
		if ( !PlayerState.IsValid() )
			return;

		EnsureWeaponSelection();

		if ( IsReloadOverrideActive )
		{
			ResetSwayState();
			ResetSharedAdsState();
			ResetAnimatedWeaponHandState();
			return;
		}

		// Never carry a stale ADS animation sample through a rest -> aim
		// transition. The first ADS frame falls back to the reference hand
		// marker; the first PreRender then captures the real aimed pose.
		if ( !PlayerState.IsAiming )
			ResetAnimatedWeaponHandState();

		EnsureSharedAdsState();
	}

	protected override void OnPreRender()
	{
		if ( !PlayerState.IsValid() )
			return;

		EnsureWeaponSelection();

		// During reload the animator temporarily bone-merges the weapon to the
		// body. Do not clear that merge or camera-align the weapon until the
		// reload tag ends.
		if ( IsReloadOverrideActive )
			return;

		EnsureSharedAdsState();

		// PreRender happens after animation bones are available. Capture the
		// weapon's animated grip before moving the weapon root to this frame's
		// camera-driven ADS pose. Storing it in local space isolates recoil,
		// shoot and other weapon animation from camera/root latency.
		CaptureAnimatedWeaponHandLocal();

		UpdateWeaponWorldAlignment();
	}

	private void EnsureWeaponSelection()
	{
		if ( !PlayerState.IsValid() )
			return;

		if ( PlayerState.CurrentHoldType == _lastHoldType )
			return;

		_lastHoldType = PlayerState.CurrentHoldType;
		ResetSwayState();
		ResetSharedAdsState();
		ResetAnimatedWeaponHandState();
		RefreshWeapon();
	}

	private void RefreshWeapon()
	{
		DestroyCurrentWeapon();

		var prefab = ResolvePrefab();

		if ( !prefab.IsValid() )
			return;

		if ( !EquipmentVisuals.IsValid() )
		{
			Log.Warning(
				$"{nameof( KodokuWeaponVisualController )}: "
				+ "EquipmentVisuals is invalid."
			);

			return;
		}

		ActiveWeaponInstance =
			EquipmentVisuals.SpawnEquipment( prefab );

		if ( !ActiveWeaponInstance.IsValid() )
		{
			Log.Warning(
				$"{nameof( KodokuWeaponVisualController )}: "
				+ "failed to spawn weapon."
			);

			return;
		}

		_weaponRenderer =
			ActiveWeaponInstance.GetComponentInChildren<SkinnedModelRenderer>();

		foreach (
			var renderer in
				ActiveWeaponInstance.GetComponentsInChildren<SkinnedModelRenderer>()
		)
		{
			if ( !renderer.IsValid() )
				continue;

			renderer.SceneModel?.ClearBoneOverrides();
			renderer.BoneMergeTarget = null;
		}

		_weaponWorldScale = ActiveWeaponInstance.WorldScale;
		_hasWeaponHandLocal = false;
		_hasSightLocal = false;
		ResetSharedAdsState();
		ResetAnimatedWeaponHandState();

		var itemAnimator =
			ActiveWeaponInstance
				.GetComponentInChildren<EquippedItemAnimator>();

		var fireController =
			ActiveWeaponInstance
				.GetComponentInChildren<KodokuWeaponFireController>();

		if ( Animator.IsValid() )
		{
			Animator.SetEquippedItemAnimator(
				itemAnimator
			);

			Animator.SetEquippedWeaponFireController(
				fireController
			);
		}

		DisableAnimGraphSway();
		ResetAlignmentWarnings();
	}

	private void UpdateWeaponSway()
	{
		if (
			PlayerState.CurrentHoldType != KodokuPlayerState.HoldType.Pistol
			|| !_weaponRenderer.IsValid()
		)
		{
			ResetSwayState();
			return;
		}

		DisableAnimGraphSway();

		if (
			IsReloadOverrideActive
			|| !PlayerState.IsAiming
			|| !EnableSway
		)
		{
			ResetSwayState();
			return;
		}

		var eyeAngles = PlayerState.EyeAngles;

		if ( !_swayInitialized )
		{
			_lastSwayEyeAngles = eyeAngles;
			_swayInitialized = true;
			return;
		}

		var deltaTime = System.MathF.Max( Time.Delta, 0.0001f );
		var fullSpeed =
			System.MathF.Max(
				System.MathF.Abs( SwayFullSpeed ),
				1.0f
			);

		var yawDelta = Angles.NormalizeAngle(
			eyeAngles.yaw - _lastSwayEyeAngles.yaw
		);

		var pitchDelta = Angles.NormalizeAngle(
			eyeAngles.pitch - _lastSwayEyeAngles.pitch
		);

		_lastSwayEyeAngles = eyeAngles;

		var yawSpeed = yawDelta / deltaTime;
		var pitchSpeed = pitchDelta / deltaTime;

		var targetX = MathX.Clamp(
			yawSpeed / fullSpeed * SwayXScale,
			-1.0f,
			1.0f
		);

		var targetY = MathX.Clamp(
			pitchSpeed / fullSpeed * SwayYScale,
			-1.0f,
			1.0f
		);

		var xSpeed = System.MathF.Abs( targetX ) > 0.001f
			? SwayResponseSpeed
			: SwayReturnSpeed;

		var ySpeed = System.MathF.Abs( targetY ) > 0.001f
			? SwayResponseSpeed
			: SwayReturnSpeed;

		_swayX = MathX.Lerp(
			_swayX,
			targetX,
			ExponentialBlend( xSpeed, Time.Delta ),
			true
		);

		_swayY = MathX.Lerp(
			_swayY,
			targetY,
			ExponentialBlend( ySpeed, Time.Delta ),
			true
		);
	}

	private void DisableAnimGraphSway()
	{
		if (
			PlayerState.CurrentHoldType != KodokuPlayerState.HoldType.Pistol
			|| !_weaponRenderer.IsValid()
		)
		{
			return;
		}

		_weaponRenderer.Set( "x_sway", 0.0f );
		_weaponRenderer.Set( "y_sway", 0.0f );
	}

	private void ResetSwayState()
	{
		_swayX = 0.0f;
		_swayY = 0.0f;
		_swayInitialized = false;
	}

	private void EnsureSharedAdsState()
	{
		if ( IsReloadOverrideActive )
		{
			ResetSharedAdsState();
			return;
		}

		var now = Time.Now;

		if ( _sharedAdsUpdateTime == now )
			return;

		_sharedAdsUpdateTime = now;

		UpdateWeaponSway();
		RebuildSharedAdsPose();
	}

	private void RebuildSharedAdsPose()
	{
		_hasSharedAdsPose = false;

		if (
			IsReloadOverrideActive
			|| IsProxy
			|| !AlignSightToAimEye
			|| !PlayerState.IsValid()
			|| !PlayerState.IsAiming
			|| !ActiveWeaponInstance.IsValid()
			|| !EnsureWeaponRenderer()
			|| !EnsureSightLocal()
			|| !EnsureWeaponHandLocal()
		)
		{
			return;
		}

		PlayerCamera ??= GetComponent<KodokuPlayerCamera>();

		if ( !PlayerCamera.IsValid() )
		{
			if ( !_warnedMissingPlayerCamera )
			{
				Log.Warning(
					$"{nameof( KodokuWeaponVisualController )}: "
					+ "KodokuPlayerCamera is invalid during ADS."
				);

				_warnedMissingPlayerCamera = true;
			}

			return;
		}

		if ( !PlayerCamera.TryGetAdsReferenceTransform( out var adsReference ) )
			return;

		var baseSightPosition =
			adsReference.Position
			+ adsReference.Rotation.Forward * AdsSightForwardDistance
			+ adsReference.Rotation * AdsSightLocalOffset;

		var baseSightTarget =
			global::Transform.Zero
				.WithPosition( baseSightPosition )
				.WithRotation(
					adsReference.Rotation
					* AdsSightRotationOffset.ToRotation()
				);

		_sharedAdsSightTarget = ApplySharedSway( baseSightTarget );

		_sharedAdsWeaponWorld =
			CalculateMarkerAlignment(
				_sightLocal,
				_sharedAdsSightTarget
			);

		_sharedAdsHandTarget =
			TransformMarkerToWorld(
				_sharedAdsWeaponWorld,
				_weaponHandLocal
			);

		_hasSharedAdsPose = true;
	}

	private global::Transform ApplySharedSway(
		global::Transform baseTarget
	)
	{
		if (
			!EnableSway
			|| PlayerState.CurrentHoldType != KodokuPlayerState.HoldType.Pistol
		)
		{
			return baseTarget;
		}

		var localPositionOffset = new Vector3(
			0.0f,
			_swayX * SwayHorizontalPosition,
			_swayY * SwayVerticalPosition
		);

		var localRotationOffset = new Angles(
			_swayY * SwayPitchDegrees,
			_swayX * SwayYawDegrees,
			_swayX * SwayRollDegrees
		).ToRotation();

		return baseTarget
			.WithPosition(
				baseTarget.Position
				+ baseTarget.Rotation * localPositionOffset
			)
			.WithRotation(
				baseTarget.Rotation * localRotationOffset
			);
	}

	public bool TryGetAdsHandTarget(
		out global::Transform targetWorld
	)
	{
		targetWorld = global::Transform.Zero;

		if (
			!PlayerState.IsValid()
			|| IsReloadOverrideActive
		)
		{
			return false;
		}

		EnsureWeaponSelection();
		EnsureSharedAdsState();

		if ( !_hasSharedAdsPose )
			return false;

		// Camera/sway/root motion always comes from the current shared ADS pose,
		// so the hand does not inherit the one-frame world-space lag of the
		// weapon renderer. Only the weapon-local animated grip is carried over
		// from the most recently evaluated weapon AnimGraph.
		if (
			FollowAnimatedWeaponHand
			&& _hasAnimatedWeaponHandLocal
		)
		{
			targetWorld =
				TransformMarkerToWorld(
					_sharedAdsWeaponWorld,
					_animatedWeaponHandLocal
				);

			return true;
		}

		targetWorld = _sharedAdsHandTarget;
		return true;
	}

	private void CaptureAnimatedWeaponHandLocal()
	{
		if (
			!FollowAnimatedWeaponHand
			|| IsReloadOverrideActive
			|| IsProxy
			|| !PlayerState.IsValid()
			|| !PlayerState.IsAiming
			|| !ActiveWeaponInstance.IsValid()
			|| !EnsureWeaponRenderer()
			|| _weaponRenderer.BoneMergeTarget.IsValid()
			|| string.IsNullOrWhiteSpace( WeaponHandBoneName )
		)
		{
			return;
		}

		if (
			!_weaponRenderer.TryGetBoneTransform(
				WeaponHandBoneName,
				out var animatedHandWorld
			)
		)
		{
			_hasAnimatedWeaponHandLocal = false;
			return;
		}

		_animatedWeaponHandLocal =
			ActiveWeaponInstance.WorldTransform.ToLocal(
				animatedHandWorld
			);

		_hasAnimatedWeaponHandLocal = true;
	}

	private static global::Transform TransformMarkerToWorld(
		global::Transform rootWorld,
		global::Transform markerLocal
	)
	{
		return global::Transform.Zero
			.WithPosition(
				rootWorld.PointToWorld(
					markerLocal.Position
				)
			)
			.WithRotation(
				rootWorld.Rotation
				* markerLocal.Rotation
			);
	}

	private void ResetAnimatedWeaponHandState()
	{
		_animatedWeaponHandLocal = global::Transform.Zero;
		_hasAnimatedWeaponHandLocal = false;
	}

	private void UpdateWeaponWorldAlignment()
	{
		if (
			IsReloadOverrideActive
			|| !ActiveWeaponInstance.IsValid()
		)
		{
			return;
		}

		var bodyRenderer = EquipmentVisuals?.BodyRenderer;

		if ( !bodyRenderer.IsValid() )
		{
			if ( !_warnedMissingBodyRenderer )
			{
				Log.Warning(
					$"{nameof( KodokuWeaponVisualController )}: "
					+ "BodyRenderer is invalid."
				);

				_warnedMissingBodyRenderer = true;
			}

			return;
		}

		if ( !EnsureWeaponRenderer() )
			return;

		if ( _weaponRenderer.BoneMergeTarget.IsValid() )
		{
			_weaponRenderer.BoneMergeTarget = null;
		}

		var useAdsAlignment =
			!IsProxy
			&& AlignSightToAimEye
			&& PlayerState.IsValid()
			&& PlayerState.IsAiming;

		if ( useAdsAlignment )
		{
			EnsureSharedAdsState();

			if ( _hasSharedAdsPose )
			{
				ActiveWeaponInstance.WorldTransform =
					_sharedAdsWeaponWorld;
			}

			return;
		}

		AlignWeaponHandToPlayerHand( bodyRenderer );
	}

	private void AlignWeaponHandToPlayerHand(
		SkinnedModelRenderer bodyRenderer
	)
	{
		if (
			string.IsNullOrWhiteSpace( PlayerHandBoneName )
			|| !bodyRenderer.TryGetBoneTransform(
				PlayerHandBoneName,
				out var playerHandTransform
			)
		)
		{
			if ( !_warnedMissingPlayerHand )
			{
				Log.Warning(
					$"{nameof( KodokuWeaponVisualController )}: "
					+ $"player hand bone '{PlayerHandBoneName}' was not found."
				);

				_warnedMissingPlayerHand = true;
			}

			return;
		}

		if ( !EnsureWeaponHandLocal() )
			return;

		ApplyCachedMarkerAlignment(
			_weaponHandLocal,
			playerHandTransform
		);
	}

	private bool EnsureWeaponRenderer()
	{
		if ( _weaponRenderer.IsValid() )
			return true;

		if ( ActiveWeaponInstance.IsValid() )
		{
			_weaponRenderer =
				ActiveWeaponInstance.GetComponentInChildren<SkinnedModelRenderer>();
		}

		if ( _weaponRenderer.IsValid() )
			return true;

		if ( !_warnedMissingWeaponRenderer )
		{
			Log.Warning(
				$"{nameof( KodokuWeaponVisualController )}: "
				+ "weapon SkinnedModelRenderer is invalid."
			);

			_warnedMissingWeaponRenderer = true;
		}

		return false;
	}

	private bool EnsureWeaponHandLocal()
	{
		if ( _hasWeaponHandLocal )
			return true;

		if ( !EnsureWeaponRenderer() )
			return false;

		if ( !TryCacheWeaponMarkerLocal(
			WeaponHandBoneName,
			out _weaponHandLocal
		) )
		{
			if ( !_warnedMissingWeaponHand )
			{
				Log.Warning(
					$"{nameof( KodokuWeaponVisualController )}: "
					+ $"weapon hand bone '{WeaponHandBoneName}' was not found."
				);

				_warnedMissingWeaponHand = true;
			}

			return false;
		}

		_hasWeaponHandLocal = true;
		return true;
	}

	private bool EnsureSightLocal()
	{
		if ( _hasSightLocal )
			return true;

		if ( !EnsureWeaponRenderer() )
			return false;

		if ( !TryCacheWeaponMarkerLocal(
			SightBoneName,
			out _sightLocal
		) )
		{
			if ( !_warnedMissingSight )
			{
				Log.Warning(
					$"{nameof( KodokuWeaponVisualController )}: "
					+ $"weapon sight bone '{SightBoneName}' was not found."
				);

				_warnedMissingSight = true;
			}

			return false;
		}

		_hasSightLocal = true;
		return true;
	}

	private bool TryCacheWeaponMarkerLocal(
		string boneName,
		out global::Transform markerLocal
	)
	{
		markerLocal = global::Transform.Zero;

		if (
			string.IsNullOrWhiteSpace( boneName )
			|| !_weaponRenderer.TryGetBoneTransform(
				boneName,
				out var markerWorld
			)
		)
		{
			return false;
		}

		markerLocal =
			ActiveWeaponInstance.WorldTransform.ToLocal(
				markerWorld
			);

		return true;
	}

	private global::Transform CalculateMarkerAlignment(
		global::Transform markerLocal,
		global::Transform targetWorld
	)
	{
		var desiredWeaponRotation =
			targetWorld.Rotation
			* markerLocal.Rotation.Inverse;

		var desiredWeaponWithoutPosition =
			global::Transform.Zero
				.WithRotation( desiredWeaponRotation )
				.WithScale( _weaponWorldScale );

		var markerOffsetWorld =
			desiredWeaponWithoutPosition.PointToWorld(
				markerLocal.Position
			);

		var desiredWeaponPosition =
			targetWorld.Position
			- markerOffsetWorld;

		return desiredWeaponWithoutPosition
			.WithPosition( desiredWeaponPosition );
	}

	private void ApplyCachedMarkerAlignment(
		global::Transform markerLocal,
		global::Transform targetWorld
	)
	{
		ActiveWeaponInstance.WorldTransform =
			CalculateMarkerAlignment(
				markerLocal,
				targetWorld
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
			- System.MathF.Exp( -speed * deltaTime );
	}

	private GameObject ResolvePrefab()
	{
		return PlayerState.CurrentHoldType switch
		{
			KodokuPlayerState.HoldType.Pistol
				=> PistolPrefab,

			KodokuPlayerState.HoldType.Rifle
				=> RiflePrefab,

			_ => null
		};
	}

	private void DestroyCurrentWeapon()
	{
		if ( Animator.IsValid() )
		{
			Animator.SetEquippedItemAnimator( null );
			Animator.SetEquippedWeaponFireController( null );
		}

		if (
			ActiveWeaponInstance.IsValid()
			&& EquipmentVisuals.IsValid()
		)
		{
			EquipmentVisuals.DestroyEquipment(
				ActiveWeaponInstance
			);
		}

		ActiveWeaponInstance = null;
		_weaponRenderer = null;
		_hasWeaponHandLocal = false;
		_hasSightLocal = false;
		_weaponWorldScale = Vector3.One;
		ResetSwayState();
		ResetSharedAdsState();
		ResetAnimatedWeaponHandState();
	}

	private void ResetSharedAdsState()
	{
		_hasSharedAdsPose = false;
		_sharedAdsUpdateTime = float.NegativeInfinity;
		_sharedAdsSightTarget = global::Transform.Zero;
		_sharedAdsWeaponWorld = global::Transform.Zero;
		_sharedAdsHandTarget = global::Transform.Zero;
	}

	private void ResetAlignmentWarnings()
	{
		_warnedMissingBodyRenderer = false;
		_warnedMissingWeaponRenderer = false;
		_warnedMissingPlayerHand = false;
		_warnedMissingWeaponHand = false;
		_warnedMissingPlayerCamera = false;
		_warnedMissingSight = false;
	}
}
using Sandbox;

namespace Kodoku;

/// <summary>
/// Full body first-person camera.
/// The normal view follows the animated head bone and blends to aim_eye.r while aiming.
/// During ADS, the exact same smoothed presentation transform is also exposed to
/// the weapon and hand IK systems so they cannot drift from the rendered camera.
/// </summary>
[Title( "Kodoku Full Body Camera" )]
[Category( "Kodoku/Player" )]
[Icon( "videocam" )]
public sealed class KodokuPlayerCamera : Component, ICameraModifier
{
	[Property, Group( "References" )]
	public KodokuPlayerState PlayerState { get; set; }

	[Property, Group( "References" )]
	public SkinnedModelRenderer BodyRenderer { get; set; }

	[Property, Group( "Bones" )]
	public string CameraBoneName { get; set; } = "head";

	[Property, Group( "Bones" )]
	public string AimEyeBoneName { get; set; } = "aim_eye.r";

	[Property, Group( "Bones" )]
	public string AimEyeFallbackBoneName { get; set; } = "aim_eye_r";

	[Property, Group( "Position" )]
	public Vector3 CameraBoneOffset { get; set; }

	[Property, Group( "Position" )]
	public Vector3 AimEyeOffset { get; set; }

	[Property, Group( "Position" )]
	public float FallbackEyeHeight { get; set; } = 64.0f;

	[Property, Group( "Position" )]
	public float PositionSmoothing { get; set; } = 24.0f;

	[Property, Group( "Rotation" ), Range( 0.0f, 1.0f )]
	public float HeadRotationInfluence { get; set; } = 0.25f;

	[Property, Group( "Rotation" )]
	public float RotationSmoothing { get; set; } = 24.0f;

	[Property, Group( "Aiming" )]
	public float AimTransitionSpeed { get; set; } = 12.0f;

	[Property, Group( "Rendering" )]
	public string FirstPersonHiddenTag { get; set; } = "fps_hide_head";

	[Property, Group( "Rendering" )]
	public bool UseFovFromPreferences { get; set; } = true;

	private Vector3 _smoothedPosition;
	private Rotation _smoothedRotation = Rotation.Identity;
	private float _aimWeight;
	private bool _cameraInitialized;
	private float _presentationUpdateTime = float.NegativeInfinity;

	private bool _headReferenceInitialized;
	private Rotation _headReferenceLocalRotation = Rotation.Identity;

	private bool _warnedMissingCameraBone;
	private bool _warnedMissingAimBone;

	protected override void OnAwake()
	{
		base.OnAwake();

		PlayerState ??= GetComponent<KodokuPlayerState>();
		BodyRenderer ??= GetComponentInChildren<SkinnedModelRenderer>();
	}

	int ICameraModifier.CameraOrder => 0;

	void ICameraModifier.ModifyCamera(
		CameraComponent camera,
		ref CameraView view
	)
	{
		if ( IsProxy || Scene.Camera != camera )
			return;

		if ( !PlayerState.IsValid() || !BodyRenderer.IsValid() )
			return;

		if (
			!string.IsNullOrWhiteSpace( FirstPersonHiddenTag )
			&& !camera.RenderExcludeTags.Contains( FirstPersonHiddenTag )
		)
		{
			camera.RenderExcludeTags.Add( FirstPersonHiddenTag );
		}

		EnsurePresentationTransform();

		if ( !_cameraInitialized )
			return;

		view.Position = _smoothedPosition;
		view.Rotation = _smoothedRotation;

		if ( UseFovFromPreferences )
		{
			view.FieldOfView = Preferences.FieldOfView;
		}
	}

	/// <summary>
	/// Returns the exact presentation transform used by this camera for the
	/// current frame. Weapon ADS alignment and hand IK both consume this value.
	/// Calling this during Update is safe: the first caller builds the shared
	/// presentation state and ModifyCamera reuses that exact state later.
	/// </summary>
	public bool TryGetAdsReferenceTransform(
		out global::Transform transform
	)
	{
		transform = global::Transform.Zero;

		if (
			IsProxy
			|| !PlayerState.IsValid()
			|| !BodyRenderer.IsValid()
		)
		{
			return false;
		}

		EnsurePresentationTransform();

		if ( !_cameraInitialized )
			return false;

		transform =
			global::Transform.Zero
				.WithPosition( _smoothedPosition )
				.WithRotation( _smoothedRotation );

		return true;
	}

	private void EnsurePresentationTransform()
	{
		if ( !PlayerState.IsValid() || !BodyRenderer.IsValid() )
			return;

		var now = Time.Now;

		if ( _presentationUpdateTime == now )
			return;

		_presentationUpdateTime = now;

		var headTransform = ResolveCameraBone();
		var aimTransform = ResolveAimBone( headTransform );

		_aimWeight = _aimWeight.LerpTo(
			PlayerState.IsAiming ? 1.0f : 0.0f,
			ExponentialBlend( AimTransitionSpeed, Time.Delta )
		);

		var aimWeight = _aimWeight.Clamp( 0.0f, 1.0f );

		var normalPosition =
			headTransform.Position
			+ headTransform.Rotation * CameraBoneOffset;

		var aimPosition =
			aimTransform.Position
			+ aimTransform.Rotation * AimEyeOffset;

		var targetPosition = Vector3.Lerp(
			normalPosition,
			aimPosition,
			aimWeight
		);

		var normalRotation = CalculateCameraRotation( headTransform );

		// At full ADS the camera rotation is the gameplay EyeRotation exactly.
		// This removes animated-head rotation from the sight alignment while
		// preserving it during the normal first-person view and ADS transition.
		var targetRotation = Rotation.Slerp(
			normalRotation,
			PlayerState.EyeRotation,
			aimWeight
		);

		if ( !_cameraInitialized )
		{
			_smoothedPosition = targetPosition;
			_smoothedRotation = targetRotation;
			_cameraInitialized = true;
			return;
		}

		_smoothedPosition = Vector3.Lerp(
			_smoothedPosition,
			targetPosition,
			ExponentialBlend( PositionSmoothing, Time.Delta )
		);

		_smoothedRotation = Rotation.Slerp(
			_smoothedRotation,
			targetRotation,
			ExponentialBlend( RotationSmoothing, Time.Delta )
		);
	}

	private Transform ResolveCameraBone()
	{
		if (
			!string.IsNullOrWhiteSpace( CameraBoneName )
			&& BodyRenderer.TryGetBoneTransform(
				CameraBoneName,
				out var transform
			)
		)
		{
			return transform;
		}

		if ( !_warnedMissingCameraBone )
		{
			Log.Warning(
				$"{nameof( KodokuPlayerCamera )}: "
				+ $"bone '{CameraBoneName}' was not found. "
				+ "Using the fallback eye height."
			);

			_warnedMissingCameraBone = true;
		}

		return new Transform(
			WorldPosition + Vector3.Up * FallbackEyeHeight,
			PlayerState.EyeRotation
		);
	}

	private Transform ResolveAimBone( Transform fallback )
	{
		if (
			!string.IsNullOrWhiteSpace( AimEyeBoneName )
			&& BodyRenderer.TryGetBoneTransform(
				AimEyeBoneName,
				out var transform
			)
		)
		{
			return transform;
		}

		if (
			!string.IsNullOrWhiteSpace( AimEyeFallbackBoneName )
			&& BodyRenderer.TryGetBoneTransform(
				AimEyeFallbackBoneName,
				out transform
			)
		)
		{
			return transform;
		}

		if ( PlayerState.IsAiming && !_warnedMissingAimBone )
		{
			Log.Warning(
				$"{nameof( KodokuPlayerCamera )}: "
				+ $"bones '{AimEyeBoneName}' and "
				+ $"'{AimEyeFallbackBoneName}' were not found. "
				+ "ADS remains on the head bone."
			);

			_warnedMissingAimBone = true;
		}

		return fallback;
	}

	private Rotation CalculateCameraRotation( Transform headTransform )
	{
		var eyeRotation = PlayerState.EyeRotation;
		var currentHeadLocal =
			BodyRenderer.WorldRotation.Inverse
			* headTransform.Rotation;

		if ( !_headReferenceInitialized )
		{
			_headReferenceLocalRotation = currentHeadLocal;
			_headReferenceInitialized = true;
		}

		var animatedHeadDelta =
			_headReferenceLocalRotation.Inverse
			* currentHeadLocal;

		var influencedDelta = Rotation.Slerp(
			Rotation.Identity,
			animatedHeadDelta,
			HeadRotationInfluence.Clamp( 0.0f, 1.0f )
		);

		return eyeRotation * influencedDelta;
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
}

using Sandbox;

namespace Kodoku;

public sealed partial class KodokuCharacterMotor
{
	private void UpdateWishVelocity()
	{
		var running = _wantsRun
			&& !_wantsDuck
			&& _moveInput.Length > 0.05f;

		if ( PlayerState.IsValid() )
		{
			PlayerState.SetRunning( running );
		}

		var speed = _wantsDuck
			? DuckedSpeed
			: running
				? RunSpeed
				: WalkSpeed;

		WishVelocity = _moveRotation * _moveInput * speed;
	}

	private void ApplyHorizontalMovement()
	{
		var worldVelocity = Body.Velocity;
		var groundVelocity = IsGrounded ? GroundVelocity : Vector3.Zero;
		var relativeVelocity = worldVelocity - groundVelocity;

		var currentHorizontal = relativeVelocity.WithZ( 0.0f );
		var targetHorizontal = WishVelocity.WithZ( 0.0f );

		if ( IsGrounded && targetHorizontal.IsNearlyZero( 0.01f ) )
		{
			currentHorizontal = ApplyFriction( currentHorizontal );
		}
		else
		{
			var acceleration = IsGrounded
				? GroundAcceleration
				: AirAcceleration;

			currentHorizontal = MoveTowards(
				currentHorizontal,
				targetHorizontal,
				acceleration * Time.Delta
			);
		}

		var verticalVelocity = worldVelocity.z;

		Body.Velocity =
			currentHorizontal
			+ groundVelocity.WithZ( 0.0f )
			+ Vector3.Up * verticalVelocity;
	}

	private Vector3 ApplyFriction( Vector3 velocity )
	{
		var speed = velocity.Length;

		if ( speed < 0.01f )
			return Vector3.Zero;

		var control = System.MathF.Max( speed, StopSpeed );
		var drop = control * GroundFriction * Time.Delta;
		var newSpeed = System.MathF.Max( speed - drop, 0.0f );

		if ( newSpeed <= 0.0f )
			return Vector3.Zero;

		return velocity * (newSpeed / speed);
	}

	private void TryJump()
	{
		if ( !_jumpRequested )
			return;

		_jumpRequested = false;

		if ( PlayerState.IsValid() && PlayerState.IsDucking )
			return;

		if ( TimeSinceGrounded > CoyoteTime )
			return;

		if ( _timeSinceJump < JumpCooldown )
			return;

		if ( JumpSpeed <= 0.0f )
			return;

		_timeSinceJump = 0.0f;
		_groundingPrevented = 0.2f;

		var velocity = Body.Velocity;
		velocity.z = System.MathF.Max( velocity.z, JumpSpeed );
		Body.Velocity = velocity;

		ClearGround();
	}

	private void UpdateCrouching()
	{
		var wantsDuck = _wantsDuck;

		if (
			PlayerState.IsValid()
			&& !wantsDuck
			&& PlayerState.IsDucking
			&& !CanStandUp()
		)
		{
			wantsDuck = true;
		}

		if ( PlayerState.IsValid() )
		{
			PlayerState.SetDucking( wantsDuck );
		}

		var targetHeight = wantsDuck
			? DuckedHeight
			: StandingHeight;

		CurrentHeight = MoveTowards(
			CurrentHeight,
			targetHeight,
			CrouchResizeSpeed * Time.Delta
		);

		UpdateColliderGeometry();
	}

	private bool CanStandUp()
	{
		var tracePosition = WorldPosition + Vector3.Up * (Skin * 2.0f);
		var result = TraceBody(
			tracePosition,
			tracePosition,
			StandingHeight,
			0.9f
		);

		return !result.StartedSolid;
	}

	private void TryStepUp()
	{
		if ( !IsGrounded || StepHeight <= 0.0f )
			return;

		var horizontalVelocity = (Body.Velocity - GroundVelocity).WithZ( 0.0f );

		if ( horizontalVelocity.Length < 10.0f )
			return;

		var direction = horizontalVelocity.Normal;
		var from = WorldPosition;
		var forward = direction * StepCheckDistance;

		var lowTrace = TraceBody(
			from + Vector3.Up * Skin,
			from + forward + Vector3.Up * Skin,
			CurrentHeight,
			0.9f
		);

		if ( !lowTrace.Hit )
			return;

		var raisedFrom = from + Vector3.Up * StepHeight;

		var highTrace = TraceBody(
			raisedFrom,
			raisedFrom + forward,
			CurrentHeight,
			0.9f
		);

		if ( highTrace.StartedSolid || highTrace.Hit )
			return;

		var downStart = highTrace.EndPosition + Vector3.Up * GroundProbeStart;
		var downEnd = highTrace.EndPosition + Vector3.Down * (StepHeight + GroundProbeDistance);

		var downTrace = Scene.Trace
			.Ray( downStart, downEnd )
			.Radius( BodyRadius * GroundProbeRadiusScale )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithCollisionRules( Tags )
			.Run();

		if ( !downTrace.Hit || !IsStandableSurface( downTrace ) )
			return;

		var heightDelta = downTrace.HitPosition.z - from.z;

		if ( heightDelta <= Skin || heightDelta > StepHeight + Skin )
			return;

		WorldPosition = new Vector3(
			highTrace.EndPosition.x,
			highTrace.EndPosition.y,
			downTrace.HitPosition.z + Skin
		);

		Transform.ClearInterpolation();

		var velocity = Body.Velocity;
		velocity.z = 0.0f;
		Body.Velocity = velocity;
	}

	private static Vector3 MoveTowards(
		Vector3 current,
		Vector3 target,
		float maximumDelta
	)
	{
		var delta = target - current;
		var distance = delta.Length;

		if ( distance <= maximumDelta || distance <= 0.0001f )
			return target;

		return current + delta / distance * maximumDelta;
	}

	private static float MoveTowards(
		float current,
		float target,
		float maximumDelta
	)
	{
		if (
			System.MathF.Abs( target - current )
			<= maximumDelta
		)
		{
			return target;
		}

		return current
			+ System.MathF.Sign( target - current )
			* maximumDelta;
	}
}

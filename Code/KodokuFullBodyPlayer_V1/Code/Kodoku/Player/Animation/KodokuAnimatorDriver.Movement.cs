using Sandbox;

namespace Kodoku;

public sealed partial class KodokuAnimatorDriver
{
	private Vector3 _smoothedMovement;

	private void UpdateMovementParameters()
	{
		if ( !Motor.IsValid() )
		{
			MoveX = 0.0f;
			MoveY = 0.0f;

			BodyRenderer.Set( Parameters.MoveX, MoveX );
			BodyRenderer.Set( Parameters.MoveY, MoveY );
			return;
		}

		var forwardAmount = BodyRenderer.WorldRotation.Forward.Dot(
			Motor.WishVelocity
		);

		var sideAmount = BodyRenderer.WorldRotation.Right.Dot(
			Motor.WishVelocity
		);

		var localWish = new Vector3(
			forwardAmount,
			sideAmount,
			0.0f
		);

		var running = PlayerState.IsValid() && PlayerState.IsRunning;
		var ducking = PlayerState.IsValid() && PlayerState.IsDucking;

		var graphMagnitude = running ? 100.0f : 50.0f;

		if ( ducking )
		{
			graphMagnitude = 50.0f;
		}

		var referenceSpeed = ducking
			? Motor.DuckedSpeed
			: running
				? Motor.RunSpeed
				: Motor.WalkSpeed;

		var inputStrength = referenceSpeed <= 0.001f
			? 0.0f
			: (
				Motor.WishVelocity.Length
				/ referenceSpeed
			).Clamp( 0.0f, 1.0f );

		var target = Vector3.Zero;

		if (
			Motor.IsGrounded
			&& !localWish.IsNearlyZero( 0.01f )
		)
		{
			target = localWish.Normal * graphMagnitude * inputStrength;
		}

		_smoothedMovement = Vector3.Lerp(
			_smoothedMovement,
			target,
			ExponentialBlend( MovementBlendSpeed, Time.Delta )
		);

		MoveX = _smoothedMovement.x.Clamp( -100.0f, 100.0f );
		MoveY = _smoothedMovement.y.Clamp( -100.0f, 100.0f );

		BodyRenderer.Set( Parameters.MoveX, MoveX );
		BodyRenderer.Set( Parameters.MoveY, MoveY );
	}
}

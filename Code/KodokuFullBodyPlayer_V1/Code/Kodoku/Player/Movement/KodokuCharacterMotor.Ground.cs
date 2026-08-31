using Sandbox;

namespace Kodoku;

public sealed partial class KodokuCharacterMotor
{
	private void CategorizeGround()
	{
		if ( _groundingPrevented > 0.0f || Body.Velocity.z > 40.0f )
		{
			ClearGround();
			return;
		}

		var from = WorldPosition + Vector3.Up * GroundProbeStart;
		var to = WorldPosition + Vector3.Down * GroundProbeDistance;

		var trace = Scene.Trace
			.Ray( from, to )
			.Radius( BodyRadius * GroundProbeRadiusScale )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithCollisionRules( Tags )
			.Run();

		if ( !trace.Hit || !IsStandableSurface( trace ) )
		{
			ClearGround();
			return;
		}

		var wasGrounded = IsGrounded;

		IsGrounded = true;
		GroundObject = trace.GameObject;
		GroundCollider = trace.Component as Collider;
		GroundNormal = trace.Normal;
		GroundVelocity = GetGroundVelocity();

		TimeSinceGrounded = 0.0f;

		if ( !wasGrounded )
		{
			var velocity = Body.Velocity;

			if ( velocity.z < 0.0f )
			{
				velocity.z = 0.0f;
				Body.Velocity = velocity;
			}
		}
	}

	private void ClearGround()
	{
		if ( IsGrounded )
		{
			TimeSinceUngrounded = 0.0f;
		}

		IsGrounded = false;
		GroundObject = null;
		GroundCollider = null;
		GroundNormal = Vector3.Up;
		GroundVelocity = Vector3.Zero;
	}

	private Vector3 GetGroundVelocity()
	{
		if ( GroundCollider.IsValid() )
		{
			return GroundCollider.GetVelocityAtPoint( WorldPosition );
		}

		return Vector3.Zero;
	}

	private bool IsStandableSurface( in SceneTraceResult result )
	{
		return Vector3.GetAngle( Vector3.Up, result.Normal ) <= MaxGroundAngle;
	}

	public BBox BodyBox(
		float height,
		float radiusScale = 1.0f
	)
	{
		var radius = BodyRadius * radiusScale;

		return new BBox(
			new Vector3( -radius, -radius, Skin ),
			new Vector3( radius, radius, height - Skin )
		);
	}

	public SceneTraceResult TraceBody(
		Vector3 from,
		Vector3 to,
		float height,
		float radiusScale = 1.0f
	)
	{
		return Scene.Trace
			.Box( BodyBox( height, radiusScale ), from, to )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithCollisionRules( Tags )
			.Run();
	}
}

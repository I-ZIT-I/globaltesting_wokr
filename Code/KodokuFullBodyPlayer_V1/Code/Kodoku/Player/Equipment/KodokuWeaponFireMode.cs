using Sandbox;

namespace Kodoku;

/// <summary>
/// Base class for a weapon firing behaviour.
/// Add a new weapon firing system by deriving from this class.
/// It does not assume that every weapon uses a raycast.
/// </summary>
public abstract class KodokuWeaponFireMode : Component
{
	public abstract void Fire(
		KodokuWeaponFireContext context
	);

	/// <summary>
	/// Shared helper for fire modes that use scene traces.
	/// </summary>
	protected SceneTraceResult Trace(
		KodokuWeaponFireContext context,
		Vector3 direction,
		float range
	)
	{
		if ( range <= 0.0f )
			return default;

		direction = direction.Normal;

		var start = context.Origin;
		var end = start + direction * range;

		var traceBuilder = Scene.Trace
			.Ray( start, end )
			.UseHitboxes( true );

		if ( context.Owner.IsValid() )
		{
			traceBuilder =
				traceBuilder.IgnoreGameObjectHierarchy(
					context.Owner
				);
		}

		var result = traceBuilder.Run();

		if (
			context.Controller.IsValid()
			&& context.Controller.DrawDebugTraces
		)
		{
			DebugOverlay.Trace(
				result,
				context.Controller.DebugTraceDuration,
				context.Controller.DebugTraceOverlay
			);
		}

		if ( context.Controller.IsValid() )
		{
			context.Controller.ReportTraceResult(
				result
			);
		}

		return result;
	}
}

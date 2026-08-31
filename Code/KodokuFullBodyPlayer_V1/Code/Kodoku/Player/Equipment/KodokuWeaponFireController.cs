using Sandbox;

namespace Kodoku;

[Title( "Kodoku Weapon Fire Controller" )]
[Category( "Kodoku/Weapons" )]
[Icon( "gps_fixed" )]
public sealed class KodokuWeaponFireController : Component
{
	[Property, Group( "References" )]
	public GameObject Muzzle { get; set; }

	[Property, Group( "Fire Mode" )]
	public KodokuWeaponFireMode PrimaryFireMode { get; set; }

	[Property, Group( "Debug" )]
	public bool DrawDebugTraces { get; set; } = true;

	[Property, Group( "Debug" ), Range( 0.05f, 10.0f )]
	public float DebugTraceDuration { get; set; } = 2.0f;

	[Property, Group( "Debug" )]
	public bool DebugTraceOverlay { get; set; } = true;

	[Property, Group( "Debug" )]
	public bool LogHits { get; set; } = true;

	protected override void OnAwake()
	{
		base.OnAwake();

		PrimaryFireMode ??=
			GetComponent<KodokuWeaponFireMode>();

		if ( !Muzzle.IsValid() )
		{
			Log.Warning(
				$"{nameof( KodokuWeaponFireController )}: "
				+ $"{GameObject.Name} has no Muzzle assigned."
			);
		}

		if ( !PrimaryFireMode.IsValid() )
		{
			Log.Warning(
				$"{nameof( KodokuWeaponFireController )}: "
				+ $"{GameObject.Name} has no PrimaryFireMode."
			);
		}
	}

	public void Fire()
	{
		if ( !Muzzle.IsValid() )
			return;

		if ( !PrimaryFireMode.IsValid() )
			return;

		var context =
			new KodokuWeaponFireContext(
				GameObject.Root,
				GameObject,
				Muzzle,
				this
			);

		PrimaryFireMode.Fire(
			context
		);
	}

	/// <summary>
	/// Central impact hook for trace-based fire modes.
	/// Damage, surface impacts and hit reactions can be added here later.
	/// </summary>
	public void ReportTraceResult(
		SceneTraceResult trace
	)
	{
		if ( !trace.Hit )
			return;

		if ( LogHits )
		{
			Log.Info(
				$"[WeaponTrace] Hit "
				+ $"{trace.GameObject?.Name} "
				+ $"at {trace.EndPosition}"
			);
		}
	}
	public enum FireDirectionAxis
{
	Forward,
	Backward,
	Right,
	Left,
	Up,
	Down
}

[Property, Group( "References" )]
public FireDirectionAxis DirectionAxis { get; set; }
	= FireDirectionAxis.Forward;

public Vector3 GetFireDirection()
{
	if ( !Muzzle.IsValid() )
		return GameObject.WorldRotation.Forward;

	var rotation = Muzzle.WorldRotation;

	return DirectionAxis switch
	{
		FireDirectionAxis.Forward	=> rotation.Forward,
		FireDirectionAxis.Backward	=> -rotation.Forward,

		FireDirectionAxis.Right		=> rotation.Right,
		FireDirectionAxis.Left		=> -rotation.Right,

		FireDirectionAxis.Up		=> rotation.Up,
		FireDirectionAxis.Down		=> -rotation.Up,

		_ => rotation.Forward
	};
}
}

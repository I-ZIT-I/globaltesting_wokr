using Sandbox;

namespace Kodoku;

[Title( "Scatter Trace Fire Mode" )]
[Category( "Kodoku/Weapons/Fire Modes" )]
[Icon( "flare" )]
public sealed class KodokuScatterTraceFireMode
	: KodokuWeaponFireMode
{
	[Property, Group( "Trace" )]
	public float Range { get; set; } = 3000.0f;

	[Property, Group( "Scatter" ), Range( 1, 64 )]
	public int PelletCount { get; set; } = 8;

	[Property, Group( "Scatter" ), Range( 0.0f, 0.5f )]
	public float Spread { get; set; } = 0.06f;

	public override void Fire(
		KodokuWeaponFireContext context
	)
	{
		var pelletCount =
			System.Math.Max( 1, PelletCount );

		for ( var i = 0; i < pelletCount; i++ )
		{
			var randomOffset =
				Vector3.Random * Spread;

			var direction =
				(
					context.Forward
					+ randomOffset
				).Normal;

			Trace(
				context,
				direction,
				Range
			);
		}
	}
}

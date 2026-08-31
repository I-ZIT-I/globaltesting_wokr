using Sandbox;

namespace Kodoku;

[Title( "Single Trace Fire Mode" )]
[Category( "Kodoku/Weapons/Fire Modes" )]
[Icon( "horizontal_rule" )]
public sealed class KodokuSingleTraceFireMode
	: KodokuWeaponFireMode
{
	[Property, Group( "Trace" )]
	public float Range { get; set; } = 5000.0f;

	public override void Fire(
		KodokuWeaponFireContext context
	)
	{
		Trace(
			context,
			context.Forward,
			Range
		);
	}
}

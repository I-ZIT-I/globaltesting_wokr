using Sandbox;

namespace Kodoku;

[Title( "Equipped Item Animator" )]
[Category( "Kodoku/Equipment" )]
[Icon( "animation" )]
public sealed class EquippedItemAnimator : Component
{
	[Property, Group( "References" )]
	public SkinnedModelRenderer Renderer { get; set; }

	[Property, Group( "Effects" )]
	public GameObject MuzzleGameObject { get; set; }

	[Property, Group( "Effects" )]
	public GameObject ShellEjectGameObject { get; set; }

	[Property, Group( "Effects" )]
	public GameObject MuzzleEffect { get; set; }
		= GameObject.GetPrefab(
			"prefabs/effects/default_muzzleflash.prefab"
		);

	[Property, Group( "Effects" )]
	public GameObject EjectBrass { get; set; }
		= GameObject.GetPrefab(
			"prefabs/effects/default_brasseject.prefab"
		);

	[Property, Group( "Debug" )]
	public bool DebugLogs { get; set; }

	protected override void OnAwake()
	{
		base.OnAwake();

		Renderer ??=
			GetComponentInChildren<SkinnedModelRenderer>();

		if ( !Renderer.IsValid() )
		{
			Log.Warning(
				$"{nameof( EquippedItemAnimator )}: "
				+ $"{GameObject.Name} has no SkinnedModelRenderer."
			);
		}
	}

	public void Attack()
	{
		if ( Renderer.IsValid() )
		{
			Renderer.Set( "b_attack", true );
		}

		DoMuzzleEffect();
		DoEjectBrass();

		if ( DebugLogs )
		{
			Log.Info(
				$"[WeaponAnim] Attack -> {GameObject.Name}"
			);
		}
	}

	public void Reload()
	{
		if ( !Renderer.IsValid() )
			return;

		Renderer.Set( "b_reload", true );

		if ( DebugLogs )
		{
			Log.Info(
				$"[WeaponAnim] Reload -> {GameObject.Name}"
			);
		}
	}

	public void SetAiming( bool aiming )
	{
		if ( !Renderer.IsValid() )
			return;

		Renderer.Set( "b_aim", aiming );
	}

	private void DoMuzzleEffect()
	{
		if ( !MuzzleEffect.IsValid() )
			return;

		if ( !MuzzleGameObject.IsValid() )
			return;

		MuzzleEffect.Clone(
			new CloneConfig
			{
				Parent = MuzzleGameObject,
				Transform = global::Transform.Zero,
				StartEnabled = true
			}
		);
	}

	private void DoEjectBrass()
	{
		if ( !EjectBrass.IsValid() )
			return;

		if ( !ShellEjectGameObject.IsValid() )
			return;

		var effect = EjectBrass.Clone(
			new CloneConfig
			{
				Transform =
					ShellEjectGameObject.WorldTransform
						.WithScale( 1 ),

				StartEnabled = true
			}
		);

		if ( !effect.IsValid() )
			return;

		//
		// Orientation used by the stock S&box brass prefab.
		//
		effect.WorldRotation =
			effect.WorldRotation
			* new Angles( 90, 0, 0 );

		var ejectDirection =
			ShellEjectGameObject.WorldRotation.Forward * 250f
			+
			(
				ShellEjectGameObject.WorldRotation.Right
				+ Vector3.Random * -0.35f
			) * 250f;

		var rb =
			effect.GetComponentInChildren<Rigidbody>();

		if ( !rb.IsValid() )
			return;

		rb.Velocity = ejectDirection;

		rb.AngularVelocity =
			ShellEjectGameObject.WorldRotation.Right
			* 50f;
	}
}
using Sandbox;

namespace Kodoku;

[Title( "Kodoku Equipment Hand IK Driver" )]
[Category( "Kodoku/Equipment" )]
[Icon( "pan_tool" )]
public sealed class KodokuEquipmentHandIkDriver : Component
{
	[Property, Group( "IK" )]
	public string RightHandIkName { get; set; } = "hand_right";

	private KodokuEquipmentVisualController _equipmentVisuals;
	private KodokuWeaponVisualController _weaponVisuals;
	private KodokuPlayerState _playerState;
	private KodokuAnimatorDriver _animator;
	private bool _ownsTarget;

	protected override void OnUpdate()
	{
		UpdateTarget();
	}

	protected override void OnDestroy()
	{
		ClearOwnedTarget();
		base.OnDestroy();
	}

	private void UpdateTarget()
	{
		_equipmentVisuals ??=
			GetComponentInParent<KodokuEquipmentVisualController>();

		_weaponVisuals ??=
			GetComponentInParent<KodokuWeaponVisualController>();

		_playerState ??=
			GetComponentInParent<KodokuPlayerState>();

		_animator ??=
			GetComponentInParent<KodokuAnimatorDriver>();

		var bodyRenderer = _equipmentVisuals?.BodyRenderer;

		if ( !bodyRenderer.IsValid() )
			return;

		// Reload animations own the hands while reload_active is set.
		if ( _animator.IsValid() && _animator.IsReloadAnimationActive )
		{
			ClearOwnedTarget();
			return;
		}

		// Outside ADS the equipment follows the animated right hand. Driving the
		// right-hand IK at the same time would create a transform feedback loop.
		if ( !_playerState.IsValid() || !_playerState.IsAiming )
		{
			ClearOwnedTarget();
			return;
		}

		if (
			string.IsNullOrWhiteSpace( RightHandIkName )
			|| !_weaponVisuals.IsValid()
			|| !_weaponVisuals.TryGetAdsHandTarget(
				out var hybridTarget
			)
		)
		{
			ClearOwnedTarget();
			return;
		}

		// The target is hybrid:
		// - current-frame camera/sway/root alignment from the shared ADS pose;
		// - most recently evaluated weapon-local hand_r animation for recoil,
		//   shoot and other weapon-authored motion.
		//
		// Do this during Update so the body AnimGraph consumes it during its
		// normal animation pass. OnPreRender would be too late for current-frame
		// body bone solving.
		bodyRenderer.SetIk(
			RightHandIkName,
			hybridTarget
		);

		_ownsTarget = true;
	}

	private void ClearOwnedTarget()
	{
		if ( !_ownsTarget )
			return;

		var bodyRenderer = _equipmentVisuals?.BodyRenderer;

		if (
			bodyRenderer.IsValid()
			&& !string.IsNullOrWhiteSpace( RightHandIkName )
		)
		{
			bodyRenderer.ClearIk( RightHandIkName );
		}

		_ownsTarget = false;
	}
}
using Sandbox;

namespace Kodoku;

public sealed partial class KodokuAnimatorDriver
{
	private bool _reloadTriggerPending;

	public void TriggerAttack()
	{
		if ( IsProxy )
			return;

		if ( ActiveWeaponFireController.IsValid() )
		{
			ActiveWeaponFireController.Fire();
		}

		BroadcastAttack();
	}

	public void TriggerReload()
	{
		if ( IsProxy )
			return;

		if ( IsReloadAnimationActive || _reloadTriggerPending )
			return;

		BroadcastReload();
	}

	[Rpc.Broadcast( NetFlags.Unreliable )]
	private void BroadcastAttack()
	{
		if ( BodyRenderer.IsValid() )
		{
			BodyRenderer.Set(
				Parameters.Attack,
				true
			);
		}

		if ( ActiveItemAnimator.IsValid() )
		{
			ActiveItemAnimator.Attack();
		}
	}

	[Rpc.Broadcast]
	private void BroadcastReload()
	{
		if ( IsReloadAnimationActive || _reloadTriggerPending )
			return;

		// Prepare the visual handoff first. The actual reload pulse is sent on the
		// next frame so BoneMerge / IK ownership has already settled before the
		// AnimGraph consumes its auto-reset b_reload parameter.
		BeginReloadAnimationOverride();
		_reloadTriggerPending = true;
		_ = DispatchReloadNextFrame();
	}

	private async System.Threading.Tasks.Task DispatchReloadNextFrame()
	{
		try
		{
			await Task.Frame();
		}
		catch ( System.Threading.Tasks.TaskCanceledException )
		{
			_reloadTriggerPending = false;
			return;
		}

		_reloadTriggerPending = false;

		if ( BodyRenderer.IsValid() )
		{
			BodyRenderer.Set(
				Parameters.Reload,
				true
			);
		}

		if ( ActiveItemAnimator.IsValid() )
		{
			ActiveItemAnimator.Reload();
		}
	}
}

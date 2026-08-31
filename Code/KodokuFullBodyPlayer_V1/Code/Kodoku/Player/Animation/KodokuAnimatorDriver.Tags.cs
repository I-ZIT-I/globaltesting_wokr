using Sandbox;

namespace Kodoku;

public sealed partial class KodokuAnimatorDriver
{
	public const string ReloadActiveEventTag = "reload_active";

	public bool IsReloadAnimationActive { get; private set; }

	private GameObject _reloadEquipmentInstance;
	private bool _reloadVisualOverrideActive;

	protected override void OnEnabled()
	{
		base.OnEnabled();
		SubscribeAnimationTagEvents();
	}

	protected override void OnDisabled()
	{
		UnsubscribeAnimationTagEvents();
		EndReloadVisualOverride();
		IsReloadAnimationActive = false;
		base.OnDisabled();
	}

	public void BeginReloadAnimationOverride()
	{
		IsReloadAnimationActive = true;
		BeginReloadVisualOverride();
	}

	private void BeginReloadVisualOverride()
	{
		if ( _reloadVisualOverrideActive )
			return;

		var equipmentVisuals = GetComponent<KodokuEquipmentVisualController>();
		var bodyRenderer = equipmentVisuals?.BodyRenderer;

		if ( !equipmentVisuals.IsValid() || !bodyRenderer.IsValid() )
			return;

		var ikDriver = ActiveItemAnimator.IsValid()
			? ActiveItemAnimator.GetComponentInParent<KodokuEquipmentHandIkDriver>()
			: null;

		var instance = ikDriver.IsValid()
			? ikDriver.GameObject
			: null;

		if ( !instance.IsValid() )
			return;

		// Reload animation owns the arms. The weapon visual controller remains
		// enabled, but it sees IsReloadAnimationActive and temporarily stops
		// camera/hand alignment while this bone-merge override is active.
		bodyRenderer.ClearIk( "hand_right" );

		if ( equipmentVisuals.EquipmentRoot.IsValid() )
		{
			instance.WorldTransform = equipmentVisuals.EquipmentRoot.WorldTransform;
		}

		foreach (
			var renderer in
				instance.GetComponentsInChildren<SkinnedModelRenderer>()
		)
		{
			if ( !renderer.IsValid() )
				continue;

			renderer.SceneModel?.ClearBoneOverrides();
			renderer.BoneMergeTarget = bodyRenderer;
		}

		_reloadEquipmentInstance = instance;
		_reloadVisualOverrideActive = true;
	}

	private void EndReloadVisualOverride()
	{
		if ( !_reloadVisualOverrideActive )
			return;

		if ( _reloadEquipmentInstance.IsValid() )
		{
			foreach (
				var renderer in
					_reloadEquipmentInstance.GetComponentsInChildren<SkinnedModelRenderer>()
			)
			{
				if ( !renderer.IsValid() )
					continue;

				renderer.SceneModel?.ClearBoneOverrides();
				renderer.BoneMergeTarget = null;
			}
		}

		_reloadEquipmentInstance = null;
		_reloadVisualOverrideActive = false;
	}

	private void SubscribeAnimationTagEvents()
	{
		if ( !BodyRenderer.IsValid() )
			return;

		BodyRenderer.OnAnimTagEvent -= HandleAnimTagEvent;
		BodyRenderer.OnAnimTagEvent += HandleAnimTagEvent;
	}

	private void UnsubscribeAnimationTagEvents()
	{
		if ( BodyRenderer.IsValid() )
		{
			BodyRenderer.OnAnimTagEvent -= HandleAnimTagEvent;
		}
	}

	private void HandleAnimTagEvent(
		SceneModel.AnimTagEvent animTagEvent
	)
	{
		if ( animTagEvent.Name != ReloadActiveEventTag )
			return;

		switch ( animTagEvent.Status )
		{
			case SceneModel.AnimTagStatus.Start:
				IsReloadAnimationActive = true;
				BeginReloadVisualOverride();
				break;

			case SceneModel.AnimTagStatus.End:
				IsReloadAnimationActive = false;
				EndReloadVisualOverride();
				break;
		}
	}
}

using Sandbox;

namespace Kodoku;

/// <summary>
/// Generic visual controller for every equipped Kodoku item.
///
/// Every equipped item is a prefab containing one or more
/// SkinnedModelRenderer components. Those renderers are
/// bone-merged to the player's BodyRenderer at runtime.
/// </summary>
[Title( "Kodoku Equipment Visual Controller" )]
[Category( "Kodoku/Equipment" )]
[Icon( "backpack" )]
public sealed class KodokuEquipmentVisualController : Component
{
	[Property, Group( "References" )]
	public SkinnedModelRenderer BodyRenderer { get; set; }

	[Property, Group( "References" )]
	public GameObject EquipmentRoot { get; set; }

	protected override void OnAwake()
	{
		base.OnAwake();

		if ( !BodyRenderer.IsValid() )
		{
			Log.Error(
				$"{nameof( KodokuEquipmentVisualController )}: "
				+ "BodyRenderer is not assigned."
			);
		}

		if ( !EquipmentRoot.IsValid() )
		{
			Log.Error(
				$"{nameof( KodokuEquipmentVisualController )}: "
				+ "EquipmentRoot is not assigned."
			);
		}
	}

	/// <summary>
	/// Creates an equipment prefab and bone-merges every
	/// SkinnedModelRenderer it contains to the player body.
	/// </summary>
	public GameObject SpawnEquipment( GameObject prefab )
	{
		if ( !prefab.IsValid() )
			return null;

		if ( !BodyRenderer.IsValid() )
		{
			Log.Warning(
				$"{nameof( KodokuEquipmentVisualController )}: "
				+ "cannot spawn equipment because BodyRenderer is invalid."
			);

			return null;
		}

		if ( !EquipmentRoot.IsValid() )
		{
			Log.Warning(
				$"{nameof( KodokuEquipmentVisualController )}: "
				+ "cannot spawn equipment because EquipmentRoot is invalid."
			);

			return null;
		}

		var instance = prefab.Clone(
			new CloneConfig
			{
				Parent = EquipmentRoot,
				Transform = global::Transform.Zero,
				StartEnabled = true
			}
		);

		if ( !instance.IsValid() )
			return null;

		instance.Flags |=
			GameObjectFlags.NotSaved
			| GameObjectFlags.NotNetworked;

		foreach (
			var renderer in
				instance.GetComponentsInChildren<SkinnedModelRenderer>()
		)
		{
			if ( !renderer.IsValid() )
				continue;

			renderer.BoneMergeTarget = BodyRenderer;
		}

		// Every spawned equipment instance gets the generic hand-IK bridge.
		// Bone-merged clothing stays ignored by the bridge. Independently
		// animated equipment can expose a hand_r marker and drive hand_right.
		instance.GetOrAddComponent<KodokuEquipmentHandIkDriver>();

		return instance;
	}

	/// <summary>
	/// Removes an equipment instance previously created by this controller.
	/// </summary>
	public void DestroyEquipment( GameObject instance )
	{
		if ( !instance.IsValid() )
			return;

		instance.Destroy();
	}
}

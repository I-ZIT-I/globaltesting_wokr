using Sandbox;
using System;
using System.Collections.Generic;

namespace Kodoku;

/// <summary>
/// Reconstructs synchronized wearable state as local visual instances.
/// The spawned visuals are NotNetworked; only stable wearable IDs are synced.
/// </summary>
[Title( "Kodoku Wearable Visual Controller" )]
[Category( "Kodoku/Equipment" )]
[Icon( "checkroom" )]
public sealed class KodokuWearableVisualController : Component
{
	[Property, Group( "References" )]
	public KodokuEquipmentState EquipmentState { get; set; }

	[Property, Group( "References" )]
	public SkinnedModelRenderer BodyRenderer { get; set; }

	[Property, Group( "References" )]
	public GameObject ClothingRoot { get; set; }

	[Property, Group( "References" )]
	public GameObject EquipmentRoot { get; set; }

	[Property, Group( "References" )]
	public KodokuSocketController SocketController { get; set; }

	/// <summary>
	/// Optional fixed hairstyle controller for the current character.
	/// Wearables only request a HairMode; the hairstyle itself is not spawned
	/// or selected by the equipment system.
	/// </summary>
	[Property, Group( "Hair" )]
	public KodokuHairController HairController { get; set; }

	[Property, Group( "First Person" )]
	public string FirstPersonHiddenTag { get; set; }
		= "fps_hide_head";

	private readonly Dictionary<KodokuWearableSlot, GameObject>
		_activeInstances = new();

	private readonly Dictionary<KodokuWearableSlot, KodokuWearableDefinition>
		_activeDefinitions = new();

	private readonly Dictionary<KodokuWearableSlot, string>
		_appliedIds = new();

	private readonly Dictionary<string, int> _bodyGroupDefaults =
		new( StringComparer.OrdinalIgnoreCase );

	private readonly HashSet<string> _warnedMissingBodyGroups =
		new( StringComparer.OrdinalIgnoreCase );

	protected override void OnAwake()
	{
		base.OnAwake();

		EquipmentState ??=
			GetComponent<KodokuEquipmentState>();

		BodyRenderer ??=
			GetComponentInChildren<SkinnedModelRenderer>();

		SocketController ??=
			GetComponentInChildren<KodokuSocketController>();

		HairController ??=
			GetComponentInChildren<KodokuHairController>();
	}

	protected override void OnEnabled()
	{
		base.OnEnabled();
		RefreshVisuals( true );
	}

	protected override void OnUpdate()
	{
		RefreshVisuals( false );
	}

	protected override void OnDisabled()
	{
		ClearRuntimeVisuals();
		RestoreBodyGroups();
		ApplyHairMode( KodokuHairMode.Free );
		base.OnDisabled();
	}

	[Button( "Force Refresh Wearables", "refresh" )]
	public void ForceRefresh()
	{
		RefreshVisuals( true );
	}

	private void RefreshVisuals( bool force )
	{
		if ( !EquipmentState.IsValid() )
			return;

		var changed = false;

		foreach ( var slot in Enum.GetValues<KodokuWearableSlot>() )
		{
			var wantedId = EquipmentState.GetWearableId( slot ) ?? "";
			_appliedIds.TryGetValue( slot, out var appliedId );

			if (
				!force
				&& string.Equals(
					wantedId,
					appliedId ?? "",
					StringComparison.OrdinalIgnoreCase
				)
			)
			{
				continue;
			}

			ApplySlot( slot, wantedId );
			_appliedIds[slot] = wantedId;
			changed = true;
		}

		if ( !changed )
			return;

		RecalculateBodyMasks();
		RecalculateHairMode();
	}

	private void ApplySlot(
		KodokuWearableSlot slot,
		string wearableId
	)
	{
		DestroySlotInstance( slot );

		if ( string.IsNullOrWhiteSpace( wearableId ) )
			return;

		if (
			!EquipmentState.TryResolveWearable(
				wearableId,
				out var prefab,
				out var definition
			)
		)
		{
			Log.Warning(
				$"{nameof( KodokuWearableVisualController )}: wearable '{wearableId}' is not in the catalog."
			);
			return;
		}

		if ( definition.Slot != slot )
		{
			Log.Warning(
				$"{nameof( KodokuWearableVisualController )}: wearable '{wearableId}' declares '{definition.Slot}' but was synchronized in '{slot}'."
			);
			return;
		}

		var instance = prefab.Clone(
			new CloneConfig
			{
				Parent = ResolveVisualRoot( definition.Category ),
				Transform = global::Transform.Zero,
				StartEnabled = true
			}
		);

		if ( !instance.IsValid() )
			return;

		instance.Flags |=
			GameObjectFlags.NotSaved
			| GameObjectFlags.NotNetworked;

		switch ( definition.AttachmentMode )
		{
			case KodokuWearableAttachmentMode.BoneMerge:
				ApplyBoneMerge( instance );
				break;
			case KodokuWearableAttachmentMode.BoneSocket:
				ApplyBoneSocket( instance, definition );
				break;
		}

		var hideForThisCamera =
			!IsProxy
			&& definition.MustHideForOwnerFirstPerson;

		SetFirstPersonTagRecursively(
			instance,
			hideForThisCamera
		);

		_activeInstances[slot] = instance;
		_activeDefinitions[slot] = definition;
	}

	private GameObject ResolveVisualRoot(
		KodokuWearableCategory category
	)
	{
		if (
			category == KodokuWearableCategory.Clothing
			|| category == KodokuWearableCategory.Appearance
		)
		{
			if ( ClothingRoot.IsValid() )
				return ClothingRoot;
		}
		else if ( EquipmentRoot.IsValid() )
		{
			return EquipmentRoot;
		}

		return GameObject;
	}

	private void ApplyBoneMerge( GameObject instance )
	{
		if ( !BodyRenderer.IsValid() )
		{
			Log.Warning(
				$"{nameof( KodokuWearableVisualController )}: BodyRenderer is invalid."
			);
			return;
		}

		foreach (
			var renderer in
				instance.GetComponentsInChildren<SkinnedModelRenderer>()
		)
		{
			if ( !renderer.IsValid() || renderer == BodyRenderer )
				continue;

			renderer.BoneMergeTarget = BodyRenderer;
		}
	}

	private void ApplyBoneSocket(
		GameObject instance,
		KodokuWearableDefinition definition
	)
	{
		if ( !SocketController.IsValid() )
		{
			Log.Warning(
				$"{nameof( KodokuWearableVisualController )}: '{definition.WearableId}' requires a socket but SocketController is invalid."
			);
			return;
		}

		var socket = SocketController.GetSocket( definition.Socket );

		if ( !socket.IsValid() )
		{
			Log.Warning(
				$"{nameof( KodokuWearableVisualController )}: socket '{definition.Socket}' is invalid for '{definition.WearableId}'."
			);
			return;
		}

		instance.Parent = socket.GameObject;
		instance.LocalPosition = definition.SocketLocalPosition;
		instance.LocalRotation = definition.SocketLocalRotation.ToRotation();
		instance.LocalScale = definition.SocketLocalScale;
	}

	private void RecalculateBodyMasks()
	{
		if ( !BodyRenderer.IsValid() )
			return;

		RestoreBodyGroups();

		foreach ( var definition in _activeDefinitions.Values )
		{
			if ( !definition.IsValid() )
				continue;

			foreach ( var bodyGroupName in definition.HiddenBodyGroups )
			{
				if ( string.IsNullOrWhiteSpace( bodyGroupName ) )
					continue;

				if ( !TryCacheBodyGroupDefault( bodyGroupName ) )
					continue;

				BodyRenderer.SetBodyGroup(
					bodyGroupName,
					definition.HiddenBodyGroupValue
				);
			}
		}
	}

	private bool TryCacheBodyGroupDefault( string bodyGroupName )
	{
		if ( _bodyGroupDefaults.ContainsKey( bodyGroupName ) )
			return true;

		try
		{
			_bodyGroupDefaults[bodyGroupName] =
				BodyRenderer.GetBodyGroup( bodyGroupName );
			return true;
		}
		catch ( Exception )
		{
			if ( _warnedMissingBodyGroups.Add( bodyGroupName ) )
			{
				Log.Warning(
					$"{nameof( KodokuWearableVisualController )}: bodygroup '{bodyGroupName}' was not found on the player model."
				);
			}
			return false;
		}
	}

	private void RestoreBodyGroups()
	{
		if ( !BodyRenderer.IsValid() )
			return;

		foreach ( var pair in _bodyGroupDefaults )
		{
			BodyRenderer.SetBodyGroup( pair.Key, pair.Value );
		}
	}

	private void RecalculateHairMode()
	{
		var finalMode = KodokuHairMode.Free;

		foreach ( var definition in _activeDefinitions.Values )
		{
			if ( !definition.IsValid() )
				continue;

			if ( definition.HairMode > finalMode )
				finalMode = definition.HairMode;
		}

		ApplyHairMode( finalMode );
	}

	private void ApplyHairMode( KodokuHairMode mode )
	{
		if ( HairController.IsValid() )
		{
			HairController.SetHairMode( mode );
		}
	}

	private void DestroySlotInstance( KodokuWearableSlot slot )
	{
		if (
			_activeInstances.TryGetValue( slot, out var instance )
			&& instance.IsValid()
		)
		{
			instance.Destroy();
		}

		_activeInstances.Remove( slot );
		_activeDefinitions.Remove( slot );
	}

	private void ClearRuntimeVisuals()
	{
		foreach ( var instance in _activeInstances.Values )
		{
			if ( instance.IsValid() )
				instance.Destroy();
		}

		_activeInstances.Clear();
		_activeDefinitions.Clear();
		_appliedIds.Clear();
	}

	private void SetFirstPersonTagRecursively(
		GameObject gameObject,
		bool enabled
	)
	{
		if (
			!gameObject.IsValid()
			|| string.IsNullOrWhiteSpace( FirstPersonHiddenTag )
		)
		{
			return;
		}

		gameObject.Tags.Set( FirstPersonHiddenTag, enabled );
		SynchronizeRendererTags( gameObject );

		foreach ( var child in gameObject.Children )
		{
			SetFirstPersonTagRecursively( child, enabled );
		}
	}

	private static void SynchronizeRendererTags( GameObject gameObject )
	{
		foreach ( var renderer in gameObject.GetComponents<ModelRenderer>() )
		{
			if ( !renderer.SceneObject.IsValid() )
				continue;

			renderer.SceneObject.Tags.SetFrom( gameObject.Tags );
		}
	}
}

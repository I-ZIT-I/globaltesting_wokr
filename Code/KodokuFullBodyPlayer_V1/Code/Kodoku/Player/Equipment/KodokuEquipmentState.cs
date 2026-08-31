using Sandbox;
using System;
using System.Collections.Generic;

namespace Kodoku;

/// <summary>
/// Authoritative logical equipment state for one player.
///
/// Only stable wearable IDs are synchronized. Visuals are reconstructed
/// locally by KodokuWearableVisualController.
///
/// The final inventory host handler must validate item ownership before it
/// calls TryEquipAuthoritative / TryUnequipAuthoritative.
/// </summary>
[Title( "Kodoku Equipment State" )]
[Category( "Kodoku/Equipment" )]
[Icon( "inventory_2" )]
public sealed class KodokuEquipmentState : Component
{
	/// <summary>
	/// Prototype registry. Every prefab must contain one
	/// KodokuWearableDefinition on its root and use a unique WearableId.
	/// </summary>
	[Property, Group( "Catalog" )]
	public List<GameObject> WearableCatalog { get; set; } = new();

	[Sync] public string UpperBaseWearableId { get; private set; } = "";
	[Sync] public string ChestArmorWearableId { get; private set; } = "";
	[Sync] public string ChestUtilityWearableId { get; private set; } = "";
	[Sync] public string UpperOuterWearableId { get; private set; } = "";
	[Sync] public string BackWearableId { get; private set; } = "";
	[Sync] public string LowerWearableId { get; private set; } = "";
	[Sync] public string FeetWearableId { get; private set; } = "";
	[Sync] public string FaceWearableId { get; private set; } = "";
	[Sync] public string HeadWearableId { get; private set; } = "";
	[Sync] public string WaistUtilityWearableId { get; private set; } = "";
	[Sync] public string HairWearableId { get; private set; } = "";
	[Sync] public string BeardWearableId { get; private set; } = "";

	[Property, Group( "Debug" )]
	public string DebugWearableId { get; set; } = "";

	protected override void OnAwake()
	{
		base.OnAwake();
		ValidateCatalog();
	}

	protected override void OnValidate()
	{
		ValidateCatalog();
	}

	public string GetWearableId( KodokuWearableSlot slot )
	{
		return slot switch
		{
			KodokuWearableSlot.UpperBase => UpperBaseWearableId,
			KodokuWearableSlot.ChestArmor => ChestArmorWearableId,
			KodokuWearableSlot.ChestUtility => ChestUtilityWearableId,
			KodokuWearableSlot.UpperOuter => UpperOuterWearableId,
			KodokuWearableSlot.Back => BackWearableId,
			KodokuWearableSlot.Lower => LowerWearableId,
			KodokuWearableSlot.Feet => FeetWearableId,
			KodokuWearableSlot.Face => FaceWearableId,
			KodokuWearableSlot.Head => HeadWearableId,
			KodokuWearableSlot.WaistUtility => WaistUtilityWearableId,
			KodokuWearableSlot.Hair => HairWearableId,
			KodokuWearableSlot.Beard => BeardWearableId,
			_ => ""
		};
	}

	public bool IsSlotOccupied( KodokuWearableSlot slot )
	{
		return !string.IsNullOrWhiteSpace( GetWearableId( slot ) );
	}

	public bool TryResolveWearable(
		string wearableId,
		out GameObject prefab,
		out KodokuWearableDefinition definition
	)
	{
		prefab = null;
		definition = null;

		if ( string.IsNullOrWhiteSpace( wearableId ) )
			return false;

		foreach ( var candidate in WearableCatalog )
		{
			if ( !candidate.IsValid() )
				continue;

			var candidateDefinition =
				candidate.GetComponent<KodokuWearableDefinition>();

			if ( !candidateDefinition.IsValid() )
				continue;

			if (
				!string.Equals(
					candidateDefinition.WearableId,
					wearableId,
					StringComparison.OrdinalIgnoreCase
				)
			)
			{
				continue;
			}

			prefab = candidate;
			definition = candidateDefinition;
			return true;
		}

		return TryResolveWearableByConvention(
			wearableId,
			out prefab,
			out definition
		);
	}

	/// <summary>
	/// Prototype convenience fallback: a wearable whose prefab filename matches
	/// its WearableId can be tested without editing the player prefab catalog.
	/// The explicit WearableCatalog remains the primary lookup path.
	/// </summary>
	private static bool TryResolveWearableByConvention(
		string wearableId,
		out GameObject prefab,
		out KodokuWearableDefinition definition
	)
	{
		prefab = null;
		definition = null;

		string[] folders =
		{
			"prefab/clothing",
			"prefab/equipment",
			"prefab/appearance"
		};

		foreach ( var folder in folders )
		{
			var candidate = GameObject.GetPrefab(
				$"{folder}/{wearableId}.prefab"
			);

			if ( !candidate.IsValid() )
				continue;

			var candidateDefinition =
				candidate.GetComponent<KodokuWearableDefinition>();

			if ( !candidateDefinition.IsValid() )
				continue;

			if (
				!string.Equals(
					candidateDefinition.WearableId,
					wearableId,
					StringComparison.OrdinalIgnoreCase
				)
			)
			{
				continue;
			}

			prefab = candidate;
			definition = candidateDefinition;
			return true;
		}

		return false;
	}

	/// <summary>
	/// Slot exclusivity is automatic: another item using the same Slot will
	/// replace the current ID in that slot.
	/// </summary>
	public bool CanEquip(
		string wearableId,
		out KodokuWearableDefinition definition
	)
	{
		definition = null;

		if (
			!TryResolveWearable(
				wearableId,
				out _,
				out var resolvedDefinition
			)
		)
		{
			return false;
		}

		if ( !resolvedDefinition.IsConfigured )
			return false;

		definition = resolvedDefinition;
		return true;
	}

	/// <summary>
	/// Host-only mutation. The future inventory bridge must validate that the
	/// player owns the item before calling this method.
	/// </summary>
	public bool TryEquipAuthoritative( string wearableId )
	{
		if ( !Networking.IsHost )
			return false;

		if ( !CanEquip( wearableId, out var definition ) )
			return false;

		SetWearableId(
			definition.Slot,
			definition.WearableId
		);

		return true;
	}

	public bool TryUnequipAuthoritative(
		KodokuWearableSlot slot
	)
	{
		if ( !Networking.IsHost )
			return false;

		if ( !IsSlotOccupied( slot ) )
			return false;

		SetWearableId( slot, "" );
		return true;
	}
	private void DebugEquipById( string wearableId )
{
	if ( !Networking.IsHost )
	{
		Log.Warning(
			$"{nameof( KodokuEquipmentState )}: debug equip is host-only."
		);
		return;
	}

	if ( !TryEquipAuthoritative( wearableId ) )
	{
		Log.Warning(
			$"{nameof( KodokuEquipmentState )}: unable to equip '{wearableId}'."
		);
	}
}

[Button( "Equip T-Shirt", "checkroom" )]
public void DebugEquipTshirt()
{
	DebugEquipById( "tshirt_base" );
}

[Button( "Equip Jeans", "checkroom" )]
public void DebugEquipJeans()
{
	DebugEquipById( "jean_base" );
}

[Button( "Equip Timber Boots", "checkroom" )]
public void DebugEquipTimberBoots()
{
	DebugEquipById( "timber_boots" );
}

[Button( "Equip Helmet", "checkroom" )]
public void DebugEquipHelmet()
{
	DebugEquipById( "helmet_base" );
}

[Button( "Unequip T-Shirt", "remove_circle_outline" )]
public void DebugUnequipTshirt()
{
	TryUnequipAuthoritative( KodokuWearableSlot.UpperBase );
}

[Button( "Unequip Jeans", "remove_circle_outline" )]
public void DebugUnequipJeans()
{
	TryUnequipAuthoritative( KodokuWearableSlot.Lower );
}

[Button( "Unequip Timber Boots", "remove_circle_outline" )]
public void DebugUnequipTimberBoots()
{
	TryUnequipAuthoritative( KodokuWearableSlot.Feet );
}

[Button( "Unequip Helmet", "remove_circle_outline" )]
public void DebugUnequipHelmet()
{
	TryUnequipAuthoritative( KodokuWearableSlot.Head );
}

	[Button( "Equip Debug Wearable", "checkroom" )]
	public void EquipDebugWearable()
	{
		if ( !Networking.IsHost )
		{
			Log.Warning(
				$"{nameof( KodokuEquipmentState )}: debug equip is host-only."
			);
			return;
		}

		if ( !TryEquipAuthoritative( DebugWearableId ) )
		{
			Log.Warning(
				$"{nameof( KodokuEquipmentState )}: unable to equip '{DebugWearableId}'."
			);
		}
	}

	[Button( "Unequip All Debug", "remove_circle_outline" )]
	public void UnequipAllDebug()
	{
		if ( !Networking.IsHost )
		{
			Log.Warning(
				$"{nameof( KodokuEquipmentState )}: debug unequip is host-only."
			);
			return;
		}

		foreach ( var slot in Enum.GetValues<KodokuWearableSlot>() )
		{
			SetWearableId( slot, "" );
		}
	}

	[Button( "Validate Wearable Catalog", "fact_check" )]
	public void ValidateCatalog()
	{
		var ids = new HashSet<string>(
			StringComparer.OrdinalIgnoreCase
		);

		foreach ( var prefab in WearableCatalog )
		{
			if ( !prefab.IsValid() )
			{
				Log.Warning(
					$"{nameof( KodokuEquipmentState )}: catalog contains an invalid prefab."
				);
				continue;
			}

			var definition =
				prefab.GetComponent<KodokuWearableDefinition>();

			if ( !definition.IsValid() )
			{
				Log.Warning(
					$"{nameof( KodokuEquipmentState )}: '{prefab.Name}' has no {nameof( KodokuWearableDefinition )} on its root."
				);
				continue;
			}

			if ( !definition.IsConfigured )
			{
				Log.Warning(
					$"{nameof( KodokuEquipmentState )}: '{prefab.Name}' has an empty WearableId."
				);
				continue;
			}

			if ( !ids.Add( definition.WearableId ) )
			{
				Log.Error(
					$"{nameof( KodokuEquipmentState )}: duplicate WearableId '{definition.WearableId}'."
				);
			}
		}
	}

	private void SetWearableId(
		KodokuWearableSlot slot,
		string wearableId
	)
	{
		wearableId ??= "";

		switch ( slot )
		{
			case KodokuWearableSlot.UpperBase:
				UpperBaseWearableId = wearableId;
				break;
			case KodokuWearableSlot.ChestArmor:
				ChestArmorWearableId = wearableId;
				break;
			case KodokuWearableSlot.ChestUtility:
				ChestUtilityWearableId = wearableId;
				break;
			case KodokuWearableSlot.UpperOuter:
				UpperOuterWearableId = wearableId;
				break;
			case KodokuWearableSlot.Back:
				BackWearableId = wearableId;
				break;
			case KodokuWearableSlot.Lower:
				LowerWearableId = wearableId;
				break;
			case KodokuWearableSlot.Feet:
				FeetWearableId = wearableId;
				break;
			case KodokuWearableSlot.Face:
				FaceWearableId = wearableId;
				break;
			case KodokuWearableSlot.Head:
				HeadWearableId = wearableId;
				break;
			case KodokuWearableSlot.WaistUtility:
				WaistUtilityWearableId = wearableId;
				break;
			case KodokuWearableSlot.Hair:
				HairWearableId = wearableId;
				break;
			case KodokuWearableSlot.Beard:
				BeardWearableId = wearableId;
				break;
		}
	}
}

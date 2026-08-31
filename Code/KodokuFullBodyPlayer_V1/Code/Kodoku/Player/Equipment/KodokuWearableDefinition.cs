using Sandbox;
using System.Collections.Generic;

namespace Kodoku;

public enum KodokuWearableCategory
{
	Clothing,
	Protective,
	Utility,
	Appearance
}

public enum KodokuWearableSlot
{
	UpperBase,
	ChestArmor,
	ChestUtility,
	UpperOuter,
	Back,
	Lower,
	Feet,
	Face,
	Head,
	WaistUtility,
	Hair,
	Beard
}

/// <summary>
/// Higher values have priority when several equipped items affect the hair.
/// Hidden > Compressed > Free.
/// </summary>
public enum KodokuHairMode
{
	Free = 0,
	Compressed = 1,
	Hidden = 2
}

public enum KodokuWearableAttachmentMode
{
	BoneMerge,
	BoneSocket
}

/// <summary>
/// Describes one wearable prefab. Put this component on the root of every
/// clothing, protection, utility or appearance prefab.
/// </summary>
[Title( "Kodoku Wearable Definition" )]
[Category( "Kodoku/Equipment" )]
[Icon( "checkroom" )]
public sealed class KodokuWearableDefinition : Component
{
	[Property, Group( "Identity" )]
	public string WearableId { get; set; } = "";

	[Property, Group( "Identity" )]
	public string DisplayName { get; set; } = "";

	[Property, Group( "Identity" )]
	public KodokuWearableCategory Category { get; set; }
		= KodokuWearableCategory.Clothing;

	[Property, Group( "Identity" )]
	public KodokuWearableSlot Slot { get; set; }
		= KodokuWearableSlot.UpperBase;

	[Property, Group( "Attachment" )]
	public KodokuWearableAttachmentMode AttachmentMode { get; set; }
		= KodokuWearableAttachmentMode.BoneMerge;

	[Property, Group( "Attachment" ), ShowIf(
		nameof( AttachmentMode ),
		KodokuWearableAttachmentMode.BoneSocket
	)]
	public KodokuSocketController.SocketId Socket { get; set; }
		= KodokuSocketController.SocketId.Head;

	[Property, Group( "Attachment" ), ShowIf(
		nameof( AttachmentMode ),
		KodokuWearableAttachmentMode.BoneSocket
	)]
	public Vector3 SocketLocalPosition { get; set; }

	[Property, Group( "Attachment" ), ShowIf(
		nameof( AttachmentMode ),
		KodokuWearableAttachmentMode.BoneSocket
	)]
	public Angles SocketLocalRotation { get; set; }

	[Property, Group( "Attachment" ), ShowIf(
		nameof( AttachmentMode ),
		KodokuWearableAttachmentMode.BoneSocket
	)]
	public Vector3 SocketLocalScale { get; set; } = Vector3.One;

	/// <summary>
	/// Bodygroups replaced by this wearable. Current Kodoku convention:
	/// 0 = visible, 1 = hidden.
	/// </summary>
	[Property, Group( "Body Mask" )]
	public List<string> HiddenBodyGroups { get; set; } = new();

	[Property, Group( "Body Mask" )]
	public int HiddenBodyGroupValue { get; set; } = 1;

	[Property, Group( "Hair" )]
	public KodokuHairMode HairMode { get; set; } = KodokuHairMode.Free;

	/// <summary>
	/// Optional for Face/accessory items. Head-slot items are always hidden
	/// from their owner's FPS camera, regardless of this value.
	/// </summary>
	[Property, Group( "First Person" )]
	public bool HideForOwnerFirstPerson { get; set; }

	public bool MustHideForOwnerFirstPerson =>
		Slot == KodokuWearableSlot.Head
		|| HideForOwnerFirstPerson;

	public bool IsConfigured =>
		!string.IsNullOrWhiteSpace( WearableId );
}

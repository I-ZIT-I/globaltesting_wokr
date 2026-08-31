using Sandbox;

namespace Kodoku;

/// <summary>
/// Simplified attachment used by weapons, props, hair and skinned armor.
/// </summary>
[Title( "Kodoku Equipment Attachment" )]
[Category( "Kodoku/Equipment" )]
[Icon( "backpack" )]
public sealed class KodokuEquipmentAttachment : Component
{
	public enum AttachmentMode
	{
		BoneSocket,
		BoneMerge
	}

	[Property]
	public AttachmentMode Mode { get; set; }
		= AttachmentMode.BoneSocket;

	[Property, ShowIf(
		nameof( Mode ),
		AttachmentMode.BoneSocket
	)]
	public KodokuSocketController SocketController { get; set; }

	[Property, ShowIf(
		nameof( Mode ),
		AttachmentMode.BoneSocket
	)]
	public KodokuSocketController.SocketId Socket { get; set; }

	[Property, ShowIf(
		nameof( Mode ),
		AttachmentMode.BoneSocket
	)]
	public Vector3 AttachmentLocalPosition { get; set; }

	[Property, ShowIf(
		nameof( Mode ),
		AttachmentMode.BoneSocket
	)]
	public Angles AttachmentLocalRotation { get; set; }

	[Property, ShowIf(
		nameof( Mode ),
		AttachmentMode.BoneSocket
	)]
	public Vector3 AttachmentLocalScale { get; set; }
		= Vector3.One;

	[Property, ShowIf(
		nameof( Mode ),
		AttachmentMode.BoneMerge
	)]
	public SkinnedModelRenderer BodyRenderer { get; set; }

	[Property, ShowIf(
		nameof( Mode ),
		AttachmentMode.BoneMerge
	)]
	public SkinnedModelRenderer EquipmentRenderer { get; set; }

	[Property, Group( "First Person" )]
	public bool HideInFirstPerson { get; set; }

	[Property, Group( "First Person" )]
	public string FirstPersonHiddenTag { get; set; }
		= "fps_hide_head";

	protected override void OnEnabled()
	{
		base.OnEnabled();

		ApplyAttachment();
	}

	protected override void OnValidate()
	{
		ApplyAttachment();
	}

	[Button( "Apply Attachment", "link" )]
	public void ApplyAttachment()
	{
		if ( Mode == AttachmentMode.BoneMerge )
		{
			ApplyBoneMerge();
		}
		else
		{
			ApplyBoneSocket();
		}

		ApplyFirstPersonTag();
	}

	private void ApplyBoneSocket()
	{
		var socket =
			SocketController?.GetSocket( Socket );

		if ( !socket.IsValid() )
			return;

		GameObject.Parent =
			socket.GameObject;

		GameObject.LocalPosition =
			AttachmentLocalPosition;

		GameObject.LocalRotation =
			AttachmentLocalRotation.ToRotation();

		GameObject.LocalScale =
			AttachmentLocalScale;
	}

	private void ApplyBoneMerge()
	{
		EquipmentRenderer ??=
			GetComponent<SkinnedModelRenderer>();

		if (
			!EquipmentRenderer.IsValid()
			|| !BodyRenderer.IsValid()
			|| EquipmentRenderer == BodyRenderer
		)
		{
			return;
		}

		EquipmentRenderer.BoneMergeTarget =
			BodyRenderer;
	}

	private void ApplyFirstPersonTag()
	{
		if (
			!HideInFirstPerson
			|| string.IsNullOrWhiteSpace(
				FirstPersonHiddenTag
			)
		)
		{
			return;
		}

		ApplyTagRecursively( GameObject );
	}

	private void ApplyTagRecursively(
		GameObject gameObject
	)
	{
		if ( !gameObject.IsValid() )
			return;

		gameObject.Tags.Set(
			FirstPersonHiddenTag,
			true
		);

		SynchronizeRendererTags(
			gameObject
		);

		foreach ( var child in gameObject.Children )
		{
			ApplyTagRecursively( child );
		}
	}

	private static void SynchronizeRendererTags(
		GameObject gameObject
	)
	{
		foreach (
			var renderer
			in gameObject.GetComponents<ModelRenderer>()
		)
		{
			if ( !renderer.SceneObject.IsValid() )
				continue;

			renderer.SceneObject.Tags.SetFrom(
				gameObject.Tags
			);
		}
	}
}
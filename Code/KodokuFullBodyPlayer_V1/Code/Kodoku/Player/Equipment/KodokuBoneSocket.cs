using Sandbox;

namespace Kodoku;

/// <summary>
/// Moves this GameObject to a final animated bone transform.
/// Equipment and props can be parented under this socket.
/// </summary>
[Title( "Kodoku Bone Socket" )]
[Category( "Kodoku/Equipment" )]
[Icon( "link" )]
public sealed class KodokuBoneSocket : Component, Component.ExecuteInEditor
{
	[Property]
	public SkinnedModelRenderer BodyRenderer { get; set; }

	[Property]
	public string BoneName { get; set; } = "head";

	[Property]
	public Vector3 PositionOffset { get; set; }

	[Property]
	public Angles RotationOffset { get; set; }

	[Property]
	public Vector3 Scale { get; set; } = Vector3.One;

	[Property]
	public bool FollowPosition { get; set; } = true;

	[Property]
	public bool FollowRotation { get; set; } = true;

	protected override void OnAwake()
	{
		base.OnAwake();

		BodyRenderer ??=
			GameObject.Root.GetComponentInChildren<SkinnedModelRenderer>();
	}

	protected override void OnUpdate()
	{
		UpdateSocketTransform();
	}

	protected override void OnPreRender()
	{
		// Bones are settled by this stage, so this removes the visible one-frame lag.
		UpdateSocketTransform();
	}

	private void UpdateSocketTransform()
	{
		if (
			!BodyRenderer.IsValid()
			|| string.IsNullOrWhiteSpace( BoneName )
			|| !BodyRenderer.TryGetBoneTransform(
				BoneName,
				out var boneTransform
			)
		)
		{
			return;
		}

		if ( FollowPosition )
		{
			WorldPosition =
				boneTransform.Position
				+ boneTransform.Rotation * PositionOffset;
		}

		if ( FollowRotation )
		{
			WorldRotation =
				boneTransform.Rotation
				* RotationOffset.ToRotation();
		}

		WorldScale = Scale;
	}
}

using Sandbox;

namespace Kodoku;

/// <summary>
/// Strongly typed registry for the sockets used by the Kodokan character.
/// Socket components do the actual bone following.
/// </summary>
[Title( "Kodoku Socket Controller" )]
[Category( "Kodoku/Equipment" )]
[Icon( "account_tree" )]
public sealed class KodokuSocketController : Component
{
	public enum SocketId
	{
		Head,
		Face,
		Back,
		RightHand,
		LeftHand
	}

	[Property, Group( "Sockets" )]
	public KodokuBoneSocket Head { get; set; }

	[Property, Group( "Sockets" )]
	public KodokuBoneSocket Face { get; set; }

	[Property, Group( "Sockets" )]
	public KodokuBoneSocket Back { get; set; }

	[Property, Group( "Sockets" ), Title( "Right Hand" )]
	public KodokuBoneSocket RightHand { get; set; }

	[Property, Group( "Sockets" ), Title( "Left Hand" )]
	public KodokuBoneSocket LeftHand { get; set; }

	public KodokuBoneSocket GetSocket( SocketId id )
	{
		return id switch
		{
			SocketId.Head => Head,
			SocketId.Face => Face,
			SocketId.Back => Back,
			SocketId.RightHand => RightHand,
			SocketId.LeftHand => LeftHand,
			_ => null
		};
	}
}

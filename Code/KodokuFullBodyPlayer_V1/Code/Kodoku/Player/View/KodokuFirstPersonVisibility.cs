using Sandbox;
using System.Collections.Generic;

namespace Kodoku;

/// <summary>
/// Applies one render tag to the local owner's first-person hidden hierarchy.
/// Proxy players explicitly have that tag removed so they remain visible.
/// </summary>
[Title( "Kodoku First Person Visibility" )]
[Category( "Kodoku/Player" )]
[Icon( "visibility_off" )]
public sealed class KodokuFirstPersonVisibility : Component
{
	[Property]
	public string HiddenTag { get; set; } = "fps_hide_head";

	/// <summary>
	/// Add the head root and any other independent roots that must disappear
	/// only in this player's own FPS view, such as fixed hair.
	/// </summary>
	[Property]
	public List<GameObject> HiddenRoots { get; set; } = new();

	[Property]
	public bool RefreshContinuously { get; set; } = true;

	protected override void OnEnabled()
	{
		base.OnEnabled();
		RefreshTags();
	}

	protected override void OnUpdate()
	{
		if ( RefreshContinuously )
		{
			RefreshTags();
		}
	}

	protected override void OnDisabled()
	{
		SetHiddenForThisClient( false );
		base.OnDisabled();
	}

	[Button( "Refresh FPS Hidden Tags", "refresh" )]
	public void RefreshTags()
	{
		if ( string.IsNullOrWhiteSpace( HiddenTag ) )
			return;

		// The owning player's local representation hides these roots.
		// Every proxy explicitly clears the tag, including tags that may have
		// been serialized previously in an older prefab version.
		SetHiddenForThisClient( !IsProxy );
	}

	private void SetHiddenForThisClient( bool hidden )
	{
		foreach ( var root in HiddenRoots )
		{
			ApplyTagRecursively( root, hidden );
		}
	}

	private void ApplyTagRecursively(
		GameObject gameObject,
		bool enabled
	)
	{
		if ( !gameObject.IsValid() )
			return;

		gameObject.Tags.Set( HiddenTag, enabled );
		SynchronizeRendererTags( gameObject );

		foreach ( var child in gameObject.Children )
		{
			ApplyTagRecursively( child, enabled );
		}
	}

	/// <summary>
	/// Renderer scene objects copy GameObject tags when they are created.
	/// This explicitly refreshes them when the local visibility state changes.
	/// </summary>
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

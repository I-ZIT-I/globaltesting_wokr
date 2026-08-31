using Sandbox;
using System;

namespace Kodoku;

/// <summary>
/// Drives the fixed hairstyle belonging to one character.
///
/// The hair VMDL owns one ModelDoc bodygroup named Hair_State:
/// 0 = Free, 1 = Compressed, 2 = Hidden.
/// Those values intentionally match KodokuHairMode.
/// </summary>
[Title( "Kodoku Hair Controller" )]
[Category( "Kodoku/Appearance" )]
[Icon( "content_cut" )]
public sealed class KodokuHairController : Component
{
	[Property, Group( "References" )]
	public SkinnedModelRenderer HairRenderer { get; set; }

	[Property, Group( "References" )]
	public SkinnedModelRenderer BodyRenderer { get; set; }

	[Property, Group( "Body Group" )]
	public string HairStateBodyGroup { get; set; } = "Hair_State";

	[Property, Group( "First Person" )]
	public string FirstPersonHiddenTag { get; set; }
		= "fps_hide_head";

	[Property, ReadOnly, Group( "Runtime" )]
	public KodokuHairMode CurrentMode { get; private set; }
		= KodokuHairMode.Free;

	private bool _warnedMissingBodyGroup;
	private bool _firstPersonVisibilityInitialized;
	private bool _lastFirstPersonHidden;

	protected override void OnAwake()
	{
		base.OnAwake();
		ResolveReferences();
		BindToBody();
		ApplyCurrentMode();
		RefreshFirstPersonVisibility( true );
	}

	protected override void OnEnabled()
	{
		base.OnEnabled();
		ResolveReferences();
		BindToBody();
		ApplyCurrentMode();
		RefreshFirstPersonVisibility( true );
	}

	protected override void OnUpdate()
	{
		RefreshFirstPersonVisibility( false );
	}

	protected override void OnDisabled()
	{
		SetFirstPersonTagRecursively( GameObject, false );
		_firstPersonVisibilityInitialized = false;
		base.OnDisabled();
	}

	public void SetHairMode( KodokuHairMode mode )
	{
		CurrentMode = mode;
		ApplyCurrentMode();
	}

	[Button( "Hair: Free", "content_cut" )]
	public void DebugSetFree()
	{
		SetHairMode( KodokuHairMode.Free );
	}

	[Button( "Hair: Compressed", "compress" )]
	public void DebugSetCompressed()
	{
		SetHairMode( KodokuHairMode.Compressed );
	}

	[Button( "Hair: Hidden", "visibility_off" )]
	public void DebugSetHidden()
	{
		SetHairMode( KodokuHairMode.Hidden );
	}

	[Button( "Refresh Hair State", "refresh" )]
	public void RefreshHairState()
	{
		ResolveReferences();
		BindToBody();
		ApplyCurrentMode();
		RefreshFirstPersonVisibility( true );
	}

	private void ResolveReferences()
	{
		HairRenderer ??=
			GetComponent<SkinnedModelRenderer>();

		HairRenderer ??=
			GetComponentInChildren<SkinnedModelRenderer>();

		if ( BodyRenderer.IsValid() )
			return;

		var animator =
			GameObject.Root.GetComponent<KodokuAnimatorDriver>();

		if ( animator.IsValid() && animator.BodyRenderer.IsValid() )
		{
			BodyRenderer = animator.BodyRenderer;
		}
	}

	private void BindToBody()
	{
		if (
			!HairRenderer.IsValid()
			|| !BodyRenderer.IsValid()
			|| HairRenderer == BodyRenderer
		)
		{
			return;
		}

		HairRenderer.BoneMergeTarget = BodyRenderer;
	}

	private void ApplyCurrentMode()
	{
		ResolveReferences();

		if (
			!HairRenderer.IsValid()
			|| string.IsNullOrWhiteSpace( HairStateBodyGroup )
		)
		{
			return;
		}

		try
		{
			HairRenderer.SetBodyGroup(
				HairStateBodyGroup,
				(int)CurrentMode
			);
		}
		catch ( Exception )
		{
			if ( _warnedMissingBodyGroup )
				return;

			Log.Warning(
				$"{nameof( KodokuHairController )}: bodygroup '{HairStateBodyGroup}' was not found on the hair model."
			);

			_warnedMissingBodyGroup = true;
		}
	}

	private void RefreshFirstPersonVisibility( bool force )
	{
		var hidden = !IsProxy;

		if (
			!force
			&& _firstPersonVisibilityInitialized
			&& hidden == _lastFirstPersonHidden
		)
		{
			return;
		}

		SetFirstPersonTagRecursively( GameObject, hidden );
		_lastFirstPersonHidden = hidden;
		_firstPersonVisibilityInitialized = true;
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

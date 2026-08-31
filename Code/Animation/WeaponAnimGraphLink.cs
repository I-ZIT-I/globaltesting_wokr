using Sandbox;

public sealed class WeaponAnimGraphLink : Component
{
	[Property]
	public SkinnedModelRenderer CharacterRenderer { get; set; }

	[Property]
	public SkinnedModelRenderer PistolRenderer { get; set; }

	[Property]
	public string CharacterShootTag { get; set; } = "isShooting";

	[Property]
	public string PistolShootParameter { get; set; } = "b_attack";

	protected override void OnEnabled()
	{
		if ( CharacterRenderer is not null )
		{
			CharacterRenderer.OnAnimTagEvent += OnCharacterAnimTag;
		}
	}

	protected override void OnDisabled()
	{
		if ( CharacterRenderer is not null )
		{
			CharacterRenderer.OnAnimTagEvent -= OnCharacterAnimTag;
		}
	}

	private void OnCharacterAnimTag( SceneModel.AnimTagEvent animEvent )
	{
		if ( animEvent.Name != CharacterShootTag )
			return;

		// On déclenche le pistolet au commencement du tag.
		if ( animEvent.Status != SceneModel.AnimTagStatus.Start &&
			 animEvent.Status != SceneModel.AnimTagStatus.Fired )
		{
			return;
		}

		PistolRenderer?.Set( PistolShootParameter, true );
	}
}
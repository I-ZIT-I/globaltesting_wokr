using Sandbox;

public sealed class KodokanController : Component
{
	[RequireComponent] SkinnedModelRenderer model { get; set; }
	protected override void OnUpdate()
	{
		model.Set("IsRunning", Input.Down("Forward"));
	}
}

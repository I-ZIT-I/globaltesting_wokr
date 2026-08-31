using Sandbox;

public sealed class TurnInPlaceAnimator : Component
{
	[Property]
	public SkinnedModelRenderer Body { get; set; }

	private float _previousYaw;
	private bool _initialized;

	protected override void OnUpdate()
	{
		if ( Body is null )
		{
			Log.Warning( "TurnInPlaceAnimator: Body n'est pas assigné" );
			return;
		}

		// IMPORTANT :
		// on regarde la rotation du Renderer, pas celle de player_perso
		float currentYaw = Body.WorldRotation.Angles().yaw;

		if ( !_initialized )
		{
			_previousYaw = currentYaw;
			_initialized = true;
			return;
		}

		float deltaYaw = MathX.DeltaDegrees(
			_previousYaw,
			currentYaw
		);

		_previousYaw = currentYaw;

		float turnRate = 0.0f;

		if ( Time.Delta > 0.0001f )
			turnRate = deltaYaw / Time.Delta;

		if ( turnRate > -5.0f && turnRate < 5.0f )
			turnRate = 0.0f;

		// Ton AnimGraph est limité à -100 / +100
		turnRate = MathX.Clamp( turnRate, -100.0f, 100.0f );

		Body.Set( "turn_rate", turnRate );
	}
}
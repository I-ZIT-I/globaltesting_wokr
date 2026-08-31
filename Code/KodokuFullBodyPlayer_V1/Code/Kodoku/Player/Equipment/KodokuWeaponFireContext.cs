using Sandbox;

namespace Kodoku;

/// <summary>
/// Runtime information shared with every weapon fire mode.
/// </summary>
public readonly struct KodokuWeaponFireContext
{
	public GameObject Owner { get; }
	public GameObject Weapon { get; }
	public GameObject Muzzle { get; }
	public KodokuWeaponFireController Controller { get; }

	public Vector3 Origin =>
		Muzzle.IsValid()
			? Muzzle.WorldPosition
			: Weapon.WorldPosition;

	public Rotation Rotation =>
		Muzzle.IsValid()
			? Muzzle.WorldRotation
			: Weapon.WorldRotation;

	public Vector3 Forward =>
	Controller.IsValid()
		? Controller.GetFireDirection()
		: Rotation.Forward;

	public KodokuWeaponFireContext(
		GameObject owner,
		GameObject weapon,
		GameObject muzzle,
		KodokuWeaponFireController controller
	)
	{
		Owner = owner;
		Weapon = weapon;
		Muzzle = muzzle;
		Controller = controller;
	}
}

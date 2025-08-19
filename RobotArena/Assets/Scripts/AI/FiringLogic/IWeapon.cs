// Weapons/IWeapon.cs
using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    /// World-space point to aim at. Returns true if a shot was actually fired.
    public abstract bool TryFireAt(Vector3 worldPoint);

    /// Can the weapon fire right now. Useful if you ever want pre-checks.
    public abstract bool CanFire { get; }

    /// Effective maximum range for aiming/AI decisions.
    public abstract float EffectiveRange { get; }

    /// Optional per-frame updates for beams etc.
    public virtual void Tick() { }
}

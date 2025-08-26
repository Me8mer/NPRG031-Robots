using UnityEngine;

/// <summary>
/// Abstract base for all robot weapons.
/// Provides unified API for firing, cooldowns, and range checks.
/// </summary>
public abstract class WeaponBase : MonoBehaviour
{
    /// <summary>
    /// Attempts to fire at the given world-space point.
    /// Returns true if a shot was actually fired.
    /// </summary>
    public abstract bool TryFireAt(Vector3 worldPoint);

    /// <summary>
    /// Whether the weapon can currently fire (e.g. cooldown ready).
    /// </summary>
    public abstract bool CanFire { get; }

    /// <summary>
    /// Effective maximum range for aiming/AI decisions.
    /// </summary>
    public abstract float EffectiveRange { get; }

    /// <summary>
    /// Optional per-frame updates (for beams, charging, etc.).
    /// Default does nothing.
    /// </summary>
    public virtual void Tick() { }
}

/// <summary>
/// Marker interface for projectile-based weapons.
/// </summary>
public interface IProjectileWeapon { }

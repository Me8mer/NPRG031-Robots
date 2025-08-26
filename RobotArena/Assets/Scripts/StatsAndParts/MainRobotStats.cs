using System;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

/// <summary>
/// Aggregate robot stats after assembly.
/// These values are built by combining definitions (Frame, Core, Lower, Weapon).
/// </summary>
[Serializable]
public class RobotStats
{
    [System.NonSerialized] private bool _derivedReady;
    private float _damageBoostMultiplier = 1f;
    [System.NonSerialized] public float bakedMaxMoveSpeed;

    /// <summary>Maximum health points (from frame, etc.).</summary>
    public float maxHealth;

    /// <summary>Maximum armor points.</summary>
    public float maxArmor;

    /// <summary>Base movement speed before weight modifiers.</summary>
    public float baseSpeed;

    /// <summary>Damage per projectile or attack.</summary>
    public float damage;

    /// <summary>Maximum attack range in meters.</summary>
    public float attackRange;

    /// <summary>Attack speed in shots per minute.</summary>
    public float attackSpeed;

    /// <summary>Turning speed in degrees per second.</summary>
    public float turningSpeed;

    /// <summary>Field of view angle in degrees for perception.</summary>
    public float sightAngle;

    /// <summary>Total weight of all parts (affects final speed).</summary>
    public float weight;

    /// <summary>
    /// Copies all values from another stats object.
    /// Useful when duplicating or refreshing a robot’s current stats.
    /// </summary>
    public void CopyFrom(RobotStats other)
    {
        if (other == null) return;
        maxHealth = other.maxHealth;
        maxArmor = other.maxArmor;
        baseSpeed = other.baseSpeed;
        damage = other.damage;
        attackRange = other.attackRange;
        attackSpeed = other.attackSpeed;
        turningSpeed = other.turningSpeed;
        sightAngle = other.sightAngle;
        weight = other.weight;
    }
    /// <summary>Bake all derived values that depend on other fields. Call once after stats are filled.</summary>
    public void BakeDerived()
    {
        bakedMaxMoveSpeed = Mathf.Max(0f, baseSpeed) / (Mathf.Max(1f, weight));
        _derivedReady = true;
    }
    public float GetEffectiveDamage()
    {
        return damage * _damageBoostMultiplier;
    }

    /// <summary>Returns baked speed. If something forgot to bake, do it now.</summary>
    public float RequireBakedSpeed()
    {
        if (!_derivedReady) BakeDerived();
        return bakedMaxMoveSpeed;
    }
}

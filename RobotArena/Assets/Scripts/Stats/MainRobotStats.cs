using UnityEngine;

/// <summary>
/// Tunable data container that defines all static parameters for a robot
/// </summary>
[CreateAssetMenu(menuName = "BattleRobots/RobotStats", fileName = "NewRobotStats")]
public class RobotStats : ScriptableObject
{
    #region Movement
    /// <summary>Baseline speed before weight and state modifiers.</summary>
    [Header("Core Movement")] public float baseSpeed = 100f;

    /// <summary>Aggregate mass of armor + load‑out; slows movement.</summary>
    [Tooltip("Weight factor: higher slows the robot")]
    public float weight = 1f;

    /// <summary>Speed multiplier when <see cref="RobotState.Idle"/>.</summary>
    [Header("State Speed Modifiers")] public float idleSpeedModifier = 0f;
    /// <summary>Speed multiplier when chasing an enemy.</summary>
    public float chaseSpeedModifier = 1f;
    /// <summary>Speed multiplier while attacking.</summary>
    public float attackSpeedModifier = 0.8f;
    /// <summary>Speed multiplier while retreating.</summary>
    public float retreatSpeedModifier = 0.5f;
    #endregion

    #region Durability
    /// <summary>Maximum health points</summary>
    [Header("Health & Armor")] public float maxHealth = 100f;
    /// <summary>Maximum armor points that absorb damage first.</summary>
    public float maxArmor = 50f;
    #endregion

    #region Perception
    /// <summary>Radius in metres of the detection sphere.</summary>
    [Header("Perception")] public float detectionRadius = 10f;
    /// <summary>Total field‑of‑view angle (degrees).</summary>
    [Range(0f, 360f)] public float sightAngle = 360f;
    #endregion

    #region Combat
    /// <summary>Effective weapon range (metres).</summary>
    [Header("Combat")] public float attackRange = 8f;
    /// <summary>Cooldown between successive shots (seconds).</summary>
    public float fireCooldown = 1f;
    #endregion

    #region Regeneration
    /// <summary>Armor regen rate per second while idle.</summary>
    [Header("Regen Rates")] public float armorRegenIdle = 2f;
    /// <summary>Armor regen rate per second while chasing.</summary>
    public float armorRegenChase = 1f;
    #endregion
}

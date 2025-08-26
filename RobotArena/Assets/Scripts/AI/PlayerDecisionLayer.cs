using System.Collections.Generic;
using UnityEngine;
using static StateTransitionHelper;

/// <summary>
/// AI decision layer for autonomous robots.
/// Chooses between Retreat, Chasing pickups, Strafing, or Chasing enemies,
/// while maintaining a firing target independently.
/// 
/// Priority:
/// 0) Retreat if low health/armor
/// 1) Chase pickups
/// 2) Strafe if enemy is in effective range and line of fire
/// 3) Otherwise chase nearest enemy
/// </summary>
public class PlayerDecisionLayer : DecisionLayer
{
    // --- Tuning constants ---
    private const float FireStickySeconds = 0.5f;     // hold onto a fire target briefly to reduce jitter
    private const float StrafeStickySeconds = 0.6f;   // hold onto strafe target briefly
    private const float RangeTolerance = 1.25f;       // leeway for deciding "in attack range"

    // --- Stickiness memory ---
    private RobotController _fireStickyTarget;
    private float _fireStickyUntil;

    private RobotController _strafeStickyTarget;
    private float _strafeStickyUntil;

    public PlayerDecisionLayer(RobotController controller) : base(controller) { }

    /// <summary>
    /// Main decision entrypoint.
    /// Evaluates perception and returns a <see cref="DecisionResult"/> containing:
    /// - Movement intent (Idle / ChaseEnemy / StrafeEnemy / ChasePickup / Retreat)
    /// - Target robot for movement/strafe
    /// - Fire target (may differ from move target)
    /// </summary>
    public override DecisionResult Decide()
    {
        var perception = _controller.GetPerception();
        var stats = _controller.GetStats();
        var health = _controller.GetHealth();

        // Awareness snapshot
        List<RobotController> enemiesAll = perception.GetAllOpponents();
        List<Pickup> pickupsAll = perception.GetAllPickups();

        // Fire target = nearest enemy overall, even if out of range.
        var nearestOverallEnemy = GetNearest(enemiesAll);
        var fireEnemy = ApplyFireStickiness(nearestOverallEnemy);

        // --- Priority 0: Retreat if health very low ---
        float hpPct = health.CurrentHealth / Mathf.Max(0.001f, stats.maxHealth);
        float armPct = health.CurrentArmor / Mathf.Max(0.001f, stats.maxArmor);
        if (hpPct <= 0.30f && armPct < 0.75f)
        {
            return new DecisionResult
            {
                Move = MovementIntent.Retreat,
                FireEnemy = fireEnemy
            };
        }

        // --- Priority 1: Chase pickups ---
        if (pickupsAll != null && pickupsAll.Count > 0)
        {
            var nearestPickup = GetNearest(pickupsAll);
            return new DecisionResult
            {
                Move = MovementIntent.ChasePickup,
                MovePickup = nearestPickup,
                FireEnemy = fireEnemy
            };
        }

        // --- Priority 2: Strafe if enemy in range & LOS ---
        var enemyInRange = SelectEnemyInEffectiveRange(enemiesAll);
        if (enemyInRange != null)
        {
            var strafeEnemy = ApplyStrafeStickiness(enemyInRange);
            return new DecisionResult
            {
                Move = MovementIntent.StrafeEnemy,
                MoveEnemy = strafeEnemy,
                FireEnemy = fireEnemy
            };
        }

        // --- Priority 3: Otherwise chase nearest enemy ---
        if (nearestOverallEnemy != null)
        {
            return new DecisionResult
            {
                Move = MovementIntent.ChaseEnemy,
                MoveEnemy = nearestOverallEnemy,
                FireEnemy = fireEnemy
            };
        }

        // Fallback: Idle
        return new DecisionResult { Move = MovementIntent.Idle, FireEnemy = null };
    }

    // ---------- Helpers ----------

    /// <summary>
    /// Picks the nearest enemy within effective attack range and line of fire.
    /// Returns null if none are valid.
    /// </summary>
    private RobotController SelectEnemyInEffectiveRange(List<RobotController> enemies)
    {
        if (enemies == null || enemies.Count == 0) return null;

        var nav = _controller.GetNavigator();
        RobotController best = null;
        float bestDist = float.MaxValue;
        Vector3 me = _controller.transform.position;

        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e == null) continue;

            if (nav.InEffectiveAttackRange(e.transform, RangeTolerance) &&
                nav.HasLineOfFireTo(e.transform))
            {
                float d = Vector3.Distance(me, e.transform.position);
                if (d < bestDist) { best = e; bestDist = d; }
            }
        }
        return best;
    }

    /// <summary>
    /// Keeps last fire target for a short duration to reduce jitter.
    /// </summary>
    private RobotController ApplyFireStickiness(RobotController candidate)
    {
        if (_fireStickyTarget != null && Time.time < _fireStickyUntil)
            return _fireStickyTarget;

        _fireStickyTarget = candidate;
        _fireStickyUntil = Time.time + FireStickySeconds;
        return _fireStickyTarget;
    }

    /// <summary>
    /// Keeps strafing same target for a short duration if still valid.
    /// </summary>
    private RobotController ApplyStrafeStickiness(RobotController candidate)
    {
        if (_strafeStickyTarget != null && Time.time < _strafeStickyUntil)
        {
            var nav = _controller.GetNavigator();
            if (_strafeStickyTarget != null &&
                nav.InEffectiveAttackRange(_strafeStickyTarget.transform, RangeTolerance))
            {
                return _strafeStickyTarget;
            }
        }

        _strafeStickyTarget = candidate;
        _strafeStickyUntil = Time.time + StrafeStickySeconds;
        return _strafeStickyTarget;
    }
}

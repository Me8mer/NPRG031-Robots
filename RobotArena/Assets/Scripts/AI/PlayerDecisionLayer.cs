using System.Collections.Generic;
using UnityEngine;
using static StateTransitionHelper;

public class PlayerDecisionLayer : DecisionLayer
{
    // Small hysteresis to reduce jitter.
    private const float FireStickySeconds = 0.5f;
    private const float StrafeStickySeconds = 0.6f;
    private const float RangeTolerance = 1.25f;

    private RobotController _fireStickyTarget;
    private float _fireStickyUntil;

    private RobotController _strafeStickyTarget;
    private float _strafeStickyUntil;


    public PlayerDecisionLayer(RobotController controller) : base(controller) { }

    public override DecisionResult Decide()
    {
        var perception = _controller.GetPerception();
        var stats = _controller.GetStats();
        var health = _controller.GetHealth();

        // Global awareness: all opponents and all pickups
        List<RobotController> enemiesAll = perception.GetAllOpponents();
        List<Pickup> pickupsAll = perception.GetAllPickups();

        // Fire target is the closest enemy overall, even out of range.
        var nearestOverallEnemy = GetNearest(enemiesAll);
        var fireEnemy = ApplyFireStickiness(nearestOverallEnemy);

        // 0) Emergency: retreat on low HP, but keep aiming/shooting if possible.
        float hpPct = health.CurrentHealth / Mathf.Max(0.001f, stats.maxHealth);
        if (hpPct <= 0.30f)
        {
            return new DecisionResult
            {
                Move = MovementIntent.Retreat,
                FireEnemy = fireEnemy
            };
        }

        // 1) If any pickup exists, we chase pickup. Firing stays independent.
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
        // 2) If any enemy is within effective attack range, strafe that enemy.
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

        // 3) Else chase the nearest enemy we know about.
        if (nearestOverallEnemy != null)
        {
            return new DecisionResult
            {
                Move = MovementIntent.ChaseEnemy,
                MoveEnemy = nearestOverallEnemy,
                FireEnemy = fireEnemy
            };
        }

        // Nothing to do
        return new DecisionResult { Move = MovementIntent.Idle, FireEnemy = null };
    }
    // ---------- Helpers ----------
    private RobotController SelectEnemyInEffectiveRange(List<RobotController> enemies)
    {
        if (enemies == null || enemies.Count == 0) return null;

        var nav = _controller.GetNavigator();
        RobotController best = null;
        float bestDist = float.MaxValue;
        Vector3 me = _controller.transform.position;

        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i]; if (e == null) continue;
            if (nav.InEffectiveAttackRange(e.transform, RangeTolerance))
            {
                // Use muzzle LOF
                if (!nav.HasLineOfFireTo(e.transform)) continue;

                float d = Vector3.Distance(me, e.transform.position);
                if (d < bestDist) { best = e; bestDist = d; }
            }
        }
        return best;
    }

    private RobotController ApplyFireStickiness(RobotController candidate)
    {
        // Keep previous fire target for a short time if still valid.
        if (_fireStickyTarget != null && Time.time < _fireStickyUntil)
        {
            return _fireStickyTarget;
        }

        _fireStickyTarget = candidate;
        _fireStickyUntil = Time.time + FireStickySeconds;
        return _fireStickyTarget;
    }

    private RobotController ApplyStrafeStickiness(RobotController candidate)
    {
        // Keep strafing the same target for a short time as long as it remains in range.
        if (_strafeStickyTarget != null && Time.time < _strafeStickyUntil)
        {
            var nav = _controller.GetNavigator();
            if (_strafeStickyTarget != null &&  nav.InEffectiveAttackRange(_strafeStickyTarget.transform, RangeTolerance))
            {
                return _strafeStickyTarget;
            }
        }

        _strafeStickyTarget = candidate;
        _strafeStickyUntil = Time.time + StrafeStickySeconds;
        return _strafeStickyTarget;
    }
}

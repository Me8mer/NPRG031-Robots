using System.Collections.Generic;
using UnityEngine;
using static StateTransitionHelper;

public class PlayerDecisionLayer : DecisionLayer
{
    public PlayerDecisionLayer(RobotController controller) : base(controller) { }

    public override DecisionResult Decide()
    {
        var perception = _controller.GetPerception();
        var stats = _controller.GetStats();
        var health = _controller.GetHealth();

        // 0) Emergency rule
        float hpPct = health.CurrentHealth / stats.maxHealth;
        if (hpPct <= 0.30f)
        {
            return new DecisionResult
            {
                Move = MovementIntent.Retreat,
                FireEnemy = FindBestEnemyInRange(perception) // can still shoot while retreating
            };
        }

        // 1) Whole-map lists
        var enemies = perception.GetAllOpponents();
        var pickups = perception.GetAllPickups();

        // 2) Fire intent: best enemy in effective attack range (or null)
        var fireEnemy = FindBestEnemyInRange(perception);

        // 3) Movement intent
        if (fireEnemy != null)
        {
            // Enemy in range -> strafe that enemy
            return new DecisionResult
            {
                Move = MovementIntent.StrafeEnemy,
                MoveEnemy = fireEnemy,
                FireEnemy = fireEnemy
            };
        }

        // No enemy in range
        if (pickups != null && pickups.Count > 0)
        {
            var nearestPickup = GetNearest(pickups);
            if (nearestPickup != null)
            {
                return new DecisionResult
                {
                    Move = MovementIntent.ChasePickup,
                    MovePickup = nearestPickup,
                    FireEnemy = null
                };
            }
        }

        if (enemies != null && enemies.Count > 0)
        {
            var nearestEnemy = GetNearest(enemies);
            return new DecisionResult
            {
                Move = MovementIntent.ChaseEnemy,
                MoveEnemy = nearestEnemy,
                FireEnemy = null
            };
        }

        // Nothing to do
        return new DecisionResult { Move = MovementIntent.Idle, FireEnemy = null };
    }

    private RobotController FindBestEnemyInRange(Perception perception)
    {
        var enemies = perception.GetAllOpponents();
        RobotController best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i]; if (e == null) continue;
            if (CombatHelpers.InEffectiveAttackRange(_controller, e.transform, 1.5f))
            {
                float d = Vector3.Distance(_controller.transform.position, e.transform.position);
                if (d < bestDist) { best = e; bestDist = d; }
            }
        }
        return best;
    }
}

using System.Collections.Generic;
using UnityEngine;
using static StateTransitionHelper;

public class PlayerDecisionLayer : DecisionLayer
{
    public PlayerDecisionLayer(RobotController controller) : base(controller) { }

    public override RobotObjective Decide()
    {
        var perception = _controller.GetPerception();
        var stats = _controller.GetStats();
        var health = _controller.GetHealth();

        float hpPct = health.CurrentHealth / stats.maxHealth;
        if (hpPct <= 0.30f)
        {
            return RobotObjective.Retreat();
        }

        // whole-map awareness
        var enemies = perception.GetAllOpponents();
        if (enemies.Count == 0)
            return RobotObjective.Idle();

        // Attack if any enemy is inside attack range
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i]; if (e == null) continue;
            if (CombatHelpers.InEffectiveAttackRange(_controller, e.transform, 1.5f))
            {
                return new RobotObjective { Type = RobotObjectiveType.AttackEnemy, TargetEnemy = e };
            }
        }

        // Else chase the nearest enemy
        var nearest = GetNearest(enemies);
        return new RobotObjective { Type = RobotObjectiveType.ChaseEnemy, TargetEnemy = nearest };
    }
}

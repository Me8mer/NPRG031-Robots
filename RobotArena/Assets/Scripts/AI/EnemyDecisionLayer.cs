using System.Collections.Generic;
using UnityEngine;


public class EnemyDecisionLayer : DecisionLayer
{
    public EnemyDecisionLayer(RobotController controller) : base(controller) { }

    public override RobotObjective Decide()
    {
        var perception = _controller.GetPerception();
        var stats = _controller.GetStats();

        var enemies = perception.GetEnemiesInRange();

        // No need for health check – enemies fight to the death

        // 1. Attack if in range
        foreach (var enemy in enemies)
        {
            if (Vector3.Distance(_controller.transform.position, enemy.transform.position) <= stats.attackRange)
                return new RobotObjective { Type = RobotObjectiveType.AttackEnemy, TargetEnemy = enemy };
        }

        // 2. Chase enemy
        if (enemies.Count > 0)
        {
            var nearest = GetNearest(enemies);
            return new RobotObjective { Type = RobotObjectiveType.ChaseEnemy, TargetEnemy = nearest };
        }

        return RobotObjective.Idle();
    }
}

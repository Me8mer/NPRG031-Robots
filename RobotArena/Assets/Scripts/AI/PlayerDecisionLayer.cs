using System.Collections.Generic;
using UnityEngine;

public class PlayerDecisionLayer : DecisionLayer
{
    public PlayerDecisionLayer(RobotController controller) : base(controller) { }

    public override RobotObjective Decide()
    {
        var perception = _controller.GetPerception();
        var stats = _controller.GetStats();
        var health = _controller.GetHealth();

        float healthPercent = health.CurrentHealth / stats.maxHealth;
        if (healthPercent <= 0.3f)
            return RobotObjective.Retreat();

        var enemies = perception.GetEnemiesInRange();
        var pickups = perception.GetPickupsInRange();

        //Perception log
        //Debug.Log($"{_controller.name} sees {enemies.Count} enemies and {pickups.Count} pickups");
        // 1. Attack if enemy in range
        foreach (var enemy in enemies)
        {
            if (Vector3.Distance(_controller.transform.position, enemy.transform.position) <= stats.attackRange)
                return new RobotObjective { Type = RobotObjectiveType.AttackEnemy, TargetEnemy = enemy };
        }

        // 2. Prioritize pickup if visible
        if (pickups.Count > 0)
        {
            var nearest = GetNearest(pickups);
            return new RobotObjective { Type = RobotObjectiveType.SeekPickup, TargetPickup = nearest };
        }

        // 3. Chase enemy
        if (enemies.Count > 0)
        {
            var nearest = GetNearest(enemies);
            return new RobotObjective { Type = RobotObjectiveType.ChaseEnemy, TargetEnemy = nearest };
        }

        // 4. Idle
        return RobotObjective.Idle();
    }
}

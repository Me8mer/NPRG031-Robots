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

        //// -------- NEW: pickup has priority over attack or chase --------
        var pickups = perception.GetAllPickups();
        //if (pickups.Count > 0)
        //{
        //    var nearestPickup = GetNearest(pickups);
        //    if (nearestPickup != null)
        //    {
        //        return new RobotObjective { Type = RobotObjectiveType.SeekPickup, TargetPickup = nearestPickup };
        //    }
        //}


        float hpPct = health.CurrentHealth / stats.maxHealth;
        if (hpPct <= 0.30f)
        {
            return RobotObjective.Retreat();
        }

        // whole-map awareness
        var enemies = perception.GetAllOpponents();
        if (enemies.Count == 0)
            return RobotObjective.Idle();

        // 1) If any enemy is inside effective attack range → ATTACK,
        //    but also carry a pickup target if available so AttackState can move to it.
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i]; if (e == null) continue;
            if (CombatHelpers.InEffectiveAttackRange(_controller, e.transform, 1.5f))
            {
                Pickup nearestPickup = (pickups != null && pickups.Count > 0) ? GetNearest(pickups) : null;
                return new RobotObjective
                {
                    Type = RobotObjectiveType.AttackEnemy,
                    TargetEnemy = e,
                    TargetPickup = nearestPickup
                };
            }
        }

        // 2) No enemy in range → seek pickup if any
        if (pickups != null && pickups.Count > 0)
        {
            var nearestPickup = GetNearest(pickups);
            if (nearestPickup != null)
            {
                return new RobotObjective { Type = RobotObjectiveType.SeekPickup, TargetPickup = nearestPickup };
            }
        }

        // 3) Else chase nearest enemy, or idle if none
        if (enemies.Count == 0)
            return RobotObjective.Idle();

        var nearestEnemy = GetNearest(enemies);
        return new RobotObjective { Type = RobotObjectiveType.ChaseEnemy, TargetEnemy = nearestEnemy };
    }
}

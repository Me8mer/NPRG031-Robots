using System.Collections.Generic;
using UnityEngine;

public enum RobotObjectiveType
{
    Idle,
    SeekPickup,
    ChaseEnemy,
    AttackEnemy,
    Retreat
}

public struct RobotObjective
{
    public RobotObjectiveType Type;
    public RobotController TargetEnemy;
    public Pickup TargetPickup;
    public static RobotObjective Idle() => new() { Type = RobotObjectiveType.Idle };
    public static RobotObjective Retreat() => new() { Type = RobotObjectiveType.Retreat };
}

public abstract class DecisionLayer
{
    protected readonly RobotController _controller;

    protected DecisionLayer(RobotController controller)
    {
        _controller = controller;
    }

    /// <summary>
    /// Decide what the robot should focus on this frame.
    /// </summary>
    public abstract RobotObjective Decide();

    /// <summary>
    /// Helper to find nearest MonoBehaviour-based target.
    /// </summary>
    protected T GetNearest<T>(List<T> items) where T : MonoBehaviour
    {
        T closest = null;
        float closestDist = float.MaxValue;
        Vector3 myPos = _controller.transform.position;

        foreach (var item in items)
        {
            float dist = Vector3.Distance(myPos, item.transform.position);
            if (dist < closestDist)
            {
                closest = item;
                closestDist = dist;
            }
        }

        return closest;
    }
}




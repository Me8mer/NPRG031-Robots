using System.Collections.Generic;
using UnityEngine;

public enum MovementIntent
{
    Idle,
    ChaseEnemy,
    ChasePickup,
    StrafeEnemy,
    Retreat
}

public struct DecisionResult
{
    // Movement channel
    public MovementIntent Move;
    public RobotController MoveEnemy; // used when Move is StrafeEnemy or ChaseEnemy
    public Pickup MovePickup;         // used when Move is ChasePickup

    // Fire channel
    public RobotController FireEnemy; // shoot this if non-null, independent of movement
}

public abstract class DecisionLayer
{
    protected readonly RobotController _controller;

    protected DecisionLayer(RobotController controller)
    {
        _controller = controller;
    }

    /// <summary>
    /// Decide movement and firing for this frame. Movement is handled by FSM,
    /// firing is handled directly by the controller.
    /// </summary>
    public abstract DecisionResult Decide();

    /// <summary>
    /// Helper to find nearest MonoBehaviour-based target.
    /// </summary>
    protected T GetNearest<T>(List<T> items) where T : MonoBehaviour
    {
        T closest = null;
        float closestDist = float.MaxValue;
        Vector3 myPos = _controller.transform.position;

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item == null) continue;

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



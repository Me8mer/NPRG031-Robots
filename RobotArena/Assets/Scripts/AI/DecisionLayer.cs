using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// High-level movement intents decided by a <see cref="DecisionLayer"/>.
/// </summary>
public enum MovementIntent
{
    Idle,
    ChaseEnemy,
    ChasePickup,
    StrafeEnemy,
    Retreat
}

/// <summary>
/// Result of a decision cycle. Contains both movement intent and firing target.
/// </summary>
public struct DecisionResult
{
    // Movement channel
    public MovementIntent Move;
    /// <summary>Target enemy for Chase/Strafe movement (null if not applicable).</summary>
    public RobotController MoveEnemy;
    /// <summary>Target pickup for ChasePickup movement (null if not applicable).</summary>
    public Pickup MovePickup;

    // Fire channel
    /// <summary>Enemy to fire at this frame (null if none).</summary>
    public RobotController FireEnemy;
}

/// <summary>
/// Abstract base class for decision-making layers (AI or player).
/// Decides desired movement and firing targets for a robot.
/// </summary>
public abstract class DecisionLayer
{
    /// <summary>Robot that owns this decision layer.</summary>
    protected readonly RobotController _controller;

    protected DecisionLayer(RobotController controller)
    {
        _controller = controller;
    }

    /// <summary>
    /// Computes the next decision for this frame.
    /// Movement intent is executed via the FSM, 
    /// firing is handled directly by <see cref="RobotController"/>.
    /// </summary>
    public abstract DecisionResult Decide();

    /// <summary>
    /// Helper that finds the nearest item in <paramref name="items"/>.
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

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centralized helper that manages transitions between states based on the
/// current <see cref="DecisionResult"/> of a <see cref="RobotController"/>.
/// 
/// Keeps short-term memory per robot to avoid jittery oscillations.
/// </summary>
public static class StateTransitionHelper
{
    // --- Transition tuning ---
    private const float ChaseStrafeGate = 0.35f;   // delay before allowing Chase <-> Strafe flip
    private const float TargetSwitchGate = 0.30f;  // delay before retargeting within same state

    // Per-controller memory
    private class Mem
    {
        public MovementIntent lastMove;
        public Transform lastTarget;
        public float gateUntil;
        public float targetSwitchGateUntil;
    }
    private static readonly Dictionary<int, Mem> _mem = new();

    private static Mem GetMem(RobotController c)
    {
        int id = c.GetInstanceID();
        if (!_mem.TryGetValue(id, out var m))
        {
            m = new Mem();
            _mem[id] = m;
        }
        return m;
    }

    /// <summary>Clears per-controller memory (called when controller is destroyed).</summary>
    public static void Forget(RobotController c) => _mem.Remove(c.GetInstanceID());

    /// <summary>
    /// Examines the current decision of <paramref name="controller"/> and
    /// switches to a new state if required. Handles hysteresis to avoid
    /// rapid oscillations.
    /// </summary>
    public static void HandleTransition(StateMachine fsm, RobotController controller)
    {
        var decision = controller.GetDecision();
        var currentState = controller.CurrentState;
        var mem = GetMem(controller);

        // Determine target transform for this move
        Transform desiredTarget = decision.Move switch
        {
            MovementIntent.StrafeEnemy => decision.MoveEnemy ? decision.MoveEnemy.transform : null,
            MovementIntent.ChaseEnemy => decision.MoveEnemy ? decision.MoveEnemy.transform : null,
            MovementIntent.ChasePickup => decision.MovePickup ? decision.MovePickup.transform : null,
            _ => null
        };

        // Flip detection
        bool chaseStrafeFlip =
            (mem.lastMove == MovementIntent.ChaseEnemy && decision.Move == MovementIntent.StrafeEnemy) ||
            (mem.lastMove == MovementIntent.StrafeEnemy && decision.Move == MovementIntent.ChaseEnemy);

        // IDLE
        if (decision.Move == MovementIntent.Idle)
        {
            if (currentState == RobotState.Idle) return;
            if (Time.time < mem.gateUntil && (currentState == RobotState.Chase || currentState == RobotState.Strafe))
                return;

            fsm.ChangeState(new IdleState(fsm));
            mem.lastMove = MovementIntent.Idle;
            mem.lastTarget = null;
            return;
        }

        // RETREAT
        if (decision.Move == MovementIntent.Retreat)
        {
            if (currentState == RobotState.Retreat) return;

            fsm.ChangeState(new RetreatState(fsm));
            mem.lastMove = MovementIntent.Retreat;
            mem.lastTarget = null;
            return;
        }

        // Shared helpers
        bool sameTargetAsBefore = desiredTarget != null && mem.lastTarget == desiredTarget;

        // STRAFE
        if (decision.Move == MovementIntent.StrafeEnemy)
        {
            if (currentState == RobotState.Strafe && sameTargetAsBefore) return;
            if (chaseStrafeFlip && Time.time < mem.gateUntil) return;
            if (currentState == RobotState.Strafe && !sameTargetAsBefore && Time.time < mem.targetSwitchGateUntil) return;

            if (desiredTarget != null)
            {
                fsm.ChangeState(new StrafeState(fsm, desiredTarget));
                mem.lastMove = MovementIntent.StrafeEnemy;
                mem.lastTarget = desiredTarget;
                mem.gateUntil = Time.time + ChaseStrafeGate;
                mem.targetSwitchGateUntil = Time.time + TargetSwitchGate;
            }
            else
            {
                fsm.ChangeState(new IdleState(fsm));
                mem.lastMove = MovementIntent.Idle;
                mem.lastTarget = null;
            }
            return;
        }

        // PICKUP
        if (decision.Move == MovementIntent.ChasePickup)
        {
            if (currentState == RobotState.Chase && sameTargetAsBefore) return;
            if (currentState == RobotState.Chase && !sameTargetAsBefore && Time.time < mem.targetSwitchGateUntil) return;

            if (desiredTarget != null)
            {
                fsm.ChangeState(new ChaseState(fsm, desiredTarget));
                mem.lastMove = MovementIntent.ChasePickup;
                mem.lastTarget = desiredTarget;
                mem.targetSwitchGateUntil = Time.time + TargetSwitchGate;
            }
            else
            {
                fsm.ChangeState(new IdleState(fsm));
                mem.lastMove = MovementIntent.Idle;
                mem.lastTarget = null;
            }
            return;
        }

        // ENEMY CHASE
        if (decision.Move == MovementIntent.ChaseEnemy)
        {
            if (currentState == RobotState.Chase && sameTargetAsBefore) return;
            if (chaseStrafeFlip && Time.time < mem.gateUntil) return;
            if (currentState == RobotState.Chase && !sameTargetAsBefore && Time.time < mem.targetSwitchGateUntil) return;

            if (desiredTarget != null)
            {
                fsm.ChangeState(new ChaseState(fsm, desiredTarget));
                mem.lastMove = MovementIntent.ChaseEnemy;
                mem.lastTarget = desiredTarget;
                mem.gateUntil = Time.time + ChaseStrafeGate;
                mem.targetSwitchGateUntil = Time.time + TargetSwitchGate;
            }
            else
            {
                fsm.ChangeState(new IdleState(fsm));
                mem.lastMove = MovementIntent.Idle;
                mem.lastTarget = null;
            }
            return;
        }

        // Fallback
        fsm.ChangeState(new IdleState(fsm));
        mem.lastMove = MovementIntent.Idle;
        mem.lastTarget = null;
    }
}

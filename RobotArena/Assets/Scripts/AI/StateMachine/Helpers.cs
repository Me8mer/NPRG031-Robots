using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;


/// <summary>
/// Movement transitions based on the controller's latest DecisionResult.
/// </summary>
public static class StateTransitionHelper
{
    // --- Transition tuning ---
    private const float ChaseStrafeGate = 0.35f;   // gate flips between ChaseEnemy <-> StrafeEnemy
    private const float TargetSwitchGate = 0.30f;  // gate rapid target retargets within same state
    private const float RangeTolerance = 2;

    // Per-controller memory to reduce oscillations
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

    // Allow RobotController to clean up on destroy
    public static void Forget(RobotController c)
    {
        _mem.Remove(c.GetInstanceID());
    }


    public static void HandleTransition(StateMachine fsm, RobotController controller)
    {
        var decision = controller.GetDecision();             // movement + targets
        var currentState = controller.CurrentState;
        var mem = GetMem(controller);

        // Figure out the desired target transform for this move
        Transform desiredTarget = null;
        switch (decision.Move)
        {
            case MovementIntent.StrafeEnemy:
                desiredTarget = decision.MoveEnemy ? decision.MoveEnemy.transform : null;
                break;
            case MovementIntent.ChaseEnemy:
                desiredTarget = decision.MoveEnemy ? decision.MoveEnemy.transform : null;
                break;
            case MovementIntent.ChasePickup:
                desiredTarget = decision.MovePickup ? decision.MovePickup.transform : null;
                break;
        }

        // Gate frequent flips between ChaseEnemy and StrafeEnemy
        bool chaseStrafeFlip =
            (mem.lastMove == MovementIntent.ChaseEnemy && decision.Move == MovementIntent.StrafeEnemy) ||
            (mem.lastMove == MovementIntent.StrafeEnemy && decision.Move == MovementIntent.ChaseEnemy);

        // If the requested move equals current and target did not change, do nothing
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

        if (decision.Move == MovementIntent.Retreat)
        {
            if (currentState == RobotState.Retreat) return;
            fsm.ChangeState(new RetreatState(fsm));
            mem.lastMove = MovementIntent.Retreat;
            mem.lastTarget = null;
            return;
        }

        // Shared gates for move types that carry a target
        bool sameMoveAsBefore = decision.Move == mem.lastMove;
        bool sameTargetAsBefore = desiredTarget != null && mem.lastTarget == desiredTarget;

        // If target is unchanged and we are already in the corresponding state, early out
        if (decision.Move == MovementIntent.StrafeEnemy)
        {
            if (currentState == RobotState.Strafe && sameTargetAsBefore) return;

            // Gate rapid flips Chase<->Strafe
            if (chaseStrafeFlip && Time.time < mem.gateUntil) return;

            // Gate rapid target switches while strafing
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

        if (decision.Move == MovementIntent.ChasePickup)
        {
            if (currentState == RobotState.Chase && sameTargetAsBefore) return;

            // Switching from strafing to pickup should be allowed promptly, but still respect target spam
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

        if (decision.Move == MovementIntent.ChaseEnemy)
        {
            if (currentState == RobotState.Chase && sameTargetAsBefore) return;

            // Gate rapid flips Chase<->Strafe
            if (chaseStrafeFlip && Time.time < mem.gateUntil) return;

            // Gate rapid target switches while chasing
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
